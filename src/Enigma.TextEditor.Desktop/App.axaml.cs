using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Logging.Abstractions;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Core.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop;

/// <summary>
/// The application object, and the composition root.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Read before the container is built, not from it: the theme has to be right before the
            // window is shown. An unreadable file is moved aside and the defaults are used.
            ConfigLoadResult<AppSettings> settings = SettingsLoader.Load(AppPaths.SettingsFile);
            RequestedThemeVariant = ThemeService.ToVariant(settings.Config.Value.Theme);

            IHost host = BuildHost(this, settings, AppPaths.LogFile);

            // Started, never run: the Avalonia lifetime owns the message loop; the host only supplies
            // configuration, logging and services.
            host.Start();

            IServiceProvider services = host.Services;
            ILogger<App> logger = services.GetRequiredService<ILogger<App>>();

            if (settings.Failure is not null)
            {
                logger.LogWarning(
                    settings.Failure,
                    "The settings file could not be read; running on defaults. Quarantined to: {QuarantinedFile}. Changes will be discarded: {PersistenceDisabled}.",
                    settings.QuarantinedFilePath ?? "(not moved)",
                    settings.PersistenceDisabled);
            }

            MainWindow window = services.GetRequiredService<MainWindow>();
            MainWindowViewModel viewModel = services.GetRequiredService<MainWindowViewModel>();
            window.DataContext = viewModel;
            RegisterHosts(services, window);

            desktop.MainWindow = window;

            // Posted, so the window is on screen before the first file is read, and any dialog a
            // failed open raises has a window to appear in.
            StartupFiles startup = StartupFiles.Parse(desktop.Args ?? [], Environment.CurrentDirectory, File.Exists, Directory.Exists);
            Dispatcher.UIThread.Post(() => _ = viewModel.Workspace.OpenStartupFilesAsync(startup));
            logger.LogInformation("{DisplayName} {Version} started.", AppInfo.DisplayName, AppInfo.Version);

            desktop.Exit += (_, _) => OnExit(host, logger);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Builds the host with every application service registered.
    /// </summary>
    /// <param name="application">The running application, which the theme service drives.</param>
    /// <param name="settings">The settings, loaded before the container.</param>
    /// <param name="logFilePath">Where the log is written.</param>
    /// <returns>The host, not started.</returns>
    internal static IHost BuildHost(Application application, ConfigLoadResult<AppSettings> settings, string logFilePath)
    {
        ArgumentNullException.ThrowIfNull(application);

        // A desktop app must not depend on its working directory, so configuration is read beside the
        // executable rather than from wherever the process happened to be launched.
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
        });

        builder.Services.AddSingleton(application);
        builder.Services.AddAppSettings(settings);
        builder.Services.AddAppLogging(logFilePath);
        builder.Services.AddEnigmaServices();
        builder.Services.AddShell();

        return builder.Build();
    }

    /// <summary>
    /// Hands the window's host controls and storage provider to the Enigma services. Has to run
    /// before the window is shown: a service whose host is unregistered throws on first use.
    /// </summary>
    /// <param name="services">The container.</param>
    /// <param name="window">The main window.</param>
    internal static void RegisterHosts(IServiceProvider services, MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(window);

        services.GetRequiredService<IContentDialogService>().RegisterHost(window.HostDialog);
        services.GetRequiredService<IOverlayService>().RegisterHost(window.HostOverlay);
        services.GetRequiredService<IInfoBarService>().RegisterHost(window.HostInfoBar);
        services.GetRequiredService<IFileDialogService>().SetStorageProvider(window.StorageProvider);
        services.GetRequiredService<IFolderDialogService>().SetStorageProvider(window.StorageProvider);
    }

    private static void OnExit(IHost host, ILogger<App> logger)
    {
        logger.LogInformation("{DisplayName} is shutting down.", AppInfo.DisplayName);

        // Resolved before the sequence starts: if it overruns, the work walked away from is the work
        // that disposes the container.
        IFlushableLoggerHandle loggerHandle = host.Services.GetRequiredService<IFlushableLoggerHandle>();

        // The exit handler cannot await; ShutdownSequence runs the asynchronous stop off this thread
        // and bounds the wait, so a stuck step cannot keep the process alive.
        bool finished = ShutdownSequence.Run(
            async cancellationToken =>
            {
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
                host.Dispose();
            },
            ShutdownSequence.DefaultTimeout);

        if (!finished)
        {
            logger.LogWarning(
                "Shutting down did not finish within {Timeout}; the rest was abandoned.",
                ShutdownSequence.DefaultTimeout);

            // The host.Dispose() that normally flushes NLog is part of the abandoned work.
            loggerHandle.Flush();
        }
    }
}

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
            // First: a command or a posted callback that throws from here on is caught and shown, not the end
            // of the process. The dispatcher is safe to touch now that the platform is set up; the headless
            // tests never come this way.
            UnhandledExceptionHooks.InstallDispatcher();

            // Before the load, and it has to be: the loader moves an unreadable file aside, which leaves the
            // path absent — asked one line later, a broken file would look like a first run.
            FirstRunState firstRun = FirstRunState.Detect(AppPaths.SettingsFile);

            // Read before the container is built, not from it: the theme has to be right before the
            // window is shown. An unreadable file is moved aside and the defaults are used.
            ConfigLoadResult<AppSettings> settings = SettingsLoader.Load(AppPaths.SettingsFile);
            RequestedThemeVariant = ThemeService.ToVariant(settings.Config.Value.Theme);

            IHost host = BuildHost(this, settings, AppPaths.LogFile, firstRun);

            // Started, never run: the Avalonia lifetime owns the message loop; the host only supplies
            // configuration, logging and services.
            host.Start();

            IServiceProvider services = host.Services;
            ILogger<App> logger = services.GetRequiredService<ILogger<App>>();

            // The safety net, as soon as it can be built: anything caught before it was is handed over, and
            // shown once the window has handed it its dialog host.
            IUnhandledErrorReporter errors = services.GetRequiredService<IUnhandledErrorReporter>();
            UnhandledExceptionHooks.Attach(errors);
            desktop.ShutdownRequested += (_, _) => errors.ShutdownStarted();

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

            // Posted, so the window is on screen before the welcome dialog of a first start and the first
            // file read, and any dialog either raises has a window to appear in.
            StartupFiles startup = StartupFiles.Parse(desktop.Args ?? [], Environment.CurrentDirectory, File.Exists, Directory.Exists);
            Dispatcher.UIThread.Post(() => _ = viewModel.StartAsync(startup));
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
    /// <param name="firstRun">Whether this is the very first start; not one when omitted.</param>
    /// <returns>The host, not started.</returns>
    internal static IHost BuildHost(
        Application application,
        ConfigLoadResult<AppSettings> settings,
        string logFilePath,
        FirstRunState? firstRun = null)
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

        if (firstRun is not null)
        {
            builder.Services.AddFirstRun(firstRun);
        }

        builder.Services.AddAppLogging(logFilePath);
        builder.Services.AddEnigmaServices();
        builder.Services.AddShell();

        return builder.Build();
    }

    /// <summary>
    /// Hands the window's host controls and storage provider to the Enigma services, and its dialog host
    /// to the safety net. Has to run before the window is shown: a service whose host is unregistered
    /// throws on first use.
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
        services.GetRequiredService<UnhandledErrorReporter>().RegisterHost(window.HostDialog);
    }

    private static void OnExit(IHost host, ILogger<App> logger)
    {
        logger.LogInformation("{DisplayName} is shutting down.", AppInfo.DisplayName);

        // Closing: no cancellation is worth a word now, and a late report must not log through the
        // container disposed below.
        host.Services.GetRequiredService<IUnhandledErrorReporter>().ShutdownStarted();
        UnhandledExceptionHooks.Detach();

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

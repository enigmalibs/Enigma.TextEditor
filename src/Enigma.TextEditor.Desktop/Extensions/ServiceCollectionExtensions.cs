using System;
using Enigma.Avalonia.Desktop.Services;
using Enigma.Logging.NLog;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Core.Configuration;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Targets;

namespace Enigma.TextEditor.Desktop.Extensions;

/// <summary>
/// The application's service registrations, grouped the way the composition root adds them.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// How many days of log files are kept.
    /// </summary>
    public const int LogRetentionDays = 14;

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the six Enigma.Avalonia.Desktop services. The library ships no registration of its
        /// own; three of them need a host control and two a storage provider, handed over by the
        /// composition root before the window is shown.
        /// </summary>
        public void AddEnigmaServices()
        {
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<IInfoBarService, InfoBarService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IFolderDialogService, FolderDialogService>();
        }

        /// <summary>
        /// Registers the settings, as loaded before the container was built.
        /// </summary>
        /// <param name="settings">The loaded settings.</param>
        public void AddAppSettings(ConfigLoadResult<AppSettings> settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            services.AddSingleton(settings.Config);
        }

        /// <summary>
        /// Routes <see cref="ILogger{TCategoryName}"/> to a daily file under the application-data folder.
        /// </summary>
        /// <param name="logFilePath">The current log file.</param>
        public void AddAppLogging(string logFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);

            services.AddLogging(logging =>
            {
                // The generic host installs Console, Debug and EventSource providers. A windowed app
                // has no console to write to, and each of them would compete with the file target.
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            services.AddNLogLoggerBuilder(
                AppInfo.AppDataFolderName,
                builder => builder.AddFileTarget(
                    "file",
                    target =>
                    {
                        target.Layout = LoggerProviderBuilder.DefaultLayout;
                        target.FileName = logFilePath;

                        // One file per day, two weeks of history: a desktop editor's log is read when
                        // something went wrong recently.
                        target.ArchiveEvery = FileArchivePeriod.Day;
                        target.MaxArchiveDays = LogRetentionDays;
                    },
                    minLevel: LogLevel.Information));
        }

        /// <summary>
        /// Registers the window, its view-models and the application services.
        /// </summary>
        public void AddShell()
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IAppShutdown, AppShutdown>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IFilePrompts, FilePrompts>();
            services.AddSingleton<IUnsavedChangesPrompt, UnsavedChangesPrompt>();
            services.AddSingleton<StatusMessages>();

            // One session for every pane: the status bar shows one mode, the one of the pane that has
            // the keyboard. Each engine belongs to its editor control, not to the container.
            services.AddSingleton<IVimSession, VimSession>();

            // What every pane's engine shares — registers, last search, last find — as Vim's are the
            // editor's rather than a window's.
            services.AddSingleton<VimSharedState>();

            // One registry, one editor area, one status bar: they are the window's own state, and a
            // second registry would be a second answer to "is this file already open".
            services.AddSingleton<DocumentRegistry>();
            services.AddSingleton<IDocumentService, DocumentService>();
            services.AddSingleton<EditorPreferences>();
            services.AddSingleton<WorkspaceViewModel>();
            services.AddSingleton<StatusBarViewModel>();
            services.AddSingleton<ToolsViewModel>();

            // Built once: the grammar index is parsed when it is built, and it does not change.
            services.AddSingleton<LanguageCatalog>();
            services.AddSingleton<ILanguagePicker, LanguagePicker>();
            services.AddSingleton<IEncodingPicker, EncodingPicker>();
            services.AddSingleton<IConfirmationPrompt, ConfirmationPrompt>();
            services.AddSingleton<IBinaryFilePrompt, BinaryFilePrompt>();
            services.AddSingleton<ISettingsWriter, SettingsWriter>();
            services.AddSingleton<SettingsMenuViewModel>();
            services.AddSingleton<IAboutDialogService, AboutDialogService>();
            services.AddSingleton<IHelpDialogService, HelpDialogService>();

            // One for the session: it follows every change the menu makes, and the dialog shows it again
            // each time it opens.
            services.AddSingleton<PreferencesViewModel>();
            services.AddSingleton<IPreferencesDialogService, PreferencesDialogService>();
        }
    }
}

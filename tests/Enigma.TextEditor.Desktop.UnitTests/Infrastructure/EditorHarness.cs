using System;
using System.IO;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// The editor's view-models and services wired over fakes for the user's side — pickers and messages —
/// and real file I/O in a temporary folder.
/// </summary>
/// <remarks>
/// Build it on the thread the test works on: a buffer belongs to the thread that created it.
/// </remarks>
internal sealed class EditorHarness : IDisposable
{
    /// <param name="configure">Changes the settings before anything reads them.</param>
    /// <param name="firstRun">Whether the window's start is a first one.</param>
    /// <param name="sessionFolder">
    /// Where the session is kept: this harness's own folder when omitted, another harness's to start from what it
    /// closed with.
    /// </param>
    public EditorHarness(Action<AppSettings>? configure = null, FirstRunState? firstRun = null, string? sessionFolder = null)
    {
        Settings = SettingsLoader.Load(Folder.PathOf(AppPaths.SettingsFileName)).Config;
        configure?.Invoke(Settings.Value);

        Registry = new DocumentRegistry();
        Documents = new DocumentService(Registry, Prompts, Messages, Settings, Languages, Binary, NullLogger<DocumentService>.Instance);
        Preferences = new EditorPreferences(Settings);
        Vim = new VimSession(Settings, StatusLine);
        Workspace = new WorkspaceViewModel(Documents, Registry, Prompts, Unsaved, StatusLine, Vim, VimShared, Shutdown, Preferences, Help, Languages);
        Explorer = new FileExplorerViewModel(Workspace, Prompts, ExplorerPrompts, Messages, Trash);
        StatusBar = new StatusBarViewModel(Workspace, StatusLine);
        Tools = new ToolsViewModel(Workspace, LanguagePicker, EncodingPicker, Confirm, Documents);
        SettingsWriter = new SettingsWriter(Settings, Messages, NullLogger<SettingsWriter>.Instance);
        SettingsMenu = new SettingsMenuViewModel(Settings, SettingsWriter, Theme, Vim, Preferences);
        PreferencesPage = new PreferencesViewModel(Settings, SettingsMenu);
        Session = new SessionStore(
            Path.Combine(sessionFolder ?? Folder.Root, AppPaths.SessionFileName),
            Path.Combine(sessionFolder ?? Folder.Root, AppPaths.SessionLockFileName),
            NullLogger<SessionStore>.Instance);
        Window = new MainWindowViewModel(
            Workspace, Explorer, StatusBar, Tools, SettingsMenu, Shell, PreferencesPage, About, Help, firstRun ?? FirstRunState.Returning, FirstRunDialog, Shutdown, Session);
    }

    /// <summary>
    /// Gets where the session is kept — in the temporary folder, never the user's own.
    /// </summary>
    public SessionStore Session { get; }

    public TempFolder Folder { get; } = new();

    public IConfig<AppSettings> Settings { get; }

    public FakeFilePrompts Prompts { get; } = new();

    public FakeFileExplorerPrompts ExplorerPrompts { get; } = new();

    public FakeTrashService Trash { get; } = new();

    public FakeMessageService Messages { get; } = new();

    public FakeAppShutdown Shutdown { get; } = new();

    public FakeUnsavedChangesPrompt Unsaved { get; } = new();

    public FakeBinaryFilePrompt Binary { get; } = new();

    public FakeThemeService Theme { get; } = new();

    public FakeAboutDialogService About { get; } = new();

    public FakeHelpDialogService Help { get; } = new();

    public ShellViewModel Shell { get; } = new();

    public FakeFirstRunDialogService FirstRunDialog { get; } = new();

    public SettingsWriter SettingsWriter { get; }

    public SettingsMenuViewModel SettingsMenu { get; }

    public PreferencesViewModel PreferencesPage { get; }

    public StatusMessages StatusLine { get; } = new();

    public VimSession Vim { get; }

    public Enigma.TextEditor.Core.Vim.VimSharedState VimShared { get; } = new();

    /// <summary>
    /// Gets the language catalog, shared by every harness: building it parses the grammar index.
    /// </summary>
    public LanguageCatalog Languages => SharedLanguages.Value;

    public FakeLanguagePicker LanguagePicker { get; } = new();

    public FakeEncodingPicker EncodingPicker { get; } = new();

    public FakeConfirmationPrompt Confirm { get; } = new();

    public ToolsViewModel Tools { get; }

    private static readonly Lazy<LanguageCatalog> SharedLanguages = new(() => new LanguageCatalog());

    public DocumentRegistry Registry { get; }

    public DocumentService Documents { get; }

    public EditorPreferences Preferences { get; }

    public WorkspaceViewModel Workspace { get; }

    public FileExplorerViewModel Explorer { get; }

    public StatusBarViewModel StatusBar { get; }

    public MainWindowViewModel Window { get; }

    public void Dispose()
    {
        // The lock first: the folder it sits in cannot be removed while it is held.
        Session.Dispose();
        Folder.Dispose();
    }
}

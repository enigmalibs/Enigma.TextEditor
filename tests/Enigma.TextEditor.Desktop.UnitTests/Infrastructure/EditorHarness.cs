using System;
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
    public EditorHarness(Action<AppSettings>? configure = null, FirstRunState? firstRun = null)
    {
        Settings = SettingsLoader.Load(Folder.PathOf(AppPaths.SettingsFileName)).Config;
        configure?.Invoke(Settings.Value);

        Registry = new DocumentRegistry();
        Documents = new DocumentService(Registry, Prompts, Messages, Settings, Languages, Binary, NullLogger<DocumentService>.Instance);
        Preferences = new EditorPreferences(Settings);
        Vim = new VimSession(Settings, StatusLine);
        Workspace = new WorkspaceViewModel(Documents, Registry, Prompts, Unsaved, StatusLine, Vim, VimShared, Shutdown, Preferences, Help, Languages);
        StatusBar = new StatusBarViewModel(Workspace, StatusLine);
        Tools = new ToolsViewModel(Workspace, LanguagePicker, EncodingPicker, Confirm, Documents);
        SettingsWriter = new SettingsWriter(Settings, Messages, NullLogger<SettingsWriter>.Instance);
        SettingsMenu = new SettingsMenuViewModel(Settings, SettingsWriter, Theme, Vim, Preferences);
        PreferencesPage = new PreferencesViewModel(Settings, SettingsMenu);
        Window = new MainWindowViewModel(
            Workspace, StatusBar, Tools, SettingsMenu, Shell, PreferencesPage, About, Help, firstRun ?? FirstRunState.Returning, FirstRunDialog, Shutdown);
    }

    public TempFolder Folder { get; } = new();

    public IConfig<AppSettings> Settings { get; }

    public FakeFilePrompts Prompts { get; } = new();

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

    public StatusBarViewModel StatusBar { get; }

    public MainWindowViewModel Window { get; }

    public void Dispose() => Folder.Dispose();
}

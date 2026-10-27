using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The main window: its title, its rail and its two pages, its status bar and the commands of its menu
/// bar.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IAppShutdown _shutdown;
    private readonly IAboutDialogService _about;
    private readonly IHelpDialogService _help;
    private DocumentViewModel? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="statusBar">The status bar.</param>
    /// <param name="tools">The Tools menu.</param>
    /// <param name="settings">The Settings menu and the zoom commands.</param>
    /// <param name="shell">The rail, and which page is shown.</param>
    /// <param name="settingsPage">What the settings page shows and changes.</param>
    /// <param name="about">The About dialog.</param>
    /// <param name="help">The key-binding help.</param>
    /// <param name="shutdown">Ends the application.</param>
    public MainWindowViewModel(
        WorkspaceViewModel workspace,
        StatusBarViewModel statusBar,
        ToolsViewModel tools,
        SettingsMenuViewModel settings,
        ShellViewModel shell,
        PreferencesViewModel settingsPage,
        IAboutDialogService about,
        IHelpDialogService help,
        IAppShutdown shutdown)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        StatusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
        Tools = tools ?? throw new ArgumentNullException(nameof(tools));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        SettingsPage = settingsPage ?? throw new ArgumentNullException(nameof(settingsPage));
        _about = about ?? throw new ArgumentNullException(nameof(about));
        _help = help ?? throw new ArgumentNullException(nameof(help));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));

        ExitCommand = new RelayCommand(OnExit);
        AboutCommand = new AsyncRelayCommand(() => _about.ShowAsync());
        HelpCommand = new AsyncRelayCommand(() => _help.ShowAsync());

        // Whatever opens, creates or rearranges a document brings the files back: from the settings page,
        // File → Open, Ctrl+N, :e or a split would otherwise act on documents nobody can see.
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Workspace.DocumentShown += (_, _) => Shell.ShowFiles();
        Workspace.LayoutChanged += (_, _) => Shell.ShowFiles();
        Follow(Workspace.ActiveDocument);
    }

    /// <summary>
    /// Gets the editor area.
    /// </summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>
    /// Gets the status bar.
    /// </summary>
    public StatusBarViewModel StatusBar { get; }

    /// <summary>
    /// Gets the Tools menu.
    /// </summary>
    public ToolsViewModel Tools { get; }

    /// <summary>
    /// Gets the Settings menu, and the View menu's zoom commands.
    /// </summary>
    public SettingsMenuViewModel Settings { get; }

    /// <summary>
    /// Gets the rail, and which of the files and the settings is shown.
    /// </summary>
    public ShellViewModel Shell { get; }

    /// <summary>
    /// Gets what the settings page shows and changes.
    /// </summary>
    public PreferencesViewModel SettingsPage { get; }

    /// <summary>
    /// Gets <em>Help → About</em>.
    /// </summary>
    public AsyncRelayCommand AboutCommand { get; }

    /// <summary>
    /// Gets <em>Help → Key Bindings</em> (F1), which <c>:help</c> also opens.
    /// </summary>
    public AsyncRelayCommand HelpCommand { get; }

    /// <summary>
    /// Gets the window title: the active buffer's name, a dot when it has unsaved changes, and the
    /// product's name.
    /// </summary>
    public string Title => _document is null
        ? AppInfo.DisplayName
        : $"{_document.Title}{(_document.IsDirty ? " •" : string.Empty)} — {AppInfo.DisplayName}";

    /// <summary>
    /// Gets the command behind <em>File → Exit</em>.
    /// </summary>
    public RelayCommand ExitCommand { get; }

    /// <summary>
    /// Asks about unsaved changes before the window closes.
    /// </summary>
    /// <returns>Whether the window may close.</returns>
    public Task<bool> ConfirmCloseAsync()
        => Workspace.HasUnsavedChanges && !_shutdown.DiscardChangesOnExit
            ? Workspace.ConfirmCloseAllAsync()
            : Task.FromResult(true);

    /// <summary>
    /// Gets a value indicating whether closing the window has to ask about unsaved changes first.
    /// </summary>
    public bool MustConfirmClose => Workspace.HasUnsavedChanges && !_shutdown.DiscardChangesOnExit;

    private void OnExit() => _shutdown.RequestExit();

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.ActiveDocument))
        {
            Follow(Workspace.ActiveDocument);
            Shell.ShowFiles();
        }
    }

    private void Follow(DocumentViewModel? document)
    {
        if (_document is not null)
        {
            _document.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _document = document;

        if (_document is not null)
        {
            _document.PropertyChanged += OnDocumentPropertyChanged;
        }

        OnPropertyChanged(nameof(Title));
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentViewModel.Title) or nameof(DocumentViewModel.IsDirty))
        {
            OnPropertyChanged(nameof(Title));
        }
    }
}

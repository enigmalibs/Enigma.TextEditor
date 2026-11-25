using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Sessions;
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
    private readonly EditorLifecycle _lifecycle;
    private DocumentViewModel? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="explorer">The explorer beside the panes.</param>
    /// <param name="statusBar">The status bar.</param>
    /// <param name="tools">The Tools menu.</param>
    /// <param name="settings">The Settings menu and the zoom commands.</param>
    /// <param name="shell">The rail, and which page is shown.</param>
    /// <param name="settingsPage">What the settings page shows and changes.</param>
    /// <param name="about">The About dialog.</param>
    /// <param name="help">The key-binding help.</param>
    /// <param name="shutdown">Ends the application.</param>
    /// <param name="lifecycle">What happens as the window opens and closes: first run, session, autosave.</param>
    /// <remarks>
    /// Seven of the parameters are the view-models of the window's parts, which it exposes for binding; four are
    /// services.
    /// </remarks>
    public MainWindowViewModel(
        WorkspaceViewModel workspace,
        FileExplorerViewModel explorer,
        StatusBarViewModel statusBar,
        ToolsViewModel tools,
        SettingsMenuViewModel settings,
        ShellViewModel shell,
        PreferencesViewModel settingsPage,
        IAboutDialogService about,
        IHelpDialogService help,
        IAppShutdown shutdown,
        EditorLifecycle lifecycle)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        Explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
        StatusBar = statusBar ?? throw new ArgumentNullException(nameof(statusBar));
        Tools = tools ?? throw new ArgumentNullException(nameof(tools));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        SettingsPage = settingsPage ?? throw new ArgumentNullException(nameof(settingsPage));
        _about = about ?? throw new ArgumentNullException(nameof(about));
        _help = help ?? throw new ArgumentNullException(nameof(help));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));

        ExitCommand = new RelayCommand(OnExit);
        AboutCommand = new AsyncRelayCommand(() => _about.ShowAsync());
        HelpCommand = new AsyncRelayCommand(() => _help.ShowAsync());

        // Whatever opens, creates or rearranges a document brings the files back: from the settings page,
        // File → Open, Ctrl+N, :e or a split would otherwise act on documents nobody can see.
        Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        Workspace.DocumentShown += (_, _) => Shell.ShowFiles();
        Workspace.LayoutChanged += (_, _) => Shell.ShowFiles();

        // The explorer stands on the files page: shown, or given a folder, from the settings, it brings them back.
        Explorer.PropertyChanged += (_, e) =>
        {
            if (((e.PropertyName == nameof(FileExplorerViewModel.IsVisible)) && Explorer.IsVisible)
                || ((e.PropertyName == nameof(FileExplorerViewModel.FolderPath)) && Explorer.HasFolder))
            {
                Shell.ShowFiles();
            }
        };
        Follow(Workspace.ActiveDocument);
    }

    /// <summary>
    /// Gets the editor area.
    /// </summary>
    public WorkspaceViewModel Workspace { get; }

    /// <summary>
    /// Gets the explorer beside the panes, on the files page.
    /// </summary>
    public FileExplorerViewModel Explorer { get; }

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
    /// Gets a value indicating whether <see cref="StartAsync"/> is still bringing the editor up. What it shows
    /// meanwhile — the explorer of the previous session — does not take the keyboard from the active pane.
    /// </summary>
    public bool IsStarting => _lifecycle.IsStarting;

    /// <summary>
    /// Runs what needs the window on screen — first run, session, files named on the command line, explorer — and from
    /// then on keeps the session every few seconds (<see cref="EditorLifecycle.StartAsync"/>).
    /// </summary>
    /// <param name="startup">The files and folders named on the command line.</param>
    /// <returns>A task that completes once the files are open and the explorer's folder is listed.</returns>
    public Task StartAsync(StartupFiles startup) => _lifecycle.StartAsync(startup);

    /// <summary>
    /// Captures what the window shows, for the next start (<see cref="EditorLifecycle.CaptureSession"/>).
    /// </summary>
    /// <param name="keepUnsaved">Whether unsaved changes are kept.</param>
    /// <returns>The session.</returns>
    public SessionState CaptureSession(bool keepUnsaved = false) => _lifecycle.CaptureSession(keepUnsaved);

    /// <summary>
    /// Keeps what the window shows, unsaved changes included, as it closes (<see cref="EditorLifecycle.KeepSessionOnClose"/>).
    /// </summary>
    /// <returns>Whether the session was kept.</returns>
    public bool KeepSessionOnClose() => _lifecycle.KeepSessionOnClose();

    /// <summary>
    /// Keeps the splits, the tabs and the explorer, without unsaved changes (<see cref="EditorLifecycle.SaveSession"/>).
    /// </summary>
    /// <returns>Whether it was kept.</returns>
    public bool SaveSession() => _lifecycle.SaveSession();

    /// <summary>
    /// The window has closed: no later tick replaces the session it kept (<see cref="EditorLifecycle.WindowClosed"/>).
    /// </summary>
    public void WindowClosed() => _lifecycle.WindowClosed();

    /// <summary>
    /// Opens the files another launch of the editor handed over to this one, as if they had been named on its own
    /// command line: in the active pane, on the files page.
    /// </summary>
    /// <param name="startup">The files, as read from the other launch's arguments.</param>
    /// <returns>A task that completes once they are open, or reported.</returns>
    public Task OpenForwardedAsync(StartupFiles startup)
    {
        ArgumentNullException.ThrowIfNull(startup);

        Shell.ShowFiles();

        return Workspace.OpenStartupFilesAsync(startup);
    }

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

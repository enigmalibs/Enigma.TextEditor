using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The editor area: its panes, arranged by a <see cref="LayoutTree"/>, the one with the keyboard, and
/// the file and window commands that act on it.
/// </summary>
/// <remarks>
/// <para>
/// The split model is Enigma.FileExplorer's: a binary tree of rows and columns whose leaves are
/// panes, one <see cref="EditorPaneViewModel"/> per leaf. A split opens a new untitled buffer in the
/// new pane, whatever the split pane was showing, so the two halves never start out editing one text;
/// <c>:split file</c> opens that file there instead. One buffer can still be shown in two panes, by
/// opening it in both.
/// </para>
/// <para>
/// A pane that loses its last tab closes, unless it is the only pane: closing tabs is how panes go
/// away, so <c>:q</c> in a split closes the split, and <c>:q</c> on the last tab of the last pane leaves
/// that pane empty, the application still open.
/// </para>
/// </remarks>
public sealed class WorkspaceViewModel : ObservableObject, IPaneHost
{
    private readonly IDocumentService _documents;
    private readonly DocumentRegistry _registry;
    private readonly IFilePrompts _prompts;
    private readonly IUnsavedChangesPrompt _unsaved;
    private readonly StatusMessages _status;
    private readonly ExCommandHandler _ex;
    private readonly WindowCommandHandler _window;
    private readonly Dictionary<PaneId, EditorPaneViewModel> _byId = [];
    private int _batch;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceViewModel"/> class with one empty pane.
    /// </summary>
    /// <param name="documents">Opens and saves buffers.</param>
    /// <param name="registry">The open buffers.</param>
    /// <param name="prompts">The open picker.</param>
    /// <param name="unsaved">Asks about unsaved changes before a buffer is closed.</param>
    /// <param name="status">The status bar's message line.</param>
    /// <param name="vim">The Vim session every pane's engine reports to.</param>
    /// <param name="vimShared">What every pane's Vim engine shares.</param>
    /// <param name="shutdown">Ends the application, for <c>:qa</c>.</param>
    /// <param name="preferences">How every editor looks.</param>
    /// <param name="help">The key-binding help, for <c>:help</c>.</param>
    /// <param name="languages">The languages <c>:set filetype</c> can name.</param>
    public WorkspaceViewModel(
        IDocumentService documents,
        DocumentRegistry registry,
        IFilePrompts prompts,
        IUnsavedChangesPrompt unsaved,
        StatusMessages status,
        IVimSession vim,
        VimSharedState vimShared,
        IAppShutdown shutdown,
        EditorPreferences preferences,
        IHelpDialogService help,
        LanguageCatalog languages)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _unsaved = unsaved ?? throw new ArgumentNullException(nameof(unsaved));
        _status = status ?? throw new ArgumentNullException(nameof(status));
        Vim = vim ?? throw new ArgumentNullException(nameof(vim));
        VimShared = vimShared ?? throw new ArgumentNullException(nameof(vimShared));
        _ex = new ExCommandHandler(
            this,
            shutdown ?? throw new ArgumentNullException(nameof(shutdown)),
            status,
            help ?? throw new ArgumentNullException(nameof(help)),
            languages ?? throw new ArgumentNullException(nameof(languages)));
        _window = new WindowCommandHandler(this, _ex, status);
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));

        Layout = new LayoutTree();
        Layout.Changed += (_, _) =>
        {
            // Inside a batch the panes are not in step with the tree yet; the batch reports once at its end.
            if (_batch == 0)
            {
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        };

        NewCommand = new RelayCommand(() => NewDocument());
        OpenCommand = new AsyncRelayCommand(OpenFromPromptAsync);
        OpenHexCommand = new AsyncRelayCommand(OnOpenHexAsync);
        SaveCommand = new AsyncRelayCommand(() => SaveAsync(ActiveDocument), () => ActiveDocument is { IsHex: false });
        SaveAsCommand = new AsyncRelayCommand(() => SaveAsAsync(ActiveDocument), () => ActiveDocument is { IsHex: false });
        SaveAllCommand = new AsyncRelayCommand(SaveAllAsync, () => _registry.Documents.Count > 0);
        CloseTabCommand = new AsyncRelayCommand(OnCloseActiveTabAsync, () => ActiveTab is not null);
        NextTabCommand = new RelayCommand(() => ActivePane.NextTab());
        PreviousTabCommand = new RelayCommand(() => ActivePane.PreviousTab());

        // A split needs a tab to split from: with none, the pane split would stand empty beside the new one.
        SplitRightCommand = new RelayCommand(() => Split(SplitAxis.Columns), () => ActiveTab is not null);
        SplitDownCommand = new RelayCommand(() => Split(SplitAxis.Rows), () => ActiveTab is not null);
        ClosePaneCommand = new AsyncRelayCommand(() => ClosePaneAsync(ActivePane), () => _byId.Count > 1);
        OnlyPaneCommand = new AsyncRelayCommand(OnlyAsync, () => _byId.Count > 1);
        EqualizeCommand = new RelayCommand(Equalize, () => _byId.Count > 1);
        FocusNextPaneCommand = new RelayCommand(FocusNext, () => _byId.Count > 1);
        FocusPreviousPaneCommand = new RelayCommand(FocusPrevious, () => _byId.Count > 1);

        // After the commands: making a pane active refreshes them.
        ActivePaneId = Layout.Panes[0];
        ActivePane = AddPane(ActivePaneId);
        UpdateActiveFlags();
    }

    /// <summary>
    /// Raised when the arrangement of panes has changed and the view has to be rebuilt.
    /// </summary>
    public event EventHandler? LayoutChanged;

    /// <summary>
    /// Raised when a buffer has been shown in the active pane — opened, created, or brought forward
    /// again — whether or not the active tab changed.
    /// </summary>
    public event EventHandler? DocumentShown;

    /// <summary>
    /// Gets how every editor looks.
    /// </summary>
    public EditorPreferences Preferences { get; }

    /// <inheritdoc />
    public IVimSession Vim { get; }

    /// <inheritdoc />
    public VimSharedState VimShared { get; }

    /// <summary>
    /// Gets the arrangement of panes.
    /// </summary>
    public LayoutTree Layout { get; }

    /// <summary>
    /// Gets how far one resize command moves a splitter, as a share of the split: the preference, which
    /// is in percent.
    /// </summary>
    public double ResizeStep => Preferences.PaneResizeStep / 100.0;

    /// <summary>
    /// Gets the panes, in reading order.
    /// </summary>
    public IReadOnlyList<EditorPaneViewModel> Panes => [.. Layout.Panes.Select(id => _byId[id])];

    /// <summary>
    /// Gets the id of the pane with the keyboard.
    /// </summary>
    public PaneId ActivePaneId { get; private set; }

    /// <summary>
    /// Gets the pane with the keyboard, which the file commands act on.
    /// </summary>
    public EditorPaneViewModel ActivePane
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(ActiveTab));
                OnPropertyChanged(nameof(ActiveDocument));
                RefreshCommands();
            }
        }
    }

    /// <summary>
    /// Gets the tab the active pane is showing.
    /// </summary>
    public EditorTabViewModel? ActiveTab => ActivePane.ActiveTab;

    /// <summary>
    /// Gets the buffer the active pane is showing.
    /// </summary>
    public DocumentViewModel? ActiveDocument => ActiveTab?.Document;

    /// <summary>
    /// Gets a value indicating whether any open buffer has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => _registry.Documents.Any(document => document.IsDirty);

    /// <summary>
    /// Gets a value indicating whether any pane has a tab.
    /// </summary>
    public bool HasTabs => _byId.Values.Any(pane => pane.Tabs.Count > 0);

    /// <summary>
    /// Gets <em>File → New</em>.
    /// </summary>
    public RelayCommand NewCommand { get; }

    /// <summary>
    /// Gets <em>File → Open…</em>.
    /// </summary>
    public AsyncRelayCommand OpenCommand { get; }

    /// <summary>
    /// Gets <em>File → Open as Hex…</em>.
    /// </summary>
    public AsyncRelayCommand OpenHexCommand { get; }

    /// <summary>
    /// Gets <em>File → Save</em>.
    /// </summary>
    public AsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// Gets <em>File → Save As…</em>.
    /// </summary>
    public AsyncRelayCommand SaveAsCommand { get; }

    /// <summary>
    /// Gets <em>File → Save All</em>.
    /// </summary>
    public AsyncRelayCommand SaveAllCommand { get; }

    /// <summary>
    /// Gets <em>File → Close Tab</em>.
    /// </summary>
    public AsyncRelayCommand CloseTabCommand { get; }

    /// <summary>
    /// Gets the command showing the active pane's next tab.
    /// </summary>
    public RelayCommand NextTabCommand { get; }

    /// <summary>
    /// Gets the command showing the active pane's previous tab.
    /// </summary>
    public RelayCommand PreviousTabCommand { get; }

    /// <summary>
    /// Gets <em>View → Split Right</em>.
    /// </summary>
    public RelayCommand SplitRightCommand { get; }

    /// <summary>
    /// Gets <em>View → Split Down</em>.
    /// </summary>
    public RelayCommand SplitDownCommand { get; }

    /// <summary>
    /// Gets <em>View → Close Pane</em>.
    /// </summary>
    public AsyncRelayCommand ClosePaneCommand { get; }

    /// <summary>
    /// Gets <em>View → Only This Pane</em>.
    /// </summary>
    public AsyncRelayCommand OnlyPaneCommand { get; }

    /// <summary>
    /// Gets <em>View → Equalize Panes</em>.
    /// </summary>
    public RelayCommand EqualizeCommand { get; }

    /// <summary>
    /// Gets <em>View → Focus Next Pane</em>.
    /// </summary>
    public RelayCommand FocusNextPaneCommand { get; }

    /// <summary>
    /// Gets <em>View → Focus Previous Pane</em>.
    /// </summary>
    public RelayCommand FocusPreviousPaneCommand { get; }

    /// <summary>
    /// The pane a layout leaf stands for.
    /// </summary>
    /// <param name="id">The leaf.</param>
    /// <returns>The pane.</returns>
    public EditorPaneViewModel PaneFor(PaneId id) => _byId[id];

    /// <summary>
    /// The layout leaf a pane stands at.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>Its id, or <see langword="null"/> for a pane that is not in this workspace.</returns>
    public PaneId? IdOf(EditorPaneViewModel pane)
    {
        foreach ((PaneId id, EditorPaneViewModel candidate) in _byId)
        {
            if (ReferenceEquals(candidate, pane))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>
    /// Gives a pane the keyboard.
    /// </summary>
    /// <param name="pane">The pane.</param>
    public void Activate(EditorPaneViewModel pane)
    {
        ArgumentNullException.ThrowIfNull(pane);

        if (IdOf(pane) is { } id)
        {
            Activate(id);
        }
    }

    /// <summary>
    /// Splits the active pane, with a new untitled buffer in the new pane, which takes the keyboard.
    /// </summary>
    /// <param name="axis">Side by side (<see cref="SplitAxis.Columns"/>) or one above the other.</param>
    /// <param name="newDocument">
    /// Whether the new pane starts with a new untitled buffer, as every split the user asks for does —
    /// or empty, for a split that is about to open a file there (<c>:split file</c>).
    /// </param>
    /// <returns>The new pane.</returns>
    public EditorPaneViewModel Split(SplitAxis axis, bool newDocument = true)
    {
        EditorPaneViewModel? created = null;

        Batch(() =>
        {
            PaneId id = Layout.Split(ActivePaneId, axis);
            created = AddPane(id);
            Activate(id);

            if (newDocument)
            {
                NewDocument();
            }
        });

        UpdateActiveFlags();
        RefreshCommands();

        return created ?? throw new InvalidOperationException("The split did not create a pane.");
    }

    /// <summary>
    /// Closes a pane: every tab of it, asking about unsaved changes no other pane keeps open.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>
    /// Whether the pane closed. The last pane never closes, and a cancelled question keeps the pane with
    /// the tabs it still has.
    /// </returns>
    public async Task<bool> ClosePaneAsync(EditorPaneViewModel pane)
    {
        ArgumentNullException.ThrowIfNull(pane);

        if ((_byId.Count <= 1) || (IdOf(pane) is not { } id))
        {
            return false;
        }

        foreach (EditorTabViewModel tab in pane.Tabs.ToList())
        {
            if (!await CloseTabAsync(pane, tab, discard: false, keepEmptyPane: true))
            {
                return false;
            }
        }

        RemovePane(id);

        return true;
    }

    /// <summary>
    /// Closes every pane but the active one.
    /// </summary>
    /// <returns>Whether every other pane closed.</returns>
    public async Task<bool> OnlyAsync()
    {
        foreach (EditorPaneViewModel pane in Panes.Where(pane => !ReferenceEquals(pane, ActivePane)).ToList())
        {
            EditorPaneViewModel keep = ActivePane;

            if (!await ClosePaneAsync(pane))
            {
                return false;
            }

            Activate(keep);
        }

        return true;
    }

    /// <summary>
    /// Gives every pane an equal share of its split.
    /// </summary>
    public void Equalize() => Layout.Equalize();

    /// <summary>
    /// Moves the keyboard to the pane next to the active one in a direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>Whether there was a pane there.</returns>
    public bool Focus(FocusDirection direction)
    {
        if (Layout.FindNeighbor(ActivePaneId, direction) is not { } neighbour)
        {
            return false;
        }

        Activate(neighbour);

        return true;
    }

    /// <summary>
    /// Moves the keyboard to the next pane, in reading order, wrapping round.
    /// </summary>
    public void FocusNext() => Activate(Layout.Next(ActivePaneId));

    /// <summary>
    /// Moves the keyboard to the previous pane, in reading order, wrapping round.
    /// </summary>
    public void FocusPrevious() => Activate(Layout.Previous(ActivePaneId));

    /// <summary>
    /// Grows or shrinks the active pane along an axis.
    /// </summary>
    /// <param name="axis">Columns for wider and narrower, rows for taller and shorter.</param>
    /// <param name="delta">The share of the split to move, positive to grow.</param>
    /// <returns>Whether the pane had a split along that axis to resize.</returns>
    public bool Resize(SplitAxis axis, double delta) => Layout.Resize(ActivePaneId, axis, delta);

    /// <summary>
    /// Moves the bar of the active pane's nearest split along an axis, the same way from either side of it.
    /// </summary>
    /// <param name="axis">Columns to move it left or right, rows to move it up or down.</param>
    /// <param name="delta">The share of the split to move it by, positive to the right or down.</param>
    /// <returns>Whether the pane had a split along that axis.</returns>
    public bool MoveSplitter(SplitAxis axis, double delta) => Layout.MoveSplitter(ActivePaneId, axis, delta);

    /// <summary>
    /// Moves the active tab to the pane next to the active one in a direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    /// <returns>Whether there was a tab, and a pane there to take it.</returns>
    /// <remarks>
    /// The pane is the one <see cref="Focus(FocusDirection)"/> would move to. With none, nothing happens, as
    /// nothing happens when the keyboard cannot move that way.
    /// </remarks>
    public bool MoveTab(FocusDirection direction)
        => (ActiveTab is { } tab)
           && (Layout.FindNeighbor(ActivePaneId, direction) is { } neighbour)
           && MoveTab(ActivePane, tab, _byId[neighbour]);

    /// <summary>
    /// Moves a tab from one pane to another, which takes the keyboard.
    /// </summary>
    /// <param name="source">The pane the tab is on.</param>
    /// <param name="tab">The tab.</param>
    /// <param name="target">The pane to move it to.</param>
    /// <returns>Whether it moved: not to its own pane, nor from a pane it is not on.</returns>
    /// <remarks>
    /// <para>
    /// The tab itself moves, with the caret and the scroll it was left at, to the end of the target's strip.
    /// When the target already shows its buffer, that tab is shown instead and the moved one is closed: a pane
    /// never shows one buffer in two tabs. Either way nothing is asked — the buffer stays open.
    /// </para>
    /// <para>
    /// The pane the tab leaves closes when it has no tab left, as it would had the tab been closed.
    /// </para>
    /// </remarks>
    public bool MoveTab(EditorPaneViewModel source, EditorTabViewModel tab, EditorPaneViewModel target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentNullException.ThrowIfNull(target);

        if (ReferenceEquals(source, target) || (IdOf(source) is not { } sourceId) || (IdOf(target) is null)
            || !source.Tabs.Contains(tab))
        {
            return false;
        }

        source.Remove(tab);

        if (target.Tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Document, tab.Document)) is { } existing)
        {
            target.ActiveTab = existing;
            _registry.Release(tab.Document);
        }
        else
        {
            target.Adopt(tab);
        }

        Activate(target);

        if (source.Tabs.Count == 0)
        {
            RemovePane(sourceId);
        }

        RefreshCommands();
        DocumentShown?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Opens a file in the active pane, or shows it there when it is already open.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <returns>The tab showing it, or <see langword="null"/> when it could not be opened.</returns>
    public async Task<EditorTabViewModel?> OpenAsync(string filePath)
    {
        DocumentViewModel? document = await _documents.OpenAsync(filePath);

        return document is null ? null : Show(document);
    }

    /// <summary>
    /// Asks which files to open, starting in the active buffer's folder, and opens each in the active
    /// pane — <em>File → Open…</em>.
    /// </summary>
    /// <returns>A task that completes when every chosen file has been opened or reported.</returns>
    public async Task OpenFromPromptAsync()
    {
        foreach (string path in await _prompts.AskFilesToOpenAsync(ActiveFolder()))
        {
            await OpenAsync(path);
        }
    }

    /// <summary>
    /// Opens a file in the hex viewer in the active pane, or shows its hex view there when it is open.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <returns>The tab showing it, or <see langword="null"/> when it could not be opened.</returns>
    public async Task<EditorTabViewModel?> OpenHexAsync(string filePath)
    {
        DocumentViewModel? document = await _documents.OpenHexAsync(filePath);

        return document is null ? null : Show(document);
    }

    /// <summary>
    /// Shows the active tab's file in the other view — the hex viewer for text, text for hex — in the
    /// tab's own place.
    /// </summary>
    /// <param name="asHex">Whether to reopen as hex; otherwise as text.</param>
    /// <returns>Whether the tab now shows the file the other way.</returns>
    /// <remarks>
    /// Leaving a text view asks about unsaved changes as closing it would, when no other tab still
    /// shows the buffer.
    /// </remarks>
    public async Task<bool> ReopenActiveAsync(bool asHex)
    {
        if ((ActiveTab is not { Document: { FilePath: { } path } current } tab) || (current.IsHex == asHex))
        {
            return false;
        }

        EditorPaneViewModel pane = ActivePane;

        if ((_registry.ViewCount(current) <= 1) && current.IsDirty && !await ResolveUnsavedAsync(current))
        {
            return false;
        }

        DocumentViewModel? replacement = asHex
            ? await _documents.OpenHexAsync(path)
            : await _documents.OpenAsync(path, forceText: true);

        if ((replacement is null) || !pane.Tabs.Contains(tab))
        {
            return false;
        }

        pane.Replace(tab, replacement, out bool created);

        if (created)
        {
            _registry.Retain(replacement);
        }

        _registry.Release(current);
        RefreshCommands();

        return true;
    }

    /// <summary>
    /// Creates an untitled buffer and shows it in the active pane.
    /// </summary>
    /// <returns>Its tab.</returns>
    public EditorTabViewModel NewDocument() => Show(_documents.CreateUntitled());

    /// <summary>
    /// Puts a new untitled buffer in a tab's place, without asking about the buffer it replaces.
    /// </summary>
    /// <param name="pane">The pane the tab is on.</param>
    /// <param name="tab">The tab.</param>
    /// <returns>The tab showing the new buffer.</returns>
    /// <remarks>
    /// What the replaced buffer had not saved is gone, unless another tab still shows it and keeps it
    /// open — the rule closing a tab follows.
    /// </remarks>
    public EditorTabViewModel ReplaceWithNewDocument(EditorPaneViewModel pane, EditorTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(pane);
        ArgumentNullException.ThrowIfNull(tab);

        DocumentViewModel replaced = tab.Document;
        DocumentViewModel untitled = _documents.CreateUntitled();
        EditorTabViewModel replacement = pane.Replace(tab, untitled, out bool created);

        if (created)
        {
            _registry.Retain(untitled);
        }

        _registry.Release(replaced);
        RefreshCommands();

        return replacement;
    }

    /// <summary>
    /// Shows a buffer in the active pane.
    /// </summary>
    /// <param name="document">The buffer, already in the registry.</param>
    /// <returns>Its tab.</returns>
    public EditorTabViewModel Show(DocumentViewModel document)
    {
        EditorTabViewModel tab = ActivePane.Show(document, out bool created);

        if (created)
        {
            _registry.Retain(document);
        }

        RefreshCommands();
        DocumentShown?.Invoke(this, EventArgs.Empty);

        return tab;
    }

    /// <summary>
    /// Saves a buffer.
    /// </summary>
    /// <param name="document">The buffer, or <see langword="null"/> for nothing.</param>
    /// <returns>Whether it was written.</returns>
    public Task<bool> SaveAsync(DocumentViewModel? document)
        => document is null ? Task.FromResult(false) : _documents.SaveAsync(document);

    /// <summary>
    /// Saves a buffer under a name the user chooses.
    /// </summary>
    /// <param name="document">The buffer, or <see langword="null"/> for nothing.</param>
    /// <returns>Whether it was written.</returns>
    public Task<bool> SaveAsAsync(DocumentViewModel? document)
        => document is null ? Task.FromResult(false) : _documents.SaveAsAsync(document);

    /// <summary>
    /// Saves a buffer to a given file, which becomes its file.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <param name="filePath">The file.</param>
    /// <returns>Whether it was written.</returns>
    public Task<bool> SaveAsAsync(DocumentViewModel document, string filePath)
        => _documents.SaveAsAsync(document, filePath);

    /// <summary>
    /// Saves every buffer with unsaved changes, stopping at the first that cannot be saved.
    /// </summary>
    /// <returns>Whether every one was written.</returns>
    public async Task<bool> SaveAllAsync()
    {
        foreach (DocumentViewModel document in _registry.Documents.Where(document => document.IsDirty).ToList())
        {
            if (!await _documents.SaveAsync(document))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public Task<bool> CloseTabAsync(EditorPaneViewModel pane, EditorTabViewModel tab, bool discard = false)
        => CloseTabAsync(pane, tab, discard, keepEmptyPane: false);

    /// <inheritdoc />
    public Task ExecuteExAsync(EditorPaneViewModel pane, VimExRequest request) => _ex.ExecuteAsync(pane, request);

    /// <inheritdoc />
    public Task ExecuteWindowAsync(EditorPaneViewModel pane, VimWindowCommand command) => _window.ExecuteAsync(pane, command);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The pane takes the keyboard first, so every file opens where it was dropped, each through the same
    /// door as <em>File → Open</em>. Folders and paths that have gone are skipped and named once on the
    /// status line, as the command line's are.
    /// </para>
    /// <para>
    /// A drop on a new tab — an untitled buffer nothing has been typed into, which no other tab shows —
    /// puts the first file that opens in its place rather than beside it: a new pane or a new window
    /// starts with such a tab, and it is not what the user dropped a file on to keep.
    /// </para>
    /// </remarks>
    public async Task OpenDroppedAsync(EditorPaneViewModel pane, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(pane);
        ArgumentNullException.ThrowIfNull(paths);

        Activate(pane);

        List<string> problems = [];
        EditorTabViewModel? fresh = pane.ActiveTab is { } target && IsFresh(target) ? target : null;

        foreach (string path in paths)
        {
            if (Directory.Exists(path))
            {
                problems.Add($"{NameOf(path)} is a folder");
            }
            else if (File.Exists(path) && (fresh is not null))
            {
                if (await OpenInPlaceOfAsync(pane, fresh, path))
                {
                    fresh = null;
                }
            }
            else if (File.Exists(path))
            {
                await OpenAsync(path);
            }
            else
            {
                problems.Add($"{NameOf(path)} does not exist");
            }
        }

        if (problems.Count > 0)
        {
            _status.Show($"Not opened: {string.Join("; ", problems)}.");
        }

        // A drive root has no name of its own.
        static string NameOf(string path)
            => Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) is { Length: > 0 } name ? name : path;
    }

    /// <inheritdoc />
    public void ShowStatus(string message) => _status.Show(message);

    /// <summary>
    /// Asks about every buffer with unsaved changes before the window closes.
    /// </summary>
    /// <returns>
    /// Whether the window may close: every buffer was saved or discarded. One <em>Cancel</em>, or one
    /// save that did not happen, keeps it open.
    /// </returns>
    public async Task<bool> ConfirmCloseAllAsync()
    {
        foreach (DocumentViewModel document in _registry.Documents.Where(document => document.IsDirty).ToList())
        {
            // Shown while it is asked about, so the user can see what they are deciding on.
            if (Panes.FirstOrDefault(pane => pane.Tabs.Any(tab => ReferenceEquals(tab.Document, document))) is { } pane)
            {
                pane.ActiveTab = pane.Tabs.First(tab => ReferenceEquals(tab.Document, document));
                Activate(pane);
            }

            if (!await ResolveUnsavedAsync(document))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Opens the files named on the command line, the last one active.
    /// </summary>
    /// <param name="startup">The files, as read from the arguments.</param>
    /// <returns>A task that completes when every file has been opened or reported.</returns>
    /// <remarks>
    /// A missing file becomes an empty buffer the first save creates, unless its folder does not exist
    /// either, which no save could fix. With nothing to open, an untitled buffer is ready to type into,
    /// as Vim starts.
    /// </remarks>
    public async Task OpenStartupFilesAsync(StartupFiles startup)
    {
        ArgumentNullException.ThrowIfNull(startup);

        List<string> problems = [.. startup.Skipped.Select(skipped => $"{skipped} is not a file")];

        foreach (StartupFile file in startup.Files)
        {
            if (file.Exists)
            {
                await OpenAsync(file.Path);
            }
            else if (Directory.Exists(Path.GetDirectoryName(file.Path)))
            {
                Show(_documents.CreateForMissingFile(file.Path));
            }
            else
            {
                problems.Add($"{file.Path} is in a folder that does not exist");
            }
        }

        if (ActiveTab is null)
        {
            NewDocument();
        }

        if (problems.Count > 0)
        {
            _status.Show($"Not opened: {string.Join("; ", problems)}.");
        }
    }

    /// <summary>
    /// The folder a picker starts in: the active buffer's.
    /// </summary>
    /// <returns>The folder, or <see langword="null"/> when the active buffer has no file.</returns>
    public string? ActiveFolder()
        => ActiveDocument?.FilePath is { } path ? Path.GetDirectoryName(path) : null;

    private async Task<bool> CloseTabAsync(EditorPaneViewModel pane, EditorTabViewModel tab, bool discard, bool keepEmptyPane)
    {
        ArgumentNullException.ThrowIfNull(pane);
        ArgumentNullException.ThrowIfNull(tab);

        if (!pane.Tabs.Contains(tab))
        {
            return false;
        }

        // Only the last tab of a buffer can lose anything: another tab still showing it — in this pane
        // or another — keeps the changes open.
        bool last = _registry.ViewCount(tab.Document) <= 1;

        if (last && tab.Document.IsDirty && !discard && !await ResolveUnsavedAsync(tab.Document))
        {
            return false;
        }

        pane.Remove(tab);
        _registry.Release(tab.Document);

        if (!keepEmptyPane && (pane.Tabs.Count == 0) && (_byId.Count > 1) && (IdOf(pane) is { } id))
        {
            RemovePane(id);
        }

        RefreshCommands();

        return true;
    }

    // Untitled, text, empty, never changed, and shown by this tab alone: nothing would be lost with it.
    private bool IsFresh(EditorTabViewModel tab)
        => tab.Document is { IsUntitled: true, IsHex: false, IsDirty: false, Document.TextLength: 0 }
           && (_registry.ViewCount(tab.Document) <= 1);

    /// <summary>
    /// Opens a file in a new tab's place.
    /// </summary>
    /// <returns>Whether the file took the tab's place; it may still have opened elsewhere.</returns>
    /// <remarks>
    /// The tab is checked again once the file has opened, since a question (a binary file) may have come
    /// in between. A file already on the pane is shown where it is, and the new tab is left for the next.
    /// </remarks>
    private async Task<bool> OpenInPlaceOfAsync(EditorPaneViewModel pane, EditorTabViewModel fresh, string path)
    {
        if (await _documents.OpenAsync(path) is not { } document)
        {
            return false;
        }

        if (!pane.Tabs.Contains(fresh) || !IsFresh(fresh) || pane.Tabs.Any(tab => ReferenceEquals(tab.Document, document)))
        {
            Show(document);

            return false;
        }

        pane.Replace(fresh, document, out bool created);

        if (created)
        {
            _registry.Retain(document);
        }

        _registry.Release(fresh.Document);
        RefreshCommands();

        return true;
    }

    private async Task<bool> ResolveUnsavedAsync(DocumentViewModel document)
    {
        switch (await _unsaved.AskAsync(document.Title))
        {
            case UnsavedChangesChoice.Save:
                return await _documents.SaveAsync(document);

            case UnsavedChangesChoice.Discard:
                return true;

            default:
                return false;
        }
    }

    private EditorPaneViewModel AddPane(PaneId id)
    {
        EditorPaneViewModel pane = new(Preferences, this);
        pane.PropertyChanged += OnPanePropertyChanged;
        _byId[id] = pane;

        return pane;
    }

    private void RemovePane(PaneId id)
    {
        Batch(() =>
        {
            if (Layout.Close(id, out PaneId successor) && _byId.Remove(id, out EditorPaneViewModel? removed))
            {
                removed.PropertyChanged -= OnPanePropertyChanged;

                if (id == ActivePaneId)
                {
                    Activate(successor);
                }
            }
        });

        UpdateActiveFlags();
        RefreshCommands();
    }

    private void Activate(PaneId id)
    {
        if (!_byId.TryGetValue(id, out EditorPaneViewModel? pane))
        {
            return;
        }

        ActivePaneId = id;
        ActivePane = pane;
        UpdateActiveFlags();
    }

    private void UpdateActiveFlags()
    {
        bool several = _byId.Count > 1;

        foreach ((PaneId id, EditorPaneViewModel pane) in _byId)
        {
            bool active = id == ActivePaneId;
            pane.IsActive = active;
            pane.IsFramed = active && several;
        }
    }

    private void Batch(Action action)
    {
        _batch++;

        try
        {
            action();
        }
        finally
        {
            _batch--;
        }

        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ReferenceEquals(sender, ActivePane) && (e.PropertyName == nameof(EditorPaneViewModel.ActiveTab)))
        {
            OnPropertyChanged(nameof(ActiveTab));
            OnPropertyChanged(nameof(ActiveDocument));
            RefreshCommands();
        }
    }

    private void RefreshCommands()
    {
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        SaveAllCommand.NotifyCanExecuteChanged();
        CloseTabCommand.NotifyCanExecuteChanged();
        SplitRightCommand.NotifyCanExecuteChanged();
        SplitDownCommand.NotifyCanExecuteChanged();
        ClosePaneCommand.NotifyCanExecuteChanged();
        OnlyPaneCommand.NotifyCanExecuteChanged();
        EqualizeCommand.NotifyCanExecuteChanged();
        FocusNextPaneCommand.NotifyCanExecuteChanged();
        FocusPreviousPaneCommand.NotifyCanExecuteChanged();
    }

    private async Task OnOpenHexAsync()
    {
        foreach (string path in await _prompts.AskFilesToOpenAsync(ActiveFolder()))
        {
            await OpenHexAsync(path);
        }
    }

    private Task OnCloseActiveTabAsync()
        => ActiveTab is { } tab ? CloseTabAsync(ActivePane, tab) : Task.CompletedTask;
}

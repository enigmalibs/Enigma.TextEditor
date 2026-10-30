using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Editing;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// One editor pane: a tab strip over a single <see cref="AvaloniaTextEditor"/> that shows the active tab.
/// </summary>
/// <remarks>
/// <para>
/// The editor is shared by every tab. Switching tabs swaps its document, and this view puts the caret
/// and the scroll back where the incoming tab was left — and records them for the outgoing one as the
/// user moves — so each tab behaves as if it had an editor of its own.
/// </para>
/// <para>
/// A tab dragged onto another pane is <em>not</em> a platform drag-and-drop session, and that is deliberate. The
/// gesture never leaves the window, and over its own window Avalonia's X11 drag source delivers the drag in process
/// without offering it over the protocol, while still taking the X drag selection: a Wayland compositor bridging that
/// drag sees one nobody has accepted, and draws the refusal pointer for the whole gesture, whatever this view answers
/// (Enigma.GitClient's BUG-11A4 established it, for its branch badges). Run from pointer events with a capture, the
/// gesture wears an ordinary cursor. Files dropped from outside the application still come through the platform.
/// </para>
/// </remarks>
public partial class EditorPaneView : UserControl
{
    /// <summary>
    /// The gap either side of the line numbers.
    /// </summary>
    public const double LineNumberSpacing = 4;

    /// <summary>
    /// The editor's vertical scrollbar for the minimap setting: hidden while the minimap stands in for
    /// it, shown when needed otherwise.
    /// </summary>
    public static readonly IValueConverter VerticalScrollBarFor =
        new FuncValueConverter<bool, ScrollBarVisibility>(minimap => minimap ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto);

    /// <summary>
    /// The pointer worn while a tab is carried, made once: a cursor is a platform handle, not a value, and a drag asks
    /// for it on every pointer move.
    /// </summary>
    internal static readonly Cursor CarryingCursor = new(StandardCursorType.DragMove);

    private VimEditorBehavior? _vim;

    private EditorPaneViewModel? _pane;
    private IVimSession? _paneSession;
    private IVimSession? _session;
    private EditorTabViewModel? _current;
    private DocumentViewModel? _indented;
    private EditorAppearance? _appearance;

    // The editor's own scroll viewer, once its template is applied: the one place that knows which part
    // of the text is on screen, and the one whose offset actually moves the view in AvaloniaEdit 12.
    private ScrollViewer? _scroll;

    // A left press on a tab, kept until the pointer has gone far enough with it to make a drag, or is let go.
    private EditorTabViewModel? _pressedTab;
    private Point _tabPressPoint;

    // The tab being carried, once the press has become a drag, and the pointer the strip holds for it.
    private EditorTabViewModel? _draggedTab;
    private IPointer? _dragPointer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorPaneView"/> class.
    /// </summary>
    public EditorPaneView()
    {
        InitializeComponent();

        Editor.Options.HighlightCurrentLine = true;

        // Set on the instance rather than through a style: ShowLineNumbers builds this margin in code,
        // so whichever value was assigned last wins, and assigning it here cannot lose that race.
        foreach (LineNumberMargin margin in Editor.TextArea.LeftMargins.OfType<LineNumberMargin>())
        {
            margin.Margin = new Thickness(LineNumberSpacing, 0);
        }

        Editor.DocumentChanged += OnEditorDocumentChanged;
        Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;

        Editor.TemplateApplied += OnEditorTemplateApplied;
        Minimap.ScrollRequested += OnMinimapScrollRequested;
        Minimap.PointerWheelChanged += OnMinimapPointerWheelChanged;

        // A file dropped anywhere on the pane opens in it. Handled events too: the text area handles drags
        // of its own, which move text, and must not hide a drag of files from the pane.
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, handledEventsToo: true);

        // A tab dragged off the strip moves to the pane it is dropped on. Handled events too: the strip's items
        // take the press, to show the tab, and the press is where the drag starts from.
        Tabs.AddHandler(PointerPressedEvent, OnTabsPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        Tabs.AddHandler(PointerMovedEvent, OnTabsPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        Tabs.AddHandler(PointerReleasedEvent, OnTabsPointerLetGo, RoutingStrategies.Bubble, handledEventsToo: true);
        Tabs.AddHandler(PointerCaptureLostEvent, OnTabsPointerCaptureLost, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>
    /// Gets a value indicating whether a tab of this pane is being dragged.
    /// </summary>
    internal bool IsDraggingTab => _draggedTab is not null;

    /// <summary>
    /// Gets the Vim behaviour attached to the editor, once the pane it belongs to is known.
    /// </summary>
    internal VimEditorBehavior? Vim => _vim;

    /// <summary>
    /// Gets the editor control, for the commands that act on the focused editor (cut, copy, paste…).
    /// </summary>
    public AvaloniaTextEditor EditorControl => Editor;

    /// <summary>
    /// Gets the editor's appearance, once the pane is loaded.
    /// </summary>
    internal EditorAppearance? Appearance => _appearance;

    /// <summary>
    /// Gets the tab the editor is showing now.
    /// </summary>
    internal EditorTabViewModel? CurrentTab => _current;

    /// <summary>
    /// Gets the hex view, shown in the editor's place for a hex tab.
    /// </summary>
    internal HexView HexView => Hex;

    /// <summary>
    /// Gets the minimap beside the text.
    /// </summary>
    internal EditorMinimap MinimapControl => Minimap;

    /// <summary>
    /// Moves the keyboard focus into the editor — or onto the hex rows, for a hex tab.
    /// </summary>
    public void FocusEditor()
    {
        if (_pane is { IsHexActive: true })
        {
            Hex.FocusRows();
        }
        else
        {
            Editor.TextArea.Focus();
        }
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        FollowSession(null);

        if (_pane is not null)
        {
            _pane.PropertyChanged -= OnPanePropertyChanged;
        }

        _pane = DataContext as EditorPaneViewModel;

        if (_pane is { } pane)
        {
            pane.PropertyChanged += OnPanePropertyChanged;
            _paneSession = new PaneVimSession(pane.Vim, () => pane.IsActive);
            Hex.Attach(pane, _paneSession);

            // Built with the first pane rather than in the constructor, because the engine is born
            // with the state it shares with every other pane's engine — the registers, the last search
            // — and that comes from the pane's host. Not awaited: an ex or window command ends in a
            // save, a split or a dialog, and this is a keystroke handler with nowhere to return a
            // task to; failures are reported by the services the commands go through.
            if (_vim is null)
            {
                _vim = new VimEditorBehavior(
                    Editor,
                    () => _session,
                    request => _pane?.ExecuteExAsync(request).Forget(),
                    command => _pane?.ExecuteWindowAsync(command).Forget(),
                    OnVimOptionsChanged,
                    pane.VimShared);

                // Already on screen when the pane arrived: OnLoaded has run and will not connect it.
                if (IsLoaded)
                {
                    _vim.ConnectHighlight();
                }
            }
        }
        else
        {
            _paneSession = null;
        }

        FollowSession(_paneSession);

        base.OnDataContextChanged(e);
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        // Not the constructor: finding a brush walks up to the application's resources, and the
        // variant is only resolved to light or dark once the control is in a tree.
        _appearance ??= new EditorAppearance(
            Editor,
            ActualThemeVariant,
            failure => Dispatcher.UIThread.Post(() => _pane?.ReportHighlightingFailure(failure)));
        ApplyLanguage();
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
        FollowIndentation(_current?.Document);
        FollowSession(_paneSession);
        _vim?.ConnectHighlight();

        base.OnLoaded(e);
    }

    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        FollowIndentation(null);

        // Disposed rather than kept: it holds a background tokenizer and a line transformer on the
        // text view. OnLoaded builds a new one if the pane comes back.
        _appearance?.Dispose();
        _appearance = null;

        // The session and the shared Vim state are container singletons and outlive this control:
        // leaving handlers on them would keep the whole pane alive.
        FollowSession(null);
        _vim?.DisconnectHighlight();

        base.OnUnloaded(e);
    }

    // A pane view taken out of the tree while the split host rebuilds has no variant for a moment; it
    // gets the real one again when it is put back.
    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (ActualThemeVariant is { } variant)
        {
            _appearance?.Apply(variant);
        }
    }

    // A middle click anywhere on a tab closes it — the convention every browser and editor shares.
    // PointerReleased, so a press that began elsewhere and drifted over a tab does not close it.
    private void OnTabsPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if ((e.InitialPressMouseButton != MouseButton.Middle)
            || ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>()?.DataContext is not EditorTabViewModel tab))
        {
            return;
        }

        e.Handled = true;
        _pane?.CloseTabCommand.Execute(tab);
    }

    // Not from the close button, whose press is a click on it, and only with the left button, the one that drags.
    private void OnTabsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ForgetTabPress();

        if ((_draggedTab is not null)
            || !e.GetCurrentPoint(Tabs).Properties.IsLeftButtonPressed
            || ((e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null)
            || ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is not EditorTabViewModel tab))
        {
            return;
        }

        _pressedTab = tab;
        _tabPressPoint = e.GetPosition(Tabs);
    }

    // Past the platform's drag distance — the one beyond which a press is no longer a click — the press becomes a
    // drag of its tab. Forgotten first: the drag must start once.
    private void OnTabsPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTab is not null)
        {
            SteerTab(e);
            return;
        }

        if (_pressedTab is not { } tab)
        {
            return;
        }

        if (!e.GetCurrentPoint(Tabs).Properties.IsLeftButtonPressed)
        {
            ForgetTabPress();
            return;
        }

        Size distance = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(4, 4);

        if (!TabDragGesture.IsDrag(e.GetPosition(Tabs) - _tabPressPoint, distance))
        {
            return;
        }

        ForgetTabPress();
        StartDraggingTab(tab, e);
    }

    // The release ends the drag, and moves the tab when it is let go over a pane that takes it. Moved once the release
    // has been handled: the strip the tab leaves changes under it, and its whole pane goes when the tab was its last.
    private void OnTabsPointerLetGo(object? sender, PointerReleasedEventArgs e)
    {
        ForgetTabPress();

        if (_draggedTab is not { } tab)
        {
            return;
        }

        EditorPaneViewModel? target = PaneUnder(e)?.DataContext as EditorPaneViewModel;

        StopDraggingTab();
        e.Handled = true;

        if ((target is not null) && target.CanTakeTab(tab))
        {
            Dispatcher.UIThread.Post(() => target.TakeTab(tab));
        }
    }

    // Whoever took the pointer owns it now: the drag ends where it is, dropping nothing, and the capture is left to them.
    private void OnTabsPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ForgetTabPress();

        if (_draggedTab is null)
        {
            return;
        }

        _dragPointer = null;
        StopDraggingTab();
    }

    private void ForgetTabPress() => _pressedTab = null;

    private void StartDraggingTab(EditorTabViewModel tab, PointerEventArgs e)
    {
        _draggedTab = tab;

        // The capture makes the rest of the gesture the strip's: every move, and the release that ends it, are
        // reported here wherever the pointer goes — and the pointer wears the strip's cursor all the while.
        _dragPointer = e.Pointer;
        _dragPointer.Capture(Tabs);

        SteerTab(e);
    }

    private void SteerTab(PointerEventArgs e)
    {
        e.Handled = true;

        if (TabDragGesture.CursorFor(PaneUnder(e) is not null) is not null)
        {
            Tabs.Cursor = CarryingCursor;
        }
        else
        {
            Tabs.ClearValue(CursorProperty);
        }
    }

    // Cleared before the capture is let go: letting it go reports a lost capture, which must find no drag left to end.
    private void StopDraggingTab()
    {
        _draggedTab = null;

        Tabs.ClearValue(CursorProperty);

        IPointer? pointer = _dragPointer;
        _dragPointer = null;
        pointer?.Capture(null);
    }

    // The pane under the pointer, in this window. Hit-tested: while the strip holds the pointer, every event comes
    // from the strip, wherever the pointer is.
    private EditorPaneView? PaneUnder(PointerEventArgs e)
        => (TopLevel.GetTopLevel(this) is { } top) && (top.InputHitTest(e.GetPosition(top)) is Visual hit)
            ? hit.FindAncestorOfType<EditorPaneView>(includeSelf: true)
            : null;

    // What can run is read as the menu opens: the editor's Can* answers are not observable.
    private void OnEditorMenuOpening(object? sender, CancelEventArgs e)
    {
        MenuUndo.IsEnabled = Editor.CanUndo;
        MenuRedo.IsEnabled = Editor.CanRedo;
        MenuCut.IsEnabled = Editor.CanCut;
        MenuCopy.IsEnabled = Editor.CanCopy;
        MenuPaste.IsEnabled = Editor.CanPaste;
        MenuDelete.IsEnabled = Editor.CanDelete;
        MenuSelectAll.IsEnabled = Editor.CanSelectAll;
    }

    private void OnMenuUndoClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Undo());

    private void OnMenuRedoClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Redo());

    private void OnMenuCutClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Cut());

    private void OnMenuCopyClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Copy());

    private void OnMenuPasteClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Paste());

    private void OnMenuDeleteClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.Delete());

    private void OnMenuSelectAllClick(object? sender, RoutedEventArgs e) => EditFromMenu(editor => editor.SelectAll());

    // The focus goes back to the text the menu was opened on, which also gives its pane the keyboard.
    private void EditFromMenu(Action<AvaloniaTextEditor> action)
    {
        action(Editor);
        Editor.TextArea.Focus();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        IReadOnlyList<string> paths = DroppedFiles.ReadPaths(e.DataTransfer);

        if (paths.Count == 0)
        {
            // A drag of text is the text area's, which has already said what it would do with it; anywhere
            // else on the pane it would do nothing, and the cursor says so.
            if (!e.Handled)
            {
                e.DragEffects = DragDropEffects.None;
            }

            return;
        }

        e.DragEffects = DroppedFiles.EffectFor(paths);
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // Read now: the dragged data is only there while the drop event lasts.
        IReadOnlyList<string> paths = DroppedFiles.ReadPaths(e.DataTransfer);
        DragDropEffects effect = DroppedFiles.EffectFor(paths);

        if ((effect == DragDropEffects.None) || (_pane is not { } pane))
        {
            return;
        }

        e.DragEffects = effect;
        e.Handled = true;

        // Opened once the drop has returned: the platform's drag loop has to be over before a dialog — the
        // binary-file question, a file that cannot be read — can appear. Not awaited, as the ex commands are
        // not: failures are reported by the services the opening goes through.
        Dispatcher.UIThread.Post(() => pane.OpenDroppedAsync(paths).Forget());
    }

    private void OnEditorDocumentChanged(object? sender, EventArgs e)
    {
        // Matched on the control's document rather than read from ActiveTab: the binding and the
        // property notification have no guaranteed order between them, and this answer cannot be wrong.
        _current = _pane?.Tabs.FirstOrDefault(tab => ReferenceEquals(tab.Document.Document, Editor.Document));

        FollowIndentation(_current?.Document);

        // The text under the engine has been replaced wholesale, so it starts again on this one — in
        // normal mode, with a caret normal mode allows.
        _vim?.OnDocumentChanged();

        RestoreViewState();
        ReportCaretToMinimap();
    }

    private void OnEditorTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (_scroll is not null)
        {
            _scroll.ScrollChanged -= OnEditorScrollChanged;
        }

        _scroll = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

        if (_scroll is not null)
        {
            _scroll.ScrollChanged += OnEditorScrollChanged;
        }

        ReportViewportToMinimap();
    }

    // Offset, extent and viewport all arrive here: a scroll, a longer document, a taller pane, a wrap.
    private void OnEditorScrollChanged(object? sender, ScrollChangedEventArgs e) => ReportViewportToMinimap();

    /// <summary>
    /// Tells the minimap which part of the text is on screen, and where the caret is.
    /// </summary>
    /// <remarks>
    /// The text area is a logical scrollable that counts in pixels, so the scroll viewer's offset,
    /// extent and viewport are the document's own.
    /// </remarks>
    private void ReportViewportToMinimap()
    {
        double extent = _scroll?.Extent.Height ?? 0;

        if ((_scroll is null) || (extent <= 0))
        {
            // Nothing to scroll: all of it is on screen, which is what a full window says.
            Minimap.ViewportStart = 0;
            Minimap.ViewportEnd = 1;
        }
        else
        {
            Minimap.ViewportStart = Math.Clamp(_scroll.Offset.Y / extent, 0, 1);
            Minimap.ViewportEnd = Math.Clamp((_scroll.Offset.Y + _scroll.Viewport.Height) / extent, 0, 1);
        }

        ReportCaretToMinimap();
    }

    // Measured where the caret's line is drawn, not by its number: with word wrap a long line takes
    // several rows, and the mark has to agree with the window around it.
    private void ReportCaretToMinimap()
    {
        double extent = _scroll?.Extent.Height ?? 0;

        if ((Editor.TextArea.TextView.Document is not { } document) || (extent <= 0))
        {
            Minimap.CaretPosition = double.NaN;

            return;
        }

        // Clamped: during a swap the caret can still stand where it was in the outgoing, longer text.
        int line = Math.Clamp(Editor.TextArea.Caret.Line, 1, document.LineCount);
        double top = Editor.TextArea.TextView.GetVisualTopByDocumentLine(line);
        Minimap.CaretPosition = Math.Clamp(top / extent, 0, 1);
    }

    private void OnMinimapScrollRequested(object? sender, double start)
    {
        if (_scroll is null)
        {
            return;
        }

        _scroll.Offset = new Vector(_scroll.Offset.X, Math.Clamp(start * _scroll.Extent.Height, 0, FurthestOffset(_scroll)));
    }

    // The wheel over the map scrolls the text by what the wheel over the text does: the map stands where
    // the scrollbar did, and the scrollbar passed the wheel on.
    private void OnMinimapPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_scroll is null)
        {
            return;
        }

        double step = ((ILogicalScrollable)Editor.TextArea.TextView).ScrollSize.Height;

        _scroll.Offset = new Vector(_scroll.Offset.X, Math.Clamp(_scroll.Offset.Y - (e.Delta.Y * step), 0, FurthestOffset(_scroll)));
        e.Handled = true;
    }

    private static double FurthestOffset(ScrollViewer scroll) => Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);

    private void RestoreViewState()
    {
        if (_current is not { } tab)
        {
            return;
        }

        Editor.CaretOffset = Math.Min(tab.CaretOffset, Editor.Document?.TextLength ?? 0);

        // Posted: the text view has to have measured the new document before it can scroll, and the
        // focus has to come after whatever click started this (a tab, a menu) has taken it.
        Dispatcher.UIThread.Post(
            () =>
            {
                if (!ReferenceEquals(tab, _current))
                {
                    return;
                }

                Editor.ScrollToVerticalOffset(tab.VerticalScrollOffset);
                Editor.ScrollToHorizontalOffset(tab.HorizontalScrollOffset);

                if (tab.Document.IsHex && (tab.HexRow >= 0))
                {
                    Hex.GoTo(tab.HexRow);
                }

                FocusEditor();
            },
            DispatcherPriority.Background);
    }

    // The editor's indentation options are per control, the indentation is per buffer: they are put in
    // step on every swap, and again whenever the buffer's indentation changes.
    private void FollowIndentation(DocumentViewModel? document)
    {
        if (_indented is not null)
        {
            _indented.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _indented = document;

        if (_indented is not null)
        {
            _indented.PropertyChanged += OnDocumentPropertyChanged;
            ApplyIndentation(_indented);
        }

        ApplyLanguage();
    }

    private void ApplyLanguage()
        => _appearance?.SetLanguage(_indented?.Language ?? Services.LanguageInfo.PlainText);

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(DocumentViewModel.Indentation)) && (sender is DocumentViewModel document))
        {
            ApplyIndentation(document);
        }
        else if (e.PropertyName == nameof(DocumentViewModel.Language))
        {
            ApplyLanguage();
        }
    }

    // A ':set' changed the indentation. It is the buffer's: written there, every pane showing the buffer,
    // the Tab key, the Tools menu and the status bar follow — and the engine is seeded back from it.
    private void OnVimOptionsChanged(VimOptions options)
    {
        if (_indented is { IsHex: false } document)
        {
            document.Indentation = options.ExpandTab
                ? Indentation.Spaces(options.ShiftWidth)
                : Indentation.Tabs(options.ShiftWidth);
        }
    }

    private void ApplyIndentation(DocumentViewModel document)
    {
        Editor.Options.ConvertTabsToSpaces = !document.Indentation.UseTabs;
        Editor.Options.IndentationSize = document.Indentation.Size;

        // Re-seeds Vim's shiftwidth and expandtab from the options just set, so '>>' indents as Tab does.
        _vim?.Refresh();
    }

    // Vim being switched on or off has to reach the engine's key handlers; the session is where the
    // setting announces it.
    private void FollowSession(IVimSession? session)
    {
        if (ReferenceEquals(_session, session))
        {
            return;
        }

        if (_session is not null)
        {
            _session.Changed -= OnSessionChanged;
        }

        _session = session;

        if (_session is not null)
        {
            _session.Changed += OnSessionChanged;
        }

        _vim?.Refresh();
    }

    private void OnSessionChanged(object? sender, EventArgs e) => _vim?.Refresh();

    // The pane that takes the keyboard puts its own mode back on the status bar: the chip was showing
    // the mode of the pane that had it before.
    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(EditorPaneViewModel.IsActive)) && (_pane is { IsActive: true }))
        {
            _vim?.Announce();
        }
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (!ShowsCurrentTab)
        {
            return;
        }

        _current!.CaretOffset = Editor.CaretOffset;
        _current.CaretLine = Editor.TextArea.Caret.Line;
        _current.CaretColumn = Editor.TextArea.Caret.Column;

        ReportCaretToMinimap();
    }

    private void OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (!ShowsCurrentTab)
        {
            return;
        }

        _current!.VerticalScrollOffset = Editor.VerticalOffset;
        _current.HorizontalScrollOffset = Editor.HorizontalOffset;
    }

    // Both handlers also fire during a swap, while the control already shows the incoming text but
    // _current still names the outgoing tab: recording then would overwrite the outgoing tab's place
    // with the incoming document's.
    private bool ShowsCurrentTab
        => (_current is not null) && ReferenceEquals(_current.Document.Document, Editor.Document);
}

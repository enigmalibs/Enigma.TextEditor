using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The read-only hex viewer: one row per sixteen bytes, scrolled like any list.
/// </summary>
/// <remarks>
/// <para>
/// The mouse selects bytes, in the hex column or in the text column: a press selects the byte under the pointer,
/// a drag extends the selection without leaving the column it started in, and Shift with a press extends it from
/// where it started. The tab holds the selection; each row paints its own part of it (<see cref="HexRowBlock"/>).
/// </para>
/// <para>
/// The list's own keys move through the rows — arrows, Page Up and Down, Home and End — and with Vim
/// on, so do <c>j</c>, <c>k</c>, <c>gg</c> and <c>G</c>. Every other key goes to a Vim engine over an
/// empty buffer (<see cref="HexVimBuffer"/>), so that <c>:q</c>, <c>:bn</c>, <c>:sp</c> and
/// <c>Ctrl+W</c> work here as they do in a text tab.
/// </para>
/// </remarks>
public partial class HexView : UserControl
{
    private EditorPaneViewModel? _pane;
    private IVimSession? _session;
    private VimEngine? _engine;
    private bool _pendingG;
    private bool _dragging;

    /// <summary>
    /// Initializes a new instance of the <see cref="HexView"/> class.
    /// </summary>
    public HexView()
    {
        InitializeComponent();

        Rows.AddHandler(KeyDownEvent, OnRowsKeyDown, RoutingStrategies.Tunnel);
        Rows.AddHandler(TextInputEvent, OnRowsTextInput, RoutingStrategies.Tunnel);

        // Tunnelled, so a press on a row selects bytes before the list would select the row.
        Rows.AddHandler(PointerPressedEvent, OnRowsPointerPressed, RoutingStrategies.Tunnel);
        Rows.AddHandler(PointerMovedEvent, OnRowsPointerMoved, RoutingStrategies.Tunnel);
        Rows.AddHandler(PointerReleasedEvent, OnRowsPointerReleased, RoutingStrategies.Tunnel);
        Rows.PointerCaptureLost += (_, _) => _dragging = false;
    }

    /// <summary>
    /// Gets the list of rows.
    /// </summary>
    internal ListBox RowList => Rows;

    /// <summary>
    /// Hands the view the pane it lives in, for the Vim session and the commands its keys raise.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="session">The session as this pane sees it.</param>
    internal void Attach(EditorPaneViewModel pane, IVimSession session)
    {
        _pane = pane ?? throw new ArgumentNullException(nameof(pane));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        _engine ??= CreateEngine(pane);
    }

    /// <summary>
    /// Moves the keyboard focus onto the rows.
    /// </summary>
    public void FocusRows()
    {
        if (Rows.SelectedIndex < 0 && Rows.ItemCount > 0)
        {
            Rows.SelectedIndex = 0;
        }

        Rows.Focus();
    }

    /// <summary>
    /// Selects a row and scrolls it into view.
    /// </summary>
    /// <param name="row">The row, clamped to the rows there are.</param>
    internal void GoTo(int row)
    {
        if (Rows.ItemCount == 0)
        {
            return;
        }

        int target = Math.Clamp(row, 0, Rows.ItemCount - 1);
        Rows.SelectedIndex = target;
        Rows.ScrollIntoView(target);
    }

    private VimEngine CreateEngine(EditorPaneViewModel pane)
    {
        VimEngine engine = new(
            new HexVimBuffer(
                message => _session?.ReportStatus(message),
                line => _session?.ReportCommandLine(line)),
            options: null,
            pane.VimShared);

        // Not awaited, as in the editor: a keystroke handler has nowhere to return a task to.
        engine.ExCommandRequested += (_, request) => _pane?.ExecuteExAsync(request).Forget();
        engine.WindowCommandRequested += (_, command) => _pane?.ExecuteWindowAsync(command).Forget();

        return engine;
    }

    private bool VimIsOn => (_engine is not null) && (_session?.IsEnabled == true);

    private void OnRowsKeyDown(object? sender, KeyEventArgs e)
    {
        if (!VimIsOn)
        {
            return;
        }

        // While a ':' line or a Ctrl+W is pending, every key belongs to the engine — Enter, Esc and
        // Backspace included, which the list would otherwise take.
        if (_engine!.HasPendingSequence && (VimEditorBehavior.Translate(e) is { } pending))
        {
            e.Handled = _engine.HandleKey(pending);
            return;
        }

        // Ctrl+W: the window prefix.
        if ((e.Key == Key.W) && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = _engine.HandleKey(VimKey.Typed('w', VimModifiers.Control));
        }
    }

    private void OnRowsTextInput(object? sender, TextInputEventArgs e)
    {
        if (!VimIsOn || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        e.Handled = true;

        foreach (char character in e.Text)
        {
            if (_engine!.HasPendingSequence || !Navigate(character))
            {
                _engine.HandleKey(VimKey.Typed(character));
            }
        }
    }

    // A press selects the byte under the pointer, in the column it is in; with Shift, it extends the selection
    // there is. Either way the row it reaches becomes the current one, and the rows take the keyboard.
    private void OnRowsPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties button = e.GetCurrentPoint(Rows).Properties;

        if ((DataContext is not EditorTabViewModel { Document.Hex: { } } tab)
            || !button.IsLeftButtonPressed
            || ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true) is null))
        {
            // Not on a row — the scroll bar, or below a short file's last row: the list's own.
            return;
        }

        HexSelection? current = tab.HexSelection;
        bool extend = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && current.HasValue;

        if (ByteUnder(e, extend ? current!.Value.Column : null, scroll: false) is not { } hit)
        {
            return;
        }

        Select(tab, extend ? current!.Value.ExtendedTo(hit.Offset) : HexSelection.At(hit.Offset, hit.Column));

        Rows.Focus(NavigationMethod.Pointer);
        e.Pointer.Capture(Rows);
        _dragging = true;
        e.Handled = true;
    }

    // A drag extends the selection in the column it started in, wherever the pointer goes: past the list's top or
    // bottom, the rows scroll by one at each move.
    private void OnRowsPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging || (DataContext is not EditorTabViewModel { HexSelection: { } selection } tab))
        {
            return;
        }

        if (ByteUnder(e, selection.Column, scroll: true) is { } hit)
        {
            Select(tab, selection.ExtendedTo(hit.Offset));
        }

        e.Handled = true;
    }

    private void OnRowsPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void Select(EditorTabViewModel tab, HexSelection selection)
    {
        tab.HexSelection = selection;
        Rows.SelectedIndex = (int)(selection.Active / HexFormatter.BytesPerRow);
    }

    // The byte the pointer is on. Locked to a column, a pointer over the other column is on that column's nearest
    // edge byte. Above or below the rows on screen, it is on the row nearest to it — or, when scrolling, the one
    // past that, brought into view.
    private (long Offset, HexColumn Column)? ByteUnder(PointerEventArgs e, HexColumn? within, bool scroll)
    {
        if ((DataContext is not EditorTabViewModel { Document.Hex: { Length: > 0 } hex }) || (Rows.ItemCount == 0))
        {
            return null;
        }

        double y = e.GetPosition(Rows).Y;

        if (RowAt(Math.Clamp(y, 0, Math.Max(Rows.Bounds.Height - 1, 0))) is not (int row, HexRowBlock block))
        {
            return null;
        }

        if (scroll && ((y < 0) || (y >= Rows.Bounds.Height)))
        {
            row = Math.Clamp(row + (y < 0 ? -1 : 1), 0, Rows.ItemCount - 1);
            Rows.ScrollIntoView(row);
        }

        (HexColumn column, int byteInRow) = block.ByteAt(e.GetPosition(block).X, within);
        long offset = Math.Min(HexRows.OffsetOf(row) + byteInRow, hex.Length - 1);

        return (offset, column);
    }

    // The realized row at a height in the list: the lowest one starting at or above it, so the space below a short
    // file's last row is that row's; above every row, the first.
    private (int Row, HexRowBlock Block)? RowAt(double y)
    {
        (int Row, HexRowBlock Block)? found = null;
        (int Row, HexRowBlock Block)? first = null;
        double foundTop = double.NegativeInfinity;
        double firstTop = double.PositiveInfinity;

        foreach (Control container in Rows.GetRealizedContainers())
        {
            if (!container.IsVisible
                || (container.TranslatePoint(default, Rows) is not { } origin)
                || (container.GetVisualDescendants().OfType<HexRowBlock>().FirstOrDefault() is not { } block))
            {
                continue;
            }

            int index = Rows.IndexFromContainer(container);

            if ((origin.Y <= y) && (origin.Y > foundTop))
            {
                (found, foundTop) = ((index, block), origin.Y);
            }

            if (origin.Y < firstTop)
            {
                (first, firstTop) = ((index, block), origin.Y);
            }
        }

        return found ?? first;
    }

    // The motions that mean something in a list of rows. Anything else goes to the engine.
    private bool Navigate(char character)
    {
        bool wasPendingG = _pendingG;
        _pendingG = false;

        switch (character)
        {
            case 'j':
                GoTo(Rows.SelectedIndex + 1);
                return true;

            case 'k':
                GoTo(Rows.SelectedIndex - 1);
                return true;

            case 'G':
                GoTo(Rows.ItemCount - 1);
                return true;

            case 'g' when wasPendingG:
                GoTo(0);
                return true;

            case 'g':
                _pendingG = true;
                return true;

            default:
                return false;
        }
    }
}

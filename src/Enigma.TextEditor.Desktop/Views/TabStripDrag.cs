using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// A tab dragged off a pane's strip: the press that becomes a drag, the pane and the side it would land on, and the
/// move or the split it makes when let go.
/// </summary>
/// <remarks>
/// <para>
/// Run from pointer events with a capture rather than as a platform drag-and-drop session, for the reason the pane
/// view gives (<see cref="EditorPaneView"/>): the gesture never leaves the window, and a platform drag would wear the
/// refusal pointer under a Wayland compositor. The decisions themselves — when a press is a drag, what the pointer
/// wears, which side a point is on — are <see cref="TabDragGesture"/>'s.
/// </para>
/// <para>
/// One per pane view, attached to its strip for the view's life.
/// </para>
/// </remarks>
internal sealed class TabStripDrag
{
    private readonly EditorPaneView _view;
    private readonly ListBox _tabs;

    // A left press on a tab, kept until the pointer has gone far enough with it to make a drag, or is let go.
    private EditorTabViewModel? _pressedTab;
    private Point _tabPressPoint;

    // The tab being carried, once the press has become a drag; the pointer the strip holds for it; the window
    // listening for the Escape that cancels it; and the pane showing that the tab would land there.
    private EditorTabViewModel? _draggedTab;
    private IPointer? _dragPointer;
    private TopLevel? _dragRoot;
    private EditorPaneView? _dropTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabStripDrag"/> class, listening on the strip from now on.
    /// </summary>
    /// <param name="view">The pane whose strip the tabs are dragged from.</param>
    /// <param name="tabs">The strip's list of tabs.</param>
    public TabStripDrag(EditorPaneView view, ListBox tabs)
    {
        _view = view;
        _tabs = tabs;

        // Handled events too: the strip's items take the press, to show the tab, and the press is where the drag starts
        // from.
        tabs.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        tabs.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        tabs.AddHandler(InputElement.PointerReleasedEvent, OnLetGo, RoutingStrategies.Bubble, handledEventsToo: true);

        // Direct, the only route a lost capture travels: a handler listening on any other is never called.
        tabs.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct, handledEventsToo: true);
    }

    /// <summary>
    /// Gets a value indicating whether a tab of the strip is being dragged.
    /// </summary>
    public bool IsDragging => _draggedTab is not null;

    // Not from the close button, whose press is a click on it, and only with the left button, the one that drags.
    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        ForgetPress();

        if ((_draggedTab is not null)
            || !e.GetCurrentPoint(_tabs).Properties.IsLeftButtonPressed
            || ((e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null)
            || ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(includeSelf: true)?.DataContext is not EditorTabViewModel tab))
        {
            return;
        }

        _pressedTab = tab;
        _tabPressPoint = e.GetPosition(_tabs);
    }

    // Past the platform's drag distance — the one beyond which a press is no longer a click — the press becomes a
    // drag of its tab. Forgotten first: the drag must start once.
    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedTab is not null)
        {
            Steer(e);
            return;
        }

        if (_pressedTab is not { } tab)
        {
            return;
        }

        if (!e.GetCurrentPoint(_tabs).Properties.IsLeftButtonPressed)
        {
            ForgetPress();
            return;
        }

        Size distance = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(4, 4);

        if (!TabDragGesture.IsDrag(e.GetPosition(_tabs) - _tabPressPoint, distance))
        {
            return;
        }

        ForgetPress();
        Start(tab, e);
    }

    // The release ends the drag, and moves the tab, or splits for it, when it is let go where a pane takes it. Done once
    // the release has been handled: the strip the tab leaves changes under it, and its whole pane goes when the tab was
    // its last.
    private void OnLetGo(object? sender, PointerReleasedEventArgs e)
    {
        ForgetPress();

        if (_draggedTab is not { } tab)
        {
            return;
        }

        (EditorPaneView? pane, FocusDirection? side) = DropUnder(e);

        Stop();
        e.Handled = true;

        if ((pane?.DataContext is not EditorPaneViewModel target) || !Takes(target, tab, side))
        {
            return;
        }

        if (side is { } edge)
        {
            Dispatcher.UIThread.Post(() => target.SplitWithTab(tab, edge));
        }
        else
        {
            Dispatcher.UIThread.Post(() => target.TakeTab(tab));
        }
    }

    // Whoever took the pointer owns it now: the drag ends where it is, dropping nothing, and the capture is left to them.
    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ForgetPress();

        if (_draggedTab is null)
        {
            return;
        }

        _dragPointer = null;
        Stop();
    }

    private void ForgetPress() => _pressedTab = null;

    private void Start(EditorTabViewModel tab, PointerEventArgs e)
    {
        _draggedTab = tab;

        // The capture makes the rest of the gesture the strip's: every move, and the release that ends it, are
        // reported here wherever the pointer goes — and the pointer wears the strip's cursor all the while.
        _dragPointer = e.Pointer;
        _dragPointer.Capture(_tabs);

        // A drag that brings no platform session has to bring its own way out. On the window, and tunnelling: the
        // keyboard may be in any pane's editor, and Vim must not take this Escape as its own.
        _dragRoot = TopLevel.GetTopLevel(_view);
        _dragRoot?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        Steer(e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Key != Key.Escape) || (_draggedTab is null))
        {
            return;
        }

        Stop();
        e.Handled = true;
    }

    // The cursor says a drag is under way; the hint says where it would land: a whole pane it would move to, or the half
    // of a pane, its own included, that would split off for it.
    private void Steer(PointerEventArgs e)
    {
        e.Handled = true;

        (EditorPaneView? pane, FocusDirection? side) = DropUnder(e);

        if (TabDragGesture.CursorFor(pane is not null) is not null)
        {
            _tabs.Cursor = EditorPaneView.CarryingCursor;
        }
        else
        {
            _tabs.ClearValue(InputElement.CursorProperty);
        }

        if ((pane?.DataContext is EditorPaneViewModel target) && (_draggedTab is { } tab) && Takes(target, tab, side))
        {
            MarkDropTarget(pane, side);
        }
        else
        {
            MarkDropTarget(null, null);
        }
    }

    // Marked again only when the pane or the side changes: the hint is put in place, not redrawn, on every move.
    private void MarkDropTarget(EditorPaneView? pane, FocusDirection? side)
    {
        if (ReferenceEquals(_dropTarget, pane) && (_dropTarget?.DropSide == side))
        {
            return;
        }

        _dropTarget?.HideDropHint();
        _dropTarget = pane;
        _dropTarget?.ShowDropHint(side);
    }

    // In the middle or on the strip a pane takes a tab from another pane; at an edge it splits for any tab whose pane
    // keeps another.
    private static bool Takes(EditorPaneViewModel target, EditorTabViewModel tab, FocusDirection? side)
        => side is null ? target.CanTakeTab(tab) : target.CanSplitWithTab(tab);

    // Cleared before the capture is let go: letting it go reports a lost capture, which must find no drag left to end.
    private void Stop()
    {
        _draggedTab = null;

        _tabs.ClearValue(InputElement.CursorProperty);
        MarkDropTarget(null, null);

        _dragRoot?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        _dragRoot = null;

        IPointer? pointer = _dragPointer;
        _dragPointer = null;
        pointer?.Capture(null);
    }

    // The pane under the pointer, in this window, and the side of it the pointer is near. Hit-tested: while the strip
    // holds the pointer, every event comes from the strip, wherever the pointer is.
    private (EditorPaneView? Pane, FocusDirection? Side) DropUnder(PointerEventArgs e)
    {
        EditorPaneView? pane = (TopLevel.GetTopLevel(_view) is { } top) && (top.InputHitTest(e.GetPosition(top)) is Visual hit)
            ? hit.FindAncestorOfType<EditorPaneView>(includeSelf: true)
            : null;

        return (pane, pane?.SideAt(e.GetPosition(pane)));
    }
}

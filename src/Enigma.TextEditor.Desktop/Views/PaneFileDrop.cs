using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Files dragged onto a pane from outside the application: the hint while they are over it, and opening them — in the
/// pane, or in a new half of it — when they are let go.
/// </summary>
/// <remarks>
/// <para>
/// The zones are a carried tab's (<see cref="EditorPaneView.FileSideAt"/>): over the strip or the middle the files open
/// in the pane, near an edge the pane splits on that side and they open in the new half — unless the pane has no tab,
/// which takes them whole. A platform drag is reported to the pane under the pointer, so each pane shows its own hint.
/// </para>
/// <para>
/// One per pane view, listening on it for the view's life.
/// </para>
/// </remarks>
internal sealed class PaneFileDrop
{
    private readonly EditorPaneView _view;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaneFileDrop"/> class, listening on the pane from now on.
    /// </summary>
    /// <param name="view">The pane files are dropped on.</param>
    public PaneFileDrop(EditorPaneView view)
    {
        _view = view;

        // A file dropped anywhere on the pane opens in it. Handled events too: the text area handles drags of its own,
        // which move text, and must not hide a drag of files from the pane.
        DragDrop.SetAllowDrop(view, true);
        view.AddHandler(DragDrop.DragEnterEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        view.AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        view.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, RoutingStrategies.Bubble, handledEventsToo: true);
        view.AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // Entering and moving alike: the hint follows the pointer from the strip to the middle to an edge.
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        IReadOnlyList<string> paths = DroppedFiles.ReadPaths(e.DataTransfer);

        if (paths.Count == 0)
        {
            // A drag of text is the text area's, which has already said what it would do with it; anywhere else on the
            // pane it would do nothing, and the cursor says so.
            if (!e.Handled)
            {
                e.DragEffects = DragDropEffects.None;
            }

            _view.HideDropHint();

            return;
        }

        e.DragEffects = DroppedFiles.EffectFor(paths);
        e.Handled = true;

        // Put in place again only when the side changes, as for a tab.
        FocusDirection? side = _view.FileSideAt(e.GetPosition(_view));

        if (!_view.IsDropTarget || (_view.DropSide != side))
        {
            _view.ShowDropHint(side);
        }
    }

    // Raised on the part of the pane the drag was over, whenever it moves to another part, another pane or out of the
    // window, and when the drag is cancelled. Moving within the pane, the enter on the new part follows at once and
    // shows the hint again.
    private void OnDragLeave(object? sender, DragEventArgs e) => _view.HideDropHint();

    private void OnDrop(object? sender, DragEventArgs e)
    {
        // No leave follows a drop.
        _view.HideDropHint();

        // Read now: the dragged data is only there while the drop event lasts.
        IReadOnlyList<string> paths = DroppedFiles.ReadPaths(e.DataTransfer);
        DragDropEffects effect = DroppedFiles.EffectFor(paths);

        if ((effect == DragDropEffects.None) || (_view.DataContext is not EditorPaneViewModel pane))
        {
            return;
        }

        e.DragEffects = effect;
        e.Handled = true;

        FocusDirection? side = _view.FileSideAt(e.GetPosition(_view));

        // Opened once the drop has returned: the platform's drag loop has to be over before a dialog — the binary-file
        // question, a file that cannot be read — can appear. Not awaited, as the ex commands are not: failures are
        // reported by the services the opening goes through.
        if (side is { } edge)
        {
            Dispatcher.UIThread.Post(() => pane.SplitWithFilesAsync(paths, edge).Forget());
        }
        else
        {
            Dispatcher.UIThread.Post(() => pane.OpenDroppedAsync(paths).Forget());
        }
    }
}

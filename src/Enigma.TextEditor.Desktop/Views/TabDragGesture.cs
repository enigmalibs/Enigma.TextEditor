using System;
using Avalonia;
using Avalonia.Input;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The decisions of a tab drag: when a press on a tab has become one, and what the pointer looks like while it lasts.
/// </summary>
/// <remarks>
/// Decisions about a pointer and nothing else, kept out of the pane's event handlers so a test can ask them directly.
/// </remarks>
internal static class TabDragGesture
{
    /// <summary>
    /// Says whether a press has moved far enough to be a drag rather than a click.
    /// </summary>
    /// <param name="moved">How far the pointer has gone from where it was pressed.</param>
    /// <param name="distance">The platform's tap size: within it, a press is still a click.</param>
    /// <returns><see langword="true"/> once the pointer is past the distance on either axis.</returns>
    public static bool IsDrag(Vector moved, Size distance)
        => (Math.Abs(moved.X) > distance.Width) || (Math.Abs(moved.Y) > distance.Height);

    /// <summary>
    /// What the pointer should look like while a tab is carried.
    /// </summary>
    /// <param name="isOverAPane">Whether the pointer is over a pane of the window.</param>
    /// <returns>The cursor to wear, or <see langword="null"/> to leave the strip its own.</returns>
    /// <remarks>
    /// <para>
    /// The pointer answers "is a drag under way here", not "would this pane take the tab": over the pane the tab came
    /// from, the gesture is still under way, and a drop there simply does nothing. Off the panes (the status bar, the
    /// icon rail) the gesture has nothing to say, and the strip keeps its ordinary cursor.
    /// </para>
    /// <para>
    /// <see cref="StandardCursorType.DragMove"/> and never <see cref="StandardCursorType.No"/>: the refusal pointer is
    /// what this gesture exists to be rid of.
    /// </para>
    /// </remarks>
    public static StandardCursorType? CursorFor(bool isOverAPane) => isOverAPane ? StandardCursorType.DragMove : null;
}

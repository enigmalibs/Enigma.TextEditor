using System;
using Avalonia;
using Avalonia.Input;
using Enigma.TextEditor.Core.Layout;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The decisions of a tab drag: when a press on a tab has become one, what the pointer looks like while it lasts, and
/// whether a pane it is let go over takes it whole or splits for it.
/// </summary>
/// <remarks>
/// Decisions about a pointer and nothing else, kept out of the pane's event handlers so a test can ask them directly.
/// </remarks>
internal static class TabDragGesture
{
    /// <summary>
    /// How far in from each edge of a pane's content a tab let go splits the pane rather than moving into it, as a
    /// share of the content's width (left and right) or height (top and bottom).
    /// </summary>
    public const double EdgeShare = 0.25;

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

    /// <summary>
    /// Which side of a pane a tab let go at a point of its content would split it on.
    /// </summary>
    /// <param name="point">The point, from the top left of the content: the pane below its tab strip.</param>
    /// <param name="content">The size of the content.</param>
    /// <returns>
    /// The side whose outer <see cref="EdgeShare"/> holds the point, or <see langword="null"/> for the middle, where the
    /// tab moves into the pane instead.
    /// </returns>
    /// <remarks>
    /// In a corner, where two sides would do, the point goes to the one it is nearer, measured as a share of the
    /// content's width or height: the side the eye reads it as closer to. On the boundary itself it is still the middle.
    /// Content with no width or no height has no sides.
    /// </remarks>
    public static FocusDirection? SideFor(Point point, Size content)
    {
        if ((content.Width <= 0) || (content.Height <= 0))
        {
            return null;
        }

        double fromLeft = point.X / content.Width;
        double fromTop = point.Y / content.Height;

        // How far the point is from each edge, as a share of the content across that way.
        (FocusDirection Side, double Share)[] edges =
        [
            (FocusDirection.Left, fromLeft),
            (FocusDirection.Right, 1 - fromLeft),
            (FocusDirection.Up, fromTop),
            (FocusDirection.Down, 1 - fromTop),
        ];

        FocusDirection? side = null;
        double nearest = EdgeShare;

        foreach ((FocusDirection candidate, double share) in edges)
        {
            if (share < nearest)
            {
                side = candidate;
                nearest = share;
            }
        }

        return side;
    }
}

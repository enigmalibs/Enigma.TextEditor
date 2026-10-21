namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// A window command — <c>Ctrl+W</c> and the key after it — that the engine asks the application to
/// carry out.
/// </summary>
/// <remarks>
/// Like the file requests, a window command leaves the engine: the panes are the application's, and
/// the engine only knows its own buffer.
/// </remarks>
public enum VimWindowCommand
{
    /// <summary>
    /// <c>Ctrl+W h</c> or Left — the pane to the left.
    /// </summary>
    FocusLeft,

    /// <summary>
    /// <c>Ctrl+W j</c> or Down — the pane below.
    /// </summary>
    FocusDown,

    /// <summary>
    /// <c>Ctrl+W k</c> or Up — the pane above.
    /// </summary>
    FocusUp,

    /// <summary>
    /// <c>Ctrl+W l</c> or Right — the pane to the right.
    /// </summary>
    FocusRight,

    /// <summary>
    /// <c>Ctrl+W w</c> and <c>Ctrl+W Ctrl+W</c> — the next pane, wrapping round.
    /// </summary>
    FocusNext,

    /// <summary>
    /// <c>Ctrl+W W</c> — the previous pane, wrapping round.
    /// </summary>
    FocusPrevious,

    /// <summary>
    /// <c>Ctrl+W s</c> and <c>Ctrl+W S</c> — split the pane into two, one above the other.
    /// </summary>
    SplitHorizontal,

    /// <summary>
    /// <c>Ctrl+W v</c> — split the pane into two, side by side.
    /// </summary>
    SplitVertical,

    /// <summary>
    /// <c>Ctrl+W c</c> — close the pane.
    /// </summary>
    Close,

    /// <summary>
    /// <c>Ctrl+W q</c> — <c>:q</c>: close the tab, and the pane with its last tab.
    /// </summary>
    Quit,

    /// <summary>
    /// <c>Ctrl+W o</c> — close every other pane.
    /// </summary>
    Only,

    /// <summary>
    /// <c>Ctrl+W =</c> — give every pane an equal share.
    /// </summary>
    Equalize,

    /// <summary>
    /// <c>Ctrl+W +</c> — make the pane taller.
    /// </summary>
    Taller,

    /// <summary>
    /// <c>Ctrl+W -</c> — make the pane shorter.
    /// </summary>
    Shorter,

    /// <summary>
    /// <c>Ctrl+W &gt;</c> — make the pane wider.
    /// </summary>
    Wider,

    /// <summary>
    /// <c>Ctrl+W &lt;</c> — make the pane narrower.
    /// </summary>
    Narrower,

    /// <summary>
    /// <c>&lt;Space&gt;rh</c> — move the bar between the side-by-side panes to the left, whichever of them has the
    /// keyboard: the right-hand pane grows.
    /// </summary>
    MoveSplitterLeft,

    /// <summary>
    /// <c>&lt;Space&gt;rk</c> — move the bar between the stacked panes down, whichever of them has the keyboard: the
    /// top pane grows.
    /// </summary>
    MoveSplitterDown,

    /// <summary>
    /// <c>&lt;Space&gt;rj</c> — move the bar between the stacked panes up: the bottom pane grows.
    /// </summary>
    MoveSplitterUp,

    /// <summary>
    /// <c>&lt;Space&gt;rl</c> — move the bar between the side-by-side panes to the right: the left-hand pane grows.
    /// </summary>
    MoveSplitterRight,
}

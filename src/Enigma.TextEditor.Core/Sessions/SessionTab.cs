namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// One tab of a saved session: the buffer it showed, and where it was left in it.
/// </summary>
/// <param name="Document">The index of the buffer in <see cref="SessionState.Documents"/>.</param>
/// <param name="CaretOffset">The caret, as an offset into the text; clamped to the text when restored.</param>
/// <param name="VerticalScrollOffset">How far the view was scrolled down.</param>
/// <param name="HorizontalScrollOffset">How far the view was scrolled right.</param>
/// <param name="HexRow">The row selected in the hex view, counted from zero; -1 for none.</param>
public sealed record SessionTab(
    int Document,
    int CaretOffset = 0,
    double VerticalScrollOffset = 0,
    double HorizontalScrollOffset = 0,
    int HexRow = -1);

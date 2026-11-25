using System;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// The visual selection: its anchor — the end that does not move, the caret being the other — and the selection made
/// last, for <c>gv</c> and the <c>'&lt;</c> and <c>'&gt;</c> marks.
/// </summary>
/// <remarks>
/// <para>
/// The engine owns the mode and the caret, and the edits made over a selection; this class owns what the selection is
/// made of, works out what it covers, hands it to the editor, and remembers it on the way out.
/// </para>
/// <para>
/// A selection is remembered as lines and columns rather than offsets — vim's own marks are — so that a change the
/// operator made under it, a <c>&gt;</c> that indented the lines, does not move it off them.
/// </para>
/// </remarks>
internal sealed class VimVisualSelection
{
    /// <summary>
    /// The anchor when nothing is selected.
    /// </summary>
    public const int NoAnchor = -1;

    private readonly IVimBuffer _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimVisualSelection"/> class.
    /// </summary>
    /// <param name="buffer">The text the selection is in.</param>
    public VimVisualSelection(IVimBuffer buffer) => _buffer = buffer;

    /// <summary>
    /// Gets or sets the end of the selection that does not move, or <see cref="NoAnchor"/> when nothing is selected. The
    /// moving end is the caret itself, which is why a visual mode needs one value here and not two.
    /// </summary>
    public int Anchor { get; set; } = NoAnchor;

    /// <summary>
    /// Gets where the last selection was, or <see langword="null"/> when there has not been one in this document.
    /// </summary>
    public VimRememberedSelection? Last { get; private set; }

    /// <summary>
    /// What the anchor and the caret cover between them.
    /// </summary>
    /// <param name="linewise">Whether whole lines are selected (<c>V</c>).</param>
    /// <returns>The range an operator works on.</returns>
    public VimTextRange Range(bool linewise) => VimRanges.Selection(_buffer, Anchor, _buffer.CaretOffset, linewise);

    /// <summary>
    /// Hands the editor the selection the anchor and the caret describe between them.
    /// </summary>
    /// <param name="linewise">Whether whole lines are selected.</param>
    public void Show(bool linewise)
    {
        VimTextRange range = Range(linewise);

        if (!range.IsLinewise)
        {
            _buffer.SetSelection(range.Start, range.End);

            return;
        }

        // The line break goes into the highlight, though not into the range an operator works on, so that a linewise
        // selection reads as whole lines rather than as text that happens to reach both edges of them. The last line of
        // the buffer has none to add.
        VimLine last = _buffer.GetLineByOffset(range.End);

        _buffer.SetSelection(range.Start, last.EndOffset + last.DelimiterLength);
    }

    /// <summary>
    /// Remembers the selection on screen as the last one, for <c>gv</c>.
    /// </summary>
    /// <param name="mode">The visual mode it was made in.</param>
    public void Remember(VimMode mode) => Last = Current(mode);

    /// <summary>
    /// Drops the selection: no anchor, nothing highlighted.
    /// </summary>
    public void Clear()
    {
        Anchor = NoAnchor;
        _buffer.ClearSelection();
    }

    /// <summary>
    /// Forgets the last selection: a selection is a place in a document, and the document may be another one now.
    /// </summary>
    public void Forget() => Last = null;

    /// <summary>
    /// <c>o</c> in a visual mode: the anchor goes to where the caret is, so that the motions move the other end.
    /// </summary>
    /// <returns>Where the caret goes: where the anchor was.</returns>
    public int Swap()
    {
        int anchor = Anchor;

        Anchor = _buffer.CaretOffset;

        return anchor;
    }

    /// <summary>
    /// <c>gv</c>: the anchor back where the last selection had it; in a visual mode, the selection on screen remembered in
    /// its place, so that a second <c>gv</c> comes back.
    /// </summary>
    /// <param name="mode">The mode the engine is in.</param>
    /// <returns>
    /// Where the caret goes and the mode to select in; <see langword="null"/> with nothing to go back to.
    /// </returns>
    /// <remarks>
    /// An end whose line or column has since gone is pulled onto the nearest that is still there, which is as much as
    /// "the same selection" can mean over text that has changed.
    /// </remarks>
    public (int Caret, VimMode Mode)? Reselect(VimMode mode)
    {
        if (Last is not { } previous)
        {
            return null;
        }

        if (mode is VimMode.Visual or VimMode.VisualLine)
        {
            Remember(mode);
        }

        Anchor = OffsetOf(previous.AnchorLine, previous.AnchorColumn);

        return (OffsetOf(previous.CaretLine, previous.CaretColumn), previous.Mode);
    }

    /// <summary>
    /// How far the selection reaches, as <c>.</c> will need it: how many lines, and on one line how many characters,
    /// over several the column the last line reaches to.
    /// </summary>
    /// <param name="mode">The visual mode it is in.</param>
    /// <returns>The extent.</returns>
    public VimVisualExtent Extent(VimMode mode)
    {
        VimTextRange range = Range(mode == VimMode.VisualLine);
        VimLine first = _buffer.GetLineByOffset(range.Start);
        VimLine last = _buffer.GetLineByOffset(range.IsLinewise ? range.End : Math.Max(range.Start, range.End - 1));
        int lines = (last.Number - first.Number) + 1;

        return new VimVisualExtent(mode, lines, lines == 1 ? range.Length : range.End - last.Offset);
    }

    /// <summary>
    /// Selects again, from the caret, as much as a remembered extent covered — vim's rule for <c>.</c> after a visual
    /// command. The anchor is put at the caret.
    /// </summary>
    /// <param name="extent">The extent.</param>
    /// <returns>Where the caret goes, the far end of the selection.</returns>
    public int Repeat(VimVisualExtent extent)
    {
        int anchor = _buffer.CaretOffset;
        VimLine line = _buffer.GetLineByOffset(anchor);
        int caret;

        if (extent.Lines > 1)
        {
            VimLine last = _buffer.GetLine(Math.Min((line.Number + extent.Lines) - 1, _buffer.LineCount));

            caret = extent.Mode == VimMode.VisualLine
                ? last.Offset
                : Math.Min(last.Offset + Math.Max(extent.Column - 1, 0), last.LastCaretOffset);
        }
        else
        {
            caret = extent.Mode == VimMode.VisualLine
                ? anchor
                : Math.Min(anchor + Math.Max(extent.Column - 1, 0), line.LastCaretOffset);
        }

        Anchor = anchor;

        return caret;
    }

    private VimRememberedSelection Current(VimMode mode)
    {
        VimLine anchorLine = _buffer.GetLineByOffset(Anchor);
        VimLine caretLine = _buffer.GetLineByOffset(_buffer.CaretOffset);

        return new VimRememberedSelection(
            anchorLine.Number,
            Anchor - anchorLine.Offset,
            caretLine.Number,
            _buffer.CaretOffset - caretLine.Offset,
            mode);
    }

    // The offset of a line and column, pulled onto the nearest line and character the buffer still has.
    private int OffsetOf(int lineNumber, int column)
    {
        VimLine line = _buffer.GetLine(Math.Clamp(lineNumber, 1, _buffer.LineCount));

        return Math.Min(line.Offset + column, line.LastCaretOffset);
    }
}

/// <summary>
/// Where a selection was: both ends as a line and a column, and the mode it was made in.
/// </summary>
/// <param name="AnchorLine">The anchor's line, from one.</param>
/// <param name="AnchorColumn">The anchor's column, from zero.</param>
/// <param name="CaretLine">The caret's line, from one.</param>
/// <param name="CaretColumn">The caret's column, from zero.</param>
/// <param name="Mode">The visual mode.</param>
internal readonly record struct VimRememberedSelection(int AnchorLine, int AnchorColumn, int CaretLine, int CaretColumn, VimMode Mode);

/// <summary>
/// How far a selection reached, as a size rather than a place, so that it can be made again from wherever the caret
/// is: the mode, how many lines, and — characterwise — how many characters on one line or the column the last line
/// reaches to over several.
/// </summary>
/// <param name="Mode">The visual mode.</param>
/// <param name="Lines">How many lines.</param>
/// <param name="Column">Characters on one line, or the column the last line reaches to.</param>
internal readonly record struct VimVisualExtent(VimMode Mode, int Lines, int Column);

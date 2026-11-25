using System;
using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What each key spells in normal mode and over a selection: the motions, the operators, the actions, the text objects,
/// the control keys and the leader's mappings.
/// </summary>
/// <remarks>
/// <para>
/// Tables, and nothing else: <see cref="KeySequenceParser"/> keeps the grammar — which key is waiting for which, the
/// counts, the register — and looks each key up here once it knows what kind of key it is waiting for. Adding a key is
/// a line here; adding a kind of sequence is the parser's.
/// </para>
/// <para>
/// A count is handed to the commands built here; everything else they carry comes from the key.
/// </para>
/// </remarks>
internal static class VimKeyBindings
{
    /// <summary>
    /// The leader's mappings, by the keys typed after it. The <c>r</c> keys move the bar between two panes, the
    /// <c>m</c> keys move the tab showing to another pane; each carries its direction as the argument.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, VimCommand> LeaderMappings = new Dictionary<string, VimCommand>(StringComparer.Ordinal)
    {
        ["rh"] = new(VimAction.MoveSplitter, Argument: 'h'),
        ["rj"] = new(VimAction.MoveSplitter, Argument: 'j'),
        ["rk"] = new(VimAction.MoveSplitter, Argument: 'k'),
        ["rl"] = new(VimAction.MoveSplitter, Argument: 'l'),
        ["mh"] = new(VimAction.MoveTab, Argument: 'h'),
        ["mj"] = new(VimAction.MoveTab, Argument: 'j'),
        ["mk"] = new(VimAction.MoveTab, Argument: 'k'),
        ["ml"] = new(VimAction.MoveTab, Argument: 'l'),
    };

    /// <summary>
    /// Which visual mode a key starts — <c>v</c> or <c>V</c>.
    /// </summary>
    public static VimAction? VisualFor(char character)
        => character switch
        {
            'v' => VimAction.VisualCharacterwise,
            'V' => VimAction.VisualLinewise,
            _ => null,
        };

    /// <summary>
    /// Which scroll command a key held with Control names, for the five the editor leaves to vim.
    /// </summary>
    /// <remarks>
    /// The letter is read whichever case it arrived in, because Shift held with Control changes
    /// nothing about what vim does; Alt held with it is a different key, and the window's.
    /// </remarks>
    public static VimAction? ControlActionFor(VimKey key)
    {
        if (!key.IsCharacter
            || ((key.Modifiers & VimModifiers.Control) == VimModifiers.None)
            || ((key.Modifiers & VimModifiers.Alt) != VimModifiers.None))
        {
            return null;
        }

        return char.ToLowerInvariant(key.Character) switch
        {
            'd' => VimAction.ScrollHalfPageDown,
            'u' => VimAction.ScrollHalfPageUp,
            'f' => VimAction.ScrollPageDown,
            'b' => VimAction.ScrollPageUp,
            'e' => VimAction.ScrollLineDown,
            _ => null,
        };
    }

    /// <summary>
    /// Which page scroll a page key is.
    /// </summary>
    public static VimAction? PageActionFor(VimSpecialKey key)
        => key switch
        {
            VimSpecialKey.PageDown => VimAction.ScrollPageDown,
            VimSpecialKey.PageUp => VimAction.ScrollPageUp,
            _ => null,
        };

    /// <summary>
    /// Which of the six <c>z</c> commands a key names, after the <c>z</c> that opened one.
    /// </summary>
    public static VimAction? ScrollCaretActionFor(VimKey key)
    {
        if (!key.IsCharacter)
        {
            return key.Special == VimSpecialKey.Enter ? VimAction.ScrollCaretTopToFirstNonBlank : null;
        }

        return key.Character switch
        {
            't' => VimAction.ScrollCaretTop,
            'z' => VimAction.ScrollCaretCenter,
            'b' => VimAction.ScrollCaretBottom,
            '.' => VimAction.ScrollCaretCenterToFirstNonBlank,
            '-' => VimAction.ScrollCaretBottomToFirstNonBlank,
            _ => null,
        };
    }

    /// <summary>
    /// Which operator a key names — <c>d</c> <c>y</c> <c>c</c> <c>&gt;</c> <c>&lt;</c>.
    /// </summary>
    public static VimOperator? OperatorFor(char character)
        => character switch
        {
            'd' => VimOperator.Delete,
            'y' => VimOperator.Yank,
            'c' => VimOperator.Change,
            '>' => VimOperator.ShiftRight,
            '<' => VimOperator.ShiftLeft,
            _ => null,
        };

    /// <summary>
    /// Which case operator a key names after a <c>g</c>.
    /// </summary>
    public static VimOperator? CaseOperatorFor(char character)
        => character switch
        {
            'u' => VimOperator.Lowercase,
            'U' => VimOperator.Uppercase,
            '~' => VimOperator.ToggleCase,
            _ => null,
        };

    /// <summary>
    /// The bare letter that doubles a case operator — the <c>u</c> of <c>guu</c> — or <c>'\0'</c>
    /// for an operator that is doubled only by typing it again.
    /// </summary>
    public static char DoublingKeyFor(VimOperator @operator)
        => @operator switch
        {
            VimOperator.Lowercase => 'u',
            VimOperator.Uppercase => 'U',
            VimOperator.ToggleCase => '~',
            _ => '\0',
        };

    /// <summary>
    /// Which find a key opens — <c>f</c> <c>F</c> <c>t</c> <c>T</c> — waiting for the character to look for.
    /// </summary>
    public static VimMotion? FindOpenerFor(char character)
        => character switch
        {
            'f' => VimMotion.FindForward,
            'F' => VimMotion.FindBackward,
            't' => VimMotion.TillForward,
            'T' => VimMotion.TillBackward,
            _ => null,
        };

    /// <summary>
    /// Whether a key opens a text object — <c>i</c> for inner, <c>a</c> for around.
    /// </summary>
    public static bool IsObjectOpener(char character) => (character == 'i') || (character == 'a');

    /// <summary>
    /// Which text object a key names, after the <c>i</c> or the <c>a</c> that opened one.
    /// </summary>
    /// <param name="opener">The <c>i</c> or the <c>a</c>, which decides inner from around.</param>
    /// <param name="character">The key that followed it.</param>
    /// <returns>The object, or <see langword="null"/> when the key names none.</returns>
    /// <remarks>
    /// Vim's own spellings, both of them where it has two: either delimiter of a pair names that
    /// pair, so <c>i(</c> and <c>i)</c> are the same object, and <c>b</c> and <c>B</c> are the
    /// mnemonics for the round and the curly one.
    /// </remarks>
    public static VimTextObject? TextObjectFor(char opener, char character)
    {
        bool inner = opener == 'i';

        return character switch
        {
            'w' => inner ? VimTextObject.InnerWord : VimTextObject.AroundWord,
            'W' => inner ? VimTextObject.InnerBigWord : VimTextObject.AroundBigWord,

            '(' or ')' or 'b' => inner ? VimTextObject.InnerParen : VimTextObject.AroundParen,
            '[' or ']' => inner ? VimTextObject.InnerBracket : VimTextObject.AroundBracket,
            '{' or '}' or 'B' => inner ? VimTextObject.InnerBrace : VimTextObject.AroundBrace,
            '<' or '>' => inner ? VimTextObject.InnerAngle : VimTextObject.AroundAngle,

            '"' => inner ? VimTextObject.InnerDoubleQuote : VimTextObject.AroundDoubleQuote,
            '\'' => inner ? VimTextObject.InnerSingleQuote : VimTextObject.AroundSingleQuote,
            '`' => inner ? VimTextObject.InnerBacktick : VimTextObject.AroundBacktick,

            _ => null,
        };
    }

    /// <summary>
    /// Which motion a key is, in normal mode, behind an operator and over a selection alike.
    /// </summary>
    public static VimMotion? MotionFor(VimKey key)
    {
        if (!key.IsCharacter)
        {
            return key.Special switch
            {
                VimSpecialKey.Left => VimMotion.Left,
                VimSpecialKey.Right => VimMotion.Right,
                VimSpecialKey.Up => VimMotion.Up,
                VimSpecialKey.Down => VimMotion.Down,
                VimSpecialKey.Home => VimMotion.LineStart,
                VimSpecialKey.End => VimMotion.LineEnd,

                // Outside the command line, which has already taken its own keys before the parser
                // sees anything: Enter is '+', and Backspace is 'h' allowed across a line break.
                VimSpecialKey.Enter => VimMotion.LineDownFirstNonBlank,
                VimSpecialKey.Backspace => VimMotion.LeftWrapping,
                _ => null,
            };
        }

        return key.Character switch
        {
            'h' => VimMotion.Left,
            'l' => VimMotion.Right,
            'k' => VimMotion.Up,
            'j' => VimMotion.Down,

            // Reached only behind an operator: everywhere else Space is the leader.
            ' ' => VimMotion.RightWrapping,

            // Reached only when no count is being typed: a '0' that could continue one has already
            // been taken as a digit, which is the whole of vim's rule about the key.
            '0' => VimMotion.LineStart,
            '^' => VimMotion.FirstNonBlank,
            '$' => VimMotion.LineEnd,
            '|' => VimMotion.Column,
            'w' => VimMotion.WordForward,
            'b' => VimMotion.WordBackward,
            'e' => VimMotion.WordEnd,
            'W' => VimMotion.WordForwardBig,
            'B' => VimMotion.WordBackwardBig,
            'E' => VimMotion.WordEndBig,

            // The two that are complete the instant they are typed: what they look for is the
            // engine's memory of the last find, not a character still to come.
            ';' => VimMotion.RepeatFind,
            ',' => VimMotion.RepeatFindReverse,

            // And the four searches that are: 'n' and 'N' read the engine's memory of the last
            // search, '*' and '#' read the word under the caret. A search typed after '/' or '?' is
            // not here — it opens the command line, and the engine makes the motion when Enter closes it.
            'n' => VimMotion.SearchNext,
            'N' => VimMotion.SearchPrevious,
            '*' => VimMotion.SearchWordForward,
            '#' => VimMotion.SearchWordBackward,

            '%' => VimMotion.MatchingBracket,
            '}' => VimMotion.ParagraphForward,
            '{' => VimMotion.ParagraphBackward,
            ')' => VimMotion.SentenceForward,
            '(' => VimMotion.SentenceBackward,

            '+' => VimMotion.LineDownFirstNonBlank,
            '-' => VimMotion.LineUpFirstNonBlank,
            '_' => VimMotion.CurrentLineFirstNonBlank,

            'G' => VimMotion.LastLine,

            // The three that name a line by where the screen is rather than by where the text is.
            'H' => VimMotion.ScreenTop,
            'M' => VimMotion.ScreenMiddle,
            'L' => VimMotion.ScreenBottom,
            _ => null,
        };
    }

    /// <summary>
    /// What a key does in normal mode, for the keys that are whole commands rather than motions or operators.
    /// </summary>
    /// <param name="character">The key.</param>
    /// <param name="count">The count typed before it.</param>
    /// <returns>The command, or <see langword="null"/> when the key is none.</returns>
    public static VimCommand? ActionFor(char character, int count)
        => character switch
        {
            'i' => new VimCommand(VimAction.InsertBeforeCaret, VimMotion.None, count),
            'I' => new VimCommand(VimAction.InsertAtLineStart, VimMotion.None, count),
            'a' => new VimCommand(VimAction.InsertAfterCaret, VimMotion.None, count),
            'A' => new VimCommand(VimAction.InsertAtLineEnd, VimMotion.None, count),
            'o' => new VimCommand(VimAction.OpenLineBelow, VimMotion.None, count),
            'O' => new VimCommand(VimAction.OpenLineAbove, VimMotion.None, count),

            // The key that opens an ex line. The key itself is carried as the argument, because it is
            // also the line's first character and the only thing that tells an ex command from a
            // search later on — the two search openers are read above, with the motions.
            ':' => new VimCommand(VimAction.BeginCommandLine, VimMotion.None, count, VimOperator.None, ':'),

            'x' => new VimCommand(VimAction.DeleteCharacter, VimMotion.None, count),
            'p' => new VimCommand(VimAction.PutAfter, VimMotion.None, count),
            'P' => new VimCommand(VimAction.PutBefore, VimMotion.None, count),
            '~' => new VimCommand(VimAction.ToggleCase, VimMotion.None, count),
            'u' => new VimCommand(VimAction.Undo, VimMotion.None, count),
            'J' => new VimCommand(VimAction.Join, VimMotion.None, count),
            '.' => new VimCommand(VimAction.Repeat, VimMotion.None, count),

            // The capitals and the two substitutes vim spells as an operator over a motion, because
            // that is what they are: 'D' is 'd$', 'C' is 'c$', 'Y' is 'yy', 's' is 'cl', 'S' is 'cc'
            // and 'X' is 'dh'. Counted the same way, so '3D' reaches the end of the second line below
            // exactly as '3d$' does, and '3X' takes the three characters before the caret.
            'D' => VimCommand.Operate(VimOperator.Delete, VimMotion.LineEnd, count),
            'C' => VimCommand.Operate(VimOperator.Change, VimMotion.LineEnd, count),
            'Y' => VimCommand.Operate(VimOperator.Yank, VimMotion.WholeLine, count),
            's' => VimCommand.Operate(VimOperator.Change, VimMotion.Right, count),
            'S' => VimCommand.Operate(VimOperator.Change, VimMotion.WholeLine, count),
            'X' => VimCommand.Operate(VimOperator.Delete, VimMotion.Left, count),

            _ => null,
        };

    /// <summary>
    /// What a key does over a selection, for the keys that are commands there rather than motions,
    /// operators or the visual pair.
    /// </summary>
    /// <remarks>
    /// Vim's table, and most of it is spelling. <c>x</c> is <c>d</c> over the selection and <c>s</c>
    /// is <c>c</c>; the capitals <c>X</c> <c>D</c> <c>Y</c> <c>C</c> <c>S</c> are the same operators
    /// made linewise whatever the mode, which <see cref="VimMotion.WholeLine"/> in place of the
    /// selection's own <see cref="VimMotion.None"/> is how the command says. <c>~</c>, <c>u</c> and
    /// <c>U</c> are the case operators, so <c>u</c> is not undo while there is a selection — vim's
    /// rule, kept. <c>p</c> and <c>P</c> carry their key as the argument, the way <c>:</c> and
    /// <c>/</c> do, because the engine has to know which of the two it was.
    /// </remarks>
    public static VimCommand? VisualActionFor(char character, int count)
        => character switch
        {
            'x' => VimCommand.Operate(VimOperator.Delete, VimMotion.None, count),
            'X' or 'D' => VimCommand.Operate(VimOperator.Delete, VimMotion.WholeLine, count),
            'Y' => VimCommand.Operate(VimOperator.Yank, VimMotion.WholeLine, count),
            's' => VimCommand.Operate(VimOperator.Change, VimMotion.None, count),
            'C' or 'S' => VimCommand.Operate(VimOperator.Change, VimMotion.WholeLine, count),

            '~' => VimCommand.Operate(VimOperator.ToggleCase, VimMotion.None, count),
            'u' => VimCommand.Operate(VimOperator.Lowercase, VimMotion.None, count),
            'U' => VimCommand.Operate(VimOperator.Uppercase, VimMotion.None, count),

            'p' => new VimCommand(VimAction.ReplaceSelection, VimMotion.None, count, VimOperator.None, 'p'),
            'P' => new VimCommand(VimAction.ReplaceSelection, VimMotion.None, count, VimOperator.None, 'P'),

            'J' => new VimCommand(VimAction.Join, VimMotion.None, count),
            'o' => new VimCommand(VimAction.SwapVisualEnds, VimMotion.None, count),

            _ => null,
        };
}

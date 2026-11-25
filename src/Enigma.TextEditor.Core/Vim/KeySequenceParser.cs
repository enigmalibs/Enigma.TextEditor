using System;
using System.Linq;
using System.Text;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// Turns the keys typed in normal mode into commands.
/// </summary>
/// <remarks>
/// <para>
/// Separate from the engine because they answer different questions. This class knows only what the
/// keys spell; the engine knows what the buffer looks like and what to do about it. Keeping the two
/// apart is what makes a half-typed sequence — <c>3d</c> waiting for its motion — a state a test can
/// put the parser into in one line, without a document.
/// </para>
/// <para>
/// The grammar is vim's: <c>[count] [operator] [count] [motion]</c>, or a count and a command that
/// takes no motion. The two counts multiply, so <c>2d3w</c> is <c>d6w</c> — vim's own rule, and the
/// reason the second one is kept apart until the command is complete rather than folded in as it is
/// typed.
/// </para>
/// <para>
/// Ten kinds of key hold the sequence open: the three prefixes <c>g</c>, <c>Z</c> and <c>z</c>
/// waiting for their second key, the leader waiting for one of its mappings, an operator waiting for
/// its motion, <c>r</c> waiting for the character to write, the find family — <c>f</c>, <c>F</c>,
/// <c>t</c>, <c>T</c> — waiting for the character to look for, <c>i</c> or <c>a</c> waiting for the
/// text object to name, <c>"</c> waiting for the register to name, and <c>q</c> or <c>@</c> waiting
/// for the register to record into or to run. The find family, <c>r</c>, <c>"</c>, <c>q</c> and
/// <c>@</c> take the <em>next</em> key literally, whatever it spells: <c>rd</c> writes a <c>d</c> and
/// <c>fd</c> looks for one, rather than either starting a delete.
/// </para>
/// <para>
/// The leader is Space, as a <c>mapleader</c> of <c>" "</c> makes it in Vim. It opens a table of its
/// own — <c>r</c> and a direction moves the bar between two panes, <c>m</c> and a direction moves the
/// tab to another pane — read key by key: <c>&lt;Space&gt;rk</c> is Space, then <c>r</c>, then <c>k</c>. A key
/// that no mapping starts with ends the sequence as a dead one, the way a wrong second key ends a
/// <c>g</c>. Space is the leader in normal mode and over a selection, which is where Vim's leader
/// mappings live; behind an operator it is still the motion it always was, so <c>d&lt;Space&gt;</c>
/// deletes a character as before.
/// </para>
/// <para>
/// A key held with Control is read before any of that. Five of them are vim's scroll commands —
/// <c>Ctrl+D</c> <c>Ctrl+U</c> <c>Ctrl+F</c> <c>Ctrl+B</c> <c>Ctrl+E</c> — and each is a
/// whole command that a count may stand in front of and nothing else may: <c>r</c> and <c>Ctrl+D</c>
/// is not "write a d", and <c>d</c> and <c>Ctrl+F</c> is not a delete over a page, because a scroll is
/// not a motion. Both are dead sequences, named in full. The two page keys are the two page scrolls
/// under other names, and <c>z</c> opens the six commands that put a line where the user wants it on
/// the screen. None of the eleven means anything behind an operator, and all of them work over a
/// selection, where the caret's move is the selection's. Vim's sixth, <c>Ctrl+Y</c>, is not among them:
/// it is the editor's redo, as the Edit menu says, and the engine reads it before the grammar (BUG-C6A9).
/// </para>
/// <para>
/// A register name is a prefix to the whole command rather than a command of its own: after
/// <c>"a</c> the grammar carries on as if nothing had been typed, on either side of the count, and the
/// name rides on the command that finally comes out. It only makes sense on a command that reads or
/// fills a register — an operator, <c>x</c>, a put, <c>.</c> — so <c>"aj</c> is a dead sequence and
/// is named as one, and a bare <c>"a</c> is cancelled by <c>Esc</c> like any other half-typed thing.
/// </para>
/// <para>
/// <c>g</c> is a table rather than a key: <c>gg</c> is a motion, <c>gJ</c> and <c>gv</c> are commands,
/// so are <c>gh</c> <c>gj</c> <c>gk</c> <c>gl</c>, which move to another pane, <c>g=</c> <c>g+</c>
/// <c>g-</c> <c>g&gt;</c> <c>g&lt;</c>, which size the panes, and <c>gt</c> <c>gT</c>, which move
/// between a pane's tabs; <c>gu</c>, <c>gU</c> and <c>g~</c> are operators that then wait for a motion
/// like any other. A
/// doubled case operator accepts either spelling of its second half — <c>guu</c> and <c>gugu</c> are
/// the same command — because both are vim's.
/// </para>
/// <para>
/// <c>i</c> and <c>a</c> are the one place the grammar genuinely collides with itself: they are the
/// insert keys, and they are also the two halves of every text object. Vim resolves it by position,
/// and so does this — they open an object only where one could be used, which is behind an operator
/// or over a selection, and mean insert everywhere else.
/// </para>
/// <para>
/// A visual mode is a different grammar over the same keys, which is why <see cref="Feed"/> is told
/// which mode it is reading for. An operator there is a <em>complete</em> command — there is already
/// a selection for it to act on, so <c>d</c> runs rather than waiting for a motion, and <c>dd</c>
/// cannot be typed at all because the first <c>d</c> has gone. The rest of the visual grammar is a
/// table of its own, and most of it is spelling: <c>x</c> is <c>d</c> over the selection, <c>s</c> is
/// <c>c</c>, the capitals <c>X</c> <c>D</c> <c>Y</c> <c>C</c> <c>S</c> are the same operators made
/// linewise, <c>~</c> <c>u</c> <c>U</c> are the case operators — so <c>u</c> is not undo while there
/// is a selection — and <c>p</c>, <c>P</c>, <c>r</c>, <c>J</c>, <c>gJ</c>, <c>o</c> and <c>gv</c> are
/// commands of their own. A key outside the table means nothing while there is a selection, whatever
/// it means without one — <c>:</c> among them — and is turned down here rather than in the engine,
/// so that "not a command" is one answer from one place.
/// </para>
/// <para>
/// The search family is motions. <c>n</c>, <c>N</c>, <c>*</c> and <c>#</c> are complete the instant
/// they are typed, like <c>;</c>: what they look for is the engine's memory or the text under the
/// caret. <c>/</c> and <c>?</c> are motions whose target has still to be typed, and the parser cannot
/// read a command line — so they complete as the command that opens the line, carrying whatever
/// operator and count were waiting, and the engine finishes the motion when Enter closes it. That is
/// what makes <c>d/foo</c> a delete and <c>v/foo</c> an extended selection rather than a dead
/// sequence and a stray search.
/// </para>
/// </remarks>
public sealed class KeySequenceParser
{
    // Far past any count a person types, and low enough that ten times it plus nine cannot overflow.
    // A count is the one part of the grammar a stuck key can grow without limit.
    private const int MaxCount = 100_000;

    private const char Leader = ' ';

    private readonly StringBuilder _sequence = new();

    private int _count;
    private int _operatorCount;
    private VimOperator _operator;
    private bool _awaitingG;
    private bool _awaitingUpperZ;
    private bool _awaitingLowerZ;
    private bool _awaitingLiteral;
    private bool _awaitingRegister;
    private VimMotion _awaitingFind;

    // The register a '"' named, or '\0' when none was: carried until the command completes, because
    // it belongs to the command and not to the grammar.
    private char _register;

    // '\0' when nothing is pending, otherwise the 'q' or the '@' that is waiting for its register.
    private char _awaitingMacro;

    // '\0' when nothing is pending, otherwise the 'i' or the 'a' that is waiting for its object. The
    // key itself rather than a bool, because which of the two was typed is half of what the object is.
    private char _awaitingObject;

    // Null when no leader is pending, otherwise the keys typed after it so far — empty right after
    // the Space.
    private string? _leader;

    /// <summary>
    /// Gets a value indicating whether a sequence is half typed.
    /// </summary>
    /// <remarks>
    /// What <c>Esc</c> cancels, and the reason it is observable: "Esc cancels a pending sequence" is
    /// a promise about state, and the state has to be visible for a test to hold it to it.
    /// </remarks>
    public bool HasPendingSequence
        => PrefixPending
            || (_register != '\0')
            || (_count > 0)
            || (_operatorCount > 0);

    /// <summary>
    /// Gets the keys typed so far in the sequence that is still open.
    /// </summary>
    public string PendingSequence => _sequence.ToString();

    /// <summary>
    /// Feeds one keystroke to the grammar.
    /// </summary>
    /// <param name="key">The keystroke, already known to belong to vim rather than to the window.</param>
    /// <param name="mode">
    /// The mode the keystroke was typed in, which decides which grammar reads it. Only the visual
    /// pair differs from <see cref="VimMode.Normal"/>; <see cref="VimMode.Insert"/> never reaches the
    /// parser, because in insert mode a key is text.
    /// </param>
    /// <returns>The command it completed, or why it did not.</returns>
    public VimParseResult Feed(VimKey key, VimMode mode = VimMode.Normal)
    {
        bool visual = mode is VimMode.Visual or VimMode.VisualLine;

        _sequence.Append(key.ToString());

        // Before everything: a key held with Control is one of the five scroll commands or nothing at
        // all, and either way it ends whatever was half typed rather than continuing it. 'r' and
        // Ctrl+D is not "write a d" and 'd' and Ctrl+F is not a delete over a page — a scroll is not a
        // motion — so both are dead, named in full. Only a count may stand in front of one.
        if (key.HasCommandModifier)
        {
            return (!PrefixPending && (VimKeyBindings.ControlActionFor(key) is { } control))
                ? Complete(new VimCommand(control, VimMotion.None, TotalCount))
                : Dead();
        }

        // Before anything else: 'r' has already decided what this key means, and it is not a command.
        if (_awaitingLiteral)
        {
            return key.IsCharacter
                ? Complete(new VimCommand(VimAction.ReplaceCharacter, VimMotion.None, TotalCount, VimOperator.None, key.Character))
                : Dead();
        }

        // And for the same reason: 'f' has already decided that this key is a character to look
        // for. 'fd' looks for a 'd'; it does not start a delete.
        if (_awaitingFind != VimMotion.None)
        {
            VimMotion pending = _awaitingFind;
            _awaitingFind = VimMotion.None;

            return key.IsCharacter ? FindMotion(pending, key.Character) : Dead();
        }

        // And once an 'i' or an 'a' has opened a text object, this key says which one. Not taken
        // literally, though — unlike 'r' and the finds, only a key that names an object is one.
        if (_awaitingObject != '\0')
        {
            char opener = _awaitingObject;
            _awaitingObject = '\0';

            return (key.IsCharacter && (VimKeyBindings.TextObjectFor(opener, key.Character) is { } textObject))
                ? ObjectCommand(textObject)
                : Dead();
        }

        // A '"' has already decided that this key names a register. Only a name is one: '"j' is dead
        // rather than a register called j followed by nothing.
        if (_awaitingRegister)
        {
            _awaitingRegister = false;

            if (!key.IsCharacter || !VimRegisters.IsName(key.Character))
            {
                return Dead();
            }

            _register = key.Character;

            return Pending();
        }

        // And a 'q' or an '@' has decided that this key is the register to record into or to run.
        if (_awaitingMacro != '\0')
        {
            char macro = _awaitingMacro;
            _awaitingMacro = '\0';

            return key.IsCharacter ? MacroCommand(macro, key.Character) : Dead();
        }

        // 'Z' has exactly two partners, and both are whole commands.
        if (_awaitingUpperZ)
        {
            _awaitingUpperZ = false;

            return key.Character switch
            {
                'Z' when key.IsCharacter => Complete(new VimCommand(VimAction.WriteQuit, VimMotion.None, TotalCount)),
                'Q' when key.IsCharacter => Complete(new VimCommand(VimAction.QuitDiscard, VimMotion.None, TotalCount)),
                _ => Dead(),
            };
        }

        // 'z' has six: three that scroll a line to the top, the middle or the bottom of the screen
        // and leave the caret alone, and the same three moving the caret to the line's first
        // non-blank as well. Whole commands, all of them.
        if (_awaitingLowerZ)
        {
            _awaitingLowerZ = false;

            return VimKeyBindings.ScrollCaretActionFor(key) is { } scroll
                ? Complete(new VimCommand(scroll, VimMotion.None, TotalCount))
                : Dead();
        }

        if (_awaitingG)
        {
            _awaitingG = false;

            return key.IsCharacter ? SecondG(key.Character, visual) : Dead();
        }

        // The leader has decided that this key is read against its own table, and against nothing else.
        if (_leader is not null)
        {
            return key.IsCharacter ? LeaderKey(key.Character) : Dead();
        }

        if (key.IsCharacter && TryCount(key.Character))
        {
            return Pending();
        }

        if (key.IsCharacter && (key.Character == 'g'))
        {
            _awaitingG = true;

            return Pending();
        }

        // Space opens the leader, in either grammar — but not behind an operator, where it is still the
        // motion: Vim's leader mappings are normal and visual mappings, and leave 'd<Space>' alone.
        if (key.IsCharacter && (key.Character == Leader) && (_operator == VimOperator.None))
        {
            _leader = string.Empty;

            return Pending();
        }

        // A register name, before or after the count and in either grammar — but not behind an
        // operator, where vim has no place for one either: 'd"a' falls through to the operator's dead
        // end below.
        if (key.IsCharacter && (key.Character == '"') && (_operator == VimOperator.None))
        {
            _awaitingRegister = true;

            return Pending();
        }

        // Above the operator and the motion, and above the visual cut-off with them: a find opens a
        // motion rather than completing one, and it is a motion in every mode — behind an operator
        // it says which text, and in a visual mode it says how far the selection reaches.
        if (key.IsCharacter && (VimKeyBindings.FindOpenerFor(key.Character) is { } find))
        {
            _awaitingFind = find;

            return Pending();
        }

        if (key.IsCharacter && (VimKeyBindings.OperatorFor(key.Character) is { } @operator))
        {
            return Operator(@operator, visual);
        }

        // The second half of a doubled case operator, spelled with the bare letter: 'gu' then 'u'.
        // Read before the motions and the actions because 'u' on its own is undo and '~' is a toggle,
        // and neither is what the key means with 'gu' or 'g~' waiting.
        if (key.IsCharacter && (_operator != VimOperator.None) && (key.Character == VimKeyBindings.DoublingKeyFor(_operator)))
        {
            return Complete(VimCommand.Operate(_operator, VimMotion.WholeLine, TotalCount));
        }

        if (VimKeyBindings.MotionFor(key) is { } motion)
        {
            return Motion(motion);
        }

        // The two search openers, above the operator's dead end and the visual cut-off because they
        // are motions in every grammar: the operator and the count ride on the command that opens
        // the line, and the engine runs the motion when Enter closes it.
        if (key.IsCharacter && (key.Character is '/' or '?'))
        {
            return Complete(new VimCommand(VimAction.BeginCommandLine, VimMotion.None, TotalCount, _operator, key.Character));
        }

        // The one key that means two things, resolved by where it is: 'i' and 'a' open a text object
        // where there is something for one to be handed to — an operator waiting, or a selection on
        // screen — and keep their insert meanings everywhere else. 'di' is therefore no longer a dead
        // sequence but a half-typed 'diw', while a bare 'i' is still insert.
        if (key.IsCharacter && ((_operator != VimOperator.None) || visual) && VimKeyBindings.IsObjectOpener(key.Character))
        {
            _awaitingObject = key.Character;

            return Pending();
        }

        // Everything below is a command in its own right, and an operator is still waiting for a
        // motion: 'dp' is not "delete, then put", it is a sequence that never became a command.
        if (_operator != VimOperator.None)
        {
            return Dead();
        }

        // The two page keys are the two page scrolls, and 'z' opens the six that place the caret's
        // line on the screen. Commands in both grammars — the view moves while selecting, and the
        // selection follows the caret — and nothing behind an operator, which the dead end above has
        // already said.
        if (!key.IsCharacter && (VimKeyBindings.PageActionFor(key.Special) is { } page))
        {
            return Complete(new VimCommand(page, VimMotion.None, TotalCount));
        }

        if (key.IsCharacter && (key.Character == 'z'))
        {
            _awaitingLowerZ = true;

            return Pending();
        }

        // The two keys that mean the same thing in every mode they are typed in: start selecting,
        // switch which way, or stop.
        if (key.IsCharacter && (VimKeyBindings.VisualFor(key.Character) is { } visualAction))
        {
            return Complete(new VimCommand(visualAction, VimMotion.None, TotalCount));
        }

        // 'r' takes the next key literally in both grammars: in normal mode it is what the caret's
        // character becomes, over a selection what every selected character becomes.
        if (key.IsCharacter && (key.Character == 'r'))
        {
            _awaitingLiteral = true;

            return Pending();
        }

        // 'q' and '@' take the next key literally too, in both grammars: the register to record into,
        // or the one to run. The 'q' that stops a recording never gets here — the engine takes it
        // first, because only the engine knows a recording is open.
        if (key.IsCharacter && (key.Character is 'q' or '@'))
        {
            _awaitingMacro = key.Character;

            return Pending();
        }

        // Over a selection the commands are a table of their own, and nothing outside it is one: the
        // insert keys and ':' have visual meanings in vim, and both are still on the out list.
        if (visual)
        {
            return (key.IsCharacter ? VimKeyBindings.VisualActionFor(key.Character, TotalCount) : null) is { } visualCommand
                ? Complete(visualCommand)
                : Dead();
        }

        if (key.IsCharacter && (key.Character == 'Z'))
        {
            _awaitingUpperZ = true;

            return Pending();
        }

        return (key.IsCharacter ? VimKeyBindings.ActionFor(key.Character, TotalCount) : null) is { } command ? Complete(command) : Dead();
    }

    /// <summary>
    /// Throws away a half-typed sequence.
    /// </summary>
    public void Reset()
    {
        _count = 0;
        _operatorCount = 0;
        _operator = VimOperator.None;
        _awaitingG = false;
        _awaitingUpperZ = false;
        _awaitingLowerZ = false;
        _awaitingLiteral = false;
        _awaitingRegister = false;
        _register = '\0';
        _awaitingMacro = '\0';
        _awaitingFind = VimMotion.None;
        _awaitingObject = '\0';
        _leader = null;
        _sequence.Clear();
    }

    /// <summary>
    /// Whether a key held with Control is one the grammar reads — one of vim's scroll commands.
    /// </summary>
    /// <param name="key">The keystroke.</param>
    /// <returns><see langword="true"/> for <c>Ctrl+D</c> <c>Ctrl+U</c> <c>Ctrl+F</c> <c>Ctrl+B</c> <c>Ctrl+E</c>.</returns>
    /// <remarks>
    /// What the engine asks before offering a held key to the grammar: every other key held with
    /// Control or Alt belongs to the window, and never reaches the parser at all.
    /// </remarks>
    public static bool IsControlCommand(VimKey key) => VimKeyBindings.ControlActionFor(key) is not null;

    /// <summary>
    /// Whether a prefix is waiting for its partner — anything half typed except a bare count or a
    /// register name, which a whole command may still follow.
    /// </summary>
    private bool PrefixPending
        => _awaitingG
            || _awaitingUpperZ
            || _awaitingLowerZ
            || _awaitingLiteral
            || _awaitingRegister
            || (_awaitingMacro != '\0')
            || (_awaitingFind != VimMotion.None)
            || (_awaitingObject != '\0')
            || (_leader is not null)
            || (_operator != VimOperator.None);

    /// <summary>
    /// Completes a <c>q</c> or an <c>@</c> with the register that followed it.
    /// </summary>
    /// <param name="opener">The <c>q</c> or the <c>@</c>.</param>
    /// <param name="register">The key after it.</param>
    /// <remarks>
    /// A recording goes into a letter, either case — a capital appends. A replay reads a letter, the
    /// yank register <c>0</c>, the unnamed register <c>"</c>, or <c>@</c> for the register that was
    /// last replayed. Anything else is a dead sequence, named as one.
    /// </remarks>
    private VimParseResult MacroCommand(char opener, char register)
    {
        if (opener == 'q')
        {
            return char.IsAsciiLetter(register)
                ? Complete(new VimCommand(VimAction.RecordMacro, VimMotion.None, TotalCount, VimOperator.None, register))
                : Dead();
        }

        if (register == '@')
        {
            return Complete(new VimCommand(VimAction.PlayLastMacro, VimMotion.None, TotalCount));
        }

        return char.IsAsciiLetter(register) || (register is VimRegisters.YankName or VimRegisters.UnnamedName)
            ? Complete(new VimCommand(VimAction.PlayMacro, VimMotion.None, TotalCount, VimOperator.None, register))
            : Dead();
    }

    /// <summary>
    /// Whether a command reads or fills a register, which is what makes a <c>"a</c> in front of it
    /// mean something.
    /// </summary>
    /// <remarks>
    /// A command line opened behind an operator counts — <c>"ad/foo</c> fills <c>a</c> when Enter
    /// runs the delete — and one opened on its own does not, exactly as a bare motion does not.
    /// </remarks>
    private static bool TakesRegister(VimCommand command)
        => command.Action switch
        {
            VimAction.Operate
                or VimAction.DeleteCharacter
                or VimAction.PutAfter
                or VimAction.PutBefore
                or VimAction.ReplaceSelection
                or VimAction.Repeat => true,
            VimAction.BeginCommandLine => command.Operator != VimOperator.None,
            _ => false,
        };

    /// <summary>
    /// Gets the two counts multiplied together, or zero when neither was typed.
    /// </summary>
    private int TotalCount
    {
        get
        {
            if ((_count == 0) && (_operatorCount == 0))
            {
                return 0;
            }

            return Math.Min(Math.Max(_count, 1) * Math.Max(_operatorCount, 1), MaxCount);
        }
    }

    private bool TryCount(char character)
    {
        if (!char.IsAsciiDigit(character))
        {
            return false;
        }

        int active = _operator == VimOperator.None ? _count : _operatorCount;

        // '0' cannot start a count — that is what leaves it free to be the motion to the start of the
        // line — but it continues one, which is how '10j' reaches ten lines rather than one.
        if ((character == '0') && (active == 0))
        {
            return false;
        }

        int grown = Math.Min((active * 10) + (character - '0'), MaxCount);

        if (_operator == VimOperator.None)
        {
            _count = grown;
        }
        else
        {
            _operatorCount = grown;
        }

        return true;
    }

    /// <summary>
    /// Reads the key after a <c>g</c>.
    /// </summary>
    /// <remarks>
    /// Three kinds of partner: a motion (<c>gg</c>), an operator (<c>gu</c>, <c>gU</c>, <c>g~</c>)
    /// that goes on to wait for its own motion — or, typed again behind itself, doubles — and the
    /// commands <c>gJ</c>, <c>gv</c>, the pane keys — <c>g</c> and a <c>Ctrl+W</c> key: <c>gh</c> <c>gj</c>
    /// <c>gk</c> <c>gl</c> <c>g=</c> <c>g+</c> <c>g-</c> <c>g&gt;</c> <c>g&lt;</c> — and the tab keys
    /// <c>gt</c> <c>gT</c>, which like every other command have no meaning behind an operator. All of
    /// them are commands in a visual mode too: <c>gJ</c> joins the selected lines, <c>gv</c> swaps the
    /// selection for the one before it, and the pane and tab keys act as <c>Ctrl+W</c> does from a
    /// selection.
    /// </remarks>
    private VimParseResult SecondG(char character, bool visual)
    {
        if (character == 'g')
        {
            return Motion(VimMotion.FirstLine);
        }

        if (VimKeyBindings.CaseOperatorFor(character) is { } caseOperator)
        {
            return Operator(caseOperator, visual);
        }

        if (_operator != VimOperator.None)
        {
            return Dead();
        }

        return character switch
        {
            'J' => Complete(new VimCommand(VimAction.JoinRaw, VimMotion.None, TotalCount)),
            'v' => Complete(new VimCommand(VimAction.ReselectVisual, VimMotion.None, TotalCount)),
            'h' or 'j' or 'k' or 'l' or '=' or '+' or '-' or '>' or '<'
                => Complete(new VimCommand(VimAction.PaneCommand, VimMotion.None, TotalCount, VimOperator.None, character)),
            't' => Complete(new VimCommand(VimAction.NextTab, VimMotion.None, TotalCount)),
            'T' => Complete(new VimCommand(VimAction.PreviousTab, VimMotion.None, TotalCount)),
            _ => Dead(),
        };
    }

    /// <summary>
    /// Reads a key after the leader.
    /// </summary>
    /// <remarks>
    /// Pending while the keys so far begin a mapping, complete when they spell one, dead otherwise. A
    /// count typed in front of the leader rides on the command, as it does on <c>gh</c>, and the pane
    /// keys ignore it the same way.
    /// </remarks>
    private VimParseResult LeaderKey(char character)
    {
        string typed = _leader + character;

        if (VimKeyBindings.LeaderMappings.TryGetValue(typed, out VimCommand command))
        {
            return Complete(command with { Count = TotalCount });
        }

        if (VimKeyBindings.LeaderMappings.Keys.Any(keys => keys.StartsWith(typed, StringComparison.Ordinal)))
        {
            _leader = typed;

            return Pending();
        }

        return Dead();
    }

    /// <summary>
    /// Reads an operator key, in whichever of its three positions it was typed.
    /// </summary>
    private VimParseResult Operator(VimOperator @operator, bool visual)
    {
        // In a visual mode the operator is the whole command: what it acts on is on screen
        // already, so there is nothing to wait for and no doubled form to type.
        if (visual)
        {
            return Complete(VimCommand.Operate(@operator, VimMotion.None, TotalCount));
        }

        if (_operator == VimOperator.None)
        {
            _operator = @operator;

            return Pending();
        }

        // An operator typed twice is that operator over its own line — 'dd', 'yy', 'cc', '>>',
        // 'gugu'. Typed over a different one it is nothing at all: 'dy' is a mistake, not a command.
        return (@operator == _operator)
            ? Complete(VimCommand.Operate(_operator, VimMotion.WholeLine, TotalCount))
            : Dead();
    }

    private VimParseResult Motion(VimMotion motion)
        => Complete(_operator == VimOperator.None
            ? VimCommand.Move(motion, TotalCount)
            : VimCommand.Operate(_operator, motion, TotalCount));

    /// <summary>
    /// Completes a text object, for whichever of its two uses the sequence was opened in.
    /// </summary>
    /// <remarks>
    /// Behind an operator it is that operator over the object. Without one it can only have come from
    /// a visual mode — that is what opened it — and there the object is a move: it says what is
    /// selected, and the operator comes later or not at all.
    /// </remarks>
    private VimParseResult ObjectCommand(VimTextObject textObject)
        => Complete(_operator == VimOperator.None
            ? new VimCommand(VimAction.Move, Count: TotalCount, TextObject: textObject)
            : VimCommand.Object(_operator, textObject, TotalCount));

    /// <summary>
    /// Completes a find, whose character is an argument rather than part of the motion.
    /// </summary>
    /// <remarks>
    /// Written out rather than routed through <see cref="Motion"/> because the two factories on
    /// <see cref="VimCommand"/> take no argument, and a find is the one motion that carries one.
    /// </remarks>
    private VimParseResult FindMotion(VimMotion motion, char character)
        => Complete(new VimCommand(
            _operator == VimOperator.None ? VimAction.Move : VimAction.Operate,
            motion,
            TotalCount,
            _operator,
            character));

    private VimParseResult Pending() => VimParseResult.Pending(_sequence.ToString());

    private VimParseResult Complete(VimCommand command)
    {
        // The register rides on the command it was typed in front of — and a command that has no use
        // for one is not a command with a register, it is a mistake, named in full: '"aj'.
        if (_register != '\0')
        {
            if (!TakesRegister(command))
            {
                return Dead();
            }

            command = command with { Register = _register };
        }

        VimParseResult result = VimParseResult.Complete(command, _sequence.ToString());
        Reset();

        return result;
    }

    private VimParseResult Dead()
    {
        VimParseResult result = VimParseResult.Invalid(_sequence.ToString());
        Reset();

        return result;
    }
}

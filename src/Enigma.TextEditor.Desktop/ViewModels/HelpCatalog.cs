using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Enigma.Icons.Phosphor;
using Enigma.TextEditor.Core.Vim;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What the key-binding help says: the editor's keys and ex commands, by section.
/// </summary>
/// <remarks>
/// <para>
/// Written out rather than generated. The Vim grammar is a set of switches in the engine, not a table a
/// help could be read from; the requests that leave the engine are the exception, and their spellings
/// come from <see cref="ExCommandParser.Definitions"/>, so <c>:vs[plit]</c> here is what the parser reads.
/// </para>
/// <para>
/// Each line says which of those requests, and which window commands, it documents. The tests hold the
/// help to every one of them, so a request or a window command added to the engine without a line
/// here fails the build's tests rather than going unmentioned.
/// </para>
/// </remarks>
public static class HelpCatalog
{
    /// <summary>
    /// Gets the sections, in reading order.
    /// </summary>
    public static IReadOnlyList<HelpSection> Sections { get; } =
    [
        Section("Moving", PhosphorIcon.Compass, "A count in front repeats: 3j, 5w",
        [
            Keys("h, j, k, l, ←, ↓, ↑, →", "Left, down, up, right"),
            Keys("w, b, e", "Next word, previous word, end of the word"),
            Keys("W, B, E", "The same, words being separated by blanks only"),
            Keys("0, Home", "Start of the line"),
            Keys("^", "First non-blank of the line"),
            Keys("$, End", "End of the line"),
            Keys("|", "Column: 8| goes to the eighth"),
            Keys("gg, G", "First line, last line; a count goes to that line: 42G"),
            Keys("H, M, L", "Top, middle, bottom of the screen"),
            Keys("{, }", "Previous, next paragraph"),
            Keys("(, )", "Previous, next sentence"),
            Keys("%", "The matching bracket"),
            Keys("f{c}, F{c}", "The next, previous {c} on the line"),
            Keys("t{c}, T{c}", "Just before the next, after the previous {c}"),
            Keys(";, ,", "Repeat the last f, F, t or T, forwards or backwards"),
            Keys("+, Enter, -", "First non-blank of the next, previous line"),
            Keys("Space, Backspace", "Right, left, going on to the next or previous line"),
            Text("", "Go to line n, to the last line", ":{n}, :$"),
        ]),
        Section("Scrolling", PhosphorIcon.ArrowsDownUp, null,
        [
            Keys("Ctrl+D, Ctrl+U", "Half a page down, up"),
            Keys("Ctrl+F, Ctrl+B, PgDn, PgUp", "A page down, up"),
            Keys("Ctrl+E, Ctrl+Y", "One line down, up; the caret stays on its line"),
            Keys("zt, zz, zb", "The caret's line to the top, middle, bottom of the screen"),
            Keys("z Enter, z., z-", "The same, the caret to the first non-blank"),
        ]),
        Section("Inserting", PhosphorIcon.CursorText, null,
        [
            Keys("i, a", "Insert before, after the caret"),
            Keys("I, A", "Insert at the first non-blank, at the end of the line"),
            Keys("o, O", "Open a line below, above, with this line's indentation"),
            Keys("Esc", "Back to normal mode"),
        ]),
        Section("Editing", PhosphorIcon.PencilSimple, "A count in front repeats: 3x, 2p",
        [
            Keys("x, X", "Delete the character under, before the caret"),
            Keys("r{c}", "Replace the character under the caret with {c}"),
            Keys("s, S", "Change the character, the whole line"),
            Keys("D, C", "Delete, change to the end of the line"),
            Keys("Y", "Yank the line"),
            Keys("p, P", "Put after, before the caret"),
            Keys("J, gJ", "Join with the next line, with a space or as it is"),
            Keys("~", "Toggle the case of the character"),
            Keys("u, Ctrl+R", "Undo, redo"),
            Keys(".", "Repeat the last change"),
        ]),
        Section("Operators & text objects", PhosphorIcon.BracketsCurly, "An operator, then a motion or a text object: dw, c$, yiw, >}",
        [
            Keys("d, c, y", "Delete, change, yank"),
            Keys(">, <", "Indent, unindent the lines"),
            Keys("gu, gU, g~", "Lowercase, uppercase, toggle case"),
            Keys("dd, cc, yy, >>, <<", "The operator over the whole line; guu, gUU, g~~ too"),
            Keys("2d3w, 3dd", "Counts multiply, and repeat a line operator"),
            Keys("iw, aw, iW, aW", "Inside a word, a word with its blanks; W for blank-separated words"),
            Keys("i( a(, i[ a[, i{ a{, i< a<", "Inside, around a pair of brackets; b and B are ( and {"),
            Keys("i\" a\", i' a', i` a`", "Inside, around quotes"),
        ]),
        Section("Visual mode", PhosphorIcon.Selection, "Over the selection",
        [
            Keys("v, V", "Select characters, whole lines; the same key again stops"),
            Keys("o", "Go to the other end of the selection"),
            Keys("gv", "Select the last selection again"),
            Keys("d, x, c, s, y", "Delete, change, yank the selection"),
            Keys("D, X, C, S, Y", "The same, over whole lines"),
            Keys(">, <", "Indent, unindent the selected lines"),
            Keys("~, u, U", "Toggle case, lowercase, uppercase"),
            Keys("p, P", "Replace the selection with what was yanked"),
            Keys("r{c}", "Replace every selected character with {c}"),
            Keys("J, gJ", "Join the selected lines"),
            Keys("Esc", "Stop selecting"),
        ]),
        Section("Search & replace", PhosphorIcon.MagnifyingGlass, "Ranges: % the whole file, '<,'> the last selection, 3,7 lines",
        [
            Keys("/{pattern}, ?{pattern}", "Search forwards, backwards; Enter runs it"),
            Keys("n, N", "Next, previous match"),
            Keys("*, #", "Next, previous occurrence of the word under the caret"),
            Text("", "Replace on the line or the range; g every match, i ignoring case", ":[range]s/{pattern}/{text}/[g][i]"),
            Text("", "Hide the highlighted matches until the next search", ":noh[lsearch]"),
        ]),
        Section("Registers & macros", PhosphorIcon.Clipboard, null,
        [
            Keys("\"{a-z}", "Use that register for the next delete, yank or put: \"ayy, \"ap"),
            Keys("\"{A-Z}", "Append to it instead"),
            Keys("\"0", "The last yank"),
            Keys("\"_", "The black hole: delete without keeping it"),
            Keys("q{a-z}, q", "Record the keys typed into a register; q again stops"),
            Keys("@{a-z}, @@", "Play a register's keys, the last one played again"),
        ]),
        Section("Files & tabs", PhosphorIcon.Files, null,
        [
            Ex("Ctrl+S", "Save; with a file, save under that name", VimExCommand.Write, " [file]"),
            Ex("ZZ", "Save, then close the tab", VimExCommand.WriteQuit),
            Ex("", "Close the tab, asking about unsaved changes; quit on the last one", VimExCommand.Quit),
            Ex("ZQ", "Close the tab, throwing its changes away", VimExCommand.QuitDiscard, bang: true),
            Ex("", "Save every buffer with changes", VimExCommand.WriteAll),
            Ex("", "Quit, asking about unsaved changes", VimExCommand.QuitAll),
            Ex("", "Quit, throwing every change away", VimExCommand.QuitAllDiscard, bang: true),
            Ex("Ctrl+O", "Open a file in this pane; without one, pick it", VimExCommand.Edit, " [file]"),
            Ex("Ctrl+N", "New untitled buffer in a new tab", VimExCommand.EditNew),
            Ex("", "New untitled buffer in this tab's place, throwing its changes away", VimExCommand.EditNewDiscard, bang: true),
            Ex("Ctrl+Tab", "Next tab", VimExCommand.BufferNext),
            Ex("Ctrl+Shift+Tab", "Previous tab", VimExCommand.BufferPrevious),
            Keys("Drop files on a pane", "Open them in that pane"),
        ]),
        Section("Panes", PhosphorIcon.Columns, "Ctrl+W, then the key",
        [
            Ex("Ctrl+W s, Ctrl+W S", "Split one above the other, with a new buffer; with a file, open it there", VimExCommand.Split, " [file]")
                with { WindowCommands = [VimWindowCommand.SplitHorizontal] },
            Ex("Ctrl+W v", "Split side by side, with a new buffer; with a file, open it there", VimExCommand.VerticalSplit, " [file]")
                with { WindowCommands = [VimWindowCommand.SplitVertical] },
            Ex("", "Split one above the other, with a new buffer", VimExCommand.New),
            Ex("", "Split side by side, with a new buffer", VimExCommand.VerticalNew),
            Window("Ctrl+W h, j, k, l, Ctrl+W ←, ↓, ↑, →", "The pane to the left, below, above, to the right",
                VimWindowCommand.FocusLeft, VimWindowCommand.FocusDown, VimWindowCommand.FocusUp, VimWindowCommand.FocusRight),
            Window("gh, gj, gk, gl", "The same, without Ctrl+W",
                VimWindowCommand.FocusLeft, VimWindowCommand.FocusDown, VimWindowCommand.FocusUp, VimWindowCommand.FocusRight),
            Window("Ctrl+W w, Ctrl+W W", "Next, previous pane", VimWindowCommand.FocusNext, VimWindowCommand.FocusPrevious),
            Ex("Ctrl+W c", "Close the pane", VimExCommand.Close) with { WindowCommands = [VimWindowCommand.Close] },
            Window("Ctrl+W q", "Close the tab, as :q does", VimWindowCommand.Quit),
            Ex("Ctrl+W o", "Close every other pane", VimExCommand.Only) with { WindowCommands = [VimWindowCommand.Only] },
            Window("Ctrl+W =", "Make the panes the same size", VimWindowCommand.Equalize),
            Window("Ctrl+W +, Ctrl+W -", "Taller, shorter", VimWindowCommand.Taller, VimWindowCommand.Shorter),
            Window("Ctrl+W >, Ctrl+W <", "Wider, narrower", VimWindowCommand.Wider, VimWindowCommand.Narrower),
        ]),
        Section("Options", PhosphorIcon.SlidersHorizontal, ":set {name} turns one on, :set no{name} off, :set {name}={n} sets a number",
        [
            Text("", "Show every option", ":se[t]"),
            Text("", "Ignore case in searches", ":set ignorecase, ic"),
            Text("", "…unless the pattern has a capital", ":set smartcase, scs"),
            Text("", "Searches wrap round the end of the file", ":set wrapscan, ws"),
            Text("", "Highlight every match", ":set hlsearch, hls"),
            Text("", "Show the match while the pattern is typed", ":set incsearch, is"),
            Text("", "Indent with spaces (the file's indentation follows)", ":set expandtab, et"),
            Text("", "Indentation size, for Tab, > and <", ":set shiftwidth=n, sw, tabstop, ts"),
        ]),
        Section("Application", PhosphorIcon.Keyboard, null,
        [
            Ex("F1", "This help", VimExCommand.Help),
            Keys("Ctrl+Shift+S", "Save as"),
            Keys("Ctrl+F4", "Close the tab"),
            Keys("Ctrl+=, Ctrl+-, Ctrl+0", "Zoom in, out, back to the default"),
        ]),
    ];

    private static HelpSection Section(string category, PhosphorIcon icon, string? note, IReadOnlyList<HelpRow> rows)
    {
        string count = string.Create(CultureInfo.InvariantCulture, $"{rows.Count} {(rows.Count == 1 ? "command" : "commands")}");

        return new HelpSection(category, rows, icon, note is null ? count : $"{count} · {note}");
    }

    private static HelpRow Keys(string keys, string title) => new(keys, title, string.Empty);

    private static HelpRow Text(string keys, string title, string exCommand) => new(keys, title, exCommand);

    private static HelpRow Window(string keys, string title, params VimWindowCommand[] commands)
        => new(keys, title, string.Empty) { WindowCommands = commands };

    /// <summary>
    /// A line for a request that leaves the engine, its ex column written from the parser's own table:
    /// every spelling that raises it, <c>:wq, :x</c>.
    /// </summary>
    private static HelpRow Ex(string keys, string title, VimExCommand command, string argument = "", bool bang = false)
    {
        IEnumerable<string> spellings = ExCommandParser.Definitions
            .Where(definition => (bang ? definition.BangCommand : definition.Command) == command)
            .Select(definition => definition.Spelling(bang) + argument);

        return new HelpRow(keys, title, string.Join(", ", spellings)) { ExCommands = [command] };
    }
}

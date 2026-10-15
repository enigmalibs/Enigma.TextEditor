# FEATURE-12AF — Help dialog & ex abbreviations

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-12af-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-drop-icon-help

## Objective

1. Ex commands accept vim's abbreviations: `:vsp` (and `:vspl`, `:spl`, `:clos`, …) work as vim reads
   them.
2. `:help` opens a key-bindings help dialog, the same one Enigma.FileExplorer shows.

## Context & constraints

- `ExCommandParser.FileCommand` maps a parsed ex line to a `VimExCommand` with a switch. Each command
  has exactly one or two literal spellings (`"vs" or "vsplit"`), so `:vsp` is reported as
  `Not an editor command: vsp`.
- Vim accepts any prefix of a command's name from its minimum length on: `:vs[plit]` is `vs`, `vsp`,
  `vspl`, `vspli` and `vsplit`.
- `VimEngine.RunEx` already reads `:noh[lsearch]` this way. It handles `s`/`substitute`, `se`/`set`,
  a bare range and `noh…` itself, then hands the rest to `FileCommand`.
- The parser deliberately refuses spellings the editor does not mean. The spelled-out `:write` and
  `:quit` are refused, as is `:q` with an argument.
- Requests that leave the engine go through `ExCommandHandler`, which holds the workspace, the shutdown
  service and the status line.
- Enigma.FileExplorer's help (`HelpDialogService`, `HelpViewModel`, `HelpView`, `DialogFit`,
  `IconGeometryConverter`, styles `helpRow` / `helpKeys` / `helpEx`):
  - a ContentDialog titled "Key bindings", with one *Close* button;
  - a 760-wide view with a single `ScrollViewer` (at most 440 high, fitted to the dialog card);
  - one `SettingsCardExpander` per section, with a Phosphor icon and a "N commands" subtitle;
  - three columns: keys (200, monospace), description, ex command (130, monospace, secondary);
  - keys: `j`/`k`/↓/↑, `Ctrl+D`/`Ctrl+U`, `PgDn`/`Space`, `PgUp`, `g`/`Home`, `G`/`End`; `q` and
    `Esc` close it.

  It is generated from FileExplorer's `CommandCatalog`. The text editor has no catalog: its grammar
  lives in `KeySequenceParser`, `VimWindowKeys` and `ExCommandParser`.
- The menu bar's Help menu holds *About Enigma Text Editor* (FEATURE-6BB4).

## Decisions

| Decision | Rationale |
|---|---|
| Vim's `name[rest]` rule for every command that already had a short and a long spelling: `sp[lit]`, `vs[plit]`, `e[dit]`, `bn[ext]`, `bp[revious]`, `wa[ll]`, `qa[ll]`, `vne[w]`, `on[ly]`, `clo[se]` | `:vsp` is one of vim's accepted forms; a lone alias would leave `:vspl` broken |
| Single-spelling commands unchanged: `:w`, `:q`, `:wq`, `:x`, `:new` | The parser's documented strictness |
| A public definition table in Core replaces the switch: name, minimum length, command, bang command, argument rule. It also gives the help its `:vs[plit]` spellings | One source for the parser and the help |
| `:h[elp]` opens the help; an argument (`:help vsplit`) is accepted and ignored | There is no per-topic help; opening the general help is the useful answer |
| *Help → Key Bindings* (F1) above *About* | Discoverability; F1 is vim's and Windows' help key |
| The help content is a curated catalog in Desktop. Sections: Moving, Scrolling, Inserting, Editing, Operators & text objects, Visual mode, Search & replace, Registers & macros, Files & tabs, Panes, Options, Application. The ex column comes from the definition table | The grammar is switch-based; generating from it would mean rewriting the engine |
| Tests guard the catalog: every `VimExCommand` and every `VimWindowCommand` is documented | The help cannot silently fall behind the commands that leave the engine |
| FileExplorer's look and keys, with its `DialogFit` and `IconGeometryConverter` copied into Desktop/Views | "The same help content dialog"; no shared package exists |

## PHASE01 — Ex command abbreviations

**Status:** DONE — see `docs/done/FEATURE-12AF-PHASE01.md`
**Branch:** `feature/feature-12af-phase01-ex-abbreviations`

### Steps

1. `Core/Vim/VimExCommandDefinition.cs`: a record holding the name, minimum length, command, bang
   command (optional) and argument rule. `Core/Vim/VimExArgument.cs`: the argument rule — none,
   optional or required.
2. `ExCommandParser`:
   - `Definitions`, the table;
   - `FileCommand` resolves the typed name with vim's prefix rule, then applies the bang, argument
     and range rules exactly as before;
   - `Spelling(definition, bang)` returns `:vs[plit]`, `:w` or `:q!`.
3. Keep the `VimExCommand` docs in step with the spellings.
4. Tests (Core):
   - every prefix from the minimum to the full name is read (`:vsp`, `:vspl`, `:spl`, `:bne`, `:bpr`,
     `:wal`, `:qal!`, `:onl`, `:clos`, `:ed file`);
   - below the minimum, past the name, and misspellings are refused (`:v`, `:cl`, `:vsplitx`,
     `:write`);
   - no typed name resolves to two definitions;
   - the spellings are right;
   - the engine raises `VerticalSplit` for `:vsp`;
   - every existing ex test is unchanged and green.

### Acceptance criteria

- `:vsp` splits side by side; `:spl`, `:clos`, `:onl`, `:bne` work as in vim.
- `:w`, `:q`, `:wq`, `:x`, `:new` accept exactly what they accepted before.
- Zero warnings, whole suite green.

## PHASE02 — Key bindings help dialog

**Status:** TODO
**Branch:** `feature/feature-12af-phase02-help-dialog`

### Steps

1. `VimExCommand.Help`, defined as `h[elp]` with an optional argument.
2. `IHelpDialogService` / `HelpDialogService`: the view in the content dialog, titled
   "Key bindings", with a *Close* button. Registered in `AddShell`.
3. `ExCommandHandler` runs `Help` through the service. The service reaches the handler through
   `WorkspaceViewModel`'s constructor.
4. `MainWindowViewModel.HelpCommand`, and *Help → Key Bindings* with `F1` (menu gesture and window
   key binding).
5. `ViewModels/HelpCatalog.cs` (sections, rows) and `ViewModels/HelpViewModel.cs` (sections with
   icon and subtitle, and a close command).
6. `Views/HelpView.axaml(.cs)`, `Views/DialogFit.cs`, `Views/IconGeometryConverter.cs`, and the help
   styles. The view handles the keys FileExplorer's does, and the editor gets the keyboard back once
   the help closes.
7. Tests:
   - `:help`, `:h` and `:help vsplit` open the help;
   - *Help → Key Bindings* opens it;
   - catalog coverage of `VimExCommand` and `VimWindowCommand`;
   - every ex spelling in the help parses to its command;
   - the subtitles;
   - headless:
     - the view renders one card per section;
     - `j` scrolls, `G` reaches the bottom, `q` closes;
     - the menu holds the item.

### Acceptance criteria

- `:help` (`:h`, `:help {anything}`) and F1 / *Help → Key Bindings* open the dialog.
- The dialog lists the editor's keys and ex commands by section, with vim's `[…]` spellings.
- It scrolls with `j`/`k`, `Ctrl+D`/`Ctrl+U`, `g`/`G`, `PgUp`/`PgDn`. *Close*, `Esc` and `q` close it.
- Zero warnings, whole suite green.

## Out of scope

- Per-topic help (`:help {subject}` jumping to a section).
- Vim's other ex commands (`:bd`, `:ls`, `:tabnew`, …).
- Abbreviations for single-spelling commands (`:write`, `:quit`).
- A searchable or filterable help.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to add `:vsp` | Vim's prefix rule for the commands that already had two spellings | Faithful to vim; fixes `:vspl` and siblings by the same rule | A literal `"vsp"` alias (half-fixes it); prefix rule for every command including `:write`/`:quit` (reverses the parser's documented strictness) |
| Phasing | Two phases: abbreviations, then help | Each is a reviewable commit; the help uses the table PHASE01 adds | One dev (one big commit) |
| `:help {arg}` | Opens the general help | Harmless and what a vim user expects | Refuse it as "Not an editor command" |
| Help source | Curated catalog + coverage tests | No catalog exists; the engine's grammar is switch-based | Generate from the parser (engine rewrite) |
| Extra entry points | F1 and *Help → Key Bindings* | Discoverable; vim maps F1 to help too | `:help` only |
| Shared code with FileExplorer | Copy `DialogFit` and `IconGeometryConverter` | No shared package exists; tiny files | A new shared package (new infrastructure) |

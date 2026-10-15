# FEATURE-12AF-PHASE02 — Key bindings help dialog

**Item:** FEATURE-12AF — Help dialog & ex abbreviations
**Branch:** `feature/feature-12af-phase02-help-dialog`
**Run:** vibe/2026-10-15-drop-icon-help

## Summary

`:help` opens a key-bindings help, the same dialog Enigma.FileExplorer shows.

**How to open it:**

- `:help` or `:h`. Anything after the name is accepted and ignored: `:help vsplit` shows the one help
  there is.
- *Help → Key Bindings*, or F1. F1 is no Vim key, so the editor lets it through to the window's binding.

**The dialog** is FileExplorer's: a content dialog titled "Key bindings", with one *Close* button.

- A 760-wide view with a single scroll area, at most 440 high and fitted so the dialog card itself
  never scrolls.
- One card per section, each with a Phosphor icon and a subtitle such as `12 commands · Ctrl+W, then
  the key`.
- Three columns: the keys, what they do, and the ex command that does it too. Both key and ex
  columns use the editor's monospace font.
- Keys: `j`/`k`/↓/↑ scroll a line and a half; `Ctrl+D`/`Ctrl+U` half a page; `PgDn`/`Space`/`PgUp` a
  page; `g`/`Home` and `G`/`End` to the top and bottom.
- `q`, Esc and *Close* close it, and the keyboard goes back to whatever had it, normally the editor.

**What it says:** twelve sections.

- Moving, Scrolling, Inserting, Editing.
- Operators & text objects, Visual mode.
- Search & replace, Registers & macros.
- Files & tabs, Panes, Options, Application.

The content was checked against the engine: its key parser's tables, `VimWindowKeys`, `VimSet`'s
options, `VimSubstitute`'s flags and `VimRegisters`. It lists only what the editor does (no `ip`
object, no `:` over a selection, for example).

The ex column of every request that leaves the engine is written from `ExCommandParser.Definitions`
(PHASE01): `:vs[plit] [file]`, `:wq, :x`, `:qa[ll]!`. Each line records which requests and window
commands it documents. The tests require every `VimExCommand` and every `VimWindowCommand` to be
documented, and every ex spelling shown to parse to its command. So the help cannot fall silently
behind those two sets.

## Files / modules touched

**Created**

- `Services/IHelpDialogService.cs`, `Services/HelpDialogService.cs` — the help in the content
  dialog; gives the keyboard back once the dialog has closed.
- `ViewModels/HelpViewModel.cs` — `HelpRow`, `HelpSection`, and the view-model (sections and close).
- `ViewModels/HelpCatalog.cs` — the twelve sections.
- `Views/HelpView.axaml(.cs)` — the cards, the keys, and fitting the dialog card.
- `Views/DialogFit.cs`, `Views/IconGeometryConverter.cs` — copied from Enigma.FileExplorer.
- Tests:
  - `ViewModels/HelpTests.cs` (14 cases);
  - `Views/HelpViewTests.cs` (3);
  - `Infrastructure/FakeHelpDialogService.cs`.

**Modified**

- Core:
  - `Vim/VimExCommand.cs`: `Help`;
  - `Vim/ExCommandParser.cs`: `h[elp]` with an optional argument.
- `ViewModels/ExCommandHandler.cs` runs `Help` through the service.
- `ViewModels/WorkspaceViewModel.cs` passes the service to the handler.
- `ViewModels/MainWindowViewModel.cs`: `HelpCommand`.
- `Views/MainWindow.axaml`: *Help → Key Bindings* (F1) above *About*, and the F1 key binding.
- `Extensions/ServiceCollectionExtensions.cs`: the service.
- Tests:
  - `Core.UnitTests/Vim/ExCommandParserTests.cs`: `:h`, `:hel`, `:help`, `:help vsplit` read;
    `:help!` and `:helps` refused;
  - `CompositionRootTests.cs`: the service resolves, plus the new constructor argument;
  - `Infrastructure/EditorHarness.cs`: the fake help;
  - `ViewModels/SettingsMenuTests.cs`: the Help menu now holds *Key Bindings* (F1) and *About*.
- `docs/roadmap.md`, `docs/plan/FEATURE-12AF.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the catalog lives | `Desktop/ViewModels/HelpCatalog.cs`, public static | It documents the window's shortcuts as well as the engine; tests read it directly | Core (it would carry Desktop shortcuts and Phosphor icons) |
| How a line says what it documents | Internal `ExCommands` / `WindowCommands` on `HelpRow` | Keeps the displayed record FileExplorer's three-column shape, with the coverage data beside it | Parsing the display strings back in the tests alone (a typo would read as "engine-internal") |
| Card subtitle | "N commands", with a note where useful | FileExplorer's subtitle | Counting "lines" |
| Column widths | Keys 190, ex 200, both wrapping | `:[range]s/{pattern}/{text}/[g][i]` needs more than FileExplorer's 130 | FileExplorer's 200/130, which would clip |
| Fonts | `EditorPreferences.MonospaceFamily` | This app has no `MonospaceFontFamily` resource; the editor's own chain | A new theme resource |
| How the letters are read | `KeySymbol` (`j`, `G`, `q`), `Key` for arrows, pages, Home/End and Ctrl+D/U | Letters follow the user's layout; FileExplorer's key-notation layer does not exist here | `Key.J` etc. (wrong on non-QWERTY layouts) |
| Giving the keyboard back | The view records the focused element as it opens; the service hands focus back once `ShowAsync` returns | The content dialog keeps its content attached after closing, so the view's detach hook never runs on close (the first attempt relied on it) | Restoring from `OnDetachedFromVisualTree`; refocusing the active pane from the window |

## Deviations & follow-ups

- **Focus return was not in FileExplorer's code path.** FileExplorer returns focus elsewhere; here the
  help does it, as described above. The other dialogs in this app (language and encoding pickers,
  About) do not give the keyboard back to the editor either. That is a possible follow-up, out of
  this phase's scope.
- **The catalog is curated.** Keys inside the engine (motions, operators, objects) are not checked
  against the parser by a test; only the requests and window commands are. A key added to the engine
  needs a line added here by hand.
- **Fix cycles used: 2 of 3.** Both were about the keyboard going back to the editor (see above).
- **Not done:** no manual look at the running Debug build. The dialog's content, its keys, its
  fitting and the focus return are exercised headlessly in the real window.
- Line endings: the new and touched files are LF; no CRLF churn.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, there is no
`CLAUDE.md` or `AGENTS.md`, and `src/Enigma.TextEditor.Core/Vim/README.md` describes provenance only.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (no AVLN warnings).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2436 passed**, 0 failed, 0 skipped (2411 before,
  plus 25 new cases). The new tests cover:
  - `:help`, `:h`, `:help vsplit` and F1 typed in the real editor ask for the help, and type nothing;
  - *Help → Key Bindings* asks for it; the menu holds it with F1;
  - every `VimExCommand` and every `VimWindowCommand` has a line;
  - every ex spelling shown, short and long, parses to its command;
  - the ex column is the parser's spelling;
  - every section has lines, a unique name and a subtitle counting them, in the planned order;
  - the real dialog:
    - it is titled "Key bindings", with *Close*;
    - one card per section;
    - it takes the keyboard;
    - its card does not scroll;
    - `j j k G Ctrl+U g` move the scroll area as specified;
    - `q` and Esc close it, and the editor has the keyboard again.

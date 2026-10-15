# FEATURE-04BC — Multi-split panes

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-04bc-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

Let the content area split into any arrangement of editor panes, the way Enigma.FileExplorer
splits its panes:

- split right and down, close, keep only one, equalize, move focus by direction;
- from the View menu and from Vim's `Ctrl+W` keys and `:sp`/`:vs`;
- with Vim's registers and search shared across every pane.

## Context & constraints

- Enigma.FileExplorer @ `ce34411` is the reference:
  - `Core/Layout/{LayoutTree,LayoutNode,PaneNode,SplitNode,SplitAxis,PaneId,LayoutRect,LayoutSnapshot,FocusDirection}`;
  - `Desktop/Views/SplitHost` (nested `Grid`s with `GridSplitter`s, ratio written back on drag end,
    pane views reused across rebuilds);
  - `Desktop/ViewModels/WorkspaceViewModel` (pane per `PaneId`, batches, active pane, framing);
  - `Desktop/Commands/WindowCommands` (resize step 0.05, `:q` on the last pane quits).
- Builds on FEATURE-3EE8: `EditorPaneViewModel` with tabs, `DocumentRegistry` shared buffers, one
  `VimEditorBehavior` per pane.
- The ported engine keeps registers and the last search **per engine instance**. With one engine per
  pane, a yank in one pane could not be put in another. Vim's registers and search pattern are
  global.

## Decisions

| Decision | Rationale |
|---|---|
| Port `LayoutTree` and nodes verbatim into `Enigma.TextEditor.Core.Layout`, with their tests | Proven split model; pure and testable |
| `WorkspaceViewModel` owns the `LayoutTree` and one `EditorPaneViewModel` per `PaneId`; `SplitHost` renders it | FileExplorer's shape |
| A new split shows the active tab's buffer, at the same caret | Vim `:sp` shows the same buffer |
| Closing a pane closes its tabs through the unsaved-changes rule; a buffer still shown elsewhere is not asked about | Consistent with FEATURE-3EE8 |
| The active pane is framed when there is more than one, and the status bar follows the active pane | FileExplorer's framing; one status bar |
| View menu: Split right, Split down, Close pane, Only this pane, Equalize panes, Focus next pane, Focus previous pane | Mouse and menu access to every split action |
| `Ctrl+W` prefix handled in the engine's command modes; the next key maps to a `VimWindowCommand` raised on `WindowCommandRequested` | Testable in Core; insert mode passes Ctrl+W to the editor, as Vim's delete-word does |
| Window keys: `h j k l` (and arrows) focus by direction, `w`/`W` next/previous, `s`/`S` split down, `v` split right, `c` close pane, `q` = `:q`, `o` only, `=` equalize, `+ - < >` resize by 0.05 | Vim's set, minus features with no meaning here |
| Ex: `:sp[lit] [path]`, `:vs[plit] [path]`, `:new`, `:vnew`, `:on[ly]`, `:clo[se]` | Vim's window ex commands |
| `VimSharedState` (singleton) holds the registers, the last search and the last find; every engine takes it | Vim semantics; a yank or search in one pane serves every pane |

## PHASE01 — Layout tree & split host

**Status:** TODO
**Branch:** `feature/feature-04bc-phase01-split-host`

### Steps

1. Port `Core/Layout/*` (namespace `Enigma.TextEditor.Core.Layout`) and FileExplorer's
   `LayoutTreeTests`.
2. `WorkspaceViewModel`:
   - panes by id, `ActivePane`;
   - `SplitAsync(axis)` opens the active tab's buffer in the new pane;
   - `ClosePaneAsync()` runs the unsaved-changes rule for buffers not shown elsewhere;
   - `Only()`, `Equalize()`, `Focus(direction)`, `FocusNext/Previous()`, `Resize(axis, delta)`;
   - the `LayoutChanged` batch semantics.
3. Move the File/Edit/Tools commands and the status bar onto `Workspace.ActivePane`. Tab-closing `:q`
   on the last tab of a pane closes the pane; the app quits only when it is the last pane.
4. `SplitHost` (ported):
   - pointer press or focus inside a pane activates it;
   - focus follows the active pane;
   - the splitter is styled 1px with the Enigma border brush;
   - an active-pane frame shows when there are several panes.
5. View menu items as decided.
6. Tests:
   - the ported layout tests;
   - workspace split/close/only/focus/equalize against a fake pane factory;
   - a shared buffer: an edit in pane A shows in pane B;
   - closing a pane with a dirty buffer shown elsewhere does not prompt;
   - headless: split right shows two editors; the status bar follows the clicked pane.

### Acceptance criteria

- Panes split right and down to any depth, resize by dragging, close, keep only the active one,
  equalize and take focus from the menu or a click.
- Both panes of a split edit the same buffer live.
- Zero warnings, whole suite green.

## PHASE02 — Vim window commands & shared state

**Status:** TODO
**Branch:** `feature/feature-04bc-phase02-vim-window-commands`

### Steps

1. Core, `VimSharedState`:
   - `Registers`, `LastSearch` (pattern, direction, smartcase) and `LastFind` move from engine fields
     into it;
   - `VimEngine(buffer, options, shared)`, where `shared` is optional and a test default creates a
     private one;
   - every ported test still passes.
2. Core, window commands:
   - `VimWindowCommand` enum;
   - `Ctrl+W` in normal and visual modes sets a pending window prefix, and the next key maps through
     `VimWindowKeys` and raises `WindowCommandRequested`;
   - an unknown key cancels silently;
   - `HasPendingSequence` covers the prefix.
3. Core, ex commands: `VimExCommand.Split` / `VerticalSplit` (optional path), `New` / `VerticalNew`,
   `Only`, `Close`.
4. Desktop:
   - `WindowCommandHandler` maps window commands and ex requests to `WorkspaceViewModel`;
   - `VimSharedState` is registered as a singleton and handed to each pane's engine;
   - only the active pane's engine reports mode and status to `VimSession`.
5. Tests:
   - Core: `Ctrl+W` + every mapped key; insert-mode `Ctrl+W` is not taken; the parser's new ex
     commands;
   - shared registers across two engines (`yy` in one, `p` in the other);
   - shared `n` across engines;
   - Desktop: handler mapping against a fake workspace.

### Acceptance criteria

- `Ctrl+W v` / `Ctrl+W s` split, `Ctrl+W h/j/k/l` move focus, `Ctrl+W c`/`q`/`o`/`=` behave as
  decided.
- `:sp`, `:vs`, `:sp file`, `:new`, `:vnew`, `:only` and `:close` work.
- A yank in one pane can be put in another; `n` in another pane repeats the last search.
- Zero warnings, whole suite green.

## Out of scope

- Persisting the layout across restarts (no session restore in this run).
- Moving tabs between panes by drag and drop.
- Vim tab pages (`:tabnew`, `gt`).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Split engine | Port FileExplorer's `LayoutTree` + `SplitHost` | Asked for explicitly ("use the same as Enigma.FileExplorer"); proven | Enigma.Avalonia `DockingHost` (IDE docking, not the requested model) |
| Content of a new split | The active tab's buffer | Vim `:sp` semantics; immediate comparison of two places in one file | An empty pane (extra step for the common case) |
| Where `Ctrl+W` is decoded | In the engine, as a pending prefix | The engine already arbitrates every key; testable without Avalonia | In the Avalonia behaviour (untestable state, duplicates pending-key logic) |
| Scope of registers and search | Shared across panes | Vim semantics; per-pane registers would break yank/put across a split | Per-engine (as ported) |
| Window keys and ex commands included | Focus, split, close, only, equalize, resize, `:sp`/`:vs`/`:new`/`:vnew`/`:on`/`:clo` | Vim's everyday window set | Exchange/rotate (`Ctrl+W x/r`), low value |

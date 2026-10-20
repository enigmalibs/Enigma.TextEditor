# FEATURE-4A9A — Split opens a new document

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-4a9a-split-new-document`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

Splitting a pane always opens a new untitled document in the new pane. This holds whether the split
pane was showing an untitled document or a file. The two panes no longer edit the same text after a
split.

## Context & constraints

- `WorkspaceViewModel.Split(axis, showActiveBuffer = true)` shows the active buffer in the new pane
  (Vim's `:split`), copying caret and scroll. Both halves edit one text.
- Entry points without a file:
  - View → Split Right / Split Down (`SplitRightCommand`/`SplitDownCommand`);
  - `Ctrl+W s`/`S`/`v` (`WindowCommandHandler`);
  - `:sp`/`:vs` without a file (`ExCommandHandler.SplitAsync`).
- `:new`/`:vnew` already call `Split(axis, showActiveBuffer: false)` then `NewDocument()`.
- `:sp file`/`:vs file` split empty, then open the file, and close the pane again if it cannot be opened
  (BUG-52A2).
- BUG-52A2 also made the split commands require a tab, so a split never leaves an empty pane beside
  open ones.
- Many tests use `Workspace.Split` to build layouts, and several assert that both panes share one
  document.

## Decisions

| Decision | Rationale |
|---|---|
| `Split(axis, newDocument = true)`: the new pane gets a new untitled document and takes the keyboard; `newDocument: false` gives an empty pane for `:sp file` | One method, one meaning for every user-facing split; the file case stays explicit |
| Applies to every split entry point without a file (menu, `Ctrl+W`, `:sp`/`:vs` alone) and to hex tabs | The user asked for "when splitting panes", without exception |
| `:sp file` / `:vs file` unchanged | They name what to open |
| `:new` / `:vnew` keep working; they are now the same as `:sp` / `:vs` | Their meaning was always "split with a new buffer" |
| The split commands still need a tab | With no tab, the old pane would stay empty beside the new one, breaking BUG-52A2's rule |
| The caret/scroll copy goes | There is no shared text to line up |
| Showing one buffer in two panes stays possible (`:sp file` on an open file, `:e file`) | The registry's view counting is unchanged |

## Steps

1. `Desktop/ViewModels/WorkspaceViewModel.cs`:
   - `Split` as decided;
   - class remarks and the split comments rewritten.
2. `Desktop/ViewModels/ExCommandHandler.cs`:
   - `SplitAsync` without a file → `Split(axis)`;
   - `:new`/`:vnew` → `Split(axis)`;
   - `:sp file` → `Split(axis, newDocument: false)`.
3. `Desktop/ViewModels/WindowCommandHandler.cs`: the class remark matches.
4. `Desktop/ViewModels/HelpCatalog.cs`: the split lines say "with a new buffer".
5. Tests:
   - new: splitting a file tab gives the new pane a different, untitled document and leaves the
     original in the first pane;
   - new: splitting an untitled tab does the same, with a second untitled document;
   - new: splitting a hex tab does the same;
   - new: `Ctrl+W v`, `Ctrl+W s`, `:sp`, `:vs` alone and View → Split do the same;
   - updated: existing tests that asserted a shared document now assert the new behaviour; tests that
     only needed a second pane over the same buffer build it with `:sp {file}` or `Show`;
   - headless: Split Right shows two editors over two different documents.

## Acceptance criteria

- Every split without a file opens a new untitled document in the new, active pane; the original pane
  keeps its tab.
- `:sp file` / `:vs file` open that file in the new pane, as before.
- Closing the split pane with an untouched untitled document asks nothing.
- Zero warnings, whole suite green.

## Out of scope

- A setting to choose between both behaviours.
- Moving or cloning the tab into the new pane.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which splits | Every split without a file, text or hex | The draft says "when splitting panes, force open a new document" | Only the View menu |
| Keep the "needs a tab" guard | Yes | Without a tab the source pane would be left empty beside the new one (BUG-52A2) | Always enabled |
| `:new`/`:vnew` | Kept as aliases of `:sp`/`:vs` | No reason to break Vim muscle memory | Remove them |

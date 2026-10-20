# FEATURE-4A9A — Split opens a new document

**Item:** FEATURE-4A9A — Split opens a new document
**Branch:** `feature/feature-4a9a-split-new-document`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

A split now opens a new untitled buffer in the new pane, which takes the keyboard. This holds whatever
the split pane was showing — a file, an untitled buffer or a hex view — so the two panes no longer
start out editing one text.

- **Where:** `WorkspaceViewModel.Split(axis, newDocument = true)` creates the pane and calls
  `NewDocument()` in it. `newDocument: false` gives an empty pane, for `:sp file` / `:vs file`, which
  open that file there as before.
- **Entry points:** every split without a file goes through it:
  - View → Split Right / Split Down;
  - `Ctrl+W s`, `S`, `v`;
  - `:sp` / `:vs` alone;
  - `:new` / `:vnew`, which are now the same as `:sp` / `:vs`.
- **Removed:** the caret/scroll copy into the new view, since there is no shared text to line up.
- **Kept:**
  - A buffer can still be shown in two panes, by opening it in both (`:sp file` on an open file,
    `:e file`, `Show`). The registry's view counting is unchanged.
  - The split commands still need a tab: with none, the pane split would stand empty beside the new
    one (BUG-52A2's rule).
- **Help:** the `:sp` / `:vs` lines now say "with a new buffer".

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `Split`, the class remarks, the
  split commands' comment.
- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — `:new`/`:vnew` call `Split(axis)`;
  `:sp file` calls `Split(axis, newDocument: false)`.
- `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs` — `Split` / `VerticalSplit` remarks.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the two split lines.
- `tests/…/ViewModels/SplitPaneTests.cs`:
  - new: splitting a file, an untitled buffer and a hex tab each opens a new untitled text buffer;
  - the shared-buffer tests now open the buffer in the second pane explicitly;
  - the file-commands test expects the new pane's untitled tab.
- `tests/…/ViewModels/WindowCommandTests.cs`:
  - new: `:sp`, `:vs`, `:new`, `:vnew` alone; `Ctrl+W s`/`v`; Split Right/Down — each opens a new
    buffer;
  - the typed yank/put test now yanks in the left pane and puts in a file opened in the new one.
- `tests/…/Views/SplitHostTests.cs`:
  - Split Right shows two editors over two documents;
  - quitting a split's second tab leaves its untitled tab.
- `tests/…/Views/HexViewTests.cs` — splitting a hex tab leaves the hex view in the first pane and a
  text buffer in the new one.
- `tests/…/Views/SearchHighlightTests.cs`, `tests/…/ViewModels/LineEndingAndIndentationToolsTests.cs` —
  the "in every pane" tests open the buffer in the second pane explicitly.
- `tests/…/ViewModels/FileDropTests.cs` — the pane not dropped on keeps only its own new buffer.
- `tests/…/Views/FileDropViewTests.cs` — `showActiveBuffer: false` renamed `newDocument: false` (same
  empty pane).
- `docs/roadmap.md`, `docs/plan/FEATURE-4A9A.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Tests that used a split to put one buffer in two panes | Build the sharing explicitly with `Workspace.Show(document)` in the new pane | Their subject (highlight, indentation, closing a shared pane) is still valid; only their setup leaned on the old split | Deleting them; keeping a hidden "same buffer" split mode |
| Tests that asserted the old split behaviour | Rewritten to the new contract | The behaviour change is the feature | — |
| Parameter name | `newDocument` (was `showActiveBuffer`) | Says what the flag does now | Keeping the old name with a new meaning |

## Deviations & follow-ups

- Step 3 of the plan (the `WindowCommandHandler` remark) needed no change: it maps `Ctrl+W v` to Split
  Right, which is still true.
- Fix cycles: 1. The first run had a compile error (`FileDropViewTests` used the old parameter name)
  and then four tests that relied on a split sharing the buffer. Each was adapted as above; green
  after.
- Follow-up suggestion: an explicit "Duplicate view" command (the old split), if showing one file in
  two panes turns out to be missed.
- Line endings: one rewritten test file briefly came out CRLF from a Python rewrite and was put back
  to LF before staging; no churn in the commit.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the in-app help was updated as part of the dev.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2491 passed**, 0 failed, 0 skipped (2483 before,
  net +8).

# BUG-52A2 — Empty pane after closing a split

**Item:** BUG-52A2 — Empty pane after closing a split
**Branch:** `bugfix/bug-52a2-empty-pane-on-close`
**Run:** vibe/2026-10-20-split-close-statusbar

## Summary

A pane no longer stays on screen showing "No file is open" while other panes are open.

**The bug:** take a pane with several tabs and close its active tab with `:q`, `Ctrl+W q`, the tab's
close button or `Ctrl+F4`.

- The pane kept its other tabs, but none was showing: `ActiveTab` was `null`.
- The placeholder took the pane's place, and the tab strip, which is only shown with a tab, went too.
- `:q` could not reach that pane any more (the editor is hidden), so it stayed.
- Doing the same in several splits left several such panes.

**The cause:**

- The tab strip is a `ListBox` whose selection is bound two-way to `ActiveTab`.
- `EditorPaneViewModel.Remove` took the tab off the strip first. The list saw its selected item go
  and wrote `null` back into `ActiveTab`.
- `Remove` then no longer recognised the removed tab as the active one, and never showed a
  neighbour.
- The view-model tests never saw it: no list is attached to the view-model there.

**The fix:** `Remove` shows the neighbour (right, else left) *before* the tab leaves the strip. The list
never loses its selection, and every close path benefits. `:q` keeps its documented meaning:

- in a pane with other tabs, it closes the tab and the neighbour shows;
- on a split's last tab, it closes the split, and the remaining panes take its space;
- on the last tab anywhere, it quits.

**The same rule, on two other paths:** the placeholder now appears only when a single pane has no tab.

- `:sp file` / `:vs file` on a file that cannot be opened (missing, binary question cancelled,
  unreadable) closes the pane it had just split off. The pane it came from takes the keyboard back.
  The error is still reported as before.
- *View → Split Right / Split Down* are disabled while the active pane shows nothing. Before, they
  added a second empty pane.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Desktop/ViewModels/EditorPaneViewModel.cs` — `Remove` shows the neighbour
  first; the remark says why.
- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — `SplitAsync` closes the new pane
  when its file could not be opened and the pane is still empty.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `SplitRightCommand` /
  `SplitDownCommand` can execute only with an active tab; refreshed with the other commands.
- Tests:
  - `ViewModels/EditorPaneViewModelTests.cs`: the neighbour is showing as the tab leaves, against a
    strip that clears its selection (1 case);
  - `ViewModels/SplitPaneTests.cs`: `:sp missing.txt` and `:vs missing.txt` leave one pane, the
    original active (2 cases); the split commands need a tab (1);
  - `Views/SplitHostTests.cs`, in the real window: `:q` on a two-tab split shows the other tab, with
    the strip and editor, not the placeholder; `:q` on a split's last tab leaves one pane, as wide as
    the host (2).
- `docs/roadmap.md`, `docs/plan/BUG-52A2.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which neighbour | Right, else left, as before | Unchanged behaviour; only the order of the two steps moved | — |
| Guard the `:sp file` clean-up | Close only if the new pane is still empty | A dialog during the open (binary question) gives the user time to put something in it | Closing it unconditionally |
| Leave `Replace`'s double assignment | Untouched | It already copes with the same effect, and its tab is not the active one when it calls `Remove` | Rewriting it (out of scope) |

## Deviations & follow-ups

- None from the plan.
- **Evidence the bug was reproduced:** a throw-away headless probe, before the fix: two-tab right pane,
  active tab closed → `rightTabs=1 rightActive=NULL`. It was deleted, and the two `SplitHostTests`
  above now cover the scenario.
- Follow-up, not done: `:q` closes a tab, not the whole pane as Vim's `:q` closes a window. This is the
  documented design (FEATURE-3EE8/04BC), kept on purpose.
- Line endings: the touched files keep their endings; no CRLF churn.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, there is no `CLAUDE.md`
or `AGENTS.md`, and the help catalog lists *Split Right/Down* without the condition, which needs no
mention.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (no AVLN warnings).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2442 passed**, 0 failed, 0 skipped (2436 before,
  plus 6 new).
- Not done: no manual check in the running Debug build. The scenario runs headlessly in the real window.

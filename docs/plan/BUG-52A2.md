# BUG-52A2 — Empty pane after closing a split

**Status:** TODO
**Type:** BUG
**Branch:** `bugfix/bug-52a2-empty-pane-on-close`
**Run:** vibe/2026-10-20-split-close-statusbar

## Objective

Closing a split must always rearrange the panes. The placeholder "No file is open. Use File → Open… or
File → New." must appear only when nothing is open at all: one pane, with no tab.

## Context & constraints

- Reported: after closing a split, the pane sometimes turns into the placeholder while the other panes
  stay open. It can happen to several panes.
- Reproduced headlessly in the real window. Take a pane with two tabs and close its active tab (`:q`,
  `Ctrl+W q`, the tab's close button, `Ctrl+F4`). The pane keeps one tab, but its `ActiveTab` is
  `null`.
  - The placeholder shows, and the tab strip is hidden: it is bound to `HasActiveTab`.
  - `:q` in that pane does nothing more (`Quit when tab is not null`).
- Cause:
  - The strip is a `ListBox` whose `SelectedItem` is bound two-way to `EditorPaneViewModel.ActiveTab`.
  - `EditorPaneViewModel.Remove` takes the tab off `Tabs` first. The `ListBox` sees its selected item
    leave and writes `null` back into `ActiveTab`.
  - `Remove` then checks `ReferenceEquals(ActiveTab, tab)`. That is now false, so no neighbour is
    activated.
  - `Replace` already works around the same effect: it assigns `ActiveTab` again after `Remove`.
  - Unit tests on the view-model alone never see it: no `ListBox` is attached.
- The same rule is broken by two other paths:
  - a `:sp file` / `:vs file` whose open fails (missing file, binary prompt cancelled) leaves the new
    pane empty beside the old one;
  - *View → Split Right/Down* from an empty last pane creates a second empty pane.
- Documented design, kept:
  - `:q` closes the active tab;
  - a pane that loses its last tab closes, unless it is the only pane;
  - `:q` on the last tab anywhere quits (FEATURE-3EE8, FEATURE-04BC).

## Decisions

| Decision | Rationale |
|---|---|
| `EditorPaneViewModel.Remove` activates the successor (right neighbour, else left, else none) *before* the tab leaves `Tabs` | Fixes the root cause. A bound tab strip never sees its selected tab removed. Works for every close path |
| `:q` keeps its meaning: close the tab, and the pane with its last tab | Documented design. A pane with other tabs now shows the neighbour, not the placeholder |
| `:sp file` / `:vs file` close the new pane again when the file could not be opened (and the pane is still empty) | The new pane exists only to show that file |
| *Split Right* / *Split Down* need a tab to show (`CanExecute`) | A split shows the active buffer. With none, it would only add an empty pane |

## Steps

1. `ViewModels/EditorPaneViewModel.cs` — `Remove`: when the tab is the active one, set `ActiveTab` to
   its successor first, then remove it.
2. `ViewModels/ExCommandHandler.cs` — `SplitAsync`: keep the pane `Split` returns. If `OpenAsync`
   returns `null` and the pane is still empty, close it through `ClosePaneAsync`. Its neighbour, the
   pane it was split from, takes the keyboard back.
3. `ViewModels/WorkspaceViewModel.cs` — `SplitRightCommand` / `SplitDownCommand` can execute only with
   an active tab, refreshed in `RefreshCommands`.
4. Tests:
   - headless, in the real window: closing the active tab of a two-tab pane in a split shows the other
     tab, with the strip visible;
   - headless, in the real window: `:q` in a split with one tab removes that pane and its editor;
   - view-model: while the active tab leaves `Tabs`, `ActiveTab` already names the neighbour
     (observed from `CollectionChanged`);
   - `:sp missing.txt` and `:vs missing.txt` leave one pane, the original one active;
   - the split commands cannot execute with no tab; they can with one;
   - the whole existing suite stays green.

## Acceptance criteria

- Closing a tab in a pane with other tabs shows a neighbour, never the placeholder.
- Closing a split's last tab, or the split itself, removes it and the remaining panes take its space.
- A failed `:sp file` / `:vs file` leaves no empty pane behind.
- The placeholder appears only with a single pane that has no tab.
- Zero warnings, whole suite green.

## Out of scope

- Making `:q` close a whole pane with all its tabs (Vim's window semantics).
- `:sp newfile` creating a buffer for a file that does not exist yet (`:e newfile` refuses it too).
- The transient `null` during `Replace` (reopen as hex/text); it is never rendered.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where to fix the empty pane | Successor-first removal in `EditorPaneViewModel.Remove` | Root cause; one place; every close path benefits | Ignoring `null` in the `ActiveTab` setter (the strip would show no selection while the editor shows a tab); making the binding one-way (clicking a tab must select it) |
| Should `:q` close the whole split | No: it closes the tab, and the pane goes with its last tab | Documented in FEATURE-3EE8/04BC. One `:q` closing several tabs would surprise users and stack unsaved-changes questions | Vim's window semantics (reverses a documented decision) |
| Enforce the rule beyond tab closing | Yes, for a failed `:sp file` / `:vs file` and for splitting an empty pane | The user's rule: the placeholder only when nothing is open | Fixing only the tab-close path (both cases still break the rule) |
| Split from an empty pane | Disable *Split Right/Down* with no active tab | No buffer to show; the Vim keys cannot reach an empty pane anyway | Opening an untitled buffer in the new pane (invents content) |
| Phasing | One dev | Small, one reviewable commit | — |

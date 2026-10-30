# FEATURE-9E4A-PHASE01 — Split a pane with a moved tab

**Item:** FEATURE-9E4A — Drop a tab on an edge to split
**Branch:** `feature/feature-9e4a-phase01-split-with-tab`
**Run:** feature/2026-10-30-drag-drop-split

## Summary

This phase adds the model under "drop a tab on a pane's edge to split it". There is no UI yet: PHASE02 wires it to the
drag.

- **Core:** `LayoutTree.Split(PaneId pane, FocusDirection side)` splits a pane half each.
  - The new pane goes on the given side. Left and above become the split's first side; right and below its second.
  - The existing `Split(pane, SplitAxis)` now calls it with `Right` or `Down`, so its behaviour is unchanged.
- **Workspace:** `CanSplitWithTab(target, tab)` and `SplitWithTab(target, tab, side)`.
  - The split and the move run in one batch. The target's leaf is split on that side, a pane is added, and the tab
    goes through the existing `MoveTab`. That keeps the tab instance (caret, scroll), gives the new pane the keyboard,
    closes a source left empty, refreshes commands and raises `DocumentShown`.
  - Refused, changing nothing, in three cases:
    - the tab is on no pane;
    - the target is not in the workspace;
    - the target is the tab's own pane and the tab is its only one.
- **Seam:** `IPaneHost.CanSplitWithTab` / `SplitWithTab`, reached through `EditorPaneViewModel.CanSplitWithTab(tab)` /
  `SplitWithTab(tab, side)`, alongside `CanTakeTab` / `TakeTab`.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Layout/LayoutTree.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`, `IPaneHost.cs`,
  `EditorPaneViewModel.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Layout/LayoutTreeTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/EditorPaneViewModelTests.cs` (test host gains the two members)
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SplitWithTabTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-9E4A.md`; created this file

## Deviations & follow-ups

- None from the plan. The new workspace tests went into their own class (`SplitWithTabTests`), which the plan allowed.
- **Baseline** (run branch `feature/2026-10-30-drag-drop-split` at `5226bec`, before the first dev branch): 0 warnings,
  2875 tests passed, 0 failed. Later devs of this run cite this doc.
- Documentation sweep: no user-facing behaviour changed yet (PHASE02 adds the gesture, help row and README); no
  edit.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the axis overload relates to the new one | It delegates (`Columns` → `Right`, `Rows` → `Down`) | One construction path; existing callers and tests unchanged |
| How the tab gets into the new pane | Through the existing `MoveTab`, inside the split's batch | Keeps a single rule set for moves (registry, keyboard, closing an empty source, `DocumentShown`) |
| Where the "own pane keeps a tab" rule lives | `CanSplitWithTab`, which `SplitWithTab` checks first | The view (PHASE02) asks the same question for the hint that the release relies on |
| Test placement | A new `SplitWithTabTests` class | Keeps `MoveTabTests` about moves; the new class reads as the feature's contract |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2890 passed, 0 failed, 0 skipped (+15).
- **Tests added:**
  - `LayoutTreeTests`: each of the four sides gives the new and the old pane their half (theory, 4 rows); a left
    split nested inside a column split; one `Changed` per split.
  - `SplitWithTabTests`:
    - another pane's tab splits the target on each side (theory, 4 rows): the neighbour on that side is the new
      pane, which holds the tab with its caret and the keyboard;
    - a source pane's last tab closes that pane;
    - a pane with two tabs splits itself;
    - a pane's only tab is refused;
    - a buffer the target also shows still moves, and its view count is unchanged;
    - a closed tab or a pane outside the workspace is refused.

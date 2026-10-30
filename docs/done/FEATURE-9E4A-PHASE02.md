# FEATURE-9E4A-PHASE02 — Edge drop zones and their hint

**Item:** FEATURE-9E4A — Drop a tab on an edge to split
**Branch:** `feature/feature-9e4a-phase02-edge-drop-zones`
**Run:** feature/2026-10-30-drag-drop-split

## Summary

The tab drag now splits as well as moves.

- **Moving:**
  - a tab let go over a pane's strip, or the middle of its content, moves there, as before;
  - the whole pane is highlighted while the tab is over it.
- **Splitting:**
  - a tab let go near an edge of a pane's content splits that pane on that side, with the tab in the new half
    (PHASE01's `SplitWithTab`). This works on any pane, the tab's own included, as long as that pane keeps another
    tab;
  - "near an edge" means within the outer quarter of the content's width or height;
  - while the tab is there, only the half the new pane will take is highlighted.
- **`TabDragGesture`:**
  - `EdgeShare` (0.25);
  - `SideFor(point, content)` is the pure rule. It returns the side whose outer quarter holds the point, the nearer
    side in a corner (as a share of width or height), and `null` for the middle, on the boundary, or for empty
    content.
- **`EditorPaneView`:**
  - **Locating the side:** `SideAt(point)` answers `null` over the visible strip (now named `Strip`). Elsewhere it
    applies `SideFor` to the content below the strip, or to the whole pane when no tab hides the strip.
  - **Showing the hint:**
    - `ShowDropHint(side)` / `HideDropHint()` place the existing `DropHint` inside a new non-hit-testable 2 × 2
      star grid: all four cells for a move, the two cells on the side for a split;
    - `IsDropTarget` is now read-only, and `DropSide` tells which half is shown;
    - the hint keeps the existing look: accent wash plus ring.
  - **Steering:** `SteerTab` and the release share `DropUnder` (pane and side under the pointer) and `Takes`
    (`CanTakeTab` for the middle, `CanSplitWithTab` for a side). The hint is re-placed only when the pane or side
    changes.
  - **The release** posts `TakeTab` or `SplitWithTab`. Escape and a lost capture are unchanged.
- **Help:** a new Panes row, "Drag a tab to a pane's edge — Split that pane there, the tab in the new half".
- **README:** the *Tabs and panes* paragraph describes the edge drop and its highlight.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/TabDragGesture.cs`, `EditorPaneView.axaml`,
  `EditorPaneView.axaml.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`, `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabDragViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-9E4A.md`; created this file

## Deviations & follow-ups

- None from the plan.
- **Not seen in a real window:**
  - the half-pane hint is checked by geometry in headless tests (the hint's laid-out rectangle against the pane's), not
    by eye;
  - a glance in both themes, and a feel for whether a quarter is the right edge band on very wide panes, are worth a
    follow-up.
- **Release notes** for this feature are left to the next release item, as planned.
- Baseline: as recorded in `docs/done/FEATURE-9E4A-PHASE01.md` (0 warnings, 2875 passed).
- **Documentation sweep:**
  - README *Tabs and panes* and the help's Panes rows were edited as planned (listed above);
  - the README's "What's new in 1.10" line describes the released 1.10 and was left alone;
  - nothing else describes the drag.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the half is drawn | `DropHint` inside a 2 × 2 star grid over the pane, its row/column spans set per side | Layout-driven: follows pane resizes without code reading sizes, and keeps the hint one element with one look |
| Where the strip ends | `Strip.TranslatePoint` of its bottom, in pane coordinates; 0 when the strip is hidden | Exact whatever the strip's height (it has a `MinHeight`, and grows with the font); an empty pane is all content |
| Ties in a corner | The first side in the order left, right, up, down | Any fixed order is fine on an exact tie; the plan only asks for "the nearer side" |
| Re-marking on every move | Only when the pane or the side changes | Avoids re-laying the hint on every pointer move; mirrors the old pane-only check |
| `IsDropTarget` | Kept, read-only; `DropSide` added | Existing tests read it; the setter had only one caller, now replaced by `ShowDropHint` / `HideDropHint` |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2911 passed, 0 failed, 0 skipped (+21).
- **Tests added** (`TabDragViewTests`, real window, headless mouse, unless noted):
  - the hint over another pane's right, up, down and left edges covers that half, `DropSide` names it, and the hint
    covers the whole pane in its middle (rectangles checked to within 2 px);
  - a release near another pane's right edge gives three panes, with the tab alone in the new right-hand neighbour
    and the keyboard there;
  - a release near its own pane's bottom edge, with a second tab, splits that pane below;
  - a pane's only tab near its own edge shows nothing and splits nothing;
  - a release on another pane's strip near its left end moves the tab (no split);
  - Escape near an edge clears the half and splits nothing;
  - `SideFor` (theory, 14 rows, called directly): the middle, each side, corners, the exact boundaries, and
    non-square content; empty content has no sides.
- The existing drag tests (middle and strip drops, cursor, Escape, lost capture) pass unchanged.

# FEATURE-9E4A — Drop a tab on an edge to split

**Status:** DONE
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-9e4a-phase01-split-with-tab`
**Branch (PHASE02):** `feature/feature-9e4a-phase02-edge-drop-zones`
**Run:** feature/2026-10-30-drag-drop-split

## Objective

Dragging a tab onto another pane moves it there (FEATURE-9F56, BUG-5A45), and the pane it would land on is
highlighted. Make the same gesture able to **split**, on any pane, the tab's own included:

- released over a pane's **tab strip** or the **middle** of its content, the tab **moves** there, as today, and the
  whole pane is highlighted, as today;
- released over the **top, right, bottom or left** part of a pane's content, that pane **splits** on that side and
  the tab goes into the new pane; while the tab is carried there, only that side of the pane is highlighted, showing
  where the new pane will be.

## Context & constraints

- **Gesture** (`Views/EditorPaneView.axaml.cs`): the pane a drag starts from captures the pointer on its `Tabs`
  strip and, on every move, hit-tests the top level for the `EditorPaneView` under the pointer (`PaneUnder`). It marks
  that pane (`MarkDropTarget` → `IsDropTarget`) when its ViewModel takes the tab (`CanTakeTab`). On release it posts
  `TakeTab(tab)`. Escape and a lost capture cancel. Every end clears the mark.
- **Hint** (`EditorPaneView.axaml`): `DropHint`, a non-hit-testable `Panel` (accent wash `Panel.dropFill` under a
  2 px ring `Border.dropRing`) spanning both rows of the pane's grid: strip and content.
- **Pure decisions** live in `Views/TabDragGesture.cs` (`IsDrag`, `CursorFor`), tested directly.
- **Strip:** a `Border` (row 0, `IsVisible="{Binding HasActiveTab}"`) holding the `Tabs` list box. With no tab, the
  pane is all content (the "No file is open" placeholder).
- **Layout** (`Core/Layout/LayoutTree.cs`): `Split(PaneId, SplitAxis)` always puts the new pane second (right or
  below), at a 0.5 ratio. `SplitNode(axis, first, second, ratio)` and `Attach` build any shape. `FocusDirection`
  (`Left`, `Down`, `Up`, `Right`) already names sides for `FindNeighbor` and `MoveTab`.
- **Workspace** (`ViewModels/WorkspaceViewModel.cs`): `Split(axis, newDocument)` splits the *active* pane.
  `MoveTab(source, tab, target)` moves the tab instance (caret and scroll kept). It shows an existing tab instead when
  the target already has the buffer, activates the target, closes a source left empty, refreshes commands and raises
  `DocumentShown`. `Batch` groups layout changes into a `LayoutChanged`. `PaneOf(tab)` finds a tab's pane.
- **Host seam:** `IPaneHost.CanMoveTabTo` / `MoveTabTo`, reached through `EditorPaneViewModel.CanTakeTab` /
  `TakeTab`.
- **Help:** `HelpCatalog` row `Keys("Drag a tab onto another pane", "Move it there; Escape cancels the drag")`.
  **README** *Tabs and panes* paragraph mentions dragging. `RELEASENOTES.md` belongs to release items.
- **Tests:**
  - Core: `LayoutTreeTests`.
  - Desktop: `MoveTabTests` (workspace, headless fixture), `TabDragViewTests` (real window, headless mouse:
    `Carry`, `Middle`, `OnStrip`, `DrainAsync`).
  - Existing drag tests drop on a pane's middle or strip, so they must pass unchanged.
- **Baseline:** taken on the run branch before the first dev.

## Decisions

- **Zones:** the strip is always "move". The content area below it is divided by `TabDragGesture`:
  - a point within the outer **quarter** of the content's width from the left or right edge, or of its height from
    the top or bottom edge, is that side;
  - in a corner, where two sides qualify, the side the point is nearer to wins, measured as a fraction of the
    content's width or height;
  - everything else is the middle.
- **The split hint** covers the **half of the pane** the new pane will take, strip included. The split is even, so the
  hint matches the new pane. The look is unchanged: same wash and ring.
- **Own pane:**
  - the middle still does nothing, as today;
  - an edge splits only when the pane keeps a tab after the move. A pane's only tab dropped on its own edge would
    leave that pane empty, and it would close: the layout would not change. So it is not offered (no hint, the
    release does nothing).
- **After a split:**
  - the new pane takes the keyboard and shows the tab, with its caret and scroll;
  - a source pane left empty closes;
  - a buffer the target pane also shows is still split out. The new pane is empty, so nothing is duplicated within one
    pane.
- **Layers:**
  - Core gains `LayoutTree.Split(PaneId pane, FocusDirection side)`, and the existing overload delegates to it;
  - the workspace gains `SplitWithTab`;
  - the host seam gains `CanSplitWithTab` / `SplitWithTab`, reached through `EditorPaneViewModel`.

## PHASE01 — Split a pane with a moved tab

**Status:** DONE

### Steps

1. **Core — `LayoutTree.Split(PaneId pane, FocusDirection side)`:**
   - splits the pane in two, half each, and puts the new pane on that side: `Left` and `Right` split into columns,
     `Up` and `Down` into rows. `Left` and `Up` make the new pane the split's first side;
   - raises `Changed` once and returns the new pane;
   - `Split(PaneId, SplitAxis)` becomes `Split(pane, axis == Columns ? Right : Down)`, so its behaviour is unchanged.
2. **Workspace — `WorkspaceViewModel`:**
   - `CanSplitWithTab(EditorPaneViewModel target, EditorTabViewModel tab)`: the tab is on a pane of the workspace,
     and the target is in it. When the target is that pane, it has another tab;
   - `SplitWithTab(EditorPaneViewModel target, EditorTabViewModel tab, FocusDirection side)`:
     - returns `null` and changes nothing when `CanSplitWithTab` is false;
     - otherwise, in one batch: splits the target's leaf on that side, adds the new pane, then moves the tab there
       (the existing `MoveTab` path: the new pane takes the keyboard, the source closes if left empty, commands
       refresh, `DocumentShown` is raised);
     - returns the new pane.
3. **Host seam:**
   - `IPaneHost.CanSplitWithTab(target, tab)` / `SplitWithTab(target, tab, side)`, documented like
     `CanMoveTabTo` / `MoveTabTo`;
   - `EditorPaneViewModel.CanSplitWithTab(tab)` / `SplitWithTab(tab, side)`, like `CanTakeTab` / `TakeTab`;
   - every test double implementing `IPaneHost` follows.
4. **Tests:**
   - Core `LayoutTreeTests`:
     - each side puts the new pane there: `Geometry()` left/right/top/bottom half;
     - it nests inside an existing split;
     - `Changed` is raised once;
     - the axis overload still puts the new pane right and below;
   - Desktop workspace (`MoveTabTests` or a new `SplitWithTabTests`):
     - another pane's tab, split to each side: the new pane stands on that side of the target, holds the tab
       instance (caret kept) and the keyboard;
     - the source's last tab, split beside another pane: the source closes;
     - own pane with two tabs, split right: two panes, the tab in the new one, the other tab left behind;
     - own pane with one tab: refused, nothing changes;
     - a buffer the target also shows: it moves to the new pane, and the registry view count is unchanged;
     - a tab no longer in the workspace, or a target not in it: refused.

### Acceptance criteria

- `LayoutTree.Split(pane, side)` puts the new pane on any of the four sides. The axis overload behaves as before.
- `SplitWithTab` splits the target on that side, moves the tab instance into the new pane, gives it the keyboard and
  closes a source left empty. It refuses, changing nothing, a self-split of a pane's only tab and a tab or target
  outside the workspace.
- Zero warnings, whole suite green.

## PHASE02 — Edge drop zones and their hint

**Status:** DONE

### Steps

1. **`TabDragGesture`:**
   - `EdgeShare` constant: 0.25;
   - `SideFor(Point point, Size content)` returns `FocusDirection?`: the side whose outer quarter holds the point,
     the nearer side in a corner, `null` for the middle. A size that is empty in either dimension also gives `null`.
2. **`EditorPaneView.axaml`:**
   - name the strip's border (e.g. `Strip`);
   - the hint becomes a 2 × 2 star grid overlay over the whole pane, non-hit-testable. It holds `DropHint`, placed
     over the whole grid for a move, or over the half on the split's side.
3. **`EditorPaneView.axaml.cs`:**
   - `internal FocusDirection? SideAt(Point pointInPane)`: `null` over the visible strip; otherwise
     `TabDragGesture.SideFor` on the point relative to the content area below the strip;
   - the hint API:
     - `IsDropTarget` stays, read-only: is the hint shown;
     - `DropSide` (`FocusDirection?`): the side it covers, `null` for the whole pane;
     - one internal method shows the hint for a side, or the whole pane, and another hides it;
   - `SteerTab` finds the pane and the side under the pointer. It marks the pane:
     - for the middle or the strip, when the pane `CanTakeTab`;
     - for a side, when it `CanSplitWithTab`;

     It re-marks only when the pane or the side changes;
   - the release posts `TakeTab(tab)` for the middle or the strip and `SplitWithTab(tab, side)` for a side, each only
     when its `Can…` holds;
   - Escape and a lost capture are unchanged: they clear the mark, and nothing happens;
   - the class remarks mention the edge zones.
4. **Help:** a Panes row `Keys("Drag a tab to a pane's edge", "Split that pane there, the tab in the new half")`.
5. **README:** the *Tabs and panes* sentence on dragging adds that dropping on a pane's edge splits it there.
6. **Tests:**
   - `TabDragGesture.SideFor` theory: the middle, each side, corners (the nearer side wins), the exact quarter
     boundary, and an empty size;
   - real window (`TabDragViewTests`):
     - carried over the right edge of the other pane: that pane's hint covers its right half (`DropSide == Right`);
       over its middle, the whole pane (`DropSide == null`);
     - released on the other pane's right edge: three panes; the tab is in a new pane right of the target, which
       has the keyboard;
     - released on the bottom edge of its own pane (two tabs): it splits below, and the tab is in the new pane;
     - own pane, only tab, over an edge: no hint, and the release changes nothing;
     - over the other pane's strip near its left end: the hint covers the whole pane, and the release moves;
     - Escape over an edge: the hint clears, and nothing splits;
     - the existing tests pass unchanged.

### Acceptance criteria

- Dropped on a pane's strip or the middle of its content, a tab moves there, as before, with the whole pane
  highlighted.
- Dropped on a pane's top, right, bottom or left part, that pane splits on that side and the tab is in the new pane,
  which has the keyboard. This works on any pane, the tab's own included, when it keeps a tab.
- While carried over an edge, only the half the new pane will take is highlighted. The mark never stays behind.
- The help and the README say so. Zero warnings, whole suite green.

## Out of scope

- Keys or menu entries for "split with this tab" (the `Space m` keys still only move to existing panes).
- Splitting at the window's outer edge (a pane beside the whole layout rather than beside one pane).
- Reordering tabs within a strip, or dropping at a position in it.
- Dragging a tab out of the window or into another instance.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `FEATURE` | New behaviour on top of a working drag | `BUG` |
| How big is an edge zone | The outer quarter of the content on each side | Leaves the middle half for the move, the common case, while the edges are easy to hit at any pane size | A third (middle too small); a fixed band in pixels (lost on large panes, too wide on small ones) |
| Corners | The nearer side, as a fraction of width or height | Continuous, and matches what the eye reads as "closer to the top" or "closer to the left" | A fixed priority (top/bottom first) |
| Does the strip ever split | Never: it always moves | The draft: "dragging in the tabs headers … should just move" | Splitting at the strip's ends |
| What the split hint covers | The half of the pane the new pane will take, strip included | The draft asks for the highlight "only on the top/right/bottom/left". The half shows exactly where the new pane lands, at the split's 0.5 ratio | A thin band along the edge (says "edge", not "new pane"); the zone itself (a quarter, not where the pane goes) |
| Split ratio | Even (0.5) | Every split in the app starts even (`Ctrl+W v`, `:sp`) | Splitting at the zone's size |
| Own pane, only tab, edge | Not offered: no hint, nothing on release | The pane would lose its last tab and close, giving back the same layout. A hint promising a split that will not happen would mislead | Allowing it (a no-op with a hint); leaving the old pane empty (breaks the workspace rule that tabless panes close) |
| Own pane, middle | Nothing, as today | The draft only adds splits | — |
| Who gets the keyboard | The new pane, with the tab | Same as a move and a split: the keyboard follows what the user just placed | Staying on the source |
| Buffer also shown in the target | Moved into the new pane all the same | The new pane is empty, so the rule "one buffer once per pane" holds. One buffer in two panes is allowed | Refusing; showing the target's tab instead (no split, surprising) |
| Where the side logic lives | `LayoutTree.Split(pane, FocusDirection)` in Core, `SplitWithTab` in the workspace, a pure `SideFor` in `TabDragGesture` | Each layer owns what it already owns: shape, panes and tabs, pointer decisions. Each is tested at its level | A new enum for zones (`FocusDirection?` already says it); doing the split in the view |
| Keyboard equivalent | None in this item | Not asked. `Ctrl+W v` then `Space ml` already reaches the same layout | New leader keys |
| Release notes | Left to the next release item | Release items write them (FEATURE-2448 pattern) | Editing `RELEASENOTES.md` now |
| Shape | Two phases: model then view | PHASE01 is testable without a window and reviewable alone. PHASE02 is the gesture and the hint. Each takes ~30 min to review | One phase (mixes the layout and the pointer work); three (the Core change alone is too small) |

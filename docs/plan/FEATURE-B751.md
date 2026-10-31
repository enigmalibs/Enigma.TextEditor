# FEATURE-B751 — Drop files on a pane edge to split

**Status:** DONE
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-b751-phase01-split-with-files`
**Branch (PHASE02):** `feature/feature-b751-phase02-file-drop-hint`
**Run:** feature/2026-10-31-font-size-drop-split

## Objective

Files dragged in from outside the app (a file manager) open in the pane they are dropped on today, with no hint.
Give them the tab drag's zones and hint (FEATURE-9E4A):

- over a pane's **tab strip** or the **middle** of its content, the files open **in that pane**, as today, and the
  whole pane is highlighted while they are carried there;
- over the **top, right, bottom or left** part of a pane's content, that pane **splits** on that side and the files
  open in the new pane; while they are carried there, only the half the new pane will take is highlighted.

## Context & constraints

- **The file drop** (`Views/EditorPaneView.axaml.cs`):
  - every pane allows drops. `OnDragOver` (enter and over, `handledEventsToo`) answers `Copy` for a drag carrying
    files (`DroppedFiles.ReadPaths` / `EffectFor`) and leaves text drags to the text area;
  - `OnDrop` posts `EditorPaneViewModel.OpenDroppedAsync(paths)` once the platform's drag loop is over;
  - nothing listens for `DragLeave` yet, and nothing is highlighted.
- **The hint** (FEATURE-9E4A): `ShowDropHint(FocusDirection? side)` / `HideDropHint()`, `IsDropTarget`, `DropSide`.
  `SideAt(Point)` returns `null` over the visible strip, else `TabDragGesture.SideFor` on the content (the outer
  quarter, nearer side in a corner). For a tab drag, the dragging pane drives the hint on whichever pane is under the
  pointer. A platform drag is reported to the pane under the pointer itself.
- **The workspace** (`ViewModels/WorkspaceViewModel.cs`):
  - `OpenDroppedAsync(pane, paths)` activates the pane, opens each file through the *File → Open* door, puts the first
    one in place of a fresh tab, skips folders and missing paths and names them once on the status line;
  - `Layout.Split(PaneId, FocusDirection)` puts a new pane on any side, half each;
  - `SplitWithTab` shows the shape: `Batch`, `AddPane`, `UpdateActiveFlags`;
  - `ClosePaneAsync(pane)` closes a pane, and the keyboard goes to its successor. `ExCommandHandler.SplitAsync`
    (`:split file`) closes a pane made for a file that did not open.
- **Host seam:** `IPaneHost.OpenDroppedAsync`, `CanSplitWithTab` / `SplitWithTab`, reached through
  `EditorPaneViewModel`. `EditorPaneViewModelTests.RecordingHost` implements `IPaneHost`.
- **Help:** `HelpCatalog` row `Keys("Drop files on a pane", "Open them in that pane")`. **README** *Files* paragraph:
  "by dropping them on a pane".
- **Tests:**
  - Desktop workspace: `FileDropTests` (headless fixture, `FakeBinaryFilePrompt` answers cancel by default);
  - real window: `FileDropViewTests` (`CarryingAsync`, `CarryingText`, `Hover`, `Drop`, `Middle`) and
    `TabDragViewTests` (`NearEdge`, `AssertHintCovers`);
  - existing drop tests drop on a pane's middle, so they must pass unchanged.
- **Baseline:** the run's, taken on the run branch before its first dev (FEATURE-A198's done doc).

## Decisions

- **Zones:** the tab drag's, unchanged. The strip and the middle open the files in the pane. The outer quarter of the
  content on a side splits there.
- **The hint:** the same element and look as the tab drag: the whole pane for "open here", the half the new pane will
  take for a split.
- **A pane with no tab** (the window's only pane, showing "No file is open"): no edges, the whole pane opens the files.
  A split there would leave an empty pane beside the files.
- **After an edge drop:**
  - one split, whatever the number of files; every file opens in the new pane, in drop order, and the last one is
    shown;
  - the new pane has the keyboard;
  - a fresh tab in the pane that was split stays there: the files open beside it, in the new pane;
  - a file the split pane also shows opens in the new pane too: one buffer in two panes, as `:vsp file` gives.
- **When nothing opens:**
  - no path is an existing file (folders, gone): nothing splits. The paths are named on the status line, as a drop's
    are;
  - every file was refused (a binary file declined, a read error): the new pane closes again, and the pane it came
    from takes the keyboard back, as `:split file` does.
- **Text drags:** unchanged. No hint, and the text area keeps its own text drag and drop.
- **Layers:**
  - the workspace gains `CanSplitWithFiles(target)` and `SplitWithFilesAsync(target, paths, side)`;
  - the host seam gains the same pair, reached through `EditorPaneViewModel.CanSplitWithFiles()` /
    `SplitWithFilesAsync(paths, side)`;
  - the view places the hint from its own drag events.

## PHASE01 — Split a pane for dropped files

**Status:** DONE

### Steps

1. **Workspace — `WorkspaceViewModel`:**
   - `CanSplitWithFiles(EditorPaneViewModel target)`: the target is in the workspace and has a tab;
   - `SplitWithFilesAsync(EditorPaneViewModel target, IReadOnlyList<string> paths, FocusDirection side)`:
     - `CanSplitWithFiles` false: changes nothing and returns `null`;
     - no path is an existing file: nothing splits; the paths are reported the way `OpenDroppedAsync` reports them,
       and it returns `null`;
     - otherwise, in one batch, splits the target's leaf on that side, adds the new pane and gives it the keyboard.
       Then it opens the files there through `OpenDroppedAsync(created, paths)`;
     - the new pane still has no tab: it closes (`ClosePaneAsync`), and the method returns `null`;
     - otherwise it returns the new pane.
2. **Host seam:**
   - `IPaneHost.CanSplitWithFiles(target)` / `SplitWithFilesAsync(target, paths, side)`, documented like
     `CanSplitWithTab` / `SplitWithTab`;
   - `EditorPaneViewModel.CanSplitWithFiles()` / `SplitWithFilesAsync(paths, side)`, like `CanSplitWithTab` /
     `SplitWithTab`;
   - `RecordingHost` follows.
3. **Tests** (`FileDropTests`, or a new `SplitWithFilesTests`):
   - a file dropped on each side of a pane with a tab: the new pane stands on that side, holds the file and has the
     keyboard. The split pane keeps its tabs;
   - two files: one split, both in the new pane in drop order, the last one shown;
   - a pane showing a fresh tab: the fresh tab stays, and the file is in the new pane;
   - a file the split pane already shows: a tab of it in the new pane, with a registry view count of 2;
   - a pane with no tab: refused, nothing changes;
   - only a folder and a missing path: no split, and the status line names both;
   - a binary file declined: the new pane closes, the layout is one pane again, and the split pane has the keyboard;
   - a pane not in the workspace: refused.

### Acceptance criteria

- `SplitWithFilesAsync` splits the pane on that side and opens the files in the new pane, which has the keyboard.
- It refuses a pane with no tab. It splits nothing when no path is a file. It closes the new pane again when nothing
  opened there.
- Zero warnings, whole suite green.

## PHASE02 — File drop zones and their hint

**Status:** DONE

### Steps

1. **`EditorPaneView.axaml.cs`:**
   - `FileSideAt(Point)`: `SideAt(point)` when the pane `CanSplitWithFiles()`, else `null` (the whole pane);
   - `OnDragOver` (enter and over), for a drag carrying files: answers `Copy` as today, and shows the hint for
     `FileSideAt`. It re-places the hint only when the side changes. A drag of no file hides it;
   - a `DragLeave` handler (`handledEventsToo`) hides the hint;
   - `OnDrop` hides the hint, reads the side, then posts `SplitWithFilesAsync(paths, side)` for a side, else
     `OpenDroppedAsync(paths)` as today;
   - the class remarks mention that files get the same zones.
2. **Help:** a row next to the drop row: `Keys("Drop files on a pane's edge", "Split that pane there, the files in the
   new half")`.
3. **README:** the *Files* paragraph adds that dropping them near a pane's edge splits that pane, with the files in the
   new half. The half is highlighted while they are dragged, as for a tab.
4. **Tests** (`FileDropViewTests`, real window, headless drag):
   - files carried near the other pane's right edge: that pane's hint covers its right half (`DropSide == Right`). In
     its middle and over its strip, the whole pane (`DropSide == null`);
   - carried from one pane to the other: the first one's hint goes, the second one's shows. A `DragLeave` clears it;
   - text carried over a pane: no hint;
   - files dropped near a pane's right edge: a new pane to its right holds the file and has the keyboard, and no hint
     is left;
   - the only pane, with no tab, near its edge: the whole-pane hint, and the drop opens the file there with no split;
   - a folder dropped near an edge: no split, and the status line names it;
   - the existing drop tests pass unchanged.

### Acceptance criteria

- Files carried over a pane's strip or the middle of its content highlight the whole pane, and open there when
  dropped, as before.
- Files carried near a pane's top, right, bottom or left edge highlight the half on that side. Dropped there, that
  pane splits on that side and the files open in the new pane, which has the keyboard. A pane with no tab has no
  edges.
- The hint never stays behind: leaving, dropping and cancelling clear it. Text drags are untouched.
- The help and the README say so. Zero warnings, whole suite green.

## Out of scope

- Drops outside the panes: the menu, the status bar, the rail, the settings page.
- Splitting at the window's outer edge (beside the whole layout).
- Dropping at a position within a tab strip.
- A different hint look for files than for tabs.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `FEATURE` | New behaviour on a working drop | `BUG` |
| Zones | The tab drag's: strip and middle open here, outer quarter splits | The draft asks for "the same hint as when dragging a tab". One rule for both drags | Separate sizes for files |
| Several files at an edge | One split, all files in the new pane | One drop, one new place. A pane per file would tile the window unasked | A split per file |
| A pane with no tab | No edges: the files open in it | A split would leave the empty pane beside the files. It is the counterpart of "a pane's only tab cannot split its own pane" | Offering edges (an empty pane left behind) |
| Nothing to open | No split; the paths are named as a drop's are | A split for nothing would be a layout change with no content | Splitting, then closing at once (flicker) |
| Every file refused after the split | The new pane closes, and the keyboard goes back | Same rule as `:split file`. The workspace does not keep empty panes | Leaving an empty pane |
| Fresh tab in the split pane | Kept | An edge drop asks for a new pane; the fresh tab is not in the way there. The middle drop still replaces it | Replacing it, then splitting |
| A file the split pane already shows | Opened in the new pane too | Same as `:vsp file`. The new pane is empty, so one pane never shows one buffer twice | Refusing; activating the existing tab |
| Who places the hint | Each pane, from its own drag events | A platform drag is reported to the pane under the pointer, unlike the captured tab drag | A window-level handler |
| Text drags | Unchanged, no hint | They belong to the text area's own drag and drop | Hinting text drops |
| Shape | Two phases: model then view | PHASE01 is testable without a window. PHASE02 is the events and the hint. Each takes ~30 min to review. Mirrors FEATURE-9E4A | One phase |
| Release notes | Left to the next release item | Release items write them | Editing `RELEASENOTES.md` now |

# FEATURE-B751-PHASE02 — File drop zones and their hint

**Item:** FEATURE-B751 — Drop files on a pane edge to split
**Branch:** `feature/feature-b751-phase02-file-drop-hint`
**Run:** feature/2026-10-31-font-size-drop-split

## Summary

Files dragged in from outside the app now get the tab drag's zones and hint.

- **Over a pane's strip or the middle of its text:** the whole pane is highlighted, and the files open in it, as
  before.
- **Near an edge** (the outer quarter, as for a tab):
  - only the half the new pane will take is highlighted;
  - dropped there, the pane splits on that side and the files open in the new half, which takes the keyboard
    (PHASE01's `SplitWithFilesAsync`);
  - a pane with no tab has no edges: it takes the files whole.
- **`EditorPaneView`:**
  - `FileSideAt(point)` is `SideAt` when the pane `CanSplitWithFiles()`, else `null`;
  - `OnDragOver` (enter and over) shows the hint for that side, and places it again only when the side changes. A
    drag carrying no file hides it;
  - a new `DragLeave` handler hides the hint. It is raised when the drag moves to another part of the pane, to
    another pane, or out of the window, and when it is cancelled. Within a pane, the enter on the new part shows the
    hint again;
  - `OnDrop` hides the hint, then posts `SplitWithFilesAsync` for a side, else `OpenDroppedAsync`;
  - the class remarks describe the file zones. Each pane places its own hint for files, since a platform drag is
    reported to the pane under the pointer.
- **Help:** a new Files row, "Drop files on a pane's edge — Split that pane there, the files in the new half".
- **README:** the *Files* paragraph describes the edge drop and its highlight.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`, `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/FileDropViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-B751.md`; created this file

## Deviations & follow-ups

- None from the plan.
- **Not seen in a real window:**
  - the hint is checked by geometry in headless drags (raw enter, over, leave and drop events), not with a real file
    manager;
  - how each platform reports leave and cancel (X11/Wayland bridging, Windows OLE) is worth a manual check: drag a file
    across two panes, out of the window, and cancel with Escape in the file manager.
- **Release notes** are left to the next release item.
- **Baseline:** as recorded in `docs/done/FEATURE-A198.md` (the run's: 0 warnings, 2911 passed).
- **Documentation sweep:**
  - README *Files* and the help's Files rows were edited as planned;
  - the README's "What's new in 1.11" line describes the released 1.11 and was left alone;
  - nothing else describes the file drop.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Hiding the hint between parts of one pane | Hide on every leave; the enter that follows re-shows it | Avalonia raises leave on the old part, then enter on the new one, synchronously. No bookkeeping of which part the drag is over |
| A text drag over a pane | Hides any hint | A drag carrying no file never highlights. It also clears a stale hint should a platform skip a leave |
| Where the empty-pane rule is applied | `FileSideAt`, through `CanSplitWithFiles()` | The hint and the drop decide from the same answer, so the hint never promises a split the drop would not make |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2945 passed, 0 failed, 0 skipped (+5).
- **Tests added** (`FileDropViewTests`, real window, headless drag):
  - files entered over one pane mark it whole. Moved near the other pane's right, top, bottom and left edges, they
    mark that half, `DropSide` names it, and the first pane's mark is gone (rectangles checked to within 2 px). In
    that pane's middle and over its strip they mark the whole pane, and a leave clears it;
  - text carried over a pane and near its edge marks nothing;
  - two files dropped near the only pane's right edge give a new pane to its right, holding both and with the
    keyboard. The first pane keeps its tab, and no mark is left;
  - the only pane, with no tab, near its edge: the whole-pane mark, and the drop opens the file there with no split;
  - a folder dropped near an edge splits nothing and is named on the status line.
- The existing drop tests (middle drops, cursor effects, text drops) and the tab drag tests pass unchanged.

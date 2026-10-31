# FEATURE-B751-PHASE01 — Split a pane for dropped files

**Item:** FEATURE-B751 — Drop files on a pane edge to split
**Branch:** `feature/feature-b751-phase01-split-with-files`
**Run:** feature/2026-10-31-font-size-drop-split

## Summary

The workspace can now split a pane for files dropped on one of its edges. PHASE02 wires the drop and its hint to it.

- **`WorkspaceViewModel.CanSplitWithFiles(target)`**: the pane is in the workspace and has a tab. A pane with no
  tab (the window's only pane, "No file is open") takes the files whole instead.
- **`WorkspaceViewModel.SplitWithFilesAsync(target, paths, side)`:**
  - refused (`null`, nothing changes) when `CanSplitWithFiles` is false;
  - with no existing file among the paths, nothing splits. The paths go through `OpenDroppedAsync(target, …)`,
    which names the folders and missing paths on the status line, as a drop's;
  - otherwise, in one batch, `Layout.Split(target, side)` and a new pane that takes the keyboard. The files then open
    there through `OpenDroppedAsync(created, …)`: in drop order, the last one shown;
  - when nothing opened after all (a binary file declined, a read error), the new pane closes again
    (`ClosePaneAsync`) and the split pane takes the keyboard back, as `:split file` does.
- **Host seam:** `IPaneHost.CanSplitWithFiles` / `SplitWithFilesAsync`, reached through
  `EditorPaneViewModel.CanSplitWithFiles()` / `SplitWithFilesAsync(paths, side)`. `RecordingHost` follows.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`, `IPaneHost.cs`,
  `EditorPaneViewModel.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SplitWithFilesTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/EditorPaneViewModelTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-B751.md`; created this file

## Deviations & follow-ups

- None from the plan.
- **Baseline:** as recorded in `docs/done/FEATURE-A198.md` (the run's: 0 warnings, 2911 passed).
- **Documentation sweep:** nothing user-facing changes in this phase. The README *Files* paragraph and the help row
  come with PHASE02, as planned.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Reporting paths when no file is among them | Delegate to `OpenDroppedAsync(target, paths)` | One wording for a drop's skipped paths. It opens nothing, and gives the pane the keyboard as a drop on it does |
| "Is there a file to open" | `File.Exists` on any path, before splitting | The same test `OpenDroppedAsync` makes per path. A split is only made when something can go in it |
| Undoing an empty split | `ClosePaneAsync(created)` | The path `:split file` takes. `Layout.Close` hands the keyboard to the sibling, the pane that was split |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2940 passed, 0 failed, 0 skipped (+11).
- **Tests added** (`SplitWithFilesTests`, headless fixture):
  - a file on each side (theory, 4 rows): the new pane is the split pane's neighbour on that side, holds the file and
    has the keyboard and the frame; the split pane keeps its tab; nothing on the status line;
  - two files: one split, both in the new pane in drop order, the last one shown;
  - a fresh tab of the split pane stays there;
  - a file the split pane shows gets a tab of its own in the new pane (view count 2, one document);
  - a pane with no tab: refused, nothing opened;
  - a folder and a missing path: no split, "Not opened: notes is a folder; gone.txt does not exist.";
  - a binary file declined: asked once, the new pane closes, and the split pane is alone and active again;
  - a pane outside the workspace: refused.

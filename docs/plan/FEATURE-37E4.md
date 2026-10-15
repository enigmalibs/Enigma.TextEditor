# FEATURE-37E4 — Open files by drag and drop

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-37e4-file-drop`
**Run:** vibe/2026-10-15-drop-icon-help

## Objective

Dropping files on an editor pane opens them in that pane. The files come from the platform's file
manager, or from any application that offers local files.

## Context & constraints

- `EditorPaneView` is one pane: the tab strip, a single AvaloniaEdit `TextEditor`, the `HexView`
  for hex tabs, and a placeholder when the pane has no tab.
- `WorkspaceViewModel.OpenAsync(path)` is the one way a file is opened:
  - it goes through `IDocumentService.OpenAsync`: the binary-file prompt, the size limit and the error
    dialog;
  - it shows an already-open buffer in the active pane rather than reading it again.
- `WorkspaceViewModel.Activate(pane)` makes a pane the active one.
- `OpenStartupFilesAsync` reports what it skipped on the status line as `Not opened: …`.
- Avalonia 12's drag-and-drop API:
  - `DragDrop.SetAllowDrop`;
  - `DragDrop.DragEnterEvent` / `DragOverEvent` / `DropEvent`;
  - `DragEventArgs.DataTransfer.TryGetFiles()`, `IStorageItem.TryGetLocalPath()`;
  - `DragDropEffects`.
- Enigma.FileExplorer's `FileDragData.ReadPaths` is the in-house precedent for reading a drop.
- AvaloniaEdit has its own text drag-and-drop inside the `TextArea`; text drags must keep working.

## Decisions

| Decision | Rationale |
|---|---|
| The whole pane is the drop target: the tab strip, the text, the hex view and the empty pane | "In a pane" |
| Files open in the pane they are dropped on; that pane becomes the active one | Where the user aimed |
| Several files open in drop order, and the last one is shown | Same as files named on the command line |
| A folder, a path that no longer exists, or a non-local item is skipped. Folders and missing paths are named once on the status line: `Not opened: X is a folder.` | The editor opens files; startup arguments already behave this way |
| Each file goes through `OpenAsync`, the File → Open path: binary prompt, size limit, error dialog; an already-open file is shown, not reopened | One open path |
| Hovering shows the Copy cursor when the drag carries at least one local file, and No-drop otherwise. The effect reported to the source is always Copy | A reported Move would let a file manager delete the source |
| Only drags that carry files are taken; AvaloniaEdit keeps its own text drag-and-drop | A text drag inside the editor must still move text |
| No drop highlight overlay: the cursor is the feedback | Smallest change that answers the request |

## Steps

1. `Views/DroppedFiles.cs` (internal static): `ReadPaths(IDataTransfer)` returns the local paths of the
   files a drag carries. Items without a local path are left out.
2. `WorkspaceViewModel.OpenDroppedAsync(EditorPaneViewModel pane, IReadOnlyList<string> paths)`:
   - activates the pane;
   - opens every existing file in order;
   - skips folders and missing paths, and reports them in one status message.

   It is reached through `IPaneHost`. `EditorPaneViewModel.OpenDroppedAsync(paths)` is the pane's door.
3. `EditorPaneView`:
   - allow drops;
   - DragEnter/DragOver set the effect;
   - Drop opens the files.

   The handlers are registered with `handledEventsToo`, so AvaloniaEdit's own handlers inside the pane
   cannot hide a file drop. A drag without files is left untouched.
4. Tests:
   - view-model:
     - files open in the dropped pane, which becomes active;
     - several files open in order, and the last one is shown;
     - a folder and a missing path are skipped and reported;
     - an already-open file is shown, not reopened;
   - headless window:
     - the hover effect is Copy with files, and None for a text-only drag;
     - a drop on an inactive pane opens there and activates it;
     - a text-only drop opens nothing.

## Acceptance criteria

- Dropping one or more files on any pane opens them in that pane, which becomes active.
- Folders and missing paths are skipped and named on the status line.
- Binary files get the binary-file prompt; open failures are reported as with File → Open.
- Text drags inside the editor behave as before.
- Zero warnings, whole suite green.

## Out of scope

- Drops on the menu bar or the status bar.
- Dragging tabs between panes.
- A drop highlight or overlay.
- Opening a folder as a workspace.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Drop target | The whole pane | Matches "in a pane"; every part of the pane is an obvious place to drop | The text area only (the empty pane and the hex view would refuse drops); the whole window (not asked for) |
| Which pane opens the files | The pane dropped on, made active | Where the user aimed | The active pane, whatever the drop target |
| Folders | Skipped, named on the status line | The editor opens files; same wording as startup arguments | Opening every file in the folder (surprising, unbounded) |
| Cursor effect | Copy, and always report Copy | Never lets the source delete the original | Mirror the source's effect (Move would delete files in some file managers) |
| Visual feedback | Cursor only | Enough to tell a drop will be taken; no new visual state | A highlighted pane frame or an overlay (gold-plating) |

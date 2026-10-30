# FEATURE-9F56-PHASE02 — Drag a tab onto another pane

**Item:** FEATURE-9F56 — Move tabs between panes
**Branch:** `feature/feature-9f56-phase02-drag-tab`
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Summary

You can now drag a tab from its strip and drop it anywhere on another pane: its strip, its text, its hex view or its
"no file" placeholder. The tab moves there exactly as `Space m` moves it (PHASE01): it keeps its caret and scroll,
it isn't doubled when that pane already shows the buffer, the target pane takes the keyboard, and a pane left
without tabs closes.

- **Starting a drag:** a left press on a tab, but not on its ×, becomes a drag once the pointer has moved past the
  platform's tap size (`Application.PlatformSettings.GetTapSize`, falling back to 4 px). Until then it is still a
  click that shows the tab. The drag goes through `DragDrop.DoDragDropAsync` with `Move` allowed.
- **What it carries:** the tab itself, in an in-process format (`DraggedTab.Format`). It is never handed to the
  platform, so another application, including another instance of this editor, sees a drag it can't read.
- **Dropping:** another pane shows the Move cursor and the pane the tab came from shows None. The move is posted
  until the drop has returned, as dropped files are opened. Dropped files still get Copy and open as before.
- **Plumbing:**
  - `IPaneHost` gains `CanMoveTabTo` and `MoveTabTo`. The workspace finds the pane that holds the tab.
  - The pane gains `CanTakeTab` and `TakeTab`.
- **Help:** a Panes row "Drag a tab onto another pane — Move it there". **README:** the *Tabs and panes*
  paragraph mentions dragging.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/DraggedTab.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/IPaneHost.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/EditorPaneViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-9F56.md`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabDragViewTests.cs`, this file
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/EditorPaneViewModelTests.cs` (the recording host
  implements the two new members)

## Deviations & follow-ups

- **The headless platform has no drag source.** The view starts drags through an internal `StartDrag` seam, which
  defaults to `DragDrop.DoDragDropAsync`. The tests replace it to see that a drag starts, and play the drop the way a
  platform does. The real platform drag hasn't been tried in a running window on Windows or Linux, so trying it once
  by hand is a follow-up.
- Baseline: as recorded in `docs/done/FEATURE-9F56-PHASE01.md` (0 warnings, 2780 passed). No pre-existing failure.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Drag distance | The platform's tap size, falling back to 4 px | It is the distance Avalonia's own gestures use to tell a click from a move. 4 px is the Windows default |
| How the drag start is tested | An internal `StartDrag` property on `EditorPaneView` | The headless platform cannot start a platform drag. Everything else runs through real pointer events |
| Who finds the source pane | The workspace (`MoveTabTo(target, tab)`) | The drop only knows the tab. A tab sits on one pane at most |
| Which pointer events are listened to | Pressed, moved, released and capture-lost on the strip, all with `handledEventsToo` | The strip's items handle the press to select the tab. A drag or a release ends the pending press |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2812 passed, 0 failed, 0 skipped (+6 against PHASE01's 2806).
- **Tests added** (`TabDragViewTests`, on the real window):
  - a dragged tab gets Move over the other pane's text and strip, and None over its own pane;
  - dropping it on the other pane's text moves it, gives that pane the keyboard, and the editor there shows it;
  - dropping a pane's last tab on the other pane's strip moves it and closes the pane it left;
  - dropping it on its own pane changes nothing;
  - pressing a tab and moving 2 px starts no drag; moving 30 px starts one, carrying that tab, with Move allowed;
  - a drag from the × button, or with the right button, starts nothing.
- `FileDropViewTests` still pass unchanged: dropping files works as before.

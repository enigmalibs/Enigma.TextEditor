# CODE-REVIEW-0734-PHASE13 — [Medium] SOLID-03 The pane view's code-behind is split into view helpers

## Summary

`EditorPaneView.axaml.cs` was 1,031 lines and held, besides the editor, three unrelated sets of input handling. Each
now has a view helper of its own, beside the existing `TabDragGesture`:

- `Views/TabStripDrag` (instance, one per pane): carrying a tab off the strip.
  - It covers the press that becomes a drag, the capture, the Escape that cancels, steering the hint across panes,
    and the move or split on release.
  - It attaches its own handlers to the strip. The decisions stay with `TabDragGesture`.
- `Views/PaneFileDrop` (instance, one per pane): files dragged in from outside. It covers the hint while they are over
  the pane, and opening them in the pane or in a new half of it on drop.
- `Views/EditorContextMenu` (static `Attach`): the editor's own menu.
  - Each item, found by its `x:Name`, gets its action and its enabling, read as the menu opens. Undo and redo go
    through the view, so they reach Vim while it is on.
  - The keyboard goes back to the text afterwards.
  - The XAML loses its `Opening` / `Click` handler names.
- `EditorPaneView` builds the three in its constructor and keeps the editor, the minimap, the view state, the drop
  hint and the strip's own small gestures (middle-click, wheel, double-click). `IsDraggingTab` now asks the drag
  helper.
- `EditorPaneView.axaml.cs` goes from 1,031 to 713 lines. The behaviour is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/TabStripDrag.cs`, `Views/PaneFileDrop.cs`, `Views/EditorContextMenu.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`, `Views/EditorPaneView.axaml`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorContextMenuTests.cs`

## Deviations & follow-ups

- **713 lines, against the plan's "under 700".** The three concerns the review names have left. The 13 lines left
  over the target are the strip's own small gestures (middle-click close, wheel, double-click for a new tab), which
  are not what the review named. Moving them too would be a follow-up if wanted.
- `EditorContextMenu.Attach` refuses a menu missing one of its seven items, so a renamed item fails at once instead of
  leaving a dead entry.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Helper shape | Instances for the stateful gestures (drag, file drop), a static `Attach` for the stateless menu | Each holds only what it needs |
| Menu items | Found by `x:Name` from the XAML | The XAML keeps headers, gestures and Vim hints, and the code keeps the behaviour |
| File drop hint | Stays on the view (`ShowDropHint` / `HideDropHint` / `FileSideAt`) | Tabs and files share it, and it belongs to the pane's layout |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3413 tests, 3411 passed, 2 skipped (PHASE06's), 0 failed.
- Test added (1): `EditorContextMenuTests.AMenuMissingAnItem_IsRefusedWhenAttached_RatherThanLeftHalfWorking`.
- The guards (`TabDragViewTests`, `FileDropViewTests`, `EditorContextMenuTests`, `TabStripViewTests`), 60 tests, pass
  unchanged.
- Documentation sweep (§7): nothing user-facing changed.

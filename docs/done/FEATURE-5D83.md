# FEATURE-5D83 — Editor context menu

**Item:** FEATURE-5D83 — Editor context menu
**Branch:** `feature/feature-5d83-editor-context-menu`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

A right-click in the text editor now opens a context menu with the Edit menu's commands:
- Undo, Redo;
- Cut, Copy, Paste, Delete;
- Select All.

Each item shows the Edit menu's gesture; Delete shows `Delete`.

- **Enabled state:** each item is enabled from AvaloniaEdit's own `CanUndo` … `CanSelectAll`, read when
  the menu opens (`ContextMenu.Opening`). Those answers are not observable, so binding was not an
  option.
- **Target:** the menu acts on the editor it was opened on, not the active pane's. Afterwards the focus
  goes back to that text, which also gives its pane the keyboard (through `SplitHost`'s focus
  handling).
- **Caret:** right-click does not move it (AvaloniaEdit's default), as planned.
- **Hex tabs:** no menu, because the text editor is hidden there (out of scope).

## Files / modules touched

**Created**

- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorContextMenuTests.cs` — headless, real window,
  real right-click:
  - the menu opens with the seven items, separators and gestures;
  - the enabled states follow the editor's `Can*` (Undo/Redo off on a fresh file; Undo on after an
    edit; Cut/Copy/Delete on with a selection);
  - Delete, then Undo, then Redo;
  - Select All selects everything and gives the text the focus again;
  - Cut then Paste go through the clipboard;
  - in a split, the menu of the pane without the keyboard acts on that pane's text, gives it the
    keyboard, and leaves the other pane's text alone.

**Modified**

- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml` — `TextEditor.ContextMenu` with the named
  items.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs` — `OnEditorMenuOpening`, one click
  handler per item, `EditFromMenu`.
- `docs/roadmap.md`, `docs/plan/FEATURE-5D83.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What the tests assert for Cut/Copy/Delete without a selection | That each item matches the editor's `Can*`, not a fixed "disabled" | AvaloniaEdit decides those (it can cut or copy the caret's line, and Delete takes the next character); the menu must agree with the editor, whatever its options | Asserting the plan's assumption that they are disabled |
| How the tests open the menu | A real right button press/release over the editor | Proves the text area does not swallow the gesture | `ContextMenu.Open()` (skips the path a user takes) |
| Item names | `MenuUndo` … `MenuSelectAll` | Unique in the pane's name scope; FEATURE-21A7 addresses them | — |

## Deviations & follow-ups

- The plan's test step "with no selection, Cut, Copy and Delete are disabled" became "the items match
  the editor's `Can*`", for the reason above.
- Follow-ups:
  - a menu for the hex view (Copy, Select All);
  - moving the caret to the right-click point;
  - the keyboard's context-menu key while Vim is in normal mode was not tested (the engine swallows
    function keys).
- Fix cycles: 0.
- Line endings: LF; no churn.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the in-app help covers keys only.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2497 passed**, 0 failed, 0 skipped (2491 before,
  plus 6 new).

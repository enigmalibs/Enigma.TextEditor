# BUG-5A45-PHASE02 — Escape cancels a tab drag

**Item:** BUG-5A45 — Tab drag shows the refusal cursor
**Branch:** `bugfix/bug-5a45-phase02-escape-cancels`
**Run:** bugfix/2026-10-30-tab-drag-cursor

## Summary

The platform drag session cancelled on Escape. The pointer gesture that replaced it in PHASE01 now does too:

- **Where Escape is caught:** while a tab is carried, the window has a tunnelling `KeyDown` handler. It is added
  when the drag starts and removed when the drag stops, however it stops.
- **What Escape does:** it ends the drag on the spot. The capture is released, the strip's cursor is back, nothing
  is dropped, and the key is marked handled.
- **Why on the window:** the keyboard may be in any pane's editor, and Vim must not take this Escape as its own
  (it would leave insert or visual mode).
- **Everything else is untouched:** other keys pass through, and Escape with no drag still reaches the editor.

The help row now reads "Drag a tab onto another pane — Move it there; Escape cancels the drag", and README's *Tabs
and panes* paragraph says the same.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`, `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabDragViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-5A45.md`; created this file

## Deviations & follow-ups

- None against the plan.
- Baseline: as recorded in `docs/done/BUG-5A45-PHASE01.md` (0 warnings, 2862 passed).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who holds the handler | The pane the drag started from, on its top level, for the length of the drag | The drag's state is that pane's. A permanent window handler would read every key of the session for nothing |
| The mouse after a cancel | The rest of that press does nothing: moves and the release find no press and no drag | The cancel is final. A new drag needs a new press |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2873 passed, 0 failed, 0 skipped (+3).
- **Tests added** (`TabDragViewTests`, real window, headless mouse and keyboard):
  - Escape during a drag ends it:
    - the cursor is back;
    - the editor, in insert mode with the keyboard, is still in insert mode;
    - the release that follows moves nothing;
  - a drag cancelled that way can be started again and dropped;
  - Escape with no drag still takes the editor from insert to normal mode.
- **Mutation check:** with the handler not added, the first two tests fail. Handler restored before commit.

# BUG-5A45-PHASE03 — Highlight the pane a tab drops on

**Item:** BUG-5A45 — Tab drag shows the refusal cursor
**Branch:** `bugfix/bug-5a45-phase03-drop-highlight`
**Run:** bugfix/2026-10-30-tab-drag-cursor

## Summary

Since PHASE01, the drag cursor says only that a drag is under way. It no longer tells the pane the tab came from
apart from one that would take it. This phase marks the pane a tab would move to, as GitClient rings the badge a
branch would land on.

- **The hint** (`EditorPaneView.axaml`): an overlay named `DropHint`, drawn over the whole pane (strip included).
  - It is a faint accent wash (`Panel.dropFill`, 12 % opacity) under a 2 px accent ring (`Border.dropRing`). The
    1 px accent frame of the pane with the keyboard stays distinct.
  - It is never hit-testable, and hidden by default.
- **Setting it:** `IsDropTarget` (internal) shows or hides it. On every move the dragging pane marks the pane under
  the pointer when that pane takes the tab (`CanTakeTab`) and unmarks the previous one. Its own pane and the space
  off the panes are never marked.
- **Clearing it:** every way the drag ends clears the mark: release, Escape, or a lost capture.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml`,
  `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabDragViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-5A45.md`; created this file

## Deviations & follow-ups

- **Fixed a PHASE01 bug: a lost capture didn't end the drag.**
  - The strip's `PointerCaptureLost` handler was registered for the bubble route. Avalonia raises that event on the
    Direct route only, and calls a handler only when its routes include the event's (`EventRoute`:
    `routeItem.Routes.HasAllFlags(e.Route)`). So the handler never ran.
  - The lost-capture test written for this phase found it (fix cycle 1 of 3). The handler now listens on
    `RoutingStrategies.Direct`.
  - PHASE01's done doc claims a lost capture ends the drag. That holds from this commit on.
  - The same mistake was already in FEATURE-9F56-PHASE02, where it only meant a pending press was not forgotten.
- **GitClient** registers its `PointerCaptureLost` handlers with `RoutingStrategies.Tunnel`, so it likely has the
  same gap. Worth a look there; not changed by this run.
- **Not seen in a real window:** the hint's look (12 % wash, 2 px ring) is a first pass, and a glance in both themes
  is a follow-up.
- Baseline: as recorded in `docs/done/BUG-5A45-PHASE01.md` (0 warnings, 2862 passed).
- Documentation sweep: nothing in the README or the help describes the drag feedback; no edit.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What the hint looks like | Accent wash at 12 % under a 2 px accent ring | Legible over text, hex rows and the placeholder, in either theme, and distinct from the active pane's 1 px frame |
| How the hint is drawn | A `Panel` last in the pane's grid, spanning both rows, not hit-testable | Drawn on top of everything, and never in the way of the hit test that finds the pane under the pointer |
| Who sets it | The dragging pane, on the pane it hit-tests | That pane owns the gesture and already knows the tab; the target only shows the hint |
| Fixing the capture-lost route here | Yes, in this phase | This phase's criteria require the mark to clear after a lost capture, and the test cannot pass without the fix |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2875 passed, 0 failed, 0 skipped (+2).
- **Tests added** (`TabDragViewTests`, real window, headless mouse):
  - the hint is on the other pane while the tab is carried over its text or strip;
  - it is on neither pane over the source pane or off the window, and on neither after the drop;
  - it is cleared by Escape and by a lost capture; a lost capture also ends the drag, and nothing moves.
- **Fix cycle 1:** the lost-capture assertion failed (`IsDraggingTab` still true) until the handler's route was
  corrected.

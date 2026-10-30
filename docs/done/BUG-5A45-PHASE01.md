# BUG-5A45-PHASE01 — A tab drag the pane runs itself

**Item:** BUG-5A45 — Tab drag shows the refusal cursor
**Branch:** `bugfix/bug-5a45-phase01-pointer-drag`
**Run:** bugfix/2026-10-30-tab-drag-cursor

## Summary

Dragging a tab onto another pane no longer opens a platform drag-and-drop session. That session was why the
pointer showed the "impossible" cursor (a white circle with a red bar) for the whole gesture. Over its own window,
Avalonia's X11 drag source delivers the drag in process and never offers it to the system. A Wayland compositor
bridging the drag sees one nobody accepted and draws the refusal cursor. Enigma.GitClient established this in its
BUG-11A4.

The pane now runs the gesture from pointer events, as GitClient does for its branch badges:

- **Press:** a left press on a tab (not on its ×) is remembered, as before.
- **Threshold:** past the platform's tap size, the strip captures the pointer.
- **Moves:** each one hit-tests the window for the `EditorPaneView` under the pointer. The strip wears the
  `DragMove` cursor over any pane, its own included, and its own cursor off the panes. It never wears `No`.
- **Release:** it ends the drag, and posts `TakeTab` on the pane under the pointer when that pane takes the tab.
- **Lost capture:** it ends the drag without dropping anything.

Avalonia 12.1.4 was decompiled to confirm the cursor is followed:

- while a capture is held, the cursor shown is the captured element's (`PointerOverPreProcessor` sets
  `CursorElement = pointer.Captured`);
- a change to it is applied at once (`PresentationSource.CursorElement_PropertyChanged`).

The decisions live in `TabDragGesture` (`IsDrag`, `CursorFor`). `DraggedTab`, its in-process format, the
`StartDrag` seam and the tab branches of the pane's `DragOver` / `Drop` handlers are gone. Files dropped from
outside still use the platform.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/TabDragGesture.cs`, this file
- Deleted: `src/Enigma.TextEditor.Desktop/Views/DraggedTab.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
- Rewritten: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabDragViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-5A45.md`

## Deviations & follow-ups

- **Not yet tried by hand:** the pointer has not been checked in a running window on the KDE Wayland session. The
  headless tests read the strip's `Cursor`, which the decompiled code shows is the one put on screen. A quick manual
  check is a follow-up.
- **Escape doesn't cancel yet.** The platform session used to cancel on Escape; that comes back in PHASE02.
- **Baseline** (on the run branch, before this dev):
  - `dotnet build --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test`: 2862 passed, 0 failed.
  - No pre-existing warning or failure.
- **Documentation sweep:** README's *Tabs and panes* and the help row still describe the behaviour correctly; no
  edit.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the cursor object lives | `EditorPaneView.CarryingCursor`, internal static, made once | Making a `Cursor` needs the platform, so the helper stays pure. Internal so a test can check it is the one worn |
| Restoring the strip's cursor | `ClearValue(CursorProperty)` | The strip has no cursor of its own. Writing back the value read before would pin an inherited cursor as a local one |
| Drag distance | `TabDragGesture.IsDrag` keeps today's rule (past the tap size on either axis) | Unchanged behaviour; the plan says so |
| Where the drop is decided | On the release, from a hit test of the top level | A capture makes the strip every event's source, so only the hit test knows the pane under the pointer |
| A new press during a drag | Ignored | The drag owns the pointer until it ends |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2870 passed, 0 failed, 0 skipped. That is +8 against the baseline:
  6 platform-drop tests replaced by 14 pointer tests.
- **Tests** (`TabDragViewTests`, on the real window with the headless mouse):
  - a 2 px move is a click, 30 px starts a drag, the release ends it;
  - dropping on the other pane's text moves the tab and gives that pane the keyboard;
  - a pane's last tab dropped on the other pane's strip moves it, and the pane it left closes;
  - letting go over its own pane, or outside the window, changes nothing;
  - the strip wears `DragMove` over both panes, its own cursor off them, and its own again after the drop;
  - a drag from the × or with the right button is no tab drag;
  - `IsDrag` at and past the distance, and `CursorFor` never answers `No`.
- `FileDropViewTests` pass unchanged.

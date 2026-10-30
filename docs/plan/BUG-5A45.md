# BUG-5A45 — Tab drag shows the refusal cursor

**Status:** TODO
**Type:** BUG
**Branch (PHASE01):** `bugfix/bug-5a45-phase01-pointer-drag`
**Branch (PHASE02):** `bugfix/bug-5a45-phase02-escape-cancels`
**Branch (PHASE03):** `bugfix/bug-5a45-phase03-drop-highlight`
**Run:** bugfix/2026-10-30-tab-drag-cursor

## Objective

Dragging a tab onto another pane (FEATURE-9F56-PHASE02) works, but for the whole gesture the pointer is the
"impossible" cursor: a white circle with a red bar. Fix it the way Enigma.GitClient fixed the same pointer on its
branch badges (GitClient `BUG-11A4`): stop opening a platform drag session for a tab, and run the gesture from
pointer events so the pointer becomes an ordinary `Cursor` the app sets.

## Context & constraints

- **Today's drag** (`Views/EditorPaneView.axaml.cs`): a left press on a tab (not its ×) is remembered. Past the
  platform's tap size (`PlatformSettings.GetTapSize`, falling back to 4 px), `StartDrag` (defaults to
  `DragDrop.DoDragDropAsync`) starts a platform drag carrying the tab in an in-process format (`Views/DraggedTab.cs`).
  Each pane's `OnDragOver` answers `Move` for another pane and `None` for its own. `OnDrop` → `DropTab` posts
  `EditorPaneViewModel.TakeTab(tab)`.
- **Why the pointer is wrong** (established in GitClient `BUG-11A4`, by decompiling `Avalonia.X11`):
  - Over its own window, Avalonia's X11 drag source finds the target in-process. It never sends `XdndEnter`,
    `XdndPosition` or `XdndDrop`, yet it still takes the X drag selection.
  - On KDE Wayland the app is an XWayland client, so the compositor bridges that drag. It sees a drag that nobody
    accepted and paints the refusal cursor for its whole length.
  - Nothing the app reports changes that. GitClient tried twice, once changing the effects it reported and once
    the formats it advertised.
- **The fix GitClient shipped** (`BranchesPageView`, `HistoryPageView`, `BranchDragGesture`):
  1. Press, then the threshold, as before.
  2. Then capture the pointer on the list instead of starting a platform drag.
  3. Hit-test under the pointer on every move, because a capture replaces the event's source.
  4. `list.Cursor = new Cursor(StandardCursorType.DragMove)` while over the list, the list's own cursor elsewhere.
     The cursor is created once.
  5. The release over a target drops.
  6. Escape and a lost capture cancel. A ring (`droptarget` class) marks where the drop would land.
- **Avalonia 12.1.4 checked** (`ilspycmd` on `Avalonia.Base` / `Avalonia.Controls`):
  - `PointerOverPreProcessor` sets `root.CursorElement = pointer.Captured ?? element`, and
    `GetEffectivePointerOverElement` gives `null` (hence the captured element) while a capture is held.
  - `PresentationSource.CursorElement_PropertyChanged` calls `SetCursor` when that element's `Cursor` changes.
  - `InputElement.CursorProperty` is inherited.
  - So a cursor set on the captured strip is the one on screen, wherever the pointer goes, and it updates when
    changed.
- **What a release can land on:** any part of another pane (its strip, text, hex view or "no file" placeholder),
  found with `TopLevel.InputHitTest` at the pointer and the `EditorPaneView` ancestor of the hit.
  `EditorPaneViewModel.CanTakeTab` / `TakeTab` (through `IPaneHost.CanMoveTabTo` / `MoveTabTo`) already hold the
  rules: same pane, a tab no longer in the workspace, and so on.
- **Files dropped from outside the app** keep the platform path: `DragDrop.SetAllowDrop`, `OnDragOver` / `OnDrop` and
  `DroppedFiles` stay, minus their tab branches.
- **Tests:** headless real window (`HeadlessAvaloniaFixture`, `EditorHarness`), driven with `window.MouseDown` /
  `MouseMove` / `MouseUp` and `window.KeyPress(Key.Escape, …)`. `FileDropViewTests` must pass unchanged.
- **Baseline:** taken on the run branch before the first dev.

## Decisions

- The gesture belongs to the pane the drag starts from. It captures the pointer on its `Tabs` strip, finds the pane
  under the pointer itself, and asks that pane's ViewModel whether it takes the tab.
- The pure decisions go into a small static helper beside the view, as GitClient's `BranchDragGesture` does:
  - whether a move is a drag yet;
  - which cursor to wear.

  A test can call them directly.

## PHASE01 — A tab drag the pane runs itself

**Status:** TODO

### Steps

1. **Helper** `Views/TabDragGesture.cs` (replaces `Views/DraggedTab.cs`, which is deleted):
   - `IsDrag(Vector moved, Size distance)`: true once the move is past the distance on either axis (today's rule);
   - `CursorFor(bool isOverAPane)`: `StandardCursorType.DragMove` over a pane, `null` (the strip's own cursor)
     elsewhere. Never `No`.
2. **Gesture** (`EditorPaneView.axaml.cs`):
   - the press is remembered as today (left button, on a tab, not on its ×);
   - past the threshold, instead of `StartDrag`:
     - remember the tab being dragged and the strip's own cursor;
     - capture the pointer on `Tabs`;
     - steer;
   - every move while dragging steers:
     - mark the move handled;
     - hit-test the top level at the pointer for an `EditorPaneView` of this window;
     - the strip's `Cursor` follows `CursorFor`, using a `Cursor` made once (static);
   - the release while dragging:
     - stop the gesture: capture released, cursor restored;
     - when the pane under the pointer takes the tab (`CanTakeTab`), post its `TakeTab(tab)`. Posted, as today:
       the source strip, or its whole pane, can go away with the move;
   - a lost capture while dragging stops the gesture without dropping. Whoever took the capture keeps it;
   - an internal `IsDraggingTab` for tests.
3. **Removed:**
   - `StartDrag`;
   - `DraggedTab` and its in-process format;
   - the tab branches of `OnDragOver` and `OnDrop`, and `DropTab`.

   The class remarks say why the pane does not use the platform's drag machinery for tabs, and that files dropped
   from outside still do.
4. **Tests** (`tests/.../Views/TabDragViewTests.cs`, rewritten around real pointer events):
   - a press on a tab and a 2 px move start nothing; past the distance the strip is dragging;
   - pressing a tab in the left pane, moving onto the right pane's text and releasing moves the tab there, and the
     right pane takes the keyboard;
   - dropping a pane's last tab on the other pane's strip moves it and closes the pane it left;
   - releasing over its own pane, or outside any pane, changes nothing;
   - during the drag the strip wears `DragMove` over a pane (its own included) and its own cursor off the panes;
     after the release the cursor is back;
   - a drag from the × button, or with the right button, is not a tab drag;
   - `TabDragGesture.IsDrag` and `CursorFor` answer as specified, and `CursorFor` never answers `No`.

### Acceptance criteria

- Dragging a tab onto another pane (strip, text, hex view or placeholder) moves it there, as before.
- No platform drag session is opened for a tab, so the compositor has no drag to refuse. While a tab is carried over
  the panes the pointer is the drag-move cursor, and it is back to normal after the gesture.
- Clicking, closing (×, middle click) and selecting tabs behave as before; dropped files still open as before.
- Zero warnings, whole suite green.

## PHASE02 — Escape cancels a tab drag

**Status:** TODO

### Steps

1. **`EditorPaneView.axaml.cs`:** while a tab is being dragged, a tunnelling `KeyDown` handler on the top level
   (added when the drag starts, removed when it stops) catches Escape:
   - the gesture stops: capture released, cursor restored, nothing dropped;
   - the key is marked handled.

   The handler sits on the top level because the keyboard may be in any pane's editor, and Vim must not take this
   Escape as its own. Other keys pass through.
2. **Help:** the row "Drag a tab onto another pane" reads "Move it there; Escape cancels the drag".
3. **README:** the *Tabs and panes* sentence on dragging adds that Escape cancels.
4. **Tests:**
   - Escape during a drag ends it: nothing moves on the release, the cursor is back, and the editor's Vim mode is
     unchanged (the key did not reach it);
   - a drag cancelled that way can be started again and dropped;
   - Escape with no drag still reaches the editor.

### Acceptance criteria

- Escape cancels a tab drag without moving anything, and Vim never sees that Escape.
- The help and the README say so.
- Zero warnings, whole suite green.

## PHASE03 — Highlight the pane a tab drops on

**Status:** TODO

### Steps

1. **`EditorPaneView.axaml`:** a drop hint over the whole pane. It is an overlay: not hit-testable, hidden by
   default, with an accent border and a faint accent fill. It must stay distinct from the 1 px accent frame that
   marks the active pane.
2. **`EditorPaneView.axaml.cs`:**
   - an internal `IsDropTarget` that shows or hides the hint;
   - the dragging pane sets it on the pane under the pointer when that pane takes the tab (`CanTakeTab`), and
     clears it on the previous pane;
   - every way the gesture ends (release, lost capture, Escape) clears it.
3. **Tests:**
   - over the other pane during a drag it is the drop target, and the source pane is not;
   - back over the source pane, or off the panes, no pane is;
   - after a drop, after Escape and after a lost capture, no pane is.

### Acceptance criteria

- While a tab is carried, the pane it would move to is marked, and no other is.
- The mark never stays behind after the gesture.
- Zero warnings, whole suite green.

## Out of scope

- Reordering tabs inside a pane, dropping at a position in the target's strip, or splitting by dropping on an edge.
- Dragging a tab out of the window or into another instance.
- Changing how files dropped from outside the app are handled.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `BUG` | Shipped behaviour (the pointer) is wrong. The feature itself works | `FEATURE` |
| How to fix the pointer | Stop opening a platform drag; run the gesture from pointer events with a capture | GitClient `BUG-11A4` proved, by decompiling Avalonia's X11 source, that the compositor paints the refusal for an in-window drag whatever the app reports. Two attempts there to change what the app said failed. The draft points at that fix | Reporting other effects or formats (failed twice in GitClient); a drag preview window of our own |
| The cursor | `DragMove` over any pane while dragging, the strip's own cursor off the panes, never `No` | GitClient's rule: the pointer says "a drag is under way here". The refusal pointer is the bug | `DragMove` only over a pane that would take it (the start of the gesture on its own pane would show nothing); `No` over its own pane (brings the refusal back) |
| Telling the target from the source | A drop hint on the pane that would take the tab (PHASE03) | The cursor no longer separates "here" from "not here". GitClient marks its target with a ring. The 9F56 plan deferred a highlight as "later if wanted", and this fix removes the only feedback that stood in for it | No feedback beyond the cursor; reusing the active-pane frame (ambiguous) |
| Cancelling | Escape, and a lost capture | The platform session cancelled on Escape. A gesture that brings no session must bring its own way out (GitClient `BUG-11A4-PHASE02`) | No cancel; cancelling on leaving the window |
| Where Escape is caught | A tunnelling handler on the top level, only while dragging | The keyboard may be in any pane's editor, and Vim must not see it | The pane's own handler (misses other panes' focus) |
| Drag threshold | Unchanged: platform tap size, 4 px fallback | It works and the draft does not question it | GitClient's fixed 4 px radius |
| The in-process format and `StartDrag` seam | Removed | They existed only to hand the tab to the platform and back. The gesture keeps the tab in a field | Keeping them unused |
| Files dropped from outside | Unchanged platform path | They really come from another process. Only the tab drag is in-window | — |
| When the move happens | Posted after the release | The source strip, or its whole pane, can be removed by the move while the release is still being handled | Moving inside the release handler |
| Shape | Three phases: gesture and cursor, Escape, highlight | Each is small, usable alone, and reviewable in well under 30 minutes. PHASE01 alone is the fix the draft asks for | One phase (mixes the fix with additions); two (Escape and the highlight are unrelated) |

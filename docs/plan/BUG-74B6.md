# BUG-74B6 — Wheel does not scroll the tab strip

**Status:** DONE
**Type:** BUG
**Branch:** `bugfix/bug-74b6-tab-strip-wheel`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Objective

When a pane holds more tabs than its strip can show, a horizontal scrollbar appears under them. Dragging it works, but
the mouse wheel over the strip does nothing. Make the wheel scroll the strip sideways.

## Context & constraints

- **The strip** (`Views/EditorPaneView.axaml`): `ListBox x:Name="Tabs"` with a horizontal `StackPanel`,
  `ScrollViewer.HorizontalScrollBarVisibility="Auto"`, `VerticalScrollBarVisibility="Disabled"`, a 6 px scrollbar.
- **Why the wheel does nothing:** Avalonia's `ScrollContentPresenter` scrolls each axis by its own wheel delta: a
  vertical delta moves the vertical offset only, and the strip cannot scroll vertically. Shift+wheel swaps the deltas
  (50 px per notch), and a horizontal wheel or a touchpad's sideways swipe already works. A vertical delta that moved
  nothing is not marked handled and bubbles up to the list.
- **Code-behind:** `EditorPaneView.axaml.cs` listens on `Tabs` for the drag and the middle-click close. The editor's
  own wheel (text scrolling) and the minimap's are separate controls.
- **Tests:** real window: `Views/TabDragViewTests` (headless mouse), `Views/EditorMinimapTests` (wheel).

## Decisions

- **Mapping:** a vertical wheel over the strip scrolls it sideways: down → towards the later tabs (right), up → towards
  the first (left).
- **Step:** 50 px per notch, scaled by the delta, the step Avalonia uses for Shift+wheel, so both feel the same.
- **Bounds:** clamped to the scrollable range. With no overflow, nothing happens and the event is left unhandled.
- **Untouched:** horizontal deltas and Shift+wheel (the presenter handles them first); the editor's wheel.
- **Where:** a `PointerWheelChanged` handler on `Tabs` (bubble, not `handledEventsToo`, so a delta the presenter
  already used is not applied twice). It moves the list's own `ScrollViewer`.

## Steps

1. **View** (`EditorPaneView.axaml.cs`): on `Tabs.PointerWheelChanged`, when the delta's Y is not zero and the list's
   `ScrollViewer` extent is wider than its viewport, set its `Offset.X` to `Offset.X - Delta.Y × 50`, clamped to
   `[0, Extent.Width - Viewport.Width]`, and mark the event handled when the offset changed.
2. **Tests** (real window, headless wheel, a pane with enough tabs to overflow):
   - wheel down over the strip moves its offset right; wheel up moves it back to 0;
   - many notches down stop at the end;
   - with few tabs (no overflow) the offset stays 0;
   - the editor's vertical offset does not move when the strip is wheeled.

## Acceptance criteria

- The mouse wheel over an overflowing tab strip scrolls it sideways, down towards the later tabs, and stops at both
  ends.
- Shift+wheel and horizontal scrolling behave as before; the wheel over the text scrolls the text.
- Zero warnings, whole suite green.

## Out of scope

- Scrolling the active tab into view (the list already does it on selection).
- Smooth or animated scrolling.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `BUG` | The strip scrolls, but the standard way to scroll does nothing: a defect of a working control | `FEATURE` |
| Direction | Down → right | The convention of browsers' and editors' tab strips: down goes forward | Down → left |
| Step | 50 px per notch × delta | Avalonia's own Shift+wheel step; the two gestures feel the same | One tab per notch (tabs differ in width) |
| Handled flag | Only when the offset moved | An unused wheel stays free for anything above | Always handled |
| Fix location | Pane code-behind | The strip is the pane view's; no model state is involved | A reusable attached behaviour (one user) |
| Release notes | Left to the next release item | Release items write them | Editing `RELEASENOTES.md` now |

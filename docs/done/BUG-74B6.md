# BUG-74B6 — Wheel does not scroll the tab strip

**Item:** BUG-74B6 — Wheel does not scroll the tab strip
**Branch:** `bugfix/bug-74b6-tab-strip-wheel`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

The mouse wheel over a tab strip too narrow for its tabs now scrolls it sideways. Down moves towards the later tabs and
up towards the first, 50 px per notch, and it stops at both ends.

- **Cause:** Avalonia's `ScrollContentPresenter` scrolls each axis by the wheel's own delta. The strip can only scroll
  horizontally and a plain wheel only turns vertically, so nothing moved. Only Shift+wheel, which the presenter swaps,
  and a sideways swipe scrolled it.
- **Fix** (`EditorPaneView`):
  - a `PointerWheelChanged` handler on the `Tabs` list (bubble, not `handledEventsToo`) moves the list's
    `ScrollViewer` by `-Delta.Y × TabStripWheelStep` (50, Avalonia's own Shift+wheel step), clamped to the scrollable
    range;
  - the event is marked handled only when the offset moved;
  - Shift+wheel and sideways swipes are handled by the presenter first, so they never reach the handler and are not
    applied twice.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabStripViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-74B6.md`; created this file

## Deviations & follow-ups

- None from the plan.
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md` (the run's baseline).
- **Release notes** are left to the next release item.
- **Documentation sweep:** neither the README nor the help describes how the strip scrolls. Nothing was made stale.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Finding the scroll viewer | `ListBox.Scroll` (the template's `PART_ScrollViewer`) | The list's own reference, already resolved when its template is applied. No visual-tree walk on every notch |
| "No movement" tolerance | Under half a pixel | Offsets are doubles. A clamp at an end must not count as a scroll and swallow the event |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- **Reproduced first:** with the new tests and no fix, the plain-wheel test failed (expected 50, actual 0). The
  Shift+wheel test already passed.
- **After:**
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2954 total, 2953 passed, 1 failed (the baseline's).
- **Tests added (+3, `TabStripViewTests`, real window, headless wheel):**
  - over a strip of 31 tabs: a notch down moves it 50 px. A notch up brings it back to 0, and another stays at 0. Two
    hundred notches down stop at the end. The editor's vertical offset does not move;
  - over a strip that holds its one tab, the offset stays 0;
  - Shift+wheel still moves the strip 50 px a notch.

# FEATURE-A47C-PHASE01 — Ctrl+wheel zoom

## Summary

Ctrl+wheel over the editor area (every pane: text, hex rows, tab strips, minimaps) zooms the editors' font size, one
size per notch: up zooms in, down zooms out, within 6–72. A Ctrl+wheel never scrolls; a wheel without Ctrl scrolls
as before. Fractional deltas (a precision touchpad, a high-resolution wheel, or a Windows touchpad pinch, which arrives
as Ctrl+wheel) are added up and zoom one size each time they reach a notch.

- `ZoomAccumulator` (new, `Views/`) turns the deltas into whole steps. It carries the remainder over, drops it when the
  direction reverses, tolerates floating-point sums like 10 × 0.1, and caps a huge delta at ±1000 steps.
- `SettingsMenuViewModel.ZoomByAsync(steps)` applies N sizes in one change and one settings write, through the
  existing `SetFontSizeAsync` (clamping, status item, `Changed`, write).
- `MainWindow` adds a tunnel `PointerWheelChanged` handler on `Splits`, so it sees the event before any
  `ScrollViewer`, the tab strip or the minimap.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/ZoomAccumulator.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs`, `ViewModels/HelpCatalog.cs`
- Modified: `README.md`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/ZoomAccumulatorTests.cs`, `Views/ZoomGestureTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SettingsMenuTests.cs`

## Deviations & follow-ups

- A Ctrl+wheel with no vertical delta (a horizontal tilt) is not handled, so it keeps its ordinary meaning. The plan did
  not cover this case.
- Not verified on real hardware: whether a Windows precision-touchpad pinch arrives as Ctrl+wheel in Avalonia 12.1.4.
  That is documented Windows behaviour, and the fractional path it would take is tested.
- Pre-existing failures, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`.
- Documentation sweep (§7): README's View-menu zoom sentence now mentions Ctrl+wheel; the help's *Application* section
  gained the row `Ctrl+wheel` — *Zoom in (up) or out (down), over the text*.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Floating-point sums (10 × 0.1 = 0.999…) | Tolerance of 1e-9 of a step | Otherwise a gesture would lose steps at random |
| A huge or non-finite delta | Non-finite ignored; huge capped at ±1000 steps, remainder dropped | Keeps the cast within an `int`; the font size clamps anyway |
| Ctrl+Shift+wheel | Zooms too | Ctrl is the zoom modifier; Shift adds nothing here |
| Where `ZoomAccumulator` lives | `Views/`, internal, one per window | It is pointer handling, like `TabDragGesture` |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-2BFB.md` (0 warnings; 3292 tests, 2 pre-existing failures).
- This dev: `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx`: 3328 tests, 3326 passed, 2 failed (the same two).
- Tests added (20):
  - `ZoomAccumulatorTests` (14): notches, fractions, the remainder, 100 × 0.01 at 0.1, reversal, nothing/NaN/∞, a huge
    delta, an invalid threshold.
  - `SettingsMenuTests.ZoomBy_…` (1): ±N, 0 writes nothing, clamped both ends, written to disk.
  - `ZoomGestureTests` (5, headless real window): Ctrl+wheel up/down over the text (setting, editor `FontSize`, status
    item); no scroll on Ctrl+wheel while a plain wheel still scrolls; 4 × 0.25 makes one step; over the hex view;
    outside the panes (status bar) does not zoom.
- Mutation check: with the handler's registration commented out, 4 of the 5 `ZoomGestureTests` fail. The fifth checks
  the no-zoom case. The registration was then restored.

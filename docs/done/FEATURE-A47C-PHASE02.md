# FEATURE-A47C-PHASE02 — Touchpad pinch gesture

## Summary

The touchpad's zoom gesture over the editor area zooms the editors' font size: spreading two fingers zooms in, pinching
them zooms out, one size per 0.1 of magnification, within 6–72. This covers the platforms where Avalonia reports the
gesture as `PointerTouchPadGestureMagnify` (macOS, among others). On Windows the same gesture arrives as Ctrl+wheel and
was already handled by PHASE01.

`MainWindow` adds a `PointerTouchPadGestureMagnifyEvent` handler on `Splits` with its own `ZoomAccumulator(0.1)`. It
reads the magnification from `Delta.X` and applies the whole steps through `SettingsMenuViewModel.ZoomByAsync`.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Modified: `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/ZoomGestureTests.cs`

## Deviations & follow-ups

- The plan placed the handler with `RoutingStrategies.Tunnel`; it is registered **Bubble**. Avalonia's touchpad
  gestures cannot be assumed to tunnel, and nothing inside a pane handles a magnify, so a bubble handler is the one
  that is sure to run. The tests raise the real routed event on the text and the hex view and see it handled.
- The plan's "`ZoomAccumulator` at the 0.1 threshold" test already exists from PHASE01
  (`FractionsThatDoNotAddUpExactly_StillMakeTheirSteps`, 100 × 0.01 at 0.1); no duplicate was added.
- Not verified on real hardware: that `Delta.X` carries the magnification and how large it is per event on macOS. If
  the gesture turns out too slow or too fast there, the one constant to change is the threshold (`_pinchZoom`, 0.1).
- The headless platform has no touchpad, so the tests construct and raise `PointerDeltaEventArgs` on the control under
  the fingers instead of going through the platform's raw input.
- Pre-existing failures: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` (failed
  again); `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` (flaky; passed this run).
- Documentation sweep (§7): README's zoom sentence now mentions the touchpad gesture; the help's *Application* section
  gained the row *Pinch on the touchpad* — *Zoom in (fingers apart) or out (together), over the text*.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Routing of the magnify handler | Bubble | The only routing certain to reach `Splits`; no inner control handles the event |
| Modifier keys during a pinch | Ignored | A pinch is a zoom whatever is held |
| Pointer type in the tests | `PointerType.Mouse` | Avalonia has no touchpad pointer type; touchpad gestures come from the mouse device |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-2BFB.md` (0 warnings; 3292 tests, 2 pre-existing failures).
- This dev: `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx`: 3332 tests, 3331 passed, 1 failed (`InstanceServerTests…`,
  pre-existing).
- Tests added (4, `ZoomGestureTests`, headless real window): spreading (+0.2 → +2) and pinching (−0.3 → −3) over the
  text; 4 × 0.02 does nothing and the 5th makes a step; over the hex view; over the status bar not handled, no zoom.
- Mutation check: with the magnify registration commented out, the 3 zooming tests fail. It was then restored.

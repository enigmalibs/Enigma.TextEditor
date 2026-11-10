# FEATURE-A47C — Zoom with Ctrl+wheel and pinch

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-a47c-phase01-ctrl-wheel-zoom`
**Branch (PHASE02):** `feature/feature-a47c-phase02-touchpad-pinch`
**Run:** feature/2026-11-10-set-lang-zoom-gestures

## Objective

Let the user change the editor's font size with the pointer:

- **Ctrl + mouse wheel**: up zooms in, down zooms out;
- **the zoom gesture on a laptop touchpad**: spreading two fingers zooms in, pinching them zooms out.

## Context & constraints

- The draft says the font size can only be changed from the status bar's font size button. In fact the code already
  has **View → Zoom In / Zoom Out / Reset Zoom** (Ctrl+= / Ctrl+- / Ctrl+0) and the settings page. All of them go
  through `SettingsMenuViewModel.SetFontSizeAsync(size)`, which clamps to `AppSettings.MinimumFontSize`–`MaximumFontSize`
  (6–72), applies to `EditorPreferences.FontSize` (every editor and the hex view), refreshes the status bar's list,
  raises `Changed`, and writes the settings through `SettingsWriter`, which queues writes one at a time. The zoom
  commands allow concurrent runs so a held Ctrl+= is never dropped. `SettingsMenuViewModel.Value` is private.
- `MainWindow` hosts every pane in `Splits` (`SplitHost`, files page). Its code-behind holds the
  `MainWindowViewModel` (`_viewModel.Settings`). Wheel handlers already in the panes: the tab strip
  (`OnTabsPointerWheelChanged`, scrolls the strip) and the minimap (scrolls the text). The text (AvaloniaEdit) and the
  hex rows (`ListBox`) scroll through their `ScrollViewer`s.
- Avalonia 12.1.4: `PointerWheelEventArgs.Delta` is in notches (a mouse notch is ±1; a precision touchpad or a
  high-resolution wheel sends fractions). On Windows a precision-touchpad pinch reaches the window as **Ctrl+wheel**
  with fractional deltas. Elsewhere (macOS, and wherever the platform reports it) a touchpad pinch is
  `InputElement.PointerTouchPadGestureMagnifyEvent` (`PointerDeltaEventArgs`, magnification in `Delta.X`).
- Tests: headless `window.MouseWheel(point, delta, RawInputModifiers.Control)` drives the real window
  (`TabStripViewTests`, `EditorMinimapTests`); `FontSizeStatusTests` / `EditorFontTests` check the size reaching the
  editors. Fire-and-forget work goes through `TaskExtensions.Forget()`.

## Decisions

- **Where:** the editor area only (`Splits`): text, hex rows, tab strips and minimaps. Not the explorer, the menus, the
  status bar or the settings page.
- **How:** handlers added to `Splits` with `RoutingStrategies.Tunnel`, so they run before any `ScrollViewer`, the tab
  strip or the minimap. A Ctrl+wheel is marked handled: it zooms and never scrolls. A wheel without Ctrl is left alone.
- **Steps:** a small view helper, `ZoomAccumulator` (in `Views/`), adds up deltas and returns whole steps: one step per
  1.0 of wheel delta, one per 0.1 of magnification. The remainder carries over and is dropped when the direction
  reverses. Up / spreading → larger.
- **Applying:** `SettingsMenuViewModel.ZoomByAsync(int steps)` → `SetFontSizeAsync(Value.EditorFontSize + steps)`, run
  with `Forget()`. One settings write per step, as for a held Ctrl+=. At the 6/72 limits a step does nothing, as the
  keys do.
- **Docs:** README (status bar and View menu paragraphs) and the help's *Application* section gain the pointer zoom.

## PHASE01 — Ctrl+wheel zoom

**Status:** DONE

### Steps

1. `Views/ZoomAccumulator.cs`: `int Add(double delta)` with a per-instance threshold; `Reset()`.
2. `SettingsMenuViewModel.ZoomByAsync(int steps)` (0 → nothing).
3. `MainWindow`: `Splits.AddHandler(PointerWheelChangedEvent, …, RoutingStrategies.Tunnel)`. With Ctrl held: accumulate
   `Delta.Y`, apply the whole steps, `e.Handled = true`. Without Ctrl: untouched.
4. Help catalog: `Keys("Ctrl+wheel", "Zoom in and out")` (or merged into the existing zoom row). README: Ctrl+wheel
   beside the zoom keys and the status bar's font size item.
5. Tests:
   - `ZoomAccumulator` unit tests: whole notches; fractions adding up (e.g. 4 × 0.25 → one step); the remainder carried
     over; a reversal dropping the remainder; negative steps.
   - `SettingsMenuViewModel.ZoomByAsync`: +2 / −3 / clamped at the limits / 0 does not write.
   - Headless: Ctrl+wheel up over the text raises the font size by one (`EditorPreferences.FontSize`, the editor
     control's `FontSize`, the status bar's text); down lowers it; over the hex view too; the text does not scroll
     during a Ctrl+wheel; a plain wheel still scrolls and does not zoom; fractional Ctrl+wheel deltas zoom once they
     add up to a notch; Ctrl+wheel over the explorer does not zoom.

### Acceptance criteria

- Ctrl+wheel over a pane zooms in (up) and out (down), one size per notch, within 6–72. The text does not scroll.
- Fractional deltas (precision touchpad, Windows pinch) zoom once they add up to a notch.
- A wheel without Ctrl scrolls exactly as before.
- Zero new warnings; the whole suite green against the baseline.

## PHASE02 — Touchpad pinch gesture

**Status:** TODO

### Steps

1. `MainWindow`: `Splits.AddHandler(PointerTouchPadGestureMagnifyEvent, …, RoutingStrategies.Tunnel)` with its own
   `ZoomAccumulator` (threshold 0.1): accumulate `Delta.X`, apply the whole steps, mark it handled.
2. Help/README: "pinch on the touchpad" beside Ctrl+wheel.
3. Tests:
   - `ZoomAccumulator` at the 0.1 threshold.
   - Headless: a `PointerDeltaEventArgs` raised on the text with `PointerTouchPadGestureMagnifyEvent` (positive →
     larger, negative → smaller, small deltas adding up); raised on the explorer → no zoom.

### Acceptance criteria

- A touchpad magnify gesture over a pane zooms in (spread) and out (pinch).
- Zero new warnings; the whole suite green against the baseline.

## Out of scope

- Touchscreen pinch (`PinchGestureRecognizer`): the draft asks for the touchpad.
- Fewer settings writes during a long gesture (e.g. writing once the gesture stops): today's held Ctrl+= writes
  every step too.
- Zooming the UI (menus, status bar, explorer): the zoom is the editor's font size.
- Ctrl+wheel on the settings page's font size box.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which area responds to Ctrl+wheel / pinch? | The editor area (`Splits`) | The zoomed text is there; the explorer and settings page are not zoomed | The whole window (Ctrl+wheel over the settings page's number box would be ambiguous); only the text control (the hex view and tab strips would miss out) |
| Handler placement | `MainWindow`, tunnel handlers on `Splits` | One place for every pane, text or hex, ahead of every `ScrollViewer`; the window already holds the settings view-model | A handler in each `EditorPaneView` (needs the settings view-model in every pane) |
| Wheel direction | Up = zoom in | Browsers, VS Code and Office do the same | — |
| Fractional deltas | Accumulate to whole steps, threshold 1.0, reset on reversal | Precision touchpads and high-resolution wheels send fractions; Windows sends the pinch that way | Rounding each event (a touchpad would zoom far too fast, or never) |
| Magnify step | One size per 0.1 of magnification | A full spread is roughly 0.5–1.0 of magnification, so 5–10 sizes; Ctrl+wheel and the keys step one size at a time too | Scaling proportionally from the gesture's start (needs begin/end phases Avalonia does not report) |
| Touchscreen pinch | Out of scope | The draft names the touchpad; a recognizer on the editor could also disturb touch scrolling/selection | Adding `PinchGestureRecognizer` |
| Settings writes during a gesture | One per step, as today | `SettingsWriter` queues them; held Ctrl+= already does this | Writing once the gesture stops (new timing logic for no reported problem) |
| Applying N steps | `SettingsMenuViewModel.ZoomByAsync(steps)` | `Value` is private; one method keeps the clamping and the write in one place | Calling `ZoomInCommand` N times (N writes for one event) |
| Phases | Ctrl+wheel, then the magnify gesture | Ctrl+wheel also covers Windows pinch and can ship alone; each is a small review | One dev |

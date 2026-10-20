# BUG-0D06 — Status bar button text too high

**Item:** BUG-0D06 — Status bar button text too high
**Branch:** `bugfix/bug-0d06-status-button-alignment`
**Run:** vibe/2026-10-20-split-close-statusbar

## Summary

The status bar's buttons now show their text on the same line as `Ln x, Col y`, to the pixel. The
buttons are *Spaces*, encoding, language and line endings. The bar keeps its height (26).

**The cause:** the theme gives a button `VerticalContentAlignment="Stretch"`.

- The button's text box filled the whole 22-pixel button and drew its line at the top.
- That put it 2px above the caret text, whose 16-pixel box is centred in the bar.

**The fix:** `Button.statusItem` now stretches to the bar's height and centres its content.

- The button text is centred in exactly the span the plain status texts are centred in, so the two
  land on the same pixel row.
- `MinHeight="22"` went: the bar gives the buttons its own height.
- On hover, a button's fill spans the bar's height, as status-bar items do in VS Code.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — the `Button.statusItem` style and its
  comment.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/MainWindowEditorTests.cs` — for each of the four
  buttons, in the real window: its text box is as tall as the caret text's and starts on the same
  row. The bar is still 26 high (4 cases).
- `docs/roadmap.md`, `docs/plan/BUG-0D06.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the test says "on the same line" | Same text-box height and same top, within half a pixel | Same font and size: equal boxes at equal heights draw the same line. A centre-only check would accept a text drawn at the top of a taller box | Comparing centres only |
| Check the test is meaningful | Ran it against the old style (fails: text box 22 high, not 16) and against centring the content alone (fails: 1px low, row 780 instead of 779). Then restored the planned style | Proves both the bug and the rejected alternative, measured rather than assumed | — |

## Deviations & follow-ups

- None from the plan.
- Not done: no manual look at the running Debug build. The alignment is measured headlessly in the real
  window, which uses the application's theme and fonts.
- Line endings: the touched files keep their endings; no CRLF churn.

## Documentation sweep

Nothing became wrong: no user-facing doc describes the status bar's layout.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (no AVLN warnings).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2449 passed**, 0 failed, 0 skipped (2445 before,
  plus 4 new).

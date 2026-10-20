# BUG-0D06 — Status bar button text too high

**Status:** DONE — see `docs/done/BUG-0D06.md`
**Type:** BUG
**Branch:** `bugfix/bug-0d06-status-button-alignment`
**Run:** vibe/2026-10-20-split-close-statusbar

## Objective

The status bar's buttons (indentation, encoding, language, line endings) show their text on the same
line as `Ln x, Col y`.

## Context & constraints

- Reported: `Ln x, Col y` is vertically centred, but the button texts sit a little too high. Fix the
  button style, or make the bar a little bigger, "but not too much".
- Measured headlessly in the real window:
  - the bar is 26 high (`MinHeight`, 1px top hairline);
  - `CaretStatus` (16 high) is centred in it;
  - each `Button.statusItem` is 22 high (`MinHeight="22"`, `VerticalAlignment="Center"`).
- Its `VerticalContentAlignment` is `Stretch`. The content's text box is 22 high and draws its line at
  the top, about 2px above the caret text.
- Centring the content in a 22-high button that is itself centred lands 1px off the caret text. Layout
  rounding of the two half-pixel offsets differs.

## Decisions

| Decision | Rationale |
|---|---|
| `Button.statusItem`: `VerticalAlignment="Stretch"` and `VerticalContentAlignment="Center"`; `MinHeight` dropped | The text is centred in the very span the caret text is centred in, so they line up to the pixel. The hover fill spans the bar's height, as in VS Code |
| The bar keeps its height (26) | Not needed once the content is centred; the user preferred no more than a little growth |

## Steps

1. `Views/MainWindow.axaml`: the `Button.statusItem` style as decided, with its comment updated.
2. Test (headless, real window): with a text tab, the text of each status button is vertically
   centred on the same line as `CaretStatus`, within half a pixel. The bar's height is unchanged.
3. The whole existing suite stays green.

## Acceptance criteria

- The four buttons' texts are on the caret text's line.
- The bar's height is unchanged.
- Zero warnings, whole suite green.

## Out of scope

- Restyling the status bar otherwise: mode chip, fonts, colours.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Restyle the buttons or grow the bar | Restyle the buttons | Root cause is the content alignment; no size change needed | A taller bar (moves the text, does not centre it) |
| How to centre | Stretch the button to the bar, centre its content | Pixel-exact match with the caret text | Only `VerticalContentAlignment="Center"` (1px off after rounding) |

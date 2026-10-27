# BUG-78F9-PHASE01 — Link colours

**Item:** BUG-78F9 — Readable colours in the dark theme (PHASE01 of 2)
**Branch:** `bugfix/bug-78f9-phase01-link-colours`
**Run:** bugfix/2026-10-27-link-colours-release

## Summary

AvaloniaEdit marks URLs and e-mail addresses in the text as links and paints them with
`TextView.LinkTextForegroundBrush`. Nothing set that brush, so links came out in AvaloniaEdit's default
`Brushes.Blue` (`#0000FF`). On the dark editor background (`#1E1F22`) that is 1.92:1.

- **`App.axaml`**: a theme-dictionary brush `EditorLinkBrush` sits beside the minimap brushes.
  - Dark: **`#7AABFF`**, 7.14:1 on the background and 5.72:1 on the current line.
  - Light: **`#1F5FCC`**, 5.54:1 on the background. It is no longer Light+'s keyword blue.
  - Both are on the Enigma accent's hue (218° against `#3574F0`'s 220°).
- **`EditorAppearance`** assigns the brush along with the other chrome brushes, in its constructor and in
  `Apply`. Links therefore follow a theme switch.

## Files / modules touched

- Modified: `src/Enigma.TextEditor.Desktop/App.axaml`, `src/Enigma.TextEditor.Desktop/Views/EditorAppearance.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorAppearanceTests.cs`, `docs/roadmap.md`,
  `docs/plan/BUG-78F9.md`.

## Deviations & follow-ups

- None from the plan.
- The colour was chosen by rendering six candidates per variant beside Dark+ / Light+ token colours, using a
  throwaway headless probe that is not committed. The real main window was rendered again in Dark after the change.
- Documentation sweep: nothing became wrong. README's "Colours follow the theme" still holds.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Test the colour by value or by property | Same instance as the variant's `EditorLinkBrush`, not `Colors.Blue`, and ≥ 4.5:1 against the variant's `EnigmaBackgroundColor` | Asserts what the bug is about, readability, rather than pinning a hex value |
| Test level | `EditorAppearance` directly, not a dark `MainWindow` | No test switches the application's variant; the pane builds its colouring through this class for both creation and switches |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2740 total, 2740 passed (baseline 2737).
- Tests added:
  - `Links_ArePaintedInTheVariantsLinkColour_ReadableOnTheBackground(dark: true / false)`;
  - `ASwitchOfVariant_RepaintsTheLinks`.
- Fix cycles: 0.

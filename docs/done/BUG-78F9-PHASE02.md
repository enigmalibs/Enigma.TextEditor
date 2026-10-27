# BUG-78F9-PHASE02 — Settings hints and hex row

**Item:** BUG-78F9 — Readable colours in the dark theme (PHASE02 of 2)
**Branch:** `bugfix/bug-78f9-phase02-hints-hex-row`
**Run:** bugfix/2026-10-27-link-colours-release

## Summary

The audit's two findings beyond the links:

- **Settings hint lines** (`TextBlock.preferenceHint`). These are the 11 px line under each setting and the
  header's "Changes apply at once…".
  - They move from `EnigmaForegroundTertiaryBrush` to `EnigmaForegroundSecondaryBrush`.
  - Dark: from 1.74–1.90:1 to 2.90–3.18:1. Light: from 2.15–2.27:1 to 4.06–4.29:1.
  - Secondary is the palette's tone for descriptions, and the cards' own description line already uses it.
- **The hex view's selected row** is filled with `EnigmaSelectionBrush`, the editor's selection colour. It no
  longer uses Fluent's band of the operating system's accent, a saturated stripe in dark. The style follows the
  tab strip's own `ListBoxItem:selected` override.

## Files / modules touched

- Modified:
  - `src/Enigma.TextEditor.Desktop/Views/SettingsPageView.axaml`
  - `src/Enigma.TextEditor.Desktop/Views/HexView.axaml`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SettingsPageTests.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HexViewTests.cs`
  - `docs/roadmap.md`
  - `docs/plan/BUG-78F9.md`

## Deviations & follow-ups

- None from the plan.
- Follow-up, for `Enigma.Avalonia.Desktop`, not this app: the palette's secondary tone `#6F737A` is the same in both
  variants, so it is only about 3:1 on the dark surfaces. That affects descriptions, inactive tab titles and status
  bar items. A lighter Dark value would lift them all, but the palette is the library's.
- Left as they are by design, and named in the plan: About's build line (tertiary, deliberately quiet), line
  numbers, the empty-pane placeholder, the search highlight and the minimap's viewport.
- Documentation sweep: nothing became wrong. README describes neither colour.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which selected states to restyle | `:selected` only | Styles outrank the control theme's `:selected:pointerover` / `:pressed`; a hovered unselected row keeps Fluent's faint grey |
| How to test the hex row | Same instance as `EnigmaSelectionBrush` on the selected row's presenter, not on the row above | Proves the override applies to the selected row only, independent of the OS accent |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2742 total, 2742 passed.
- Tests added:
  - `SettingsPageTests.EveryHint_IsInThePalettesDescriptionTone`;
  - `HexViewTests.TheSelectedRow_IsMarkedInTheSelectionColour_NotTheSystemAccent`.
- Fix cycles: 1. A missing `using Avalonia.Controls.Presenters;` in the new hex test.
- Visual check: the hex view and the settings page were rendered in Dark with a throwaway headless probe that is not
  committed.

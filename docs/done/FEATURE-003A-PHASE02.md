# FEATURE-003A-PHASE02 — Editor font family & size

**Item:** FEATURE-003A — Preferences dialog & fonts (PHASE02 of 2)
**Branch:** `feature/feature-003a-phase02-editor-fonts`
**Run:** bugfix/2026-10-23-minimap-preferences-fonts

## Summary

The Preferences dialog opens with a **Text** card. It has a *Font* dropdown and a *Font size* field
(6–72):

- the font is drawn in every pane's editor and in the hex view's rows;
- the choice applies at once and is remembered;
- the font size is the setting the zoom keys change, so the field and Ctrl+= / Ctrl+- / Ctrl+0 always
  show the same value.

The font list:

- **Default** (the built-in monospace stack) comes first;
- then the monospace faces this machine has. The probe is Enigma.GitClient's: `i` and `W` measure the
  same width, and a face the font manager cannot draw is treated as not monospace;
- then a saved family the machine lacks, so the dropdown still shows it.

On the development machine the probe keeps 15 of 225 system families, in 111 ms, the first time the
dialog opens: Cascadia Code, Cascadia Mono, Consolas, Courier New, Lucida Console…

`AppSettings.EditorFontFamily` stores the choice (empty = built-in stack). `EditorPreferences.FamilyFor`
puts the built-in stack behind the chosen family. A family the machine does not have therefore falls
back to a monospace face, not to the interface's Inter. A settings file without the key opens with the
built-in stack.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Services/MonospaceFonts.cs`: `IsMonospace` and `InstalledNames`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorFontTests.cs`, 7 cases:
  - a fresh install uses the built-in stack;
  - a chosen family reaches both panes with the stack behind it, and is written;
  - the hex rows take the family and the size;
  - *Default* clears the setting;
  - the list: Default first, monospace faces only, a missing saved family last;
  - a proportional face is not monospace;
  - the size field and zoom move one setting, and the dialog is told.
- `docs/done/FEATURE-003A-PHASE02.md`

**Modified**

- `src/Enigma.TextEditor.Core/Settings/AppSettings.cs`: `EditorFontFamily`.
- `src/Enigma.TextEditor.Desktop/ViewModels/EditorPreferences.cs`:
  - `FontFamily` is settable and observable, seeded from the setting;
  - `MonospaceStack` constant and `FamilyFor`.
- `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs`: `SetFontFamilyAsync`; `ResetAsync`
  also resets the family.
- `src/Enigma.TextEditor.Desktop/ViewModels/PreferencesViewModel.cs`:
  - `DefaultFontLabel`, `Fonts` (built on first read), `EditorFont` and `EditorFontSize`;
  - both are announced with the rest.
- `src/Enigma.TextEditor.Desktop/Views/PreferencesView.axaml`:
  - the *Text* card, first;
  - fields 200 px wide, so font names fit.
- `src/Enigma.TextEditor.Desktop/Views/HexView.axaml`: the rows' hard-coded family is gone, so they
  inherit.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml`: the hex panel passes
  `TextElement.FontFamily` down, as it does the size.
- Tests:
  - `tests/Enigma.TextEditor.Core.UnitTests/Settings/AppSettingsTests.cs`:
    - the default and the round trip include the family;
    - a file from before the setting uses the built-in stack.
  - `tests/…Desktop.UnitTests/ViewModels/PreferencesTests.cs`:
    - font and size in "every preference" (10 writes);
    - reset restores the family and the stack.
  - `tests/…Desktop.UnitTests/Views/PreferencesDialogTests.cs`:
    - *Text* is the first card;
    - the font and size controls show and change the settings;
    - the size field's limits are `AppSettings`'.
- `README.md`: the *Settings* paragraph (see the sweep below).
- `docs/roadmap.md`, `docs/plan/FEATURE-003A.md`: statuses. The item is done.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| A chosen family the machine lacks | The family, with the built-in stack behind it (`"Chosen, Cascadia Mono, …, monospace"`) | Avalonia's own fallback is the interface font (Inter), which is proportional. The stack keeps a carried-over settings file monospace | A bare `new FontFamily(name)`; dropping the saved name when it is not installed |
| Where the font probe lives | `Services/MonospaceFonts`, a static class | Pure function of the font manager. The list is built lazily by the view-model, as GitClient's page builds its list | An injected catalog interface (nothing to swap, and one more registration) |
| The probe itself | GitClient's `FormattedText` width of `i` vs `W`, catching `InvalidOperationException` | Proven on this Avalonia version in the sibling app. 111 ms for 225 families here | Glyph-typeface metrics (`IsFixedPitch`): an API not used anywhere in the house yet |
| Hex rows | Inherit family and size from the pane's panel; the style's family is gone | The style setter outranked inheritance. The panel already carried the size the same way | Binding inside `HexView` (its data context is the tab, not the preferences) |
| The help dialog's key columns and the ex/search line | Keep the built-in stack | They are interface text; the setting is the document's font (the plan's scope) | Following the editor's font |
| A preview of each family in the dropdown | Not done | Not asked; GitClient lists names | Drawing each item in its own face |

## Deviations & follow-ups

- **Step 3 of the plan** ("a font catalog") became `MonospaceFonts`, a static class in `Services`.
- **Step 7** (hex view) needed a change in two files: the `HexView` style and the pane's panel.
- **`FamilyFor`'s fallback stack** is beyond what the plan wrote. It comes from the plan's own
  acceptance criterion, "a family no longer installed … falls back".
- **Fix budget:** none used. The first full build and test run were green.
- **Not verified on Linux or macOS:**
  - the font list there depends on the installed faces;
  - the test that a proportional face is not monospace uses `FontFamily.Default`, which is a
    proportional face on the platforms this app ships for.
- **Follow-up candidate, not asked:** interface text size (menus, status bar, dialogs). It was out of
  scope by the plan's decision, because Avalonia has no uniform UI scale.
- **Line endings:** no line-ending noise in this dev's diff; nothing to recommend.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2706 passed, 0 failed. It was 2698 after PHASE01;
  this dev adds 8 (7 Desktop, 1 Core) and extends 4 existing tests.
- **Documentation sweep:** `README.md`'s *Settings* paragraph named the dialog's groups as Appearance,
  Editing and New files, and said only zoom set the font size. It now names the *Text* group and its
  font choice, and says zoom and the preferences set the same size. Nothing else was stale.

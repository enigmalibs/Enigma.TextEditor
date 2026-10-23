# FEATURE-003A-PHASE01 — Preferences dialog

**Item:** FEATURE-003A — Preferences dialog & fonts (PHASE01 of 2)
**Branch:** `feature/feature-003a-phase01-preferences-dialog`
**Run:** bugfix/2026-10-23-minimap-preferences-fonts

## Summary

*Settings → Preferences…* (Ctrl+,) opens a **Preferences** dialog in the window's content dialog. It
is built like Enigma.GitClient's settings page:

- one `SettingsCardExpander` per category, all open, in one scroll area kept inside the dialog card
  (`DialogFit`, as the help does);
- rows with the setting's name, a quieter line saying what it does, and the control at the trailing
  end.

The categories:

- **Appearance**: theme (ComboBox), minimap (switch), wrap long lines (switch).
- **Editing**: Vim mode (switch); pane resize step, 1–50 % (number field).
- **New files**: indent with tabs (switch); indent size, 1–16 (number field); line endings, LF or CRLF
  (ComboBox).

How changes behave:

- Every change applies at once and is written at once, as in the menu. The only button is *Close*.
- *Reset to defaults* opens a flyout whose one item puts every preference back to its default. The
  dialog host holds one dialog at a time, so the flyout is the confirmation.
- The menu and the dialog are two views of the same preferences. `PreferencesViewModel` changes
  nothing itself: each setter calls a `Set…Async` method of `SettingsMenuViewModel`, which applies and
  writes the change. Every change (from the menu, the zoom keys or the dialog) raises
  `SettingsMenuViewModel.Changed`, and the dialog announces all its values again. Within the setting's
  range, any value can be set; the menu then checks none of its presets.
- Esc or *Close* closes the dialog, and `DialogKeyboard` gives the keyboard back to the editor.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/ViewModels/PreferencesViewModel.cs`:
  - values and option lists;
  - `ResetCommand`;
  - all values announced again under an `_announcing` guard (GitClient's pattern).
- `src/Enigma.TextEditor.Desktop/Views/PreferencesView.axaml`, `.axaml.cs`:
  - the three cards, the reset flyout, the fitted scroll area;
  - the `ThemeText` and `LineEndingText` converters.
- `src/Enigma.TextEditor.Desktop/Services/IPreferencesDialogService.cs`,
  `src/Enigma.TextEditor.Desktop/Services/PreferencesDialogService.cs`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakePreferencesDialogService.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeSettingsWriter.cs`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/PreferencesTests.cs`, 8 cases:
  - every value applies and is written;
  - the same value again is not written;
  - steps and sizes the menu does not offer;
  - clamping;
  - menu changes are announced;
  - a write-back during the announcement is ignored;
  - reset restores, applies and writes every default.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/PreferencesDialogTests.cs`, 4 cases:
  - the menu item, its gesture, and Ctrl+, typed in the editor;
  - the dialog's title, button, cards and one scroll area, and Esc giving the editor the keyboard back;
  - the controls show and change the settings and follow the menu;
  - the number limits match `AppSettings`, and reset runs from the flyout.
- `docs/done/FEATURE-003A-PHASE01.md`

**Modified**

- `src/Enigma.TextEditor.Core/Settings/AppSettings.cs`:
  - `MinimumIndentSize` and `MaximumIndentSize` constants;
  - `IndentSize`'s `[Range]` uses them.
- `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs`:
  - public `SetThemeAsync`, `SetVimEnabledAsync`, `SetWordWrapAsync`, `SetMinimapAsync`,
    `SetPaneResizeStepAsync` (now clamped), `SetDefaultIndentationAsync` (size clamped),
    `SetDefaultLineEndingAsync`, `SetFontSizeAsync` and `ResetAsync`;
  - the `Changed` event;
  - the toggles call the setters;
  - the "no preferences dialog" remark is replaced.
- `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs`: `PreferencesCommand`, through
  `IPreferencesDialogService`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`: *Application* lists `Ctrl+,` →
  Preferences.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml`: the separator and *Preferences…* at the end
  of *Settings*, and the `Ctrl+OemComma` key binding.
- `src/Enigma.TextEditor.Desktop/Extensions/ServiceCollectionExtensions.cs`: `PreferencesViewModel` and
  `IPreferencesDialogService` singletons.
- `tests/…/Infrastructure/EditorHarness.cs`: `PreferencesPage` and `PreferencesDialog`.
- `tests/…/CompositionRootTests.cs`: resolves the two new services, and the new constructor argument.
- `tests/…/ViewModels/SettingsMenuTests.cs`: the Settings menu ends with `_Preferences…`.
- `README.md`: the *Settings* paragraph (see the sweep below).
- `docs/roadmap.md`, `docs/plan/FEATURE-003A.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the dialog follows the menu and the zoom keys | A `Changed` event on `SettingsMenuViewModel`, raised once per applied change before the write. The dialog announces each of its properties by name | Not tied to the menu's `Is…` names. GitClient's page does the same on its store's `Changed` | Mapping the menu's property names to the dialog's; `OnPropertyChanged(string.Empty)` |
| Write-back loops | `_announcing` guard, and no change when the value is the same | GitClient's pattern. A control that pushes a value back while it is being told one must not start a change | Relying on Avalonia never writing back |
| Reset confirmation | A `MenuFlyout` on the button with one item | The dialog host shows one dialog at a time, so GitClient's confirmation dialog cannot open over this one. A flyout is still a second, deliberate click | No confirmation; a two-state button |
| Reset writes | One `ResetAsync` that sets everything and writes once | One write and one announcement instead of nine | Calling each setter (nine writes) |
| Indentation control | "Indent with tabs" switch + "Indent size" number field | The setting is a bool and an int, and the field reaches 1–16 | A Spaces/Tabs ComboBox (needs a third type only for the dialog) |
| Out-of-range values | Clamped in the menu's setters (resize step 1–50, indent 1–16), as zoom already was | The number fields have limits, but the setters are public and must not write a value the loader would reject | Trusting the caller |
| Keyboard on open | Left to `DialogKeyboard`, which focuses the default *Close* button and gives the keyboard back on close | Already does both for every dialog. The view needs no focus code of its own, unlike the help | Focusing the first control |
| Number-field limits in XAML | Literals (1–50, 1–16), checked against the `AppSettings` constants by a test | `x:Static` of an `int` const into a `decimal` property is not something the XAML compiler converts | `x:Static`; binding to VM properties |
| Ctrl+, in the help | Added to *Application* | That section lists the window's own shortcuts (F1, zoom). A new one missing from it would make the help wrong | Leaving it out |

## Deviations & follow-ups

- **Plan step 1 said "internal/public".** They are public, because `PreferencesViewModel` is a public
  type in the same layer. The menu's setters now also clamp, which the plan did not mention.
- **Not in the plan's steps:**
  - the `Changed` event (the plan only said "a change notification the dialog can follow");
  - `ResetAsync` on the menu view-model;
  - the help row for Ctrl+,.
- **Plan step 3 said the view "hands the keyboard back on close, as `HelpView` does".**
  `DialogKeyboard` already does it for a view that takes no focus. A test checks that the editor has
  the keyboard after Esc.
- **Fix budget: 1 cycle of 3 used.** The first run had one red test. The reset item's `Command` was
  read before its flyout had opened, when a flyout's items have no data context yet. The test now opens
  the flyout and runs the reset from the item. The view was not changed.
- **Follow-up:** PHASE02 adds the *Text* card (font family and size), and `ResetAsync` gains the font
  family.
- **Line endings:** no line-ending noise in this dev's diff; nothing to recommend.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2698 passed, 0 failed. It was 2684 after BUG-7624;
  this dev adds 14.
- **Documentation sweep:** `README.md`'s *Settings* paragraph said a resize step other than 2/5/10 could
  only be set in `settings.json`. It now says the preferences set any step from 1 to 50, and it names
  *Settings → Preferences…* (Ctrl+,) and its three groups. Nothing else was stale.

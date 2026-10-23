# FEATURE-003A — Preferences dialog & fonts

**Status:** TODO
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-003a-phase01-preferences-dialog`
**Branch (PHASE02):** `feature/feature-003a-phase02-editor-fonts`
**Run:** bugfix/2026-10-23-minimap-preferences-fonts

## Objective

- A configuration page like Enigma.GitClient's settings page, opened from *Settings → Preferences…*,
  with its settings grouped by category in SettingsCardExpanders.
- Editor font family and font size settings on it, because on some screens the text is far too small.

## Context & constraints

- GitClient: `src/Enigma.GitClient.Desktop/Views/Pages/SettingsPageView.axaml`.
  - A header with the title and a *Reset to defaults* button.
  - A scroll area holding a centred column (max 900 px) of `controls:SettingsCard` /
    `controls:SettingsCardExpander`, each with `{ei:IconGeometry …}`, a header and a description.
  - Inside each expander, rows built as `Grid ColumnDefinitions="*,Auto" ColumnSpacing="12"`: a label
    (plus an 11 px tertiary hint) on the left, a ComboBox / NumericUpDown / ToggleSwitch on the right,
    each with `AutomationProperties.Name`.
  - Its font list is `DiffTypography.MonospaceFamilies()`: system fonts whose `i` and `W` measure the
    same width. *Default* comes first; a saved name that is no longer installed is kept.
- Here, the app has no navigation shell: it is one window with a menu bar.
  - Dialogs are the window's `ContentDialog` host (`DialogMaxWidth` 860, `DialogMaxHeight` 640),
    driven by `IContentDialogService` (`AboutDialogService`, `HelpDialogService`).
  - `HelpView` already shows SettingsCardExpanders in a scroll area that `DialogFit` keeps inside the
    card, and gives the keyboard back when it closes.
- `SettingsMenuViewModel` applies every preference at once and writes it at once (`ISettingsWriter`,
  which serializes writes and reports a failure once). The *Settings* menu has Theme, Vim Mode,
  Word Wrap, Minimap, Pane Resize Step (2/5/10 %), Default Indentation, Default Line Endings. *View*
  has Zoom In / Out / Reset (`EditorFontSize`, 6–72, default 14).
- `EditorPreferences` (singleton) carries the live editor look: `FontSize`, `WordWrap`, `ShowMinimap`,
  `PaneResizeStep`. `FontFamily` is fixed to `MonospaceFamily`.
- `HexView.axaml` hard-codes the same monospace stack in a style. Its size comes from the pane's
  `TextElement.FontSize`.

## Decisions

| Decision | Rationale |
|---|---|
| A `PreferencesView` in the window's ContentDialog, opened by *Settings → Preferences…* (Ctrl+,) and by `MainWindowViewModel.PreferencesCommand` through an `IPreferencesDialogService` | The app has no page shell. About and Help are dialogs the same way. Ctrl+, is the usual shortcut for preferences |
| Settings apply at once and are written at once. One *Close* button, plus *Reset to defaults* in the dialog | Same model as the menu and as GitClient's page; nothing to lose on a crash |
| `PreferencesViewModel` (singleton) exposes two-way values and option lists. It changes settings only through `SettingsMenuViewModel`, which stays the one place that applies and writes them | One code path per setting. Menu and dialog stay in sync through property notifications |
| The *Settings* menu keeps its direct items. *Preferences…* is added last, after a separator | Quick toggles and their Vim hints stay; the dialog adds the full ranges |
| Categories: **Appearance** (theme, minimap, word wrap), **Editing** (Vim mode, pane resize step 1–50 %), **New files** (indent with spaces/tabs, indent size 1–16, line endings). PHASE02 adds **Text** (font, size), first | The draft's "regroup the settings by category"; each setting's full validated range |
| Every expander starts open, inside one scroll area fitted to the dialog card (`DialogFit`) | Every setting is one scroll away. Same pattern as the help dialog |
| Font settings cover the editor text and the hex view. Interface text (menus, status bar, dialogs) is out of scope | "The text" of a text editor is its document. Avalonia offers no uniform UI scale (popups are separate top-levels, sizes are explicit across views) |
| Font list: installed monospace families, *Default* first (the built-in stack). A saved family no longer installed stays listed | GitClient's behaviour. Vim's column motions and the hex rows need a fixed advance |
| `AppSettings.EditorFontFamily` (string, empty = default). The size stays `EditorFontSize`, shared with zoom | A missing key keeps working; zoom and the dialog never disagree |

## PHASE01 — Preferences dialog

**Status:** TODO

### Steps

1. `SettingsMenuViewModel`:
   - make the apply-and-write operations reusable (internal/public `Set…Async` per setting: theme,
     Vim, word wrap, minimap, resize step, indentation, line ending, font size);
   - each raises a change notification the dialog can follow. The menu's `Is…` flags keep working;
   - update the "no preferences dialog" remark.
2. `ViewModels/PreferencesViewModel`:
   - properties: `Theme`, `ShowMinimap`, `WordWrap`, `VimEnabled`, `PaneResizeStep`,
     `IndentWithTabs` (or an indentation-kind choice), `IndentSize`, `DefaultLineEnding`;
   - option lists for the ComboBoxes, with readable labels;
   - `ResetCommand`, which puts every setting back to its `AppSettings` default;
   - follows `SettingsMenuViewModel`'s notifications, so a menu change or a zoom shows in the open
     dialog.
3. `Views/PreferencesView.axaml(.cs)`:
   - GitClient's row layout inside `controls:SettingsCardExpander`s with Phosphor icons;
   - one scroll area fitted with `DialogFit`;
   - hands the keyboard back on close, as `HelpView` does.
4. `Services/IPreferencesDialogService` + `PreferencesDialogService`: title "Preferences", content
   the view, one *Close* button.
5. `MainWindowViewModel.PreferencesCommand`; `MainWindow.axaml`: *Settings → Preferences…*
   (`InputGesture="Ctrl+OemComma"`) and the window `KeyBinding`.
6. DI registrations in `AddShell`; test fakes (`FakePreferencesDialogService`) and `EditorHarness`
   wiring.
7. Tests:
   - VM: each property applies (preferences, theme service, Vim session) and is written;
   - VM: a menu change is reflected in the VM's notifications;
   - VM: reset restores every default and writes;
   - headless: the menu item exists with its gesture and opens the dialog; the view has one expander
     per category, open; Esc closes it;
   - the composition root resolves the new service.

### Acceptance criteria

- *Settings → Preferences…* and Ctrl+, open a dialog with Appearance, Editing and New files expanders.
- Every control changes its setting at once, survives a restart, and agrees with the Settings menu
  both ways.
- *Reset to defaults* puts every preference back to its default.
- Esc or *Close* closes it, and the keyboard goes back to the editor.
- Zero warnings, whole suite green.

## PHASE02 — Editor font family & size

**Status:** TODO

### Steps

1. `Core/Settings/AppSettings.cs`: `EditorFontFamily` (string, default empty = the built-in monospace
   stack). Core tests: default, round-trip, a file without the key.
2. `EditorPreferences.FontFamily`: settable and observable, resolved from the setting (empty →
   `MonospaceFamily`).
3. A font catalog: installed monospace families (the `i`/`W` probe, failures treated as "not
   monospace"), name-ordered, built lazily (no font manager in non-UI tests).
4. `SettingsMenuViewModel.SetFontFamilyAsync`, applied live and written.
5. `PreferencesViewModel`: `Fonts` (*Default* first, the saved name kept if missing), `EditorFont`,
   `EditorFontSize` (6–72). Reset includes both.
6. `PreferencesView`: a **Text** expander, first, with *Font* (ComboBox) and *Font size*
   (NumericUpDown 6–72).
7. `HexView`: rows take the preferences' family (and keep the size they already follow).
8. Tests:
   - choosing a family changes every pane's editor `FontFamily` and the hex rows, and is written;
   - *Default* clears the setting;
   - the font size field and zoom move the same setting, and the dialog follows zoom;
   - a saved but missing family stays in the list.

### Acceptance criteria

- *Preferences → Text* sets the editor's font family and size. Every pane and the hex view follow at
  once, and the choice is remembered.
- Zoom and the font size field always show the same value.
- A settings file without the font key opens with the built-in monospace stack.
- Zero warnings, whole suite green.

## Out of scope

- Interface (menu, status bar, dialog) text size or font; per-language or per-file fonts.
- Proportional fonts, ligature or line-height settings.
- A search box over the preferences.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the configuration page lives | ContentDialog from *Settings → Preferences…* | One-window app with no page shell; About and Help are dialogs | A NavigationView page as in GitClient (needs a shell the app does not have); a separate window |
| Shortcut | Ctrl+, | The common preferences shortcut; free in the window and unused by Vim | No shortcut |
| Apply model | Immediate, written at once; Close + Reset to defaults | Same as the menu and GitClient; no unsaved state | OK/Cancel with a staged copy (two models for the same settings) |
| Fate of the Settings menu items | Kept, Preferences… added | Existing quick access and Vim hints; tests rely on them | Replace the menu with the dialog |
| Categories | Appearance · Editing · New files (+ Text in PHASE02) | What the settings act on | One flat list; GitClient's names (its settings differ) |
| Ranges in the dialog | Each setting's validated range (resize 1–50, indent 1–16, size 6–72) | The menu offers presets; the dialog is where the rest is reached | The menu's three presets only |
| Expanders open or closed | All open in one fitted scroll area | Help dialog pattern; nothing hidden behind a click | First open only |
| Which text the font settings cover | Editor text and hex view | The document is the text; zoom exists but has no family and no exact value | Interface text too (no uniform UI scale in Avalonia; partial result) |
| Font list contents | Installed monospace families, Default first | GitClient's; column motions and hex rows need monospace | Every installed family; a free-text box |
| Font family storage | `EditorFontFamily` string, empty = default stack | Old files load; an uninstalled name falls back through Avalonia | An enum of known fonts |
| Phase split | Dialog first, fonts second | Fonts need the dialog; each phase is one reviewable commit | One phase (too large); fonts first in the menu |

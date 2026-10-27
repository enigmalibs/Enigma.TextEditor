# FEATURE-856D-PHASE01 — Icon rail and settings page

**Item:** FEATURE-856D — Icon rail & settings page (PHASE01 of 2)
**Branch:** `feature/feature-856d-phase01-icon-rail-settings-page`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Summary

A narrow rail on the left of the window, between the menu bar and the status bar, replaces the
Preferences dialog:

- **Files** (Phosphor `Files`, top) shows the split editor, as before;
- **Settings** (Phosphor `Gear`, bottom) shows a settings page.

How it works:

- The rail is Enigma.Avalonia.Desktop's `NavigationView`, 44 px wide. The window gives `NavigationItem` its own
  compact theme: a 20 px icon, no label. The header is the tooltip and the automation name.
- `ShellViewModel` holds the two items and the selection (`IsFilesPage` / `IsSettingsPage`). The window shows
  one page and hides the other. The editor area is never taken out of the window, so tabs, carets, scroll and
  highlighting survive a visit to the settings.
- `SettingsPageView` is the former `PreferencesView` reshaped as GitClient's page. It has a header strip (gear,
  "Settings", the hint, *Reset to defaults* with its confirmation flyout). Below it, a full-height scroll area
  holds a centred column (max 900) of the same four expanders. Every control, name and binding is kept.
- The page is built the first time it is shown, then kept: its font list measures every installed font.
- Keyboard:
  - *Settings → All Settings* and Ctrl+, show the page;
  - Esc on the page, when no dropdown takes it first, goes back to Files;
  - the page takes the keyboard when shown, and the active pane gets it back on Files;
  - the dialog host's fallback and the status bar's flyouts now give the keyboard to the page shown, never to
    the hidden editor.
- Removed: `IPreferencesDialogService`, `PreferencesDialogService`, `MainWindowViewModel.PreferencesCommand`.

## Files / modules touched

- Created: `src/Enigma.TextEditor.Desktop/ViewModels/ShellViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/Views/SettingsPageView.axaml`, `…/Views/SettingsPageView.axaml.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/ShellTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SettingsPageTests.cs`.
- Modified: `…/Views/MainWindow.axaml`, `…/Views/MainWindow.axaml.cs`, `…/ViewModels/MainWindowViewModel.cs`,
  `…/ViewModels/PreferencesViewModel.cs` (docs), `…/ViewModels/SettingsMenuViewModel.cs` (docs),
  `…/ViewModels/HelpCatalog.cs`, `…/Extensions/ServiceCollectionExtensions.cs`, `README.md`,
  `tests/…/Infrastructure/EditorHarness.cs`, `tests/…/CompositionRootTests.cs`,
  `tests/…/ViewModels/SettingsMenuTests.cs`, `tests/…/ViewModels/PreferencesTests.cs` (docs).
- Deleted: `…/Views/PreferencesView.axaml(.cs)`, `…/Services/IPreferencesDialogService.cs`,
  `…/Services/PreferencesDialogService.cs`, `tests/…/Infrastructure/FakePreferencesDialogService.cs`,
  `tests/…/Views/PreferencesDialogTests.cs` (its cases moved to `SettingsPageTests`).

## Deviations & follow-ups

- The settings page keeps `PreferencesViewModel` as its view-model rather than renaming it: the rename would only
  churn the VM and its tests.
- The help's *Application* section now lists Ctrl+, as "Settings", and adds Esc back to the files.
- README: the *Settings* paragraph now describes the rail and the page (doc sweep). The 1.5 callout is history,
  so the release item rewrites it.
- PHASE02 still has to bring Files forward when the workspace changes while the page is shown. Until then,
  File → Open on the Settings page opens the file behind the page.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the window finds the page's view-model | `MainWindowViewModel.SettingsPage` (the `PreferencesViewModel` singleton) | The window builds the page lazily in code-behind, and needs the VM then |
| Where the page takes the keyboard | The page itself (`Focusable`), not its first control | No key lands on a control by accident; Tab walks the controls from there |
| How page switching is wired | `MainWindow` follows `Shell.PropertyChanged(SelectedItem)` and posts the focus at background priority | The page must be visible and laid out before it can take the keyboard |
| Esc | Bubbled `KeyDown` on the settings host, unmodified Esc only, ignored if handled | An open dropdown or flyout keeps its own Esc |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors; no `AVLN` warning in the build output.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2713 total, 2713 passed, 0 failed, 0 skipped.
- Added: `ShellTests` (4 cases) and `SettingsPageTests` (7 cases). The rail tests cover the 44 px width, its
  place between the menu bar and the status bar, Files on top and Settings at the foot, icon only with tooltip
  and automation name. The page tests cover: the page shown with the keyboard and the editor hidden; Files
  bringing the editor back with the same document and the keyboard; the page built once; the menu item,
  Ctrl+, and Esc; the controls changing the settings and following the menu; ranges and reset; a centred
  column that scrolls in a short window.
- Fix cycles: 1. Two expectations of the planned change were updated: the menu item now reads `_All Settings`,
  and the rail's visual tree lists the footer first, so the test orders the items by position.

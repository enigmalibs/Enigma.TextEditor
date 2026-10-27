# FEATURE-856D — Icon rail & settings page

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-856d-phase01-icon-rail-settings-page`
**Branch (PHASE02):** `feature/feature-856d-phase02-files-page-follows-editor`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Objective

- A narrow navigation rail on the left of the window, like Enigma.GitClient's but much smaller: small icons,
  no title under them.
- A *Files* icon at the top shows the editor's split view as it is today; a *Settings* icon at the bottom shows
  a settings page, which replaces the Preferences content dialog.

## Context & constraints

- GitClient (`src/Enigma.GitClient.Desktop/Views/MainWindow.axaml`, `Navigation/ShellNavigation.cs`):
  - `nav:NavigationView` docked left under its top strip, `PaneSize` 72, `Items` + `FooterItems`, two-way
    `SelectedItem`, Phosphor icons (`Gear` for Settings) through `PhosphorIconSet…ToGeometry()`.
  - Pages are built by `INavigationService.PageFactory` and swapped in a `ContentControl`.
  - Its settings page (`Views/Pages/SettingsPageView.axaml`): a header strip (gear icon, title, *Reset to
    defaults*), then a `ScrollViewer` holding a centred column (`MaxWidth` 900, margin 24) of cards.
- Enigma.Avalonia.Desktop 1.2.0 `NavigationItem` template: a `StackPanel` of a 24×24 `PathIcon` and a
  `TextBlock` for `Header` (11 px). An empty header still leaves the text line and its spacing under the icon.
  `NavigationView` only syncs its two `ListBox`es with `SelectedItem`; it needs no `INavigationService`.
- Here, `MainWindow.axaml` is a `DockPanel`: the menu bar (top), the status bar (bottom), and `views:SplitHost`
  filling the rest. The three Enigma hosts overlay it.
- Today's preferences: `PreferencesView` (a 620 px `UserControl` with a 440 px scroll area fitted to the dialog
  card by `DialogFit`), `PreferencesViewModel` (singleton, applies through `SettingsMenuViewModel`),
  `IPreferencesDialogService` / `PreferencesDialogService`, `MainWindowViewModel.PreferencesCommand`,
  *Settings → Preferences…* and the window's `Ctrl+OemComma` binding. `PreferencesViewModel.Fonts` probes the
  installed fonts on first read.
- `DialogKeyboard` gives the keyboard to a dialog and back to `MainWindow.FocusActivePane()` when it closes.
- `WorkspaceViewModel` raises `PropertyChanged(ActiveDocument)` (which `MainWindowViewModel` already follows)
  and `LayoutChanged`. `MainWindow.ActiveEditor` is what the Edit menu acts on.

## Decisions

| Decision | Rationale |
|---|---|
| The library's `NavigationView`, bound to a new `ShellViewModel` (`Items`, `FooterItems`, `SelectedItem`, `IsFilesPage`, `IsSettingsPage`, `ShowFiles()`, `ShowSettings()`) | Same control as GitClient; the shell VM is testable without a window |
| Pages switched by visibility, not through `INavigationService` | The editor area must stay attached: rebuilding or detaching it would drop its TextMate installations, scroll and focus state |
| Compact `NavigationItem` `ControlTheme` in the window's resources: one 20×20 `PathIcon`, `ToolTip.Tip` and `AutomationProperties.Name` bound to `Header`; rail `PaneSize` 44 | "Small icons with no title under them"; the header still names the item for the pointer and the screen reader |
| Icons: Phosphor `Files` (top) and `Gear` (footer) | Draft's "files icon" / "settings icon"; `Gear` is GitClient's |
| Rail between the menu bar and the status bar | GitClient docks it under its top strip; the menu and the status bar keep the full width |
| `SettingsPageView` replaces `PreferencesView`: a header strip (gear, "Settings", *Reset to defaults* with its confirmation flyout) and a scroll area with a centred column (max 900) of the same four expanders | GitClient's page; the cards, controls, names and VM are kept, so every setting behaves as before |
| The Settings page is built the first time it is shown, then kept | The font list probes every installed font; building it at start-up would slow every launch |
| `IPreferencesDialogService`, `PreferencesDialogService`, `PreferencesCommand` removed; *Settings → All Settings* (Ctrl+,) shows the page | The draft rejects the dialog; one way to the settings, not two |
| Ctrl+, shows Settings; Esc on the Settings page (no popup open) shows Files | A keyboard-only way in and out, for a Vim-first editor |
| Showing Files gives the keyboard to the active pane; showing Settings moves it into the page | A hidden editor must never receive keys |

## PHASE01 — Icon rail and settings page

**Status:** DONE

### Steps

1. `ViewModels/ShellViewModel` (singleton): two `NavigationItem`s (built with
   `PhosphorIconSet.Instance.GetGlyph(…, PhosphorWeight.Regular).ToGeometry()`, headers "Files" and
   "Settings"), `Items` / `FooterItems`, two-way `SelectedItem` (a null assignment is ignored), `IsFilesPage`,
   `IsSettingsPage`, `ShowFiles()`, `ShowSettings()`, `ShowSettingsCommand`, `ShowFilesCommand`. It starts on
   Files.
2. `MainWindowViewModel`: takes `ShellViewModel` and exposes it as `Shell`; `PreferencesCommand` and the
   `IPreferencesDialogService` dependency are removed.
3. `Views/SettingsPageView.axaml(.cs)` from `PreferencesView`: page header and centred column as above; no fixed
   width, no `DialogFit`; the expanders, controls and `x:Name`s unchanged. `PreferencesView` is deleted.
4. `MainWindow.axaml`:
   - the compact `NavigationItem` theme in `Window.Resources`; `nav:NavigationView x:Name="Rail"` docked left,
     `PaneSize` 44;
   - the content: a `Panel` with `SplitHost` (visible on Files) and a `ContentControl x:Name="SettingsHost"`
     (visible on Settings), filled with a `SettingsPageView` the first time Settings is shown;
   - *Settings → All Settings* (`InputGesture` Ctrl+OemComma) and the window binding on `Shell.ShowSettingsCommand`.
5. `MainWindow.axaml.cs`: follows `Shell`'s page — on Settings, creates the page once and focuses it; on Files,
   `FocusActivePane()` (posted). Esc on the Settings page, when no popup is open, calls `Shell.ShowFiles()`.
6. DI (`AddShell`): `ShellViewModel`; `IPreferencesDialogService` removed. Test infrastructure:
   `FakePreferencesDialogService` removed, `EditorHarness` builds `ShellViewModel`.
7. Tests:
   - VM: starts on Files; `ShowSettings` / `ShowFiles` / `SelectedItem` keep the flags in step; a null selection
     is ignored;
   - headless: the rail is 44 wide, has Files at the top and Settings in the footer, its items show no text and
     carry their name as tooltip and automation name;
   - headless: selecting Settings hides the split host, shows the page with the four expanders open and the
     reset flyout; selecting Files shows the editor again with the same document, and gives it the keyboard;
   - headless: the menu item and Ctrl+, show Settings; Esc on the page returns to Files;
   - the existing Preferences dialog tests are moved onto the page (controls change the settings, follow the
     menu, ranges, reset asks first);
   - the composition root resolves `ShellViewModel`; `IPreferencesDialogService` is gone.

### Acceptance criteria

- The window shows a 44 px rail on the left, between the menu bar and the status bar, with a Files icon at the top
  and a Settings icon at the bottom, no text under either; each names itself in a tooltip.
- Files shows the split editor as before; Settings shows the settings page; switching back keeps every tab,
  split and caret, and gives the keyboard to the active pane.
- Every setting on the page changes at once, is remembered, and agrees with the Settings menu both ways.
- No Preferences dialog remains; *Settings → All Settings* and Ctrl+, show the page, Esc leaves it.
- Zero warnings, whole suite green.

## PHASE02 — Files page follows the editor

**Status:** TODO

### Steps

1. `ShellViewModel` (or `MainWindowViewModel`) brings Files forward when the workspace's `ActiveDocument`
   changes or `LayoutChanged` fires — New, Open, Open as Hex, a drop, startup files, `:e`, splits, closing a pane.
2. `MainWindow.ActiveEditor` returns `null` while Settings is shown, so the Edit menu acts on no hidden editor.
3. Tests: New / Open / a split while on Settings show Files with the new tab; Edit → Select All on the Settings
   page changes nothing; changing a setting on the page does not leave it.

### Acceptance criteria

- Anything that opens, creates or rearranges a document while the Settings page is shown brings the Files page
  back, showing that document.
- The Edit menu never edits a document the user cannot see.
- Changing a setting keeps the Settings page shown.
- Zero warnings, whole suite green.

## Out of scope

- Removing or reorganising the Settings menu's quick toggles.
- More pages on the rail (search, file tree…); a collapsible or labelled rail.
- A search box over the settings.
- Changing the status bar on the Settings page.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Rail control | Enigma `NavigationView` with a window-scoped compact item theme | "Like Enigma.GitClient"; the supported way to restyle a templated control | Hand-made toggle buttons (a second rail look); `/template/` style overrides (priority against template values is fragile) |
| Page mechanism | Visibility switch over a `ShellViewModel` | Keeps the editors attached, so tabs, carets, undo and highlighting survive | `INavigationService` + `PageFactory` (detaches or rebuilds the editor area on every switch) |
| Rail size | 44 px wide, 20 px icons | "Way smaller" than GitClient's 72 / 24; leaves the list item's 8 px padding and layout rounding room | 36 px (icons cramped against the selection); 56 px |
| Labels | None; header as tooltip and automation name | Draft: no title under the icons; a11y still needs a name | Visible labels |
| Rail placement | Under the menu bar, above the status bar | GitClient's shape; menu and status bar keep the full width | Full-height rail beside the menu bar |
| Fate of the dialog | Removed, replaced by the page | Draft: the dialog is not ideal | Keeping both |
| Menu entry | *Settings → All Settings* (Ctrl+,), no ellipsis | It now shows a page, not a dialog | Keeping "Preferences…" |
| Page layout | GitClient's header strip + centred 900 px column; the four expanders kept open | Same look as GitClient's settings page; the controls are already tested | Flat SettingsCards (a rewrite of every row) |
| Reset confirmation | Kept as the flyout | Existing behaviour and test | GitClient's unconfirmed button |
| Settings page lifetime | Built on first visit, then kept | The font probe would otherwise run at every start | Built with the window; rebuilt on every visit |
| Keyboard | Ctrl+, in, Esc out; focus moved with the page | Keyboard-first editor; a hidden editor must not take keys | Mouse only |
| Status bar on Settings | Unchanged | It reports the active document, which still exists | Hiding it |
| Workspace activity on Settings | Brings Files forward (PHASE02) | The user would otherwise act on an invisible document | Ignoring it; disabling the menus on Settings |
| Phase split | Rail + page first, edge cases second | Each phase is one reviewable commit; PHASE01 is usable alone | One phase (too large to review) |

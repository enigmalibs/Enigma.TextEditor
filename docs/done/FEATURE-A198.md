# FEATURE-A198 — Font size picker in the status bar

**Item:** FEATURE-A198 — Font size picker in the status bar
**Branch:** `feature/feature-a198-status-font-size`
**Run:** feature/2026-10-31-font-size-drop-split

## Summary

The status bar has a new button, last on the right: `Font Size: 14`.

- **The list:** pressed, it lists 8–16, 18, 20, 24, 28, 32 and 36:
  - the current size is checked, and the default reads `14 (default)`;
  - a size that is not one of them (17 after a zoom) is added in its place, so the check always shows where the text
    is;
  - choosing a size goes through `SetFontSizeAsync`, like the zoom keys and the settings page, so every editor and
    the hex view follow it at once and it is written to the settings.
- **The button:**
  - shows for any open tab, text or hex, and hides with the other items when no tab is open;
  - takes no focus, and its flyout gives the keyboard back to the pane as it closes;
  - follows the zoom keys, the settings page and a reset.
- **The ViewModel** (`SettingsMenuViewModel`) gains:
  - `FontSizeText`;
  - `FontSizeChoices`, one `ReadOnlyObservableCollection` brought in step in place;
  - `SetFontSizeCommand`, an `AsyncRelayCommand<int>`.
- **Helpers:** `FontSizes` (the presets and `ListFor(current)`) and the `FontSizeChoice` record (size, current,
  default, command, header).

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/ViewModels/FontSizes.cs`, `FontSizeChoice.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs`, `Views/MainWindow.axaml`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FontSizesTests.cs`, `Views/FontSizeStatusTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SettingsMenuTests.cs`, `Views/StatusBarFocusTests.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-A198.md`; created this file

## Deviations & follow-ups

- **`FontSizeChoices` is one collection changed in place**, not a list rebuilt on every read as the plan's
  decisions implied:
  - Avalonia 12's `MenuFlyout` hands its presenter the `ItemsSource` it holds when it first opens. The presenter's
    `ItemsSourceView` unwraps that to the inner list, so a new list bound later never reaches the open or reopened
    flyout. This was seen in the headless test and confirmed in the decompiled `ItemsSourceView.SetSource`;
  - `RefreshFontSizeChoices` replaces, adds or removes only the entries that differ.
- **Not seen in a real window:** the flyout is checked in headless tests (headers, checks, radio type, the command),
  not by eye. A glance in both themes is worth a follow-up.
- **Release notes** are left to the next release item.
- **Baseline** (the run's, taken on `feature/2026-10-31-font-size-drop-split` before this dev): 0 warnings,
  2911 passed, 0 failed. Nothing pre-existing.
- **Documentation sweep:**
  - README *The status bar*: the list gains the font size, and "the last four" becomes "the last five" (planned);
  - README *Vim*: "The bar's other items are `:set` options" now reads "other file items": the font size is not a
    `:set` option;
  - the help lists keys only, and this item has none. The "What's new" lines describe released versions.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the flyout gets its items | `ItemsSource` bound to `Settings.FontSizeChoices`, with an `ItemContainerTheme` based on the theme's `MenuItem` | One place builds the list. The theme keeps the app's menu look |
| Where each item's command comes from | The `FontSizeChoice` carries `SetFontSizeCommand` | A binding from inside the popup back to the window's DataContext is fragile. The record keeps the item's bindings local (`Command`, `Size`) |
| Keeping the flyout current | One `ObservableCollection`, updated entry by entry | See *Deviations*: the presenter follows the first list only |
| Finding the flyout's items in a test | A `Control.LoadedEvent` class handler while the flyout opens | The popup's tree is not reachable from the window. `IPopupHostProvider` is internal in Avalonia 12 |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2929 passed, 0 failed, 0 skipped (+18).
- **Tests added:**
  - `FontSizesTests` (pure): the presets are ordered, distinct and in range; a preset adds nothing; 17, 6 and 72 are
    added in order; sizes outside 6..72 are never offered; the default's header;
  - `SettingsMenuTests`:
    - choosing 18 applies it, writes it and checks it;
    - the list checks 14, marks the default, and a choice's command takes its boxed size;
    - the zoom to 17 adds 17, checked; the reset goes back; the collection instance never changes;
  - `FontSizeStatusTests` (real window):
    - the flyout lists the 15 headers with 14 checked, as radio items;
    - choosing 18 while open moves the check at once, resizes the editor and relabels the button, and a reopened
      list checks 18;
    - the button follows Ctrl+= and shows on a hex tab, not with no tab;
  - `StatusBarFocusTests`: `FontSizeStatus` joins the no-focus and the keyboard-back theories.

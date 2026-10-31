# FEATURE-A198 — Font size picker in the status bar

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-a198-status-font-size`
**Run:** feature/2026-10-31-font-size-drop-split

## Objective

Add a button to the status bar, with the other buttons on the right, showing the editors' font size. Pressed, it
lists font sizes; choosing one sets the text size at once, as the zoom keys and the settings page do.

## Context & constraints

- **The setting** (`ViewModels/SettingsMenuViewModel.cs`):
  - `SetFontSizeAsync(int)` clamps to `AppSettings.MinimumFontSize`..`MaximumFontSize` (6..72), writes
    `Value.EditorFontSize`, sets `EditorPreferences.FontSize` (every editor and the hex view bind to it) and saves;
  - `ZoomIn/ZoomOut/ResetZoomCommand` go through it, as does the settings page (`PreferencesViewModel.EditorFontSize`);
  - `ResetAsync` and `ApplyFirstRunAsync` raise `OnPropertyChanged(string.Empty)`;
  - `AppSettings.DefaultFontSize` is 14.
- **The status bar** (`Views/MainWindow.axaml`):
  - the right-hand `StackPanel` is shown only with an active tab;
  - its buttons (`IndentationStatus`, `EncodingStatus`, `LanguageStatus`, `LineEndingStatus`) are `Button.statusItem`,
    `Focusable="False"`, with `ToolTip.Tip` and `AutomationProperties.Name`;
  - their `MenuFlyout`s carry radio `MenuItem`s (`ToggleType="Radio"`, a `GroupName`, `IsChecked` bound `OneWay`,
    a `Command`), and `Closed="OnStatusFlyoutClosed"`, which gives the keyboard back to the page.
- **Bindings:** compiled bindings are on (`AvaloniaUseCompiledBindingsByDefault`). `MainWindowViewModel.Settings` is
  the `SettingsMenuViewModel`.
- **MVVM:** CommunityToolkit.Mvvm without source generators. Commands are explicit, get-only properties.
- **Tests:**
  - `ViewModels/SettingsMenuTests` (harness `SettingsMenu`, `Preferences`, `Settings.FilePath`);
  - `Views/StatusBarFocusTests` (real window, headless mouse: `Click`, `DrainAsync`; theories over the button names).
- **Docs:** README *The status bar* list ("The last four are buttons that change them"). `RELEASENOTES.md` belongs to
  release items.

## Decisions

- **Where the list lives:** `SettingsMenuViewModel`, which already owns the font size and the zoom commands:
  - `FontSizeText`: `Font Size: 14`, in the style of the bar's `Tab Size: 4`;
  - `FontSizeChoices`: the list the flyout shows, one entry per size, the current one checked;
  - `SetFontSizeCommand`: an `AsyncRelayCommand<int>` over `SetFontSizeAsync`, allowing concurrent executions like the
    other commands.
- **The sizes:** a pure `FontSizes` helper next to the ViewModel:
  - the presets: every size from 8 to 16, then 18, 20, 24, 28, 32 and 36;
  - the current size is added in order when it is not a preset (17 after a zoom), so the check always shows where the
    text is;
  - the default reads `14 (default)`.
- **The button:** `FontSizeStatus`, last on the right:
  - shown for any active tab, text or hex: the hex rows follow the font size too;
  - hidden with the others when no tab is open;
  - takes no focus, and its flyout gives the keyboard back as it closes, like its neighbours;
  - its tooltip and automation name are `Font size`.
- **The flyout:** a `MenuFlyout` bound to `FontSizeChoices`. Each item is a radio item with the size as its header,
  checked for the current size, running `SetFontSizeCommand` with that size.
- **Notifications:** every path that changes the size (`SetFontSizeAsync`, `ResetAsync`, the zoom commands, the
  settings page) leaves `FontSizeText` and `FontSizeChoices` current.

## Steps

1. **`ViewModels/FontSizes.cs`** (internal static):
   - `Presets`: 8–16, 18, 20, 24, 28, 32, 36;
   - `ChoicesFor(int current)` returns the presets, plus `current` in order when it is in range and not a preset, as
     `FontSizeChoice` entries.
2. **`ViewModels/FontSizeChoice.cs`**: a sealed record, `Size`, `IsCurrent`, `IsDefault`, and a `Header` (`14` or
   `14 (default)`).
3. **`SettingsMenuViewModel`:**
   - `FontSizeText`, `FontSizeChoices`, `SetFontSizeCommand`, documented like their neighbours;
   - `SetFontSizeAsync` announces `FontSizeText` and `FontSizeChoices` when the size changes. `ResetAsync` already
     announces everything.
4. **`MainWindow.axaml`:**
   - the `FontSizeStatus` button after `LineEndingStatus`: `Classes="statusItem"`, `Focusable="False"`, no
     `IsTextTab` condition;
   - its `MenuFlyout` with `ItemsSource="{Binding Settings.FontSizeChoices}"` and
     `Closed="OnStatusFlyoutClosed"`. The item container theme or style binds the header, the check, the command and
     its parameter.
5. **README:** add the font size to *The status bar* list, and make "the last four" read "the last five".
6. **Tests:**
   - `FontSizes` (pure):
     - the presets come in order, all within 6..72;
     - a preset current size adds nothing;
     - an off-list size (17, 6, 72) is added in order;
     - only the current entry is checked, and only 14 says `(default)`;
   - `SettingsMenuTests`:
     - `SetFontSizeCommand` with 18 sets `Preferences.FontSize`, writes 18 to the file and reads `Font Size: 18`;
     - `FontSizeText` and the checked choice follow the zoom keys and the reset;
     - a zoom to 17 adds 17 to the choices, checked;
   - real window (`StatusBarFocusTests` or a new `FontSizeStatusTests`):
     - `FontSizeStatus` takes no focus, and its flyout gives the keyboard back to the editor as it closes (join the
       existing theories);
     - choosing `18` in the flyout sets the editor's `FontSize` to 18, and the button reads `Font Size: 18`;
     - the button shows on a hex tab, and not with no tab open.

## Acceptance criteria

- The status bar shows `Font Size: <n>` on the right, for any open tab.
- Pressed, it lists the sizes with the current one checked, and the default marked. Choosing one resizes every
  editor's text at once and remembers it.
- The button and the list follow the zoom keys, the settings page and a reset. The keyboard stays in the pane.
- The README says so. Zero warnings, whole suite green.

## Out of scope

- Per-pane or per-file font sizes (the setting is global, as the zoom's is).
- Zoom in/out entries in the flyout (Ctrl+=, Ctrl+-, Ctrl+0 and the View menu already have them).
- Changing the zoom keys, the settings page, or the 6..72 range.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `FEATURE` | New control on a working bar | `BUG` |
| Which ViewModel owns the list | `SettingsMenuViewModel` | It owns the size, its limits, the zoom commands and the write. The status bar ViewModel only describes the active tab | `StatusBarViewModel` (would need the settings and the writer); a new ViewModel (one more thing to wire for three members) |
| Which sizes | 8–16, 18, 20, 24, 28, 32, 36 | Every everyday size one step apart, then the usual larger steps. 15 items stay one short list | Every size 6–72 (67 items); a few round sizes (too coarse around the default) |
| A current size that is not a preset | Added to the list, checked | After a zoom to 17 the list still shows where the text is, and choosing it changes nothing | No item checked (the list says nothing about the text) |
| Button text | `Font Size: 14` | Mirrors the bar's own `Tab Size: 4`, and says what the number is | A bare number (unclear); `14 pt` or `14 px` (no unit is shown anywhere else, and `pt` is wrong) |
| Position | Last on the right | The other items describe the file; this one is an app-wide view setting, so it closes the row instead of sitting among them | First on the right; before the caret |
| Shown when | Any active tab, text or hex | The hex rows use the font size too. With no tab open the whole group hides, as today | Text tabs only; always (alone on the bar with no file) |
| Marking the default | `14 (default)` | Choosing it is the reset, without a separate entry | A separate *Reset* item |
| Shape | One phase | ViewModel members, one button, tests: about 30 minutes of review | Two phases (the ViewModel part alone is too small) |

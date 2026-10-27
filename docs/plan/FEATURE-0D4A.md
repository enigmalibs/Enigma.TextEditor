# FEATURE-0D4A — First-run setup dialog

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-0d4a-first-run-setup`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Objective

The first time the editor starts, a setup dialog like Enigma.MarkdownEditor's welcome dialog lets the user choose:

- Vim mode;
- the minimap;
- wrapping long lines;
- the theme: dark, light or the system's.

## Context & constraints

- MarkdownEditor (`src/Enigma.MarkdownEditor.Desktop`):
  - `Services/FirstRunState` — a record, `Detect(settingsFilePath)` = the settings file is absent; read in
    `App` **before** `SettingsLoader.Load`, because the loader moves an unreadable file aside and would make a
    broken file look like a first run. Registered with `AddFirstRun(firstRun)`, `TryAddSingleton(Returning)`
    as the fallback.
  - `Services/FirstRunDialogService` — title "Welcome", `HandWaving` icon, primary "Get started", close "Skip";
    the result is not read: every exit persists, because the file's absence is what brings the dialog back. The
    write happens after the dialog has closed, so a failure message does not replace it on the single host.
  - `Views/FirstRunView` — 520 px, an intro line, then `controls:SettingsCard`s (icon, header, description,
    control on the right).
  - `ViewModels/FirstRunViewModel` — theme applied live as it is chosen.
  - Shown from the main window VM once the window is open, before anything else is restored.
- Here:
  - `App.OnFrameworkInitializationCompleted` loads the settings, builds the host (`App.BuildHost`), and posts
    `Workspace.OpenStartupFilesAsync(startup)` so the window is on screen first.
  - `SettingsMenuViewModel` is the one place that applies (theme service, Vim session, `EditorPreferences`) and
    writes (`ISettingsWriter`, which reports a failure once through `IMessageService`) a setting. `ResetAsync`
    applies several settings and writes once.
  - `DialogKeyboard` gives the keyboard to the content dialog and back to the active pane.
  - The settings page labels themes "Follow the system" / "Light" / "Dark".

## Decisions

| Decision | Rationale |
|---|---|
| `Services/FirstRunState` as in MarkdownEditor, detected in `App` before the load; `AddFirstRun(firstRun)` + a `Returning` fallback in `AddShell` | Same signal and the same reason for its timing; tests and the harness need no file games |
| `ViewModels/FirstRunViewModel` stages `Theme`, `VimEnabled`, `ShowMinimap`, `WordWrap` from the current settings; the theme is previewed live through `IThemeService` | The dialog repaints in the theme being chosen; the rest applies once |
| `SettingsMenuViewModel.ApplyFirstRunAsync(theme, vim, minimap, wrap)` applies all four as `ResetAsync` does and writes once | One apply-and-write path; the menu and the settings page follow through `Changed` |
| `Services/IFirstRunDialogService` / `FirstRunDialogService`: "Welcome", `HandWaving`, "Get started" (primary) and "Skip" (close); every exit applies and writes what the dialog shows, after it has closed | MarkdownEditor's dialog; the file must exist afterwards or the dialog returns |
| `Views/FirstRunView`: an intro line and four `SettingsCard`s — Theme (`ComboBox`, the page's labels), Vim mode, Minimap, Wrap long lines (`ToggleSwitch`es) — with the settings page's icons | Same rows as MarkdownEditor; same words as the settings page |
| `MainWindowViewModel.StartAsync(StartupFiles)`: the first-run dialog (on a first run), then the startup files; `App` posts it instead of `OpenStartupFilesAsync` | One dialog host: a failed open must not collide with the welcome dialog; the editor's state is final before files open |
| A failure inside the first run is logged and start-up continues | The editor must open even if the welcome dialog fails |

## Steps

1. `Services/FirstRunState.cs` (record, `First`, `Returning`, `Detect`).
2. `Extensions/ServiceCollectionExtensions.cs`: `AddFirstRun(FirstRunState)`; in `AddShell`,
   `TryAddSingleton(FirstRunState.Returning)`, `FirstRunViewModel` (transient), `IFirstRunDialogService`.
   `App.BuildHost` gains the `FirstRunState` argument; `App` detects it before `SettingsLoader.Load`.
3. `SettingsMenuViewModel.ApplyFirstRunAsync`.
4. `ViewModels/FirstRunViewModel.cs`: staged values, `Themes`, live theme preview, `CompleteAsync()` which calls
   `ApplyFirstRunAsync` and logs what was chosen.
5. `Views/FirstRunView.axaml(.cs)`; `Services/FirstRunDialogService.cs`.
6. `MainWindowViewModel.StartAsync(StartupFiles)`; `App` posts it.
7. Tests:
   - `FirstRunState.Detect`: absent file → first run; present file → returning;
   - the VM starts from the settings' values; choosing a theme previews it; completing applies all four
     (preferences, Vim session, theme) and writes the file once;
   - `StartAsync` shows the dialog on a first run only, and opens the startup files after it has closed;
   - headless: the dialog is titled "Welcome", has the four rows with their controls, "Get started" and "Skip";
     Esc closes it and the settings file then exists;
   - the composition root resolves `IFirstRunDialogService` and `FirstRunState` (returning by default).

## Acceptance criteria

- With no settings file, the editor opens and shows the welcome dialog with Theme, Vim mode, Minimap and Wrap long
  lines, set to the current defaults.
- Choosing a theme repaints the window at once; closing the dialog by any means applies the four choices and
  writes the settings file, so the dialog never shows again.
- With a settings file — even an unreadable one — no dialog is shown.
- Files passed on the command line open after the dialog has closed.
- Zero warnings, whole suite green.

## Out of scope

- Other settings in the dialog (font, line endings, indentation): they are on the settings page.
- A way to show the dialog again.
- Changing what the settings page or the menu offer.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| First-run signal | The settings file is absent, checked before the load | MarkdownEditor's; a broken file is not a first run | A `FirstRunDone` key (changes the file format, needs a migration) |
| Apply model | Staged, theme previewed live, applied and written once after the dialog closes | A write failure is then reported without replacing the dialog; one write | Each toggle applied and written live (four writes; a failure message would replace the dialog) |
| Buttons | "Get started" + "Skip", every exit persists | MarkdownEditor's dialog | A single button (Esc would then need its own meaning) |
| Order at start-up | Welcome dialog, then the startup files | One dialog host; the files open with the chosen settings | Opening files first |
| Theme labels | The settings page's ("Follow the system", "Light", "Dark") | Same words for the same setting everywhere | The enum names |
| Where the apply lives | `SettingsMenuViewModel.ApplyFirstRunAsync` | The one place settings are applied and written | Applying from the dialog VM directly |
| Failure during the first run | Logged; start-up continues | The editor must open | Failing the start |

# FEATURE-0D4A — First-run setup dialog

**Item:** FEATURE-0D4A — First-run setup dialog
**Branch:** `feature/feature-0d4a-first-run-setup`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Summary

The first time the editor starts, a **Welcome** dialog (HandWaving icon) asks for four things, modelled on
Enigma.MarkdownEditor's welcome:

- the theme (*Follow the system* / *Light* / *Dark*, the settings page's words);
- Vim mode;
- the minimap;
- wrapping long lines.

Each is a `SettingsCard` row, set to the current defaults.

- **First run** = no settings file. `FirstRunState.Detect` looks for the file in `App` *before*
  `SettingsLoader.Load`, which would move an unreadable file aside and make it look absent. `AddFirstRun`
  registers the answer; `AddShell` falls back to `FirstRunState.Returning`.
- **While the dialog is open**, only the theme applies, so the window repaints in it. Nothing is written.
- **When it closes**, by *Get started*, *Skip* or Esc: `FirstRunViewModel.CompleteAsync` calls
  `SettingsMenuViewModel.ApplyFirstRunAsync`. That applies the four choices (theme service, Vim session,
  `EditorPreferences`, the menu's marks) and writes the file once, even when nothing changed — the file's
  presence is what keeps the dialog from coming back. A write failure is reported by the settings writer
  after the dialog has gone, so it never replaces it on the single dialog host.
- **Order at start-up**: `MainWindowViewModel.StartAsync` shows the welcome on a first run, then opens the
  command-line files. `App` posts it in place of `OpenStartupFilesAsync`.
- **A failure inside the welcome** is logged by `FirstRunDialogService`, and start-up continues.

## Files / modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/FirstRunState.cs`, `…/Services/IFirstRunDialogService.cs`,
  `…/Services/FirstRunDialogService.cs`, `…/ViewModels/FirstRunViewModel.cs`, `…/Views/FirstRunView.axaml`,
  `…/Views/FirstRunView.axaml.cs`, `tests/…/Infrastructure/FakeFirstRunDialogService.cs`,
  `tests/…/ViewModels/FirstRunTests.cs`, `tests/…/Views/FirstRunDialogTests.cs`.
- Modified: `src/Enigma.TextEditor.Desktop/App.axaml.cs`, `…/Extensions/ServiceCollectionExtensions.cs`,
  `…/ViewModels/MainWindowViewModel.cs`, `…/ViewModels/SettingsMenuViewModel.cs`,
  `tests/…/Infrastructure/EditorHarness.cs`, `tests/…/CompositionRootTests.cs`.

## Deviations & follow-ups

- The plan registered `FirstRunViewModel` as transient. It is a singleton: the dialog shows once per process
  at most, and the service that holds it is a singleton.
- `App.BuildHost` takes the state as an optional last argument (`null` = not a first run), so the composition
  tests build a host as before.
- Doc sweep: nothing became wrong. The README's *Your data* section still holds, and the release notes
  describe the welcome (FEATURE-4362).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where a failure of the welcome is caught | `FirstRunDialogService.ShowAsync`, broadly, logged | It owns the logger; the editor must finish starting whatever happened |
| How the choices start | Read from the settings in the constructor, the theme through a plain field | Constructing the VM must not repaint the window |
| Icons of the rows | Palette, Keyboard, MapTrifold, TextAlignLeft | The settings page's Appearance and Editing icons; Phosphor has no "wrap" glyph |
| How the window learns it is a first run | `FirstRunState` injected into `MainWindowViewModel` | Same as MarkdownEditor; the decision is testable with a fake dialog |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2737 total, 2737 passed, 0 failed, 0 skipped.
- Added:
  - `FirstRunTests` (7):
    - detection with no file and with a broken file;
    - the choices start from the settings and change nothing before closing;
    - the theme previews without a write;
    - completing applies all four and writes once;
    - the file is written even with the defaults untouched;
    - the welcome comes before the command-line files, and never on another start.
  - `FirstRunDialogTests` (3, headless): title, icon and buttons, the four rows and their defaults; Enter on
    *Get started* applies and writes; Esc still writes the file.
  - `CompositionRootTests` (+4): it resolves `FirstRunViewModel` and `IFirstRunDialogService`; the default is
    `Returning`; a state passed by the composition root is the one registered.
- Fix cycles: 0.

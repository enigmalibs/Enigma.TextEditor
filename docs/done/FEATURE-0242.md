# FEATURE-0242 — New settings defaults

**Item:** FEATURE-0242 — New settings defaults
**Branch:** `feature/feature-0242-settings-defaults`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Summary

A fresh install, and *Reset to defaults*, now start with:

| Setting | Before | Now |
|---|---|---|
| Theme | Follow the system | unchanged |
| Wrap long lines | off | **on** |
| Line endings of a new file | LF | **CRLF on Windows, LF elsewhere** |
| Pane resize step | 5 % | **10 %** |
| Minimap | on | unchanged |
| Vim mode | on | unchanged |

How the platform is decided: `LineEndings.ForPlatform(bool isWindows)` is a pure rule, so both answers are
testable on any OS. `AppSettings.DefaultLineEnding` uses it with `OperatingSystem.IsWindows()`.

Every key is written to `settings.json`, so an existing install keeps what it saved. The new defaults reach only
a fresh install, a key a file lacks, and *Reset to defaults*. `AppSettings`' remarks now say so.

`LineEndings.Default` is unchanged: it is the style of a file read with no line break, not the setting. Its docs
and `LineEndingStyle`'s now tell the two apart.

## Files / modules touched

- Modified: `src/Enigma.TextEditor.Core/Settings/AppSettings.cs`,
  `src/Enigma.TextEditor.Core/Documents/LineEndings.cs`, `src/Enigma.TextEditor.Core/Documents/LineEndingStyle.cs`.
- Tests modified: `tests/Enigma.TextEditor.Core.UnitTests/Settings/AppSettingsTests.cs`,
  `tests/Enigma.TextEditor.Core.UnitTests/Documents/LineEndingsTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/PreferencesTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SettingsMenuTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SplitPaneTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/WindowCommandTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorTextClearanceTests.cs`.

## Deviations & follow-ups

- None to the plan. The menu already offers *Pane Resize Step → 10%*; the settings page shows any value.
- Doc sweep: nothing became wrong. The README never stated the old default values.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Desktop tests that toggled from the old defaults (wrap off, 5 %, LF) | Each pins the starting value it toggles from, with a one-line why; no assertion weakened | They test the toggle, not the default; the default itself is pinned by the Core test |
| The reset test | Its starting values flipped to differ from the new defaults (wrap off, the other platform's line ending), and wrap expected on after reset | It must start away from every default to prove reset |
| Line endings in the "every setting differs" test | `LineEndings.ForPlatform(!OperatingSystem.IsWindows())` | Different from the default on every platform |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2723 total, 2723 passed, 0 failed, 0 skipped.
- Added:
  - `LineEndingsTests.ForPlatform_IsCrLfOnWindows_AndLfElsewhere` (2 cases);
  - `AppSettingsTests.ASettingsFileWithTheOldDefaults_KeepsThem`;
  - `AppSettingsTests.TheDefaults_AreWhatAFreshInstallStartsWith` now asserts all six requested values.
- Fix cycles: 1. The first run had 18 Desktop failures, every one a test that assumed the old starting state.
  They were fixed by pinning that state in each test (see the decisions above).

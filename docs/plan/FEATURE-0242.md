# FEATURE-0242 — New settings defaults

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-0242-settings-defaults`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Objective

A fresh install, and *Reset to defaults*, start with:

- Theme: follow the system;
- Wrap long lines: on;
- Line endings: CRLF on Windows, LF on every other system;
- Pane resize step: 10 %;
- Minimap: on;
- Vim mode: on.

## Context & constraints

- `src/Enigma.TextEditor.Core/Settings/AppSettings.cs` today: `Theme = System`, `VimEnabled = true`,
  `WordWrap = false`, `ShowMinimap = true`, `PaneResizeStep = DefaultPaneResizeStep` (5),
  `DefaultLineEnding = Lf`. So theme, minimap and Vim are already as requested.
- `SettingsMenuViewModel.ResetAsync` copies a `new AppSettings()`, so *Reset to defaults* follows the property
  initializers.
- Enigma.Configuration writes every key, so an existing `settings.json` holds an explicit value for each
  setting: a default only reaches a key that is absent (a fresh install, or a file from before the key).
- `LineEndings.Default` (Core/Documents) is the style a file read with no line break gets. It is not the
  setting, and it is not in this item's scope.
- Tests that rely on the old defaults: `AppSettingsTests.TheDefaults_AreWhatAFreshInstallStartsWith`, and any
  Desktop test that creates a new document and expects `\n`, or relies on word wrap being off or a 5 % step.
- `PreferencesView` / the settings page labels CRLF "Windows" and LF "Linux and macOS"; the `LineEndingStyle`
  docs call LF "what a new file gets".

## Decisions

| Decision | Rationale |
|---|---|
| `LineEndings.ForPlatform(bool isWindows)` in Core (CRLF / LF), used by `AppSettings.DefaultLineEnding = LineEndings.ForPlatform(OperatingSystem.IsWindows())` | One pure, testable rule; the setting stays an enum persisted by name |
| `WordWrap = true`; `DefaultPaneResizeStep = 10` | The draft |
| Theme, minimap and Vim initializers unchanged, pinned by the defaults test | Already the requested values |
| Existing settings files keep their saved values | A user's earlier choice is not overridden by an upgrade |

## Steps

1. `Core/Documents/LineEndings.cs`: `ForPlatform(bool isWindows)`; `LineEndingStyle` docs no longer call LF
   "what a new file gets".
2. `Core/Settings/AppSettings.cs`: `DefaultPaneResizeStep = 10`, `WordWrap = true`,
   `DefaultLineEnding = LineEndings.ForPlatform(OperatingSystem.IsWindows())`; their docs say so.
3. Core tests: the defaults test asserts all six requested values (line endings through
   `OperatingSystem.IsWindows()`); `ForPlatform(true)` is CRLF and `ForPlatform(false)` is LF; a settings file
   that holds the old values keeps them.
4. Desktop tests that relied on the old defaults: pin the setting they depend on in the harness `configure`
   callback, rather than weakening an assertion.
5. Menu and page: nothing to change (*Pane Resize Step → 10%* exists; the page shows any value).

## Acceptance criteria

- `new AppSettings()` has theme System, word wrap on, minimap on, Vim on, resize step 10, and CRLF on Windows / LF
  elsewhere.
- *Reset to defaults* gives the same values.
- A `settings.json` that already holds a value for each key keeps them.
- Zero warnings, whole suite green.

## Out of scope

- `LineEndings.Default` (the style of a file read with no line break).
- Changing any setting a user already saved.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the line-ending default depends on the OS | `OperatingSystem.IsWindows()` through a pure `LineEndings.ForPlatform` | Testable on any OS; mirrors `Environment.NewLine` without parsing it | `Environment.NewLine == "\r\n"` (untestable for the other branch); a new `Platform` enum value (changes the file format) |
| Upgrading users | Saved values kept | The file holds every key; overriding a choice on upgrade would surprise | A one-time migration to the new defaults |
| Theme / minimap / Vim | Left as they are | Already System / on / on | — |
| Tests relying on the old defaults | Pin the old setting explicitly in that test | Keeps what each test checks; the default itself is pinned by the Core test | Rewriting expectations to the new defaults everywhere |
| `LineEndings.Default` | Unchanged | The draft is about the setting; detection of a break-less file is a separate behaviour | Making it platform-dependent too |

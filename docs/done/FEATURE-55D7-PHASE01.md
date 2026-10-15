# FEATURE-55D7-PHASE01 — Solution scaffold & app shell

**Item:** FEATURE-55D7 — Text editor foundation
**Branch:** `feature/feature-55d7-phase01-solution-scaffold`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The repository is now a .NET 10 / Avalonia 12 solution that builds clean and starts an empty,
themed window.

- **Shell.** The window has a native title bar, then a menu bar (*File → Exit*), a content
  placeholder and a status bar. The three Enigma hosts (dialog, overlay, info bar) sit last in the
  root panel.
- **Composition root.** `App`:
  - loads the settings before the container is built;
  - applies the theme before the window exists;
  - builds an IHost (Enigma services, NLog file log, shell services);
  - registers the hosts and storage providers before the window is shown;
  - stops the host on exit through a bounded `ShutdownSequence`.
- **Settings.** `AppSettings` holds theme, Vim on/off, word wrap, font size, default indentation and
  default line ending. It is persisted with Enigma.Configuration in
  `%APPDATA%/Enigma.TextEditor/settings.json`. An unreadable file is quarantined as
  `settings.invalid.json` and the defaults are used.

## Files / modules touched

**Created — repository root**

- `.gitignore`, `.gitattributes`, `.editorconfig` (house templates, written as LF)
- `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `LICENSE.md`
- `RELEASENOTES.md` (empty), `Enigma.TextEditor.slnx`

**Created — `src/Enigma.TextEditor.Core`**

- `Enigma.TextEditor.Core.csproj`, `AppInfo.cs`, `ProductVersion.cs`
- `Configuration/ConfigRecovery.cs`, `Configuration/ConfigLoadResult.cs`
- `Settings/AppSettings.cs`, `AppSettingsValidator.cs`, `AppTheme.cs`, `AppPaths.cs`,
  `SettingsLoader.cs`
- `Documents/LineEndingStyle.cs`

**Created — `src/Enigma.TextEditor.Desktop`**

- `Enigma.TextEditor.Desktop.csproj`, `app.manifest`, `Program.cs`, `App.axaml`, `App.axaml.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Services/IThemeService.cs`, `ThemeService.cs`, `IAppShutdown.cs`, `AppShutdown.cs`,
  `ShutdownSequence.cs`
- `ViewModels/MainWindowViewModel.cs`, `Views/MainWindow.axaml`, `Views/MainWindow.axaml.cs`

**Created — tests**

- `tests/Enigma.TextEditor.Core.UnitTests`:
  - `AppInfoTests`, `ProductVersionTests` (ported);
  - `Settings/SettingsLoaderTests` (ported, adapted), `Settings/AppSettingsTests`;
  - `Settings/TempSettingsFolder`.
- `tests/Enigma.TextEditor.Desktop.UnitTests`:
  - `CompositionRootTests`;
  - `Infrastructure/HeadlessAvaloniaFixture` (ported from FileExplorer), `TempFolder`,
    `FakeAppShutdown`.

## Deviations & follow-ups

- **`LineEndingStyle` added in this phase, not PHASE03.** `AppSettings.DefaultLineEnding` needs the
  enum, so the 2-value enum was ported now. PHASE03 ports the rest of `Documents`.
- **No `AppSettingsValidator` rules.** All constraints are attributes (`Range`, `EnumDataType`). The
  validator class exists to name the validator the loader is handed.
- **Ported `AppShutdown` renamed `ShutdownSequence`.** This is MarkdownEditor's bounded exit runner.
  The name `AppShutdown` is taken by the new `IAppShutdown` service, which closes the main window so
  later work can intercept the close for unsaved changes.
- **Not ported from MarkdownEditor:** splash screen, crash reporter, first-run dialog. None were
  asked for.
- **No application icon.** The artwork is user-owned. Follow-up: add `Assets/app.ico` and wire it as
  `ApplicationIcon` and `Window.Icon`.
- **Smoke run side effect.** Launching the app created `%APPDATA%/Enigma.TextEditor/logs/app.log` on
  the build machine. That is the app's normal behaviour.
- **Line endings.** Recommendation only: `core.autocrlf=true` on this machine warns about LF→CRLF
  in the working copy. The new `.gitattributes` (`* text=auto eol=lf`) keeps the repository LF.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Composition timing | Synchronous in `OnFrameworkInitializationCompleted` (FileExplorer style) | No splash, so nothing to paint first | MarkdownEditor's posted composition behind a splash |
| How *File → Exit* ends the app | `IAppShutdown.RequestExit()` closes the main window | The window's closing path is where unsaved changes will be asked about (FEATURE-3EE8 PHASE02) | `desktop.Shutdown()` (bypasses the window's Closing event) |
| Core XML documentation file | Off | App-internal library, as in MarkdownEditor; the ported engine stays buildable under warnings-as-errors | `GenerateDocumentationFile` (FileExplorer; CS1591 on every public member) |
| Exit gesture shown | `Alt+F4` | The platform's own; no Ctrl gesture is reserved for exit | `Ctrl+Q` (not universal) |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: **0 warnings, 0 errors**, XAML included.
- `dotnet test --solution Enigma.TextEditor.slnx`: **46 passed**, 0 failed, 0 skipped (Core +
  Desktop).
- Manual smoke: the Debug executable started and showed a window titled "Enigma Text Editor". The
  log recorded "Enigma Text Editor 0.1.0 started."; the process was then stopped.

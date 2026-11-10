# FEATURE-34A1-PHASE03 — Restore the session at start

## Summary

The session is now kept as the window closes and brought back at the next start: the same tabs, in the same
splits, where each was left. Unsaved changes are still asked about when the app closes, as before; keeping them is
PHASE04.

- `ISessionStore` / `SessionStore` (singleton, disposable) takes `session.lock` on first use and holds it until the
  container is disposed at exit.
  - `Load()` reads the session file and logs a moved-aside file as a warning.
  - `Save(state)` writes it and logs a failure as an error.
  - For an instance that does not own the session, both do nothing.
- `MainWindowViewModel.StartAsync` runs the first-run dialog, then restores the session, then opens the
  command-line files in the active pane. `SaveSession()` captures the workspace and saves it.
- `MainWindow.OnClosing` calls `SaveSession()` when the close goes ahead, after today's questions are answered.
  That covers every way the window closes: the close button, Alt+F4, `:qa` and a system logoff.
- `App.BuildHost` now takes the session file and lock paths, as it takes the log path, and registers the store
  (`AddSession`). `App` passes `AppPaths.SessionFile` and `AppPaths.SessionLockFile`. Tests pass their temp folder,
  so the suite never touches the user's own session.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/ISessionStore.cs`, `SessionStore.cs`
- Modified: `src/Enigma.TextEditor.Desktop/App.axaml.cs`, `Extensions/ServiceCollectionExtensions.cs`,
  `ViewModels/MainWindowViewModel.cs`, `Views/MainWindow.axaml.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/CompositionRootTests.cs`, `Infrastructure/EditorHarness.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SessionStartupTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-34A1.md`

## Deviations & follow-ups

- `App.BuildHost` gained two required parameters (session file and lock), which the plan did not foresee. Without
  them, the composition-root test that shows and closes the container's window would have written the developer's
  real `%APPDATA%` session.
- `SessionStore.Save` also catches `ArgumentException`. System.Text.Json throws it for a string it cannot write as
  UTF-8 (a lone surrogate), and a closing window must not be stopped by that. It matters once PHASE04 stores text.
- `EditorHarness` takes an optional `sessionFolder`, so one harness can start from what another closed with, and it
  disposes its store (the lock) before deleting its folder.
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| When the lock is claimed | On first use (`Load` at start, or `Save`) | Building the container touches no file; the start claims it before anything else could |
| Where the close saves | `MainWindow.OnClosing`, once the close goes ahead | The buffers are still there; `App.OnExit` is too late to fall back to questions (PHASE04) |
| Registration | Factory in `AddSession(sessionFile, lockFile)` | The paths are the composition root's to give, like the log's |

## Build/test evidence

- Baseline: see `docs/done/BUG-C6A9.md` (0 warnings; 3199 tests, 1 pre-existing failure).
- `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3251 tests, 3250 passed, 1 failed (the pre-existing failure).
- `%APPDATA%\Enigma.TextEditor` was listed before and after the full run: no `session.json` or `session.lock`
  appeared there.
- Tests added:
  - `SessionStartupTests`:
    - a session kept at close comes back at the next start, with the command-line file opening after it in the
      active pane;
    - no session starts as before;
    - an unusable session file is moved aside and the start is as before;
    - a second instance neither restores nor overwrites the session the first owns;
    - the real window keeps nothing when the question is cancelled, and keeps the session once it is answered (a
      discarded file comes back as it is on disk).
  - `CompositionRootTests`: `ISessionStore` resolves.
- Docs sweep: README's "Closing a document with unsaved changes asks whether to save them, as does quitting with
  any" is still true until PHASE04. No edit.

# FEATURE-4158-PHASE02 — Autosave from start to close

## Summary

The editor now keeps its session while it runs. Once a start has completed, the window's session (tabs, splits,
explorer, unsaved text) is checked every 5 seconds and written when it changed. A crash, a kill or a power cut
therefore loses only what changed since the last write, and the next start restores that file the same way it
restores a closed session.

- DI: `AddSession` registers `SessionAutosave` as a singleton and, if nothing else has, `TimeProvider.System`. The
  container disposes the autosave on exit, which stops it.
- `MainWindowViewModel` takes the autosave. At the end of a successful `StartAsync` it starts it with
  `() => CaptureSession(keepUnsaved: !DiscardChangesOnExit)`, the same capture the close uses, so after `:qa!` no
  unsaved text is kept. `KeepSessionOnClose()` and `SaveSession()` write through `SaveNow`, which always gets the last
  word. A new `WindowClosed()` stops the autosave.
- `MainWindow.OnClosed` calls `WindowClosed()`.
- Unchanged: a non-owner instance starts no autosave. A start that throws part-way starts none either, so the session
  it was restoring is left as it was.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Extensions/ServiceCollectionExtensions.cs`,
  `ViewModels/MainWindowViewModel.cs`, `Views/MainWindow.axaml.cs`
- Modified: `README.md`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/CrashRecoveryTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`

## Deviations & follow-ups

- The harness builds the autosave with the internal synchronous runner instead of `Task.Run`, so a tick has finished
  once the dispatcher has run it. The real thread-pool path is covered by PHASE01's
  `TheClosesWrite_WaitsForABackgroundWriteAlreadyWriting_AndHasTheLastWord`.
- Not verified on a real desktop: a real crash (process killed) followed by a restart. The harness test simulates it by
  writing through a tick, releasing the lock without a close, and starting a new editor on the same folder.
- Follow-ups (out of scope in the plan): a setting for the interval or to turn the autosave off; a status-line notice
  when the session cannot be written (today only the log records it, and closing still asks the questions).
- Flaky test, unrelated: `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed in the full run
  (see `docs/done/FEATURE-4158-PHASE01.md`: it also fails on the tree without these changes).
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
- Environment: SDK 11.0.100 from a scratch `global.json`, as in `docs/done/FEATURE-768C.md`.
- Documentation sweep (§7): in `README.md`, the quitting paragraph now says the session is also kept every five seconds
  when something changed, so a crash loses no more than the last few seconds; the `session.json` line says it is kept
  up to date while the editor runs.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the autosave is stopped | `MainWindow.OnClosed` → `MainWindowViewModel.WindowClosed()` | A close cancelled by the questions keeps it running; any real close stops it |
| Where `TimeProvider` is registered | `TryAddSingleton` in `AddSession` as well | `AddSession` must not depend on `AddShell` being called after it |
| Start inside `try`, not `finally` | Yes | A start that failed must not replace the session it was restoring |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-768C.md` (0 warnings; 3332 tests, 1 pre-existing failure).
- This dev: `dotnet build Enigma.TextEditor.slnx --no-incremental` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` ran 3371 tests: 3369 passed, 2 failed (`InstanceServerTests…` and
  the flaky `FileDropViewTests…`).
- Existing session tests green: `HotExitTests` (6), `SessionStartupTests` (5), `ExplorerSessionTests` (6).
- Tests added (7):
  - `CrashRecoveryTests` (6): after a tick, a crash restores a changed file and an untitled draft, both unsaved, with
    the file on disk untouched; a change after the last tick is what a crash loses; no tick writes before the start has
    restored, and the autosave runs after it; a non-owner never writes; after `:qa!` a tick keeps no unsaved text;
    after the window closed, no tick replaces the close's write. The last test fails with the `OnClosed` call removed
    (checked).
  - `CompositionRootTests` (1): the container resolves `SessionAutosave`.

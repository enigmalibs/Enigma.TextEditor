# CODE-REVIEW-0734-PHASE10 — [Medium] SOLID-01 Start-up and session lifecycle leave `MainWindowViewModel`

## Summary

`MainWindowViewModel` took 14 constructor parameters. Four of them were there only for the window's start and close:
`FirstRunState`, `IFirstRunDialogService`, `ISessionStore` and `SessionAutosave`. Those now belong to a collaborator,
as the review recommended:

- New `Services/EditorLifecycle` (a singleton in `AddShell`) owns the start sequence and the session writes:
  - the start sequence: the first-run dialog, the session restore, the start-up files, the explorer, then the
    autosave start, with `IsStarting` set while it runs;
  - `CaptureSession`, which captures the editor area and the explorer;
  - `KeepSessionOnClose`, `SaveSession` and `WindowClosed`.
  It takes the workspace and the explorer, which the session is restored into and captured from, the four services,
  and `IAppShutdown` for the `:qa!` flag.
- `MainWindowViewModel` takes `EditorLifecycle` instead of the four, going from 14 to 11 parameters. Seven of the 11
  are the child view-models it exposes for binding; four are services.
- The window's public members (`StartAsync`, `IsStarting`, `CaptureSession`, `KeepSessionOnClose`, `SaveSession`,
  `WindowClosed`) stay and delegate to the lifecycle. `App` and `MainWindow` therefore call what they called before.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/EditorLifecycle.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs`, `Extensions/ServiceCollectionExtensions.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs` (builds and exposes `Lifecycle`),
  `CompositionRootTests.cs` (resolves it; the exit test builds the window view-model with it)
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/EditorLifecycleTests.cs`

## Deviations & follow-ups

- The sequence tests (`SessionStartupTests`, `CrashRecoveryTests`, `HotExitTests`, `ExplorerSessionTests`,
  `FirstRunTests`) still drive the start through the window, whose members delegate. They test the sequence through
  the API the application uses, so they were not moved.
- `EditorLifecycle` takes 7 parameters. It is a sequencer: each collaborator is one of its steps, and the order
  between them is what the class is for (see its remarks). PHASE11 and PHASE12 change how it reaches the editor area,
  not what it orders.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Keep the window's members | Yes, as one-line delegations | `App`, `MainWindow` and the tests keep one entry point, the window |
| Folder | `Services/` | It is not bound to a view, and like `DocumentService` it works over view-models |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3407 tests, 3405 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (4):
  - `CompositionRootTests` resolves `EditorLifecycle` (1 new theory row).
  - `EditorLifecycleTests` (3):
    - while the start runs, the lifecycle and the window both say it is starting;
    - the start starts the autosave, and the window closing stops it;
    - the window's capture is the lifecycle's (editor area and explorer).
- Start-up, hot-exit, crash-recovery, explorer-session and first-run tests pass unchanged.
- Documentation sweep (§7): nothing user-facing changed.

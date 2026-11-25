# CODE-REVIEW-0734-PHASE11 — [Medium] SOLID-01 / SOLID-03 Session capture and restore leave `WorkspaceViewModel`

## Summary

The workspace owned panes and tabs, and also turned itself into a saved session and back. That was about 210 lines and
one more reason for the class to change. Capture and restore now live in their own service:

- New `Services/WorkspaceSession` (a singleton). It is built from `IDocumentService`, `DocumentRegistry` and
  `StatusMessages`, and has two methods:
  - `Capture(workspace, keepUnsaved)` reads what the workspace shows through its public members.
  - `RestoreAsync(workspace, state)` reads the buffers, decides each pane's tabs and the one showing, then hands the
    panes back to the workspace whole.
  The workspace is passed to each call, as with the command handlers, so the service holds none.
- The workspace keeps one internal operation, `ReplacePanes(layout, panes, active)`, with a new
  `ViewModels/RestoredPane` record. This keeps the pane bookkeeping inside the workspace: registry retains, pane
  events, closing panes left empty, and the active pane.
- `EditorLifecycle` restores and captures through `WorkspaceSession`.
- `WorkspaceViewModel` loses `CaptureSession`, `RestoreSessionAsync` and `Rebuild`, going from 1,436 to 1,282 lines.
  It no longer refers to `Enigma.TextEditor.Core.Sessions` at all.
- The behaviour is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/WorkspaceSession.cs`, `src/Enigma.TextEditor.Desktop/ViewModels/RestoredPane.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`, `Services/EditorLifecycle.cs`,
  `Extensions/ServiceCollectionExtensions.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`,
  `ViewModels/HotExitTests.cs`, `ViewModels/SessionRestoreTests.cs` (call the service)

## Deviations & follow-ups

- `EditorLifecycle` now takes 8 parameters (`WorkspaceSession` added). It still needs the workspace itself for the
  start-up files; PHASE12 keeps that call on the workspace, which delegates. Folding more into it would mix sequencing
  with the steps.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who builds the restored tabs | `WorkspaceSession` | Which tabs come back, where each was left and which one shows are session rules |
| Who adopts them | The workspace (`ReplacePanes`) | A tab in a pane must always keep its buffer open in the registry, an invariant every other workspace operation keeps itself |
| Visibility of the hand-back | `internal` | Only the session service uses it, and it is not part of the editor area's API |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3408 tests, 3406 passed, 2 skipped (PHASE06's), 0 failed.
- Test added (1): `CompositionRootTests` resolves `WorkspaceSession`.
- `SessionRestoreTests` and `HotExitTests` now call `WorkspaceSession`, and assert the same things as before.
  `CrashRecoveryTests`, `SessionStartupTests` and `ExplorerSessionTests` pass unchanged.
- Documentation sweep (§7): nothing user-facing changed.

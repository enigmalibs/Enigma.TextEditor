# CODE-REVIEW-0734-PHASE04 — [Medium] LOG-01 File-operation failures in the explorer are logged

## Summary

Before this change, a failed create, rename, delete or move to the trash in the explorer was shown in a dialog and
then gone. Nothing reached `app.log`, so a report of "delete did nothing" could not be diagnosed. Both failure paths
now log at `Error` before the dialog is shown, with the exception first and the path as a placeholder, as
`DocumentService` already does for its own failures.

- Move to the trash: `IOException` or `UnauthorizedAccessException` is logged as
  `"{Path} could not be moved to the trash."`.
- `RunAsync`, used by create file, create folder, rename and permanent delete, now takes the path the operation acts
  on (the new entry's full path, or the renamed or deleted one). A `FileOperationException` is logged as
  `"{Failure}: {Path}."`, where `{Failure}` is the dialog's own title ("The file could not be created", …).
- The message the user sees, and the folder listed again afterwards, are unchanged.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/FileExplorerViewModel.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/RecordingLogger.cs` (each entry also keeps its
  exception), `Infrastructure/FakeTrashService.cs` (`Throws`), `Infrastructure/EditorHarness.cs` (the explorer logs to
  a `RecordingLogger` exposed as `ExplorerLog`)
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileExplorerCreateTests.cs`,
  `ViewModels/FileExplorerDeleteTests.cs`, `ViewModels/FileExplorerRenameTests.cs`

## Deviations & follow-ups

- No deviation from the plan.
- The permanent-delete failure goes through the same `RunAsync` path that create and rename test. It is not tested
  separately, because making `FileOperations.DeleteAsync` fail on demand would need a locked file, and that behaves
  differently per platform.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Template for the operation failures | `"{Failure}: {Path}."` with the dialog's title as `{Failure}` | One constant template for four operations. The log line reads like the dialog |
| How the tests see the log | The harness's explorer logs to a `RecordingLogger` | No new construction path in the tests. The other loggers stay `NullLogger` |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3375 tests, 3375 passed.
- Tests added (3):
  - `FileExplorerCreateTests.AFailedCreate_IsLoggedAsAnError_WithItsExceptionAndPath_BeforeItIsShown`
  - `FileExplorerRenameTests.AFailedRename_IsLoggedAsAnError_WithItsExceptionAndPath`
  - `FileExplorerDeleteTests.ATrashThatFails_IsLoggedAsAnError_WithItsExceptionAndPath_AndReported`
  - Each test checks one `Error` entry carrying the exception and the path, plus the dialog as before.
- Documentation sweep (§7): no document describes what the explorer logs, so nothing was edited.

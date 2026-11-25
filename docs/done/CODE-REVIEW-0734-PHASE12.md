# CODE-REVIEW-0734-PHASE12 — [Medium] SOLID-01 / SOLID-03 File intake leaves `WorkspaceViewModel`

## Summary

The workspace also decided how files arrive from outside it: picked in the open dialogs, named on a command line, or
dropped on a pane. That included its "fresh tab" rule and its file-system probes. All of this now lives in its own
service:

- New `Services/FileIntake` (a singleton), built from `IDocumentService`, `DocumentRegistry`, `IFilePrompts` and
  `StatusMessages`. It holds `OpenFromPromptAsync`, `OpenHexFromPromptAsync`, `OpenDroppedAsync`,
  `OpenStartupFilesAsync` and `AnyFile`, plus the fresh-tab rule they share (`IsFresh`, `OpenInPlaceOfAsync`). The
  workspace is passed to each call.
- The workspace takes `FileIntake` instead of `IFilePrompts`, staying at 10 parameters, and holds no `File` or
  `Directory` probe any more.
  - The Open and Open as Hex commands call the intake, and so do its `IPaneHost.OpenDroppedAsync`,
    `OpenStartupFilesAsync` and `OpenFromPromptAsync` (each one line).
  - `SplitWithFilesAsync` keeps the split itself (layout work) and asks the intake whether any path is a file, then
    lets it open the files.
- The workspace keeps one internal hand-back, `ReplaceTab(pane, tab, document)`, so the registry bookkeeping stays
  its own.
- `WorkspaceViewModel` goes from 1,282 to 1,152 lines (1,436 at the start of the run).
- The behaviour is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/FileIntake.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`, `Extensions/ServiceCollectionExtensions.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs` (builds and exposes `Intake`),
  `CompositionRootTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/FileIntakeTests.cs`

## Deviations & follow-ups

- **Acceptance not fully met: the workspace is 1,152 lines, not under 1,000.** The extractions the review names
  (session capture and restore in PHASE11, file intake here) are done. What remains is the workspace's own work:
  - panes, layout and focus;
  - moving and splitting tabs, which the `IPaneHost` contract and the window commands use;
  - opening, showing and saving through `IDocumentService`;
  - closing with the unsaved-changes question.
  About 40% of the file is XML documentation. A further split, most naturally the save and close-question flow
  (`SaveAllAsync`, `ConfirmCloseAllAsync`, `ResolveUnsavedAsync`), is a follow-up beyond the review's fix.
- The plan had `EditorLifecycle` and `MainWindowViewModel.OpenForwardedAsync` call `FileIntake` directly. The
  workspace's `OpenStartupFilesAsync` stays instead, as a one-line hand-off, so the lifecycle does not take a ninth
  parameter and the window a twelfth just to reach the same call.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The fresh-tab rule | Moved into the intake, with the registry | Only the intake uses it |
| The split for dropped files | Stays in the workspace | It is layout work (`Batch`, `AddPane`, `Activate`). The intake only says whether to and opens |
| Hand-back for a fresh tab | `internal ReplaceTab` | A tab's buffer is retained and released by the workspace, as in every other operation |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3412 tests, 3410 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (4):
  - `CompositionRootTests` resolves `FileIntake` (1 new theory row).
  - `FileIntakeTests` (3), called on the intake directly:
    - the open dialog starts in the active buffer's folder and opens every file chosen;
    - the hex dialog opens in the hex viewer;
    - only a file counts for a split.
- Drop, split-with-files, start-up, forwarded and open tests pass unchanged, through the workspace's hand-offs.
- Documentation sweep (§7): nothing user-facing changed.

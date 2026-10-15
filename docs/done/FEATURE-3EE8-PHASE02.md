# FEATURE-3EE8-PHASE02 — Unsaved changes & startup args

**Item:** FEATURE-3EE8 — Editor panes, tabs & files
**Branch:** `feature/feature-3ee8-phase02-unsaved-changes-args`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

Work is no longer lost silently, and the app opens the files it is started with.

**Unsaved changes**

- Closing the last tab of a dirty buffer asks *Save changes?*:
  - **Save** (default): saves, or runs Save As for an untitled buffer. If the save is cancelled or
    fails, the tab stays open.
  - **Don't save**: closes and drops the changes.
  - **Cancel**: keeps the tab.
- Closing a tab while another tab still shows the same buffer asks nothing.
- `CloseTabAsync(…, discard: true)` skips the question; it is ready for Vim's `:q!` (PHASE03).
- **Window close and File → Exit.** `MainWindow.OnClosing` cancels the close when anything is dirty
  and asks about each dirty buffer in turn, bringing its tab to the front first. It closes the window
  again only when no answer was Cancel and no save failed.

**Startup arguments**

- `StartupFiles.Parse` (Core):
  - resolves each argument against the working directory;
  - opens the same file only once;
  - skips folders and malformed paths;
  - marks which files exist.
- The workspace opens them after the window is shown, with the last one active.
- A missing file whose folder exists becomes an empty buffer bound to that path. It is not dirty,
  and the first save creates the file.
- Problems (folders, missing folders) are reported on the status bar's new message line.
- With no files to open, an untitled buffer is ready, as Vim starts with an empty buffer.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Core/Documents/StartupFiles.cs` (`StartupFile`, `StartupFiles`).
- `src/Enigma.TextEditor.Desktop/Services/`: `IUnsavedChangesPrompt`, `UnsavedChangesPrompt`,
  `UnsavedChangesChoice`, `StatusMessages`.
- Tests:
  - `tests/Enigma.TextEditor.Core.UnitTests/Documents/StartupFilesTests.cs`;
  - `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/UnsavedChangesTests.cs`;
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeUnsavedChangesPrompt.cs`.

**Modified**

- `Services/IDocumentService.cs`, `DocumentService.cs`: new `CreateForMissingFile`.
- `ViewModels/WorkspaceViewModel.cs`: `HasUnsavedChanges`, `CloseTabAsync(…, discard)` with the
  question, `ConfirmCloseAllAsync`, `OpenStartupFilesAsync`.
- `ViewModels/MainWindowViewModel.cs`: `ConfirmCloseAsync`.
- `ViewModels/StatusBarViewModel.cs`: exposes the message line.
- `Views/MainWindow.axaml`: message line in the status bar. `Views/MainWindow.axaml.cs`: closing
  guard.
- `App.axaml.cs`: parses `desktop.Args` and posts the startup open.
- `Extensions/ServiceCollectionExtensions.cs`: the new registrations.
- `tests/.../Infrastructure/EditorHarness.cs`: the new dependencies.

## Deviations & follow-ups

- **Untitled buffer at startup.** With no arguments, the app now starts on `Untitled-1` instead of
  the empty placeholder. The plan did not specify the no-argument case. Vim starts on an empty
  buffer, and this lets the user type immediately.
- **No single-instance handling.** Opening a file while the app is already running starts a second
  process. Follow-up, as planned out of scope.
- **OS shutdown and session end.** These close through the same guard. An OS that does not wait for
  the dialog ends the process regardless; that is platform behaviour.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the window-close questions run | `OnClosing` cancels, then an async walk re-closes on success (`_closeConfirmed`) | `Closing` cannot await; this is the standard pattern and keeps the logic in the VM | A blocking wait on the dialog (would deadlock the UI thread) |
| Order of the questions on window close | Registry order (order opened), each buffer's tab activated first | The user sees what they are deciding about | All buffers in one dialog (no per-file choice) |
| Dismissing the dialog (Esc or scrim) | Cancel | Nothing is lost by not answering; the Enigma dialog returns `None` there | Treat it as Don't save |
| A missing file in a folder that does not exist | Not opened, reported on the status line | No save could create it without creating folders, which was not asked | Create the folders on save |
| Where the status message lives | A `StatusMessages` service shown by the status bar | The workspace can report without depending on the status bar (which depends on it); Vim messages join it in PHASE03 | A property on the workspace |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2119 passed**, 0 failed, 0 skipped. Added:
  - 7 startup-argument tests;
  - 11 close-flow tests: clean close, save, failed save, don't save, cancel, discard, window walk
    with a cancel, window walk all resolved, the real window staying open and then closing, startup
    opening with a missing file and a folder, the no-argument start.
- Manual smoke: launching the Debug build with a file path showed the window titled
  `enigma-smoke.txt — Enigma Text Editor`.

# FEATURE-7B55-PHASE02 — Forward launches to the open window

**Item:** FEATURE-7B55 — Open files in the running instance
**Branch:** `feature/feature-7b55-phase02-forward-files`
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Summary

A file opened from the system (a double-click in Explorer, *Open with*, the Linux launcher's `%F`) while the editor
is running now opens as a tab in the window used last, and that window comes to the front. No second window
appears. Started without a file, the editor still opens a new instance, so several can run side by side.

- **Launch side** (`Program.Main` → `InstanceForwarding.TryForward`, before Avalonia starts): it forwards only
  when all of these hold:
  - the arguments name at least one file;
  - the pointer file names an instance;
  - that instance is not this process, its process is alive, and it has the same program name.

  The files go as full paths. On Windows, the launch first calls `AllowSetForegroundWindow(pid)` so the receiving
  window may take the foreground. Every failure falls back to starting an instance, as before.
- **Receiving side** (`RunningInstance`, built in `App` once the window exists):
  - it serves this process's pipe and writes the pointer;
  - every `Window.Activated` rewrites the pointer off the UI thread, so the window used last is the one that
    receives files;
  - a request is posted to the UI thread. There the window comes to the front, restored to maximised if it was
    maximised before being minimised, and the files go to `MainWindowViewModel.OpenForwardedAsync`;
  - on exit, before the host stops, the server is disposed and the pointer cleared if it still names this
    instance;
  - a pipe that can't be served is logged as a warning, and that instance simply receives no files.
- **Where files open:** `OpenForwardedAsync` shows the files page and opens the files exactly as command-line files,
  in the active pane: a missing file becomes a new buffer, a folder is reported. `OpenStartupFilesAsync` now also
  lets a fresh untitled tab give its place to the first file, as a drop does (FEATURE-4217). At startup no tab
  exists yet, so startup behaviour is unchanged.
- **README:** the *Files* paragraph describes the new behaviour.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/InstanceForwarding.cs`,
  `src/Enigma.TextEditor.Desktop/Services/RunningInstance.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Program.cs`, `src/Enigma.TextEditor.Desktop/App.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-7B55.md`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/InstanceForwardingTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Services/RunningInstanceTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/ForwardedFilesTests.cs`, this file

## Deviations & follow-ups

- **Not tried with two real processes.** This machine has no virtual X server, and the run doesn't open windows on
  the user's desktop. Both sides, and the forward between them over a real pipe, are covered in-process, but
  `Program.Main`, `App`'s wiring and the foreground hand-off on Windows have only been checked by reading the code.
  A manual check is the follow-up: on Windows, double-click an associated file with one or two windows open; on
  Linux, use the launcher.
- **Known limits, out of scope by the plan:**
  - files forwarded while a dialog is open, or while the window is asking about unsaved changes on close, open
    behind it, or are lost with the closing window;
  - a folder named on the forwarding launch is dropped rather than reported.
- Baseline: as recorded in `docs/done/FEATURE-9F56-PHASE01.md` (0 warnings, 2780 passed).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the receiving side lives | `RunningInstance`, built and disposed by `App`, not registered in the container | It is process-edge plumbing with a lifetime tied to the window, like `UnhandledExceptionHooks`. As a container singleton it would be disposed synchronously by `host.Dispose()`, which an async-only disposable refuses |
| Process check | Alive, not this process, and the same program name as this one | A pid outlives its process. Probing a dead pipe costs the full 1 s connect timeout on Windows |
| What is forwarded | The full paths of the files only | The receiver's working directory differs. Folders are not files to open |
| Window state on restore | The last state that wasn't minimised | A maximised window comes back maximised |
| A fresh tab | Replaced by the first file that opens | FEATURE-4217's rule for drops. A window opened moments before has one |
| Windows foreground | `AllowSetForegroundWindow` via `DllImport` | `LibraryImport` would need `AllowUnsafeBlocks` for one call |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2862 passed, 0 failed, 0 skipped (+14 against 2848).
- **Tests added:**
  - `InstanceForwardingTests`: files go to the instance used last, as full paths, a missing file included; no
    files or a folder only → no forward; no pointer, a dead process, or this very process → nobody asked; a refusal
    → the launch keeps them; end to end, `RunningInstance` and `InstanceClient` over a real pipe deliver the files,
    and once it is disposed the pointer is gone and a forward fails;
  - `RunningInstanceTests`: activation names the instance again after another took the pointer; closing leaves
    another's pointer alone and is safe to repeat; an unservable pipe is not listening and never names itself;
  - `ForwardedFilesTests`: files open in the active pane on the files page from the settings page; a fresh tab
    gives its place, an untitled tab with text is kept; a missing file is a new buffer and a folder is reported;
    `BringToFront` restores maximised or normal as it was.

# CODE-REVIEW-0734-PHASE20 — [Medium] PERF-02 File I/O and encoding leave the UI thread

## Summary

On a large file, or a slow or disconnected network path, the window stopped responding at each of these points,
because the work ran on the UI thread:

- the session was read and parsed;
- a save encoded the whole document before its first `await`;
- existence probes and hex opens touched the disk directly.

Each site now runs that work on the thread pool and awaits it:

- **Session load:** `EditorLifecycle.StartAsync` reads it with `await Task.Run(_session.Load)` (was
  `MainWindowViewModel.cs:190`).
- **Save:** `DocumentService.WriteAsync` still takes the text, its version and the format on the UI thread. Then
  `DocumentFile.WriteAsync`, meaning the encoding and every touch of the disk (probe, temporary file, flush, move), runs
  through `Task.Run`. A keystroke that lands during the write still leaves the buffer dirty (`DocumentFile.cs:170`,
  `DocumentService.cs:153` area).
- **Hex views:** `OpenHexAsync` and the session restore open `HexDocument` through `Task.Run`. A new `AddHex` checks
  the registry again after the open, so two opens of the same file at once still give one buffer, and the second
  handle is disposed (`DocumentService.cs:153`, `:188`).
- **Probes:** new `IDocumentService.ProbeAsync(path)` returns a new `PathKind` (`File`, `Folder` or `Missing`), off
  the UI thread. `FileIntake` uses it for drops (instead of `File.Exists` / `Directory.Exists`), for the start-up
  files' folder check and for `AnyFileAsync`, which replaces `AnyFile`. The workspace awaits it before a split for
  dropped files (was `WorkspaceViewModel.cs:617`, `:916-927`, `:1154`, now in `FileIntake`).
- **Command line:** `App` parses the start-up arguments (`StartupFiles.Parse`, which probes the disk) through
  `Task.Run` inside the posted start, and does the same for files another launch hands over (`App.axaml.cs:85`,
  `:177`).

Together with PHASE07 (the hex copy's read), every location the finding lists is covered.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Services/PathKind.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Services/IDocumentService.cs`, `Services/DocumentService.cs`,
  `Services/FileIntake.cs`, `Services/EditorLifecycle.cs`, `ViewModels/WorkspaceViewModel.cs`, `App.axaml.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/DocumentServiceIoTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/FileIntakeTests.cs` (`AnyFileAsync`),
  `Views/VimEditingTests.cs` (see below)

## Deviations & follow-ups

- `VimEditingTests` (`:w` and Ctrl+S) waited by polling `File.ReadAllText` on the file being saved. With the write on
  the thread pool, the poll could open the file while the write held it ("being used by another process"). Both now
  wait for the buffer to be marked saved, then assert the same contents as before.
- `InstanceForwarding.TryForward` still parses the arguments synchronously in `Main`. There is no window then, so
  there is no UI thread to free; it was left as it is.
- `DocumentFile.ReadAsync` looks at the file's length synchronously before its async read. That is one metadata call,
  not in the review's list, and also left as it is.
- Fix cycles used: 1. The first full run failed on `VimEditingTests.CtrlS_SavesInNormalMode` (the race above). Making
  both waits wait on the save was the fix, and the second full run was green. The save-driving classes then passed in
  three runs in a row.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where `Task.Run` goes | In Desktop (`DocumentService`, `EditorLifecycle`, `App`), around Core's calls | Core stays a library with no `Task.Run` in it |
| What runs on the pool in a save | Encoding and the whole write, not just the encoding | The probe-open, temporary file, flush and move are also blocking disk calls |
| Probe shape | One `ProbeAsync` returning `PathKind` | One trip to the disk answers file, folder or nothing, and it replaces two `Exists` calls |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - First `dotnet test --solution Enigma.TextEditor.slnx`: 3436 tests, 1 failed (the race above).
  - After the fix: 3436 tests, 3434 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (4) in `DocumentServiceIoTests`:
  - the probe's answers (file, folder, missing, empty);
  - a save writes the text it took first, and a keystroke during the write leaves the buffer dirty;
  - two hex opens of the same file at once give one buffer;
  - start-up files in an existing folder versus a missing one, through the awaited probe.
- Documentation sweep (§7): nothing user-facing changed.

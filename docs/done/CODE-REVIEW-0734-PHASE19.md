# CODE-REVIEW-0734-PHASE19 — [Medium] PERF-01 The autosave captures and writes only text that changed

## Summary

Before, every 5-second tick did the following: read `Document.Text` of every dirty buffer on the UI thread (up to
32 MB each); then, whenever anything differed (a caret move was enough), serialised all of the text into
`session.json` and flushed it. With the side files of PHASE18 in place, a buffer's text is now captured and written
only when it changed:

- `DocumentViewModel.ToSession(keepUnsaved)` now returns `(SessionDocument, SessionText?)`. A kept text is named
  `<buffer key>-<generation>.txt`:
  - The key is a GUID per open buffer.
  - The generation moves on only when the document's `ITextSourceVersion` has changed since the last capture.
  - The `SessionText` writes from `Document.CreateSnapshot()`, AvaloniaEdit's immutable rope (shared, not copied),
    so neither an unchanged nor a changed buffer is copied to a string on the UI thread.
  - The entry names the file (`UnsavedTextFile`) and carries no inline text.
- Core `SessionCapture(State, Texts)` is what is captured and saved:
  - `WorkspaceSession.Capture` returns one.
  - `EditorLifecycle` captures one, with the explorer's folder added, for the autosave and for both close writes.
  - `SessionAutosave` compares `capture.State`. That is cheap: an unchanged buffer has the same name, and only names
    are compared.
  - `ISessionStore.Save` takes a capture. `SessionStore` keeps a `Save(SessionState)` shorthand for a session with
    no texts.
- The texts are written on the thread pool, inside the store's write. A file already on the disk is not written
  again, and a text no longer named is deleted after the session (PHASE18).
- After a caret move, the tick rewrites the small `session.json` and no text. After a keystroke, the tick rewrites
  that buffer's text only. `SaveSession` (no unsaved changes kept) leaves no text behind.
- `EditorLifecycle.CaptureSession` (and the window's) still returns the `SessionState`, for the code and tests that
  look at it.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Sessions/SessionCapture.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/DocumentViewModel.cs`, `Services/WorkspaceSession.cs`,
  `Services/EditorLifecycle.cs`, `Services/SessionAutosave.cs`, `Services/ISessionStore.cs`, `Services/SessionStore.cs`
- Modified: `README.md`, `SECURITY.md` (where the unsaved text is kept)
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SessionTextCaptureTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeSessionStore.cs`,
  `Services/SessionAutosaveTests.cs` (its helper builds a capture), `ViewModels/SessionRestoreTests.cs` (`.State` of a
  capture), `ViewModels/CrashRecoveryTests.cs` (see below)

## Deviations & follow-ups

- `CrashRecoveryTests.OnceTheWindowHasClosed_NoTickReplacesTheSessionItKept` read the unsaved text straight out of
  `session.json`, which no longer holds it. It now reads the session back through `SessionFile.Read`. Its assertions
  are unchanged: the close's session holds "at the close", and a later tick leaves the file as it was.
- "No `Document.Text` read for an unchanged buffer" holds by construction: capture reads only `Document.Version` and
  takes a rope snapshot. A test cannot observe a property read on AvaloniaEdit's document, so the tests check what
  follows from it instead: same name, no rewrite.
- Fix cycles used: 0. The first run of the session classes found the one test above reading the old format, before
  the first full suite run.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Change detection | `ITextSourceVersion.CompareAge` against the version last captured | O(1), and exact: any edit, undo included, gives a new version |
| What is handed to the write | A rope snapshot (`CreateSnapshot`) written with `WriteTextTo` | No string copy on the UI thread. The snapshot is immutable, so writing it on the thread pool is safe |
| Snapshot for an unchanged buffer | Taken anyway (O(1)), its writer called only when the file is missing | A text whose earlier write failed is written at the next save, with no state kept about writes |
| The window's `CaptureSession` | Still returns `SessionState` | Its callers look at the session, not at the texts |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3432 tests, 3430 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (5) in `SessionTextCaptureTests`:
  - an unchanged buffer keeps its name, and a changed one gets a new name;
  - a caret-only tick rewrites the session but no text (same file, same write time);
  - a keystroke rewrites only that buffer's text;
  - the text written is the one captured, even when the buffer changes before the write;
  - a session kept without unsaved changes leaves no text behind.
- Crash recovery, hot exit, session restore, start-up and explorer-session tests pass (43).
- Documentation sweep (§7):
  - `README.md`: the files list now says the text of each unsaved change is in a file of its own in `unsaved` beside
    `session.json`, and that a set-aside session's texts go to `unsaved.invalid`.
  - `SECURITY.md`: the session paragraph now says the same.

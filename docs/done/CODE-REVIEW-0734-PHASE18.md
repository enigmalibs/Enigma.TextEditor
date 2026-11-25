# CODE-REVIEW-0734-PHASE18 — [Medium] PERF-01 Unsaved text is kept in side files beside the session

## Summary

This is the Core half of PERF-01: the session format can now keep each buffer's unsaved text in a file of its own, so
that rewriting `session.json` (on a caret move, a tab switch) no longer rewrites every unsaved text. PHASE19 makes the
autosave use it.

- Format version 2 (`SessionState.CurrentVersion = 2`, `OldestVersion = 1`). A new optional field,
  `SessionDocument.UnsavedTextFile`, holds a plain file name in an `unsaved` folder beside the session
  (`AppPaths.UnsavedFolderName`, `SessionFile.UnsavedFolderFor`).
- New `SessionText(FileName, WriteTo)` record: a text to write, given as a writer, so the caller can write from an
  immutable snapshot without copying it to a string. Core stays free of AvaloniaEdit.
- `SessionFile.Write(path, state, texts)` works in this order:
  1. Each text not already on the disk is written beside its final name (UTF-8 without a mark, strict), flushed to
     the disk and moved over its name. A name stands for one text, so a file already there is not written again.
  2. The session file is written the same way.
  3. Only then are the files of the folder the new session no longer names deleted, along with any stray temporary
     file. Whichever session the disk holds, the texts it names are there and whole.
- `Write(path, state)` stays as `Write(path, state, [])`.
- `SessionFile.Read` accepts versions 1 and 2:
  - It reads each named text back into `UnsavedText` (clearing `UnsavedTextFile`), so restoring is unchanged.
  - Version 1 inline text still reads.
  - A side file that has gone is listed in the new `SessionLoadResult.MissingTexts`. Its buffer comes back without
    the text, and the session is still used. `SessionStore.Load` logs a warning naming them.
- Validation: a name must match `^[A-Za-z0-9_-]{1,96}\.txt$`, never a path. A document cannot have both inline text
  and a file, and a hex document has neither. A session breaking these rules is set aside as any invalid one is.
- Setting a session aside also moves its `unsaved` folder to `unsaved.invalid`, so the next write cannot delete the
  texts the set-aside file names.
- An editor that reads only version 1 sets a version 2 file aside rather than restoring it without its text.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Sessions/SessionText.cs`
- Modified: `src/Enigma.TextEditor.Core/Sessions/SessionFile.cs`, `Sessions/SessionDocument.cs`, `Sessions/SessionState.cs`,
  `Sessions/SessionLoadResult.cs`, `Settings/AppPaths.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Services/SessionStore.cs` (logs missing texts)
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Sessions/SessionTextFilesTests.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Sessions/SessionFileTests.cs` (the "future version" case is now
  version 3, since 2 is read)

## Deviations & follow-ups

- In this phase the Desktop still writes each text inline (it does not name side files yet), into a version 2 file.
  That is valid: version 2 reads inline text too. PHASE19 switches the autosave and the close to side files, and
  updates the README and SECURITY.md lines about `session.json` holding the text.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| A text whose name exists | Not written again | Names are per content version (PHASE19), and a rename-into-place means an existing file is whole |
| Encoding of side texts | UTF-8 without a mark, throwing on a lone surrogate | The same failure as the JSON serializer had, which `SessionStore` already handles |
| Missing side file | Listed in `MissingTexts` and logged, session still used | One lost text must not cost the whole session |
| Quarantine | Moves the `unsaved` folder with the session file | Otherwise the next write would delete the texts the set-aside file names |
| Version bump | 2, with version 1 still read | An older editor sets a version 2 file aside (texts kept) rather than restoring it without them |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3427 tests, 3425 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (14) in `SessionTextFilesTests`:
  - a round trip, with the text not in `session.json`;
  - version 1 inline text still read;
  - texts no longer named are deleted after the session;
  - an existing text is not rewritten;
  - a crash between the text and the session leaves the old session whole, and the stray file is cleaned up later;
  - a missing text is named and its buffer comes back without it;
  - six hostile names (paths, another extension, an empty name) set the session aside, with its texts moved aside;
  - a bad name on write is refused with nothing written;
  - an unencodable text fails the write and leaves the previous session and texts.
- Documentation sweep (§7): what the editor writes does not change yet (see Deviations), so nothing was edited.

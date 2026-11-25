# CODE-REVIEW-0734-PHASE06 — [Medium] EXC-01 A document is saved without overwriting the file in place

## Summary

`DocumentFile.WriteAsync` used to call `File.WriteAllBytesAsync`, which truncates the target before writing. A full
disk, a dropped network share or a crash part-way through left the user's file empty or cut short. Saves are now
atomic, the way the session file already was.

- New public `DocumentFile.WriteBytesAsync(path, bytes, token)`, which `WriteAsync` now calls after `Encode`. It works
  in this order:
  1. It follows a symbolic link to its final target, so the link stays a link.
  2. If the file exists, it opens it for writing without truncating, then closes it. Whatever would have refused a
     write in place (the read-only attribute, permissions, a program holding the file) is thrown here, before
     anything is written. Failures are therefore the same as before.
  3. It writes the bytes to `.<name>.<guid>.tmp` in the same folder and flushes them to the disk.
  4. It copies the target's metadata to the temporary file: on Windows, the creation time and the attributes without
     ReadOnly; on Linux and macOS, the Unix file mode. This is best effort.
  5. It moves the temporary file over the target (`File.Move(..., overwrite: true)`).
  6. It deletes the temporary file on any failure.
- A failure while the temporary file is written is thrown, and the original stays byte for byte. In two cases the
  bytes are written in place as before, so a file that could be saved before still can:
  - the folder refuses a new file (`UnauthorizedAccessException` on creating the temporary file);
  - the move is refused while the file can be written (for example, held by a program that shares writing but not
    deleting).
- The in-place fallback opens without truncating, writes, then sets the length. This means it can also write a
  hidden file, which `FileMode.Create` refuses on Windows.
- Core now gives `InternalsVisibleTo` to `Enigma.TextEditor.Core.UnitTests`, for the internal overload that takes the
  write step, so a test can fail half-way through it.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Documents/DocumentFile.cs`, `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Documents/DocumentFileAtomicWriteTests.cs`

## Deviations & follow-ups

- The plan said `File.Replace` for an existing target; the build uses `File.Move(overwrite: true)` with the metadata
  copied by hand. Without a backup file, `ReplaceFile` has documented failure modes that leave the replaced file
  deleted and the data only in the replacement, and the cleanup would then delete it. A rename either happens or not.
  The trade-off is that explicit ACLs and hard links of the original are not carried over (alternate data streams
  neither), which is acceptable for an editor's targets.
- 2 tests skip on this machine: Unix permissions (Windows) and the symbolic link (this account cannot create links
  without developer mode). They run on Linux, or on a Windows account with the right.
- Not verified on a network share or a real full disk. The half-way failure is simulated through the write step.
- Fix cycles used: 0. One test failed during development, before the first full run: the fallback opened with
  `FileShare.None` where `WriteAllBytes` shares `Read`. It now matches.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Replace primitive | `File.Move(overwrite: true)` + metadata copy | All-or-nothing. `ReplaceFile` without a backup can lose the original on failure |
| Keep failure behaviour | Probe-open for write before anything is written | A read-only or locked file fails exactly as before, with the same exception |
| Temporary name | `.<name>.<guid>.tmp` beside the file | Same volume, so the move is a rename. Hidden on Unix, and unique |
| Encoding | Stays synchronous in `WriteAsync` | PHASE20 moves it off the UI thread through `WriteBytesAsync` |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3389 tests, 3387 passed, 2 skipped (see above), 0 failed.
- Tests added (9) in `DocumentFileAtomicWriteTests`:
  - a write failing half-way leaves the file byte for byte and no temporary file;
  - a write replaces the bytes and leaves no temporary file;
  - a new file is created;
  - a read-only file is refused as before and left alone;
  - a file held for writing by another program is still written;
  - a hidden file stays hidden and keeps its creation time (Windows);
  - permissions are kept (Linux/macOS, skipped here);
  - a symbolic link stays a link (skipped here);
  - a cancelled write leaves the file.
- Existing `DocumentFileTests` and `EncodingRoundTripTests` all pass unchanged.
- Documentation sweep (§7): no document says how a file is written, so nothing was edited.

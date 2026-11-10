# FEATURE-34A1-PHASE01 — Session file and its lock

## Summary

This phase adds the Core half of session restore: the session as plain data, its file and the lock that decides
which running instance owns it. Nothing in the app uses it yet; PHASE02 captures and restores the workspace and
PHASE03 wires it in.

- `SessionState` / `SessionPane` / `SessionTab` / `SessionDocument` hold the session. Buffers are listed once and
  tabs point at them by index, so a buffer shown in two panes comes back as one.
- `SessionFile.Write` writes JSON (Web defaults, enums by name, nulls left out) to a temp file, flushes it to disk
  and moves it over the old one.
- `SessionFile.Read` returns a `SessionLoadResult`: the state, nothing, or a failure. It checks everything a restore
  will index into:
  - the version;
  - the layout's shape, axes, ratios and pane ids;
  - that the panes are exactly the layout's leaves, and that the active pane is one of them;
  - active tabs, tab buffer indexes, carets and scroll offsets;
  - each buffer: name, full path, hex rules, code page, indentation and line endings.
- A file that is not a usable session is moved aside to `session.invalid.json`, replacing an older one. A file that
  cannot be read (I/O) is left in place.
- `SessionLock.TryAcquire` holds `session.lock` open with no sharing for the owner's life. It returns `null` when
  another process holds the lock, or when the file cannot be created.
- `AppPaths` names the three files.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Sessions/SessionState.cs`, `SessionPane.cs`, `SessionTab.cs`,
  `SessionDocument.cs`, `SessionLoadResult.cs`, `SessionFile.cs`, `SessionLock.cs`
- Modified: `src/Enigma.TextEditor.Core/Settings/AppPaths.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Sessions/SessionFileTests.cs`, `SessionLockTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-34A1.md`

## Deviations & follow-ups

- Validation goes slightly beyond the plan's list: it also rejects negative carets and scroll offsets, a hex row
  below -1, an undefined split axis or a non-finite ratio, and pane ids below 0 or at `int.MaxValue`. The tree
  numbers its next pane one past the highest, so `int.MaxValue` would overflow.
- The lock file is not deleted when it is released. Keeping it avoids a delete-versus-open race between two
  starting instances, and the file is empty.
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Error type for a bad session | `InvalidDataException` (deserialization errors stay `JsonException`) | A BCL type that says "the data is wrong"; callers only log it |
| A file that cannot be read (I/O) | Reported, not moved aside | Not the content's fault, and possibly transient (an antivirus scan) |
| Durability | `Flush(flushToDisk: true)` before the move | The file can hold the only copy of unsaved text |
| Defaults on `SessionTab` / `SessionDocument` | Optional parameters with the editor's defaults | Short test data and call sites; the JSON still holds every value |

## Build/test evidence

- Baseline: see `docs/done/BUG-C6A9.md` (0 warnings; 3199 tests, 1 pre-existing failure).
- `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3236 tests, 3235 passed, 1 failed (the pre-existing failure).
- Tests added: 33 in `SessionFileTests` (round trip; replace with no temp file left; exact unsaved text; no file; 8
  malformed files and 16 invalid sessions moved aside; the valid base; an older moved-aside file replaced) and
  `SessionLockTests` (first claim, second refused, claim after release).
- Docs sweep: nothing user-facing changed in this phase, so no doc is stale.

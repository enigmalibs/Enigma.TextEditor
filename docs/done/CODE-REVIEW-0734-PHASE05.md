# CODE-REVIEW-0734-PHASE05 — [Medium] LOG-02 An exception before the safety net is attached reaches a file

## Summary

An exception reported before the reporter was attached used to go to `Trace` only, and only as its type and message.
That covers failures while the settings are read or the host is built (`App.axaml.cs:45-56`). A windowed app has no
trace listener, and the process ends before `Attach` drains the queue, so nothing useful survived. Now such an
exception is also written in full, stack included, to `logs/early-errors.log` beside `app.log`.

- Core `AppPaths`: `EarlyErrorsFileName` (`early-errors.log`) and `EarlyErrorsFile`, in the logs folder.
- `UnhandledExceptionHooks.Install(string earlyErrorsFile)`: `Program.Main` passes `AppPaths.EarlyErrorsFile`. The
  internal `KeepEarlyErrorsIn` sets the path without hooking the process events, so tests can use it, and
  `ResetForTests` forgets it.
- `Report`, with no reporter attached, works as follows:
  - Each entry gets a UTC time stamp, the source and `exception.ToString()`, and is appended under the gate, so two
    early reports never interleave.
  - The folder is created if needed.
  - A file grown past 1 MiB (`EarlyFileLimit`) is started again rather than appended to.
  - Any `IOException`, `UnauthorizedAccessException`, `NotSupportedException` or `ArgumentException` is swallowed,
    since this runs inside the handlers of last resort.
  - The `Trace` line and the early queue for the dialog are unchanged.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Settings/AppPaths.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Services/UnhandledExceptionHooks.cs`, `src/Enigma.TextEditor.Desktop/Program.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Services/UnhandledExceptionHooksTests.cs`
- Modified: `README.md` (the files list)

## Deviations & follow-ups

- No deviation from the plan.
- `Install` itself is not exercised by a test: it hooks the process-wide events, which the test host must not have
  hooked (see the class remarks of `UnhandledExceptionHooksTests`). `KeepEarlyErrorsIn`, which `Install` calls first,
  is tested.
- Not verified on a real failed start (for example, an unreadable settings folder followed by a host that fails to
  build).
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Write inside or outside the gate | Inside | Early reports are rare, and this keeps two of them from interleaving in the file |
| Size cap | Start again past 1 MiB | A start that fails every time must not grow the file without bound, and the newest failure is the one wanted |
| Test entry point | Internal `KeepEarlyErrorsIn` | The tests must not hook `AppDomain.UnhandledException` |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3380 tests, 3380 passed.
- Tests added (5) in `UnhandledExceptionHooksTests`:
  - an early report is written in full (source, type, message, stack) and still handed to the reporter;
  - two early reports are both kept, in order;
  - nothing is written once a reporter is attached;
  - an unwritable path (a file where the folder would be) does not throw, and the report is still kept;
  - a file past the limit is started again.
- Documentation sweep (§7): in `README.md`, the list of the editor's files now names `logs/early-errors.log`, written
  only when a failure came before the log was ready. `SECURITY.md` ("its log") is unchanged.

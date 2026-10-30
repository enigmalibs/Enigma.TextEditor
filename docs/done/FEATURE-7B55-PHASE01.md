# FEATURE-7B55-PHASE01 — Instance pipe and pointer file

**Item:** FEATURE-7B55 — Open files in the running instance
**Branch:** `feature/feature-7b55-phase01-instance-pipe`
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Summary

This phase builds the transport a launch uses to hand files to a running instance, in Core with no Avalonia. It is
not wired into the application yet; PHASE02 does that.

- **`InstanceEndpoint`:** an instance's pipe name and process id. Each process has its own pipe,
  `Enigma.TextEditor.<pid>`.
- **`ActiveInstanceFile`:** the pointer to the instance used last, stored as JSON in `instance.json` in the
  application-data folder (`AppPaths.ActiveInstanceFile`).
  - It is written atomically: to a temporary file named after the process, then moved over the old one.
  - `TryRead` returns `null` for a missing or malformed file instead of throwing.
  - `Clear` deletes the file only while it still names that instance.
- **`InstanceProtocol`:** one request and one reply.
  - The request is a 4-byte little-endian length followed by UTF-8 JSON, `{"version":1,"files":[…]}`, so any path
    character survives, line breaks included.
  - The reply is one byte.
  - A wrong version, a length that is zero, negative or over 1 MiB, a cut-short stream, or anything that isn't this
    JSON shape is refused (`null`) rather than thrown.
- **`InstanceServer`:** `Start(pipeName, onRequest, onError, requestTimeout)`.
  - It runs a background loop on a current-user-only pipe.
  - The first pipe is created before `Start` returns, so a name that can't be served is known at once.
  - Each connection gets 5 s to send its request and take the answer.
  - The server keeps the pipe open until the client has read the reply.
  - A handler that throws, a stranger, or a silent client is reported through `onError`, and the server keeps
    going.
  - The next pipe is already listening while one is being served.
  - `DisposeAsync` stops it and can safely be called twice.
- **`InstanceClient.TryForwardAsync`:** connects (1 s by default), sends, and reads the answer (2 s by default).
  Every failure returns `false`, never an exception.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Instances/InstanceEndpoint.cs`, `ActiveInstanceFile.cs`,
  `InstanceProtocol.cs`, `InstanceServer.cs`, `InstanceClient.cs`
- Modified: `src/Enigma.TextEditor.Core/Settings/AppPaths.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Instances/InstanceProtocolTests.cs`, `ActiveInstanceFileTests.cs`,
  `InstanceServerTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-7B55.md`; created this file

## Deviations & follow-ups

- **One fix cycle.** On the first run, `AClientThatSaysNothing_IsDroppedAfterTheTimeout_AndTheServerCarriesOn`
  failed.
  - Cause: on Linux, the server disposed its only pipe after a connection timed out. That closed the shared
    listening socket and dropped a launch already waiting in the backlog.
  - Fix: the server now allows two pipe instances and creates the next one as soon as a connection is accepted, so
    one is always listening. A listening pipe that fails is recreated after 500 ms rather than in a tight loop.
- The tests ran on Linux only. Windows named pipes go through the same .NET API, but this was not run on Windows;
  PHASE02 is used there and covers it in practice.
- A silent current-user client holds the server for up to 5 s, while a real launch waits 2 s for its answer. In that
  case the launch falls back to opening a new instance. This is accepted, since only the same user can reach the
  pipe.
- Baseline: as recorded in `docs/done/FEATURE-9F56-PHASE01.md` (0 warnings, 2780 passed).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Reply sent, then what | The server waits for the client to close before disposing | Closing straight after the write could lose the reply with the pipe on some platforms |
| Pipe instances | 2: the one being served and the next one listening | Without a listening pipe, Linux drops a waiting connection (fix cycle 1) |
| Static or instance types | Protocol, pointer file and client are static, path- and name-parameterised; the server is an `IAsyncDisposable` instance | The client runs in `Program.Main` before any container exists, matching `SettingsLoader` and `ShutdownSequence`. The server owns a loop and has a lifetime |
| JSON | System.Text.Json with web defaults (camelCase) | Already in the framework; any path round-trips |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2848 passed, 0 failed, 0 skipped (+36 against 2812).
- The 36 instance tests passed 5 runs in a row after the fix.
- **Tests added:**
  - protocol: paths with spaces, accents, CJK, a line break and 4000 characters round-trip; both replies
    round-trip; no reply reads as a refusal; 6 malformed bodies, 4 out-of-bounds lengths and 2 truncations are
    refused; a request over the limit is not written;
  - pointer file: round-trip (the folder is created, no temporary file is left); the last writer wins; 6
    non-pointer contents and a missing file read as `null`; `Clear` removes its own pointer and leaves another's;
    the current process's endpoint is named after its id;
  - real pipes: files arrive and the client gets `true`; two requests in a row; no server gives `false` within
    the timeout; a refusing handler gives `false`; a throwing handler gives `false`, is reported, and the server
    then serves again; a stranger's bytes are refused and the server serves again; a silent client is dropped after
    the timeout while a queued real client is served; a disposed server takes nothing more.

# CODE-REVIEW-0734-PHASE01 — [High] TEST-01 The instance pipe stalls on a client that sends more than the server reads

## Summary

The suite was red at the start of the run: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
failed every time after 5 s with `IOException: Pipe is broken`. The review's guess was right. The server pipe was
created with no buffer sizes, so a write did not complete until the other end read it. The server read the 4-byte
length of the stranger's 18-byte request, refused it, and wrote its one-byte answer. Meanwhile the client was still
inside its own write. Each side waited for the other to read until the server's deadline dropped the connection. The
server's single one-byte read after the answer had the same flaw: any byte left over satisfied it, not the peer
closing.

- `InstanceServer.CreatePipe` now gives the pipe 4 KiB in and out buffers (`BufferSize`), so a short write completes
  without the peer reading it.
- `ServeAsync` drains after the answer: it reads, into one reused buffer, until the peer closes or the deadline
  passes. A client still writing a refused request can therefore finish its write and read the answer, however much
  it sent.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Instances/InstanceServer.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Instances/InstanceServerTests.cs`

## Deviations & follow-ups

- No deviation from the plan.
- Verified on Windows only (this machine). The Linux pipe is a Unix socket, where the buffer sizes set the socket
  buffers. The drain loop behaves the same there, but it was not run on Linux.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Buffer size | 4 KiB each way, one constant | Covers the one-byte answer and any short stranger write. A larger request is still read whole by the server |
| Drain shape | `do … while (read > 0)` into one reused buffer | No empty loop body, no allocation per read |

## Build/test evidence

- **Run baseline** (once for the run, before the first dev branch). It was taken on `review/solution-review @ 75b4a0b`,
  whose code is identical to the run branch's tip `9d7c873` (the planning commit adds docs only). SDK 10.0.404 via the
  repository's `global.json`:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3371 tests, 3370 passed, 1 failed:
    `Enigma.TextEditor.Core.UnitTests.Instances.InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3372 tests, all passed. The baseline failure is gone.
  - `InstanceServerTests` (9 tests) run three times in a row on its own: 9/9 each time.
- Test added: `AClientSendingFarMoreThanTheServerReads_IsRefused_AndTheServerCarriesOn`. A client sends a length the
  server refuses, followed by 256 KiB, which is far more than any pipe buffer. It gets its refusal, and the next
  client is served.
- Documentation sweep (§7): no document described the pipe's buffering. `SECURITY.md` names the pipe and stays
  correct, so nothing was edited.

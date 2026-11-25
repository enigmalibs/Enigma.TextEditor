# CODE-REVIEW-0734-PHASE16 — [Medium] SOLID-03 Dot-repeat and macros leave `VimEngine`

## Summary

Recording and replaying keys (`q`, `@`, `@@`) and remembering the last change (`.`) had about a dozen fields of state
and their logic spread through the engine. They now live in two internal classes:

- `VimMacros` holds the recording register and its keys, the replay depth, the shared "a replay failed" flag
  (`Failed`, set by anything that fails) and the last register run. It covers `Toggle`, `Stop` (writes the register),
  `Take` / `TakeTyped`, `IsTaking` (a recording is open and no replay is typing), and `Play`. `Play` runs the replay
  loop, with the nesting limit, through delegates for offering a key to the engine and typing a refused key into the
  buffer. The key helpers `TypedKeys` and `RegisterKeys` moved with it.
- `VimDotRepeat` holds the last change, the insert session's command and selection, the replaying flag and the insert
  recorder. It covers `Record` (a change just run), `CloseInsert` (the insert record closes into the last change and
  into an open recording) and `Replay`, which works out the command with the new count and register and keeps the
  replay from remembering itself.
- `VimRecordedChange` is now a namespace-level internal record.
- The engine keeps running things:
  - `RepeatTyped` and `TypeText` for count copies;
  - `TypeIntoBuffer` for keys a replay types;
  - `Execute`, `Reselect` and `LeaveInsert` inside a replay;
  - the status slot (`ClearStatus`).
  `StartRecording`, `StopRecording`, `PlayMacro`, `CloseInsertRecord` and `RepeatLastChange` are now a few lines each.
- `VimEngine` goes from 3,017 to 2,770 lines (3,709 at the start of the run). Its public API is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Vim/VimMacros.cs`, `Vim/VimDotRepeat.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`

## Deviations & follow-ups

- **`VimEngine` is still 2,770 lines, over the 1,000-line guide.** The three extractions the review names are done:
  command line and ex (PHASE14), visual mode (PHASE15), and repeat and macros (this phase). What remains is
  normal-mode dispatch, the motions, the operators, the edits and the puts. These share the engine's mode, caret,
  registers and undo grouping at every step, so splitting them would be a rewrite of the engine. As the plan said,
  that is a follow-up. If it is taken up, the natural next seam is the operators (`Yank`, `Delete`, `Change`, `Shift`,
  `ChangeCase`, `Join`) and the puts, which could move into a class over the buffer and the registers.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who runs the replay loop | `VimMacros.Play`, with the engine's `HandleKey` and `TypeIntoBuffer` passed in | The loop and its stop rules belong with the macros. What a key does stays the engine's |
| Who runs a `.` replay | The engine, through the action `VimDotRepeat.Replay` hands it | A replay is `Execute` plus insert typing, which is the engine's work |
| The "failed" flag | `VimMacros.Failed`, set by the engine's `Fail` and `StopMacro` | It exists only to stop replays |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3413 tests, 3411 passed, 2 skipped (PHASE06's), 0 failed.
- Guard: the Core Vim tests (2,231, including the macro and dot-repeat suites, all through the public API) pass
  unchanged.
- Documentation sweep (§7): nothing user-facing changed.

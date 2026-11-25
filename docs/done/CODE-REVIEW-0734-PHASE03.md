# CODE-REVIEW-0734-PHASE03 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 2)

## Summary

The rest of the review's TEST-02 list moves to `UiWait`, and the two waits on the wall clock are gone.

- `FontSizeStatusTests`, `StatusBarFocusTests`, `UnhandledErrorDialogTests` and `ZoomGestureTests`: each fixed-round
  `DrainAsync` call now waits on the condition asserted next, such as a list open, a size applied, focus back on the
  editor, an error dialog open or the log flushed. Where the test asserts an absence, it settles instead.
- `HexCopyMenuTests`: a copy reaches the clipboard after the click or the key returns. Each clipboard assertion now
  goes through `AssertClipboardAsync`, which reads the clipboard until it holds the expected text (deadline 10 s) and
  then asserts. The over-the-limit case first waits for the refusal on the status line.
- New `UiWait.ValueAsync(read, expected)`: it polls a value that can only be read asynchronously and returns the last
  value read, so the assertion still reports the real difference.
- `HexViewTests`: the inline five-round loop became a wait for the tab to close. `VimEditingTests`: its own `WaitFor`
  gave up silently after 200 tries; it is replaced by `UiWait.UntilAsync`, which fails when the deadline passes.
- `VimPointerViewTests`: the 600 ms sleep standing in for the double-click time is gone. The single click lands eight
  characters away from the double-click, outside the rectangle a second click must hit, so the platform counts it as
  a fresh single click however soon it comes. If it were counted as part of a double-click, the test would fail on
  the mode or the selection.
- `SessionAutosaveTests.TheClosesWrite_WaitsForABackgroundWriteAlreadyWriting_AndHasTheLastWord`: the 100 ms delay
  before the release is replaced by `SpinWait.SpinUntil` on the closing thread entering `WaitSleepJoin`, meaning the
  close's write is seen blocked on the gate. The test now also asserts that this was seen.

## Files/modules touched

- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/UiWait.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/FontSizeStatusTests.cs`, `Views/HexCopyMenuTests.cs`,
  `Views/HexViewTests.cs`, `Views/StatusBarFocusTests.cs`, `Views/UnhandledErrorDialogTests.cs`,
  `Views/VimEditingTests.cs`, `Views/VimPointerViewTests.cs`, `Views/ZoomGestureTests.cs`,
  `Services/SessionAutosaveTests.cs`

## Deviations & follow-ups

- `ZoomGestureTests` had a fixed pump to "let each settings write finish". Its tests assert the in-memory settings,
  never the file. A write still in flight when a test ends targets that test's own temporary folder and is caught by
  `SettingsWriter`, which reports it to that harness's fake message service. So no wait is needed for it.
- The four `Task.Yield`-based helpers outside the review's list remain for PHASE21, as planned.
- Fix cycles used: 0. The first full run was green.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Waiting for the clipboard | `UiWait.ValueAsync`, returning the last value read | Clipboard reads are asynchronous, and the assertion keeps its exact diff |
| Double-click | A click outside the double-click rectangle | Deterministic. The rectangle is what the platform checks along with the time |
| Autosave ordering signal | The closing thread seen in `WaitSleepJoin` | The gate is a `Lock` with no waiter count. Its blocked caller is the observable signal |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3372 tests, 3372 passed.
- The part-2 classes on their own (78 tests), three runs in a row: 78/78 each time.
- `grep` finds no `Task.Delay` and no `Thread.Sleep` left in the Desktop test project, except the fixture's own loop
  (`HeadlessAvaloniaFixture.cs:88`).
- Documentation sweep (§7): only tests changed, so no user-facing document is affected.

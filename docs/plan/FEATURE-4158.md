# FEATURE-4158 — Save the session every 5 seconds

**Status:** TODO
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-4158-phase01-session-writer`
**Branch (PHASE02):** `feature/feature-4158-phase02-autosave-wiring`
**Run:** feature/2026-11-16-session-explorer-autosave

## Objective

Today the session — tabs, splits, explorer (`FEATURE-768C`) and every unsaved change — is written once, as the window
closes (`FEATURE-34A1`). A crash, a kill or a power cut loses everything since the last close. Keep the session file
up to date while the editor runs, so that a crash loses at most the changes of the last few seconds:

- every 5 seconds, the session is captured and, when it differs from the last one written, written again;
- the next start after a crash restores that file exactly as it restores a closed session;
- closing works as today and has the last word.

## Context & constraints

- `ISessionStore` / `SessionStore` (Desktop): `Load()`, `Save(state)` (synchronous, logs each failure as an error),
  `IsOwner` (claims `session.lock` on first use). Only the owner writes. `SessionFile.Write` writes
  `session.json.<pid>.tmp`, flushes to disk and moves it over the file: two writes at once in one process would collide.
- `MainWindowViewModel.KeepSessionOnClose()` writes with `keepUnsaved: !DiscardChangesOnExit` (`:qa!`);
  `SaveSession()` writes without unsaved text, after the close questions. Both run on the UI thread from
  `MainWindow.OnClosing`, which closes the window synchronously once they return.
- Capturing reads the buffers, which live on the UI thread. AvaloniaEdit's `TextDocument.Text` caches its string until
  the next change, so capturing an unchanged buffer is cheap while the previous capture holds the string.
- `SessionState` and `SessionPane` hold `IReadOnlyList`s: record equality compares them by reference.
- `TimeProvider.System` is registered (`AddShell`); the tests' `ManualTimeProvider` has no timers yet.
- `MainWindowViewModel.StartAsync` restores the session; nothing may write it before that, or the previous session
  would be replaced before it is read.

## Decisions

- **Periodic check, write on change:** a timer every `SessionAutosave.Interval` (5 s) posts a tick to the UI thread;
  the tick captures, compares with the last state written, and writes only when it differs.
- **Same contents as the close:** tabs, splits, explorer, unsaved text — none after `:qa!`.
- **Write off the UI thread:** the capture is on the UI thread; serialization, the write and its flush to disk run on
  the thread pool. One background write at a time: a tick while one runs is skipped (the next one catches up).
- **The newest capture wins:** every capture is numbered on the UI thread; every write — background or the close's —
  goes through one gate and is dropped when a newer capture has been written. The close's write is synchronous, takes
  the next number, and so waits for a write in progress and outranks one not started yet.
- **Structural equality** on `SessionState` and `SessionPane` (lists compared item by item), so "unchanged" is a cheap
  comparison rather than a serialization.
- **Failures:** retried at the next tick. `SessionStore` logs the first failure of a run as an error, the following
  ones at Debug, and a success after failures at Information.
- **Lifecycle:** started at the end of a successful `StartAsync`, owner only; stopped when the window closes; disposed
  with the container.

## PHASE01 — Background session writer

**Status:** TODO

### Steps

1. Core `SessionState` / `SessionPane`: `Equals` / `GetHashCode` comparing the lists by sequence (records keep the
   rest). Core tests: equal captures are equal; a changed caret, ratio, text, tab order or explorer folder is not.
2. `SessionStore.Save`: the failure logging policy above (one error per run of failures; recovery logged).
3. Desktop `Services/SessionAutosave` (sealed, `IDisposable`), built from `ISessionStore`, `TimeProvider`,
   `ILogger<SessionAutosave>`:
   - `Interval` (5 s);
   - `Start(Func<SessionState> capture)` — UI thread; remembers the thread's `SynchronizationContext`; does nothing
     when the store is not the owner or when already started;
   - `SaveIfChangedAsync()` — UI thread: capture, compare, write in the background; completes when the write is done;
   - `SaveNow(SessionState state)` — UI thread, synchronous: the close's write, outranking every earlier capture;
   - `Stop()` / `Dispose()` — no tick runs or writes after it.
4. Test infrastructure: `ManualTimeProvider` gets timers (`CreateTimer`, fired by `Advance`); its remark is updated.
5. Unit tests with a recording fake `ISessionStore`.

### Acceptance criteria

- A tick with a changed session writes it; a second tick with nothing changed writes nothing.
- A failed write is retried at the next tick, without a change.
- Advancing the clock by `Interval` runs a tick (timer → UI thread → write).
- A store that is not the owner: `Start` starts nothing; no tick writes.
- After `Stop`, advancing the clock writes nothing.
- `SaveNow` while a background write is blocked in the store: it waits, then writes; the file ends with `SaveNow`'s
  state.
- `SessionStore`: two failing saves in a row log one error; a success after them logs once at Information.
- Zero warnings, whole suite green.

## PHASE02 — Autosave from start to close

**Status:** TODO

### Steps

1. DI: `SessionAutosave` registered as a singleton in `AddSession`; the composition-root test resolves it.
2. `MainWindowViewModel` takes it: `StartAsync` starts it once the start succeeded, with
   `() => CaptureSession(keepUnsaved: !_shutdown.DiscardChangesOnExit)`; `KeepSessionOnClose()` and `SaveSession()`
   write through `SaveNow`; a new `WindowClosed()` stops it.
3. `MainWindow.OnClosed` calls `WindowClosed()`.
4. `EditorHarness` builds the autosave over a `ManualTimeProvider` it exposes.
5. README: quitting paragraph and the `session.json` line — the session is also kept every few seconds while the editor
   runs, so a crash loses at most the last few seconds.

### Acceptance criteria

- Harness, end to end: open a file and change it, type into an untitled buffer, advance the clock by `Interval`, let the
  write finish; without closing, release the lock (the process "crashes"); the next start restores both, unsaved, with
  their text.
- No tick writes before `StartAsync` has restored the session (the previous session is read first).
- A non-owner instance never writes the session.
- `:qa!` then a tick: the session holds no unsaved text.
- After the window closed, advancing the clock writes nothing: the file is the close's.
- Closing works as before (`HotExitTests`, `SessionStartupTests` green).
- Zero warnings, whole suite green.

## Out of scope

- A setting for the interval, or to turn the autosave off — follow-up.
- Telling the user (status line) that the session cannot be written — follow-up; the log has it, and closing still
  asks the questions when the write fails.
- Backups of individual files, undo history, crash reporting.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| When to write | Check every 5 s, write only on change | The draft's model ("every x seconds"); bounded loss; no disk activity while idle | Debounce on every edit (VS Code: more wiring, a missed event is a missed save); write every N s unconditionally (needless writes and flushes) |
| Interval | 5 s, a constant | At most ~5 s of typing lost; at most 12 small writes a minute, only while something changes | A setting now (follow-up); 1–2 s; 30 s |
| Contents | Same as the close | One restore path; a crash restores what a close would have | Unsaved text only |
| Thread of the write | Background; capture on the UI thread | A flush to disk every few seconds on the UI thread would stutter typing | Everything on the UI thread |
| Ordering against the close | Numbered captures, one gate, newest wins | An older background write must never land after the close's | Waiting for the write with a timeout |
| Change detection | Structural equality on the records | O(1) per unchanged buffer thanks to the cached text | Serialize and compare bytes every tick |
| Failure logging | First failure an error, then Debug, recovery logged | A read-only folder must not write 720 errors an hour | Every failure an error |
| Start | After a successful `StartAsync`, owner only | The previous session must be read before it is replaced | At construction |
| Stop | When the window closes | No tick may replace the close's write | At application exit only |
| Phasing | Writer (Core + service, unit-tested) → wiring (DI, window, end-to-end) | Each phase reviewable alone | One phase |

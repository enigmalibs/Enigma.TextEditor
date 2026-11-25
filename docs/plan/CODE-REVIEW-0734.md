# CODE-REVIEW-0734 — Review 2026-11-19: high and medium fixes

**Status:** IN PROGRESS
**Type:** CODE-REVIEW
**Branch (PHASE01):** `review/code-review-0734-phase01-instance-pipe`
**Branch (PHASE02):** `review/code-review-0734-phase02-ui-waits-1`
**Branch (PHASE03):** `review/code-review-0734-phase03-ui-waits-2`
**Branch (PHASE04):** `review/code-review-0734-phase04-explorer-logging`
**Branch (PHASE05):** `review/code-review-0734-phase05-early-errors-file`
**Branch (PHASE06):** `review/code-review-0734-phase06-atomic-save`
**Branch (PHASE07):** `review/code-review-0734-phase07-hex-copy-model`
**Branch (PHASE08):** `review/code-review-0734-phase08-menu-choices`
**Branch (PHASE09):** `review/code-review-0734-phase09-command-handlers-di`
**Branch (PHASE10):** `review/code-review-0734-phase10-editor-lifecycle`
**Branch (PHASE11):** `review/code-review-0734-phase11-workspace-session`
**Branch (PHASE12):** `review/code-review-0734-phase12-file-intake`
**Branch (PHASE13):** `review/code-review-0734-phase13-pane-view-helpers`
**Branch (PHASE14):** `review/code-review-0734-phase14-vim-command-line`
**Branch (PHASE15):** `review/code-review-0734-phase15-vim-visual-mode`
**Branch (PHASE16):** `review/code-review-0734-phase16-vim-repeat-macros`
**Branch (PHASE17):** `review/code-review-0734-phase17-key-tables`
**Branch (PHASE18):** `review/code-review-0734-phase18-session-side-files`
**Branch (PHASE19):** `review/code-review-0734-phase19-changed-text-capture`
**Branch (PHASE20):** `review/code-review-0734-phase20-io-off-ui-thread`
**Branch (PHASE21):** `review/code-review-0734-phase21-ui-waits-3`
**Run:** review/2026-11-19-high-medium-review-fixes

## Objective

Fix every High and Medium finding of the solution review `docs/review/2026-11-19-solution.md` (1 High, 11 Medium,
reviewed at `main @ f158edd`, identical in code to the run's starting point `review/solution-review @ 75b4a0b`).
Behaviour seen by the user stays the same except where a finding is about it (a save that can no longer truncate a
file, a window that no longer stalls on I/O). Each finding becomes one or more phases, severity first.

## Context & constraints

- Baseline (SDK 10.0.404, the repository's `global.json`): `dotnet build Enigma.TextEditor.slnx` 0 warnings;
  `dotnet test --solution Enigma.TextEditor.slnx` 3371 tests, 1 failing —
  `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` (TEST-01 itself).
- Warnings are errors (`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`); explicit usings; C# 14 (`field`,
  `extension` blocks); CPM. No new package in this item.
- Desktop tests build the graph through `tests/…Desktop.UnitTests/Infrastructure/EditorHarness.cs`; constructor
  changes land there. `InternalsVisibleTo` exists for the Desktop tests only.
- House style: XML docs on every member, comments that explain why, `catch … when` filters naming the types handled,
  `ILogger<T>` with constant templates and the exception first.
- Static I/O classes in Core are the documented house divergence (review SOLID-04, Low, out of scope): new Core I/O
  follows the same static shape.

## Decisions

- One item, 20 phases, ordered High first; within Medium, the test waits first (a steadier suite for every later dev),
  then the small fixes, then the DI and single-responsibility refactors, then the performance fixes, which land in the
  classes the refactors create.
- Every phase keeps the whole suite green against the baseline, adds the tests its acceptance criteria name, and
  changes no user-visible behaviour unless the finding is about it.
- Lows (SOLID-04, EXC-02, EXC-03, ASYNC-01, ASYNC-02, MVVM-01, STYLE-01, PERF-03) are out of scope, as are the 5–7
  parameter classes SOLID-01 lists beyond the two it names.

## PHASE01 — [High] TEST-01 The instance pipe stalls on a client that sends more than the server reads

**Status:** DONE

Locations: `tests/Enigma.TextEditor.Core.UnitTests/Instances/InstanceServerTests.cs:87-104`,
`src/Enigma.TextEditor.Core/Instances/InstanceServer.cs:100`, `:158-165`, `InstanceProtocol.cs:87-93`.

### Steps

1. `InstanceServer.CreatePipe`: the constructor overload with `inBufferSize` / `outBufferSize` (a named constant,
   4 KiB each), so a short write completes without the peer reading it.
2. `ServeAsync`: after the reply, a drain loop reads (into one reused buffer) until the peer closes (`0` bytes) or the
   deadline passes, replacing the single one-byte read; the remarks say why.
3. Rerun the class on Windows; if it still fails, instrument before changing more (the review's inference is wrong).
4. A regression test: a client that writes more than the server reads — larger than the pipe buffer — is refused, and
   the server serves the next client.

### Acceptance criteria

- `InstanceServerTests` passes, including the baseline failure, three runs in a row.
- The new oversized-stranger test passes; every other instance test unchanged and green.
- Zero warnings; whole suite green (the baseline failure gone).

## PHASE02 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 1)

**Status:** DONE

Locations (part 1): `ViewModels/MoveTabTests.cs:238`, `ViewModels/WindowCommandTests.cs:490`,
`Views/AboutDialogTests.cs:54`, `Views/DialogKeyboardTests.cs:157`, `Views/EditorContextMenuTests.cs:207`,
`Views/FileExplorerViewTests.cs:566`, `Views/FirstRunDialogTests.cs:156`; added at build time:
`Views/FileDropViewTests.cs` (its four-round `DrainAsync` made the long-known flake
`ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` fail this phase's suite).

### Steps

1. `tests/…Desktop.UnitTests/Infrastructure/UiWait.cs` (internal static): `UntilAsync(Func<bool> condition,
   string? because = null)` pumps the dispatcher (`RunJobs`, then yield) until the condition holds, failing with the
   reason after a deadline (10 s, as `FileExplorerViewTests.UntilAsync`); and one bounded "settle" for a call site that
   has nothing observable to wait for, built on the dispatcher rather than a fixed count of `Task.Delay` rounds.
2. Each fixed-round helper in the part-1 files (`Pump`, `PumpAsync`, `DrainAsync`, `SettleAsync`,
   `FileExplorerViewTests.UntilAsync`) is removed; every call site waits on the condition the test asserts next (or on
   the action's completion), or settles where it asserts that nothing happened.

### Acceptance criteria

- No `Task.Delay` and no private pump helper left in the part-1 files; `FileExplorerViewTests.UntilAsync` lives on as
  `UiWait.UntilAsync`.
- No assertion weakened: every assertion of the part-1 files is still made, after its wait.
- Zero warnings; whole suite green.

## PHASE03 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 2)

**Status:** DONE

Locations (part 2): `Views/FontSizeStatusTests.cs:141`, `Views/HexCopyMenuTests.cs:243`, `Views/HexViewTests.cs:149`,
`Views/StatusBarFocusTests.cs:132`, `Views/UnhandledErrorDialogTests.cs:152`, `Views/VimEditingTests.cs:335`,
`Views/ZoomGestureTests.cs:259`, `Views/VimPointerViewTests.cs:134`, `Services/SessionAutosaveTests.cs:166`.

### Steps

1. The part-2 files move to `UiWait`, as in PHASE02.
2. `VimPointerViewTests`: the 600 ms sleep standing in for the double-click time is replaced by a click the platform
   cannot count as the second of a double-click (outside the double-click rectangle, or with a timestamp past the
   double-click time) — no wall-clock wait.
3. `SessionAutosaveTests.TheClosesWrite_WaitsForABackgroundWriteAlreadyWriting_AndHasTheLastWord`: the 100 ms delay
   before the release becomes an awaited signal that the close's write is blocked (the writing thread seen waiting),
   then the release.

### Acceptance criteria

- No `Task.Delay` / `Thread.Sleep` left in the Desktop test project outside `Infrastructure/HeadlessAvaloniaFixture.cs`
  (its own dispatcher loop), and no private pump helper in the part-2 files.
- The double-click test still proves a single click after a double-click returns to normal mode.
- The autosave ordering test still proves the close's write waits for the running background write.
- Zero warnings; whole suite green.

## PHASE04 — [Medium] LOG-01 File-operation failures in the explorer are logged

**Status:** DONE

Locations: `src/Enigma.TextEditor.Desktop/ViewModels/FileExplorerViewModel.cs:392-396`, `:436-439`.

### Steps

1. The trash catch block logs at `Error`, exception first, `{Path}` placeholder, before the message is shown.
2. `RunAsync` takes the path the operation acts on (created name, renamed or deleted entry) and logs the
   `FileOperationException` the same way.
3. Tests with a recording logger (`Infrastructure/RecordingLogger.cs`): a failed move to the trash and a failed
   create/rename/delete each log one error with the exception and the path.

### Acceptance criteria

- Both failure paths log one `Error` entry carrying the exception and the path, then show the same message as today.
- Zero warnings; whole suite green.

## PHASE05 — [Medium] LOG-02 An exception before the safety net is attached reaches a file

**Status:** DONE

Locations: `src/Enigma.TextEditor.Desktop/Services/UnhandledExceptionHooks.cs:142-146` (`App.axaml.cs:45-56`).

### Steps

1. Core `AppPaths`: `EarlyErrorsFileName` (`early-errors.log`) and `EarlyErrorsFile`, in the logs folder beside
   `app.log`.
2. `UnhandledExceptionHooks.Install(string earlyErrorsFile)`: remembers the path; `Program.Main` passes
   `AppPaths.EarlyErrorsFile`.
3. When no reporter is attached, `Report` appends the full exception (`ToString()`), its source and a UTC time stamp to
   that file — folder created, a file grown past 1 MiB started again — guarded so the write cannot throw
   (`IOException`, `UnauthorizedAccessException`, `NotSupportedException`, `ArgumentException`); `Trace` and the early
   queue stay.
4. Tests (temporary folder; `ResetForTests` forgets the path): an early report writes the stack; a reporter attached
   gets it and nothing more is written; an unwritable path does not throw.

### Acceptance criteria

- An exception reported before `Attach` is in `early-errors.log` with its stack, and still handed to the reporter.
- A failing write is swallowed, never thrown out of the handler.
- Zero warnings; whole suite green.

## PHASE06 — [Medium] EXC-01 A document is saved without overwriting the file in place

**Status:** DONE

Locations: `src/Enigma.TextEditor.Core/Documents/DocumentFile.cs:172`.

### Steps

1. `DocumentFile.WriteAsync` writes the bytes to a temporary file in the target's own folder, flushes it to disk, then
   replaces the target: `File.Replace(temporary, target, null, ignoreMetadataErrors: true)` when it exists (keeps its
   attributes and ACL on Windows), `File.Move` when it does not. A symbolic link is followed to its final target, so the
   link stays a link. On Linux/macOS the target's Unix file mode is copied to the temporary file first.
2. The temporary file is deleted on any failure. A failure while the temporary file is written (disk full) is thrown:
   the original is untouched. When the folder refuses the temporary file, or the replace step is refused (access or a
   sharing violation), the bytes are written in place as before, so a file that could be saved yesterday still can.
3. Core gets `InternalsVisibleTo` for `Enigma.TextEditor.Core.UnitTests`, for an internal overload taking the write
   step, so a test can fail half-way through it.
4. Tests: a failed write leaves the original bytes intact and no temporary file; a save keeps a read-only attribute's
   absence/presence as today, a symbolic link (where the platform allows creating one) and a hidden attribute on
   Windows; a new file is created; an in-place fallback still writes.

### Acceptance criteria

- A write that fails half-way leaves the original file byte for byte, and no temporary file beside it.
- Every existing `DocumentFile` and save test green; the new tests green.
- Zero warnings; whole suite green.

## PHASE07 — [Medium] XAML-02 The hex view's copy moves into the tab's view-model

**Status:** DONE

Locations: `src/Enigma.TextEditor.Desktop/Views/HexView.axaml.cs:281-309` (and PERF-02's `:299`).

### Steps

1. `EditorTabViewModel.CopyHexSelectionAsync(HexColumn format)` returns a `HexCopyResult` (text, or a status
   message): the size limit, the read — on the thread pool, `HexDocument.Read` being `RandomAccess`-based — and the
   formatting; a read failure or a document closed meanwhile (`ObjectDisposedException`) is a message.
2. `HexView.CopyBytes` keeps only the call, the clipboard and the status line; the focus hand-back is unchanged.
3. View-model tests for the limit, both formats, a failed read and a closed document; `HexCopyMenuTests` stay green.

### Acceptance criteria

- No file read or formatting left in `HexView.axaml.cs`; the read does not run on the UI thread.
- Same clipboard text and same status messages as before.
- Zero warnings; whole suite green.

## PHASE08 — [Medium] XAML-01 Radio menu choices are lists of choice items

**Status:** DONE

Locations: `MainWindow.axaml:218-268`, `:335-340`, `:375-378`; `SettingsMenuViewModel.cs:55-68`, `:190-255`;
`ToolsViewModel.cs:61-65`, `:129-149`.

### Steps

1. A `MenuChoice` item (Desktop view-model): header, `IsCurrent` (observable), the command that selects it, the radio
   group's name; built once per group, refreshed in place, as `FontSizeChoices` is.
2. `SettingsMenuViewModel`: `ThemeChoices`, `PaneResizeStepChoices`, `DefaultIndentationChoices`,
   `DefaultLineEndingChoices`; `ToolsViewModel`: `LineEndingChoices`, `IndentationChoices`. The per-value `Is…`
   properties and `…Command`s they replace are removed; the `Set…Async` methods stay.
3. `MainWindow.axaml`: one shared `MenuItem` container theme for choice items; the six menu groups and the two status
   bar flyouts bind to the lists (headers, access keys, order and radio marks unchanged).
4. Tests move to the choice items; a test that every group has exactly one current item for each setting value, and
   that a new value would need one list entry only.

### Acceptance criteria

- No hand-copied radio `MenuItem` left in `MainWindow.axaml`; the menus and flyouts look and act as before.
- Zero warnings; whole suite green.

## PHASE09 — [Medium] SOLID-02 The workspace no longer builds its command handlers

**Status:** DONE

Locations: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs:83-90`.

### Steps

1. `ExCommandHandler` and `WindowCommandHandler` drop the captured `WorkspaceViewModel`; their `ExecuteAsync` (and
   `QuitAsync` / `ClosePaneAsync`) take the workspace as a parameter.
2. Both registered as singletons in `AddShell`; `WorkspaceViewModel` takes them instead of `IAppShutdown`,
   `IHelpDialogService`, `LanguageCatalog` and `ILanguagePicker` (12 → 10 parameters).
3. `EditorHarness`, the composition-root test, and direct handler tests where useful.

### Acceptance criteria

- No `new ExCommandHandler` / `new WindowCommandHandler` in `src`; the container resolves both.
- Every ex and window command test green, unchanged in what it asserts.
- Zero warnings; whole suite green.

## PHASE10 — [Medium] SOLID-01 Start-up and session lifecycle leave `MainWindowViewModel`

**Status:** DONE

Locations: `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs:45`.

### Steps

1. Desktop `Services/EditorLifecycle` (singleton): the start sequence (first-run dialog, session restore, start-up
   files, explorer, autosave start), `CaptureSession`, `KeepSessionOnClose`, `SaveSession`, `WindowClosed`. It owns
   `FirstRunState`, `IFirstRunDialogService`, `ISessionStore`, `SessionAutosave` and `IAppShutdown`.
2. `MainWindowViewModel` takes it instead of those four (14 → 11 parameters: seven child view-models it exposes, four
   services) and delegates; `IsStarting` follows the lifecycle.
3. DI, harness, composition-root test; lifecycle tests move from the window's where they test the sequence.

### Acceptance criteria

- `MainWindowViewModel` holds no first-run, session-store or autosave field.
- Start-up, hot-exit, crash-recovery and session tests green, unchanged in what they assert.
- Zero warnings; whole suite green.

## PHASE11 — [Medium] SOLID-01 / SOLID-03 Session capture and restore leave `WorkspaceViewModel`

**Status:** DONE

Locations: `WorkspaceViewModel.cs:977-1114`, `:1273-1331`.

### Steps

1. Desktop `Services/WorkspaceSession` (singleton; `IDocumentService`, `DocumentRegistry`, `StatusMessages`):
   `Capture(WorkspaceViewModel, bool keepUnsaved)` and `RestoreAsync(WorkspaceViewModel, SessionState)`.
2. The workspace keeps one internal operation for the rebuild — replace the panes with a layout and the restored tabs —
   so the pane bookkeeping (registry retains, pane events, the active pane) stays inside it.
3. `EditorLifecycle` uses `WorkspaceSession`; tests move from `Workspace.CaptureSession` / `RestoreSessionAsync`.

### Acceptance criteria

- No session capture or restore code left in `WorkspaceViewModel`.
- Session restore, hot-exit, explorer-session and crash-recovery tests green, unchanged in what they assert.
- Zero warnings; whole suite green.

## PHASE12 — [Medium] SOLID-01 / SOLID-03 File intake leaves `WorkspaceViewModel`

**Status:** TODO

Locations: `WorkspaceViewModel.cs:607-673`, `:904-945`, `:1116-1180`, `:1218-1256`.

### Steps

1. Desktop `Services/FileIntake` (singleton; `IDocumentService`, `IFilePrompts`, `StatusMessages`): open from the
   picker (text and hex), the files of a start-up or another launch, files dropped on a pane, files dropped on a pane's
   edge (split), and the "fresh tab" replacement they share.
2. The workspace takes `FileIntake` instead of `IFilePrompts` (10 → 10, with `IFilePrompts` gone); its `IPaneHost`
   drop members and the Open commands delegate; `EditorLifecycle` and `MainWindowViewModel.OpenForwardedAsync` call
   `FileIntake` directly.
3. Harness, DI, composition root; tests that called the workspace's intake methods move.

### Acceptance criteria

- `WorkspaceViewModel` is under 1 000 lines, holds no file-system probe and no intake logic.
- Drop, split-with-files, start-up, forwarded-files and open tests green, unchanged in what they assert.
- Zero warnings; whole suite green.

## PHASE13 — [Medium] SOLID-03 The pane view's code-behind is split into view helpers

**Status:** TODO

Locations: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs:53` (tab drag `:410-660`, context menu
`:661-693`, file drop `:694-760`).

### Steps

1. Tab strip drag and drop (press, move, steer, drop target, keys, capture loss) into a view helper beside
   `TabDragGesture`; the editor context menu and the file drop handling into their own helpers; `EditorPaneView`
   wires them and keeps the editor, minimap and view-state code.
2. Behaviour unchanged; the existing view tests (`TabDrag…`, `FileDrop…`, `EditorContextMenu…`) are the guard.

### Acceptance criteria

- `EditorPaneView.axaml.cs` under 700 lines; each helper has one reason to change.
- Zero warnings; whole suite green.

## PHASE14 — [Medium] SOLID-03 The Vim command line and ex commands leave `VimEngine`

**Status:** TODO

Locations: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs:2450-3090`.

### Steps

1. An internal command-line class owns the line being typed, what opened it and its incsearch state, and answers each
   key (continue, cancel, run); an internal ex-command class runs `:` lines (addresses, `:s`, `:set`, line jumps,
   requests out) beside `VimSubstitute` / `VimSet`. The engine keeps the mode, the caret and the events, and calls them.
2. Public API of `VimEngine` unchanged; the Core Vim tests are the guard.

### Acceptance criteria

- No command-line editing, ex parsing/running or search-target code left in `VimEngine`.
- Every Core Vim test green, unchanged; zero warnings; whole suite green.

## PHASE15 — [Medium] SOLID-03 Visual mode leaves `VimEngine`

**Status:** TODO

Locations: `VimEngine.cs:1494-2047`.

### Steps

1. An internal visual-mode class owns the anchor and the remembered selection (`gv`), computes the selected range and
   extent, swaps the ends and reselects; the operators over a selection call it. The engine keeps the edits.
2. Public API unchanged; Core visual-mode tests are the guard.

### Acceptance criteria

- Anchor, remembered selection and range computation live in the new class; every Core Vim test green, unchanged.
- Zero warnings; whole suite green.

## PHASE16 — [Medium] SOLID-03 Dot-repeat and macros leave `VimEngine`

**Status:** TODO

Locations: `VimEngine.cs:3237-3616`.

### Steps

1. An internal class owns the last change and the insert record (`.`), another — or the same, if cohesive — the
   recording register and keys, replay depth and last macro (`q`, `@`, `@@`). The engine feeds them keys and replays.
2. Public API unchanged. Record `VimEngine`'s final line count; a remainder over 1 000 lines is a follow-up.

### Acceptance criteria

- Repeat and macro state live outside `VimEngine`; every Core Vim test green, unchanged.
- Zero warnings; whole suite green.

## PHASE17 — [Medium] SOLID-03 The key tables leave `KeySequenceParser`

**Status:** TODO

Locations: `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs:99` (tables `:462-700`).

### Steps

1. The static key-to-command tables (motions, operators, actions, control keys, text objects, leader mappings) move
   to an internal static class; the parser keeps the grammar's state machine.

### Acceptance criteria

- `KeySequenceParser.cs` under 1 000 lines; every Core parser and engine test green, unchanged.
- Zero warnings; whole suite green.

## PHASE18 — [Medium] PERF-01 Unsaved text is kept in side files beside the session

**Status:** TODO

Locations: `src/Enigma.TextEditor.Core/Sessions/SessionFile.cs:57-62` (and the records).

### Steps

1. Format version 2: `SessionDocument.UnsavedTextFile` — a plain file name (validated: no folder, no `..`, a fixed
   pattern) in an `unsaved` folder beside the session file. `SessionFile.Read` accepts versions 1 and 2, reading a side
   file's text into `UnsavedText` (a missing side file is reported as not restored, never thrown); version 1 inline
   text still reads.
2. `SessionFile.Write(path, state, texts)`: each new side text is written (temporary file, flushed, moved) before the
   session file; files of the `unsaved` folder the new session no longer names are deleted after it.
3. A Core record for a side text to write (name and a writer delegate), so Core stays free of AvaloniaEdit.
4. Core tests: round trip, v1 compatibility, stale-file cleanup, a crash between the side file and the session (the
   old session still reads), a hostile name refused.

### Acceptance criteria

- `session.json` written by this phase holds no unsaved text; a v1 file still restores its unsaved text.
- Zero warnings; whole suite green.

## PHASE19 — [Medium] PERF-01 The autosave captures and writes only text that changed

**Status:** TODO

Locations: `SessionAutosave.cs:132-140`, `WorkspaceSession` capture (was `WorkspaceViewModel.cs:1002-1009`),
`DocumentViewModel.cs:359`.

### Steps

1. A document's unsaved text is named by a per-buffer id and a counter bumped when its `ITextSourceVersion` changed
   since the last capture; an unchanged buffer is captured as its name only — no `Document.Text`.
2. A changed buffer is captured as an AvaloniaEdit snapshot (`CreateSnapshot`, cheap, immutable) written to its side
   file on the thread pool; `ISessionStore.Save` / `SessionAutosave` carry the session with its side texts.
3. The close's write keeps unsaved text the same way; `SaveSession` (no unsaved text) leaves no side file.
4. Tests: a caret move rewrites `session.json` but no side file; a keystroke rewrites that buffer's side file only; no
   `Document.Text` is read for an unchanged buffer; crash recovery and hot exit unchanged.

### Acceptance criteria

- A tick with only a caret move writes no unsaved text; a tick after typing writes only the changed buffer's text.
- Crash-recovery, hot-exit and session tests green; zero warnings; whole suite green.

## PHASE20 — [Medium] PERF-02 File I/O and encoding leave the UI thread

**Status:** TODO

Locations: `MainWindowViewModel.cs:190` (now the lifecycle), `DocumentFile.cs:170`, `DocumentService.cs:153`, `:188`,
`WorkspaceViewModel.cs:617`, `:916-927`, `:1154` (now `FileIntake`), `App.axaml.cs:85`, `:177`.

### Steps

1. The session load, `DocumentFile.Encode` (the text and its version still taken on the UI thread first) and
   `HexDocument.Open` run through `Task.Run` and are awaited.
2. `IDocumentService.ProbeAsync(path)` (file, folder or missing, off the UI thread) replaces the `File.Exists` /
   `Directory.Exists` probes of `FileIntake`.
3. The start-up and forwarded `StartupFiles.Parse` (which probes the disk) run off the UI thread before the start.
4. Tests: the probe's three answers; a save still marks the version it encoded; existing open/save/start-up tests green.

### Acceptance criteria

- None of the listed call sites touches the disk or encodes on the UI thread.
- Zero warnings; whole suite green.

## PHASE21 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 3)

**Status:** TODO

Added at build time (PHASE02): the review listed the `Task.Delay`-based helpers ("at least four names"); four more
files pump a fixed four rounds of `Task.Yield` — `Views/HelpViewTests.cs`, `Views/SettingsPageTests.cs`,
`Views/TabDragViewTests.cs`, `Views/TabStripViewTests.cs` (about 55 call sites) — the shape that made
`FileDropViewTests` flaky.

### Steps

1. The four files move to `UiWait`, as in PHASE02; their private `DrainAsync` helpers are removed.

### Acceptance criteria

- No private pump helper left anywhere in the Desktop test project; no assertion weakened.
- Zero warnings; whole suite green.

## Out of scope

- The eight Low findings, and the 5–7 parameter classes of SOLID-01 beyond `MainWindowViewModel` and
  `WorkspaceViewModel` — follow-ups.
- Release notes and a version bump (the next release item).
- Line-ending normalisation.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which findings | 1 High + 11 Medium; Lows out | The request | Lows too (not asked) |
| Item shape | One `CODE-REVIEW` item, 20 phases | House convention; large findings split so each dev is ~30 min of review | One phase per finding (VimEngine and PERF-01 would be unreviewable) |
| Order within Medium | Test waits → LOG/EXC/XAML → DI/SRP → Vim → PERF | A steadier suite first; PERF lands in the classes the refactors create, not twice | The review's listing order |
| TEST-01 fix | Explicit buffers + drain until close | The review's inferred deadlock: each side blocked writing; a short write must not need the peer | Server reading the whole stranger request (unbounded) |
| TEST-02 for call sites with nothing to wait on | A bounded dispatcher settle, no fixed `Task.Delay` count | Negative assertions need a quiet dispatcher, not a condition | Converting every negative check into a condition (none exists) |
| Yield-based fixed-round helpers the review did not list | A PHASE21 for four files; `FileDropViewTests` pulled into PHASE02 | Same smell, and `FileDropViewTests` was a real flake blocking PHASE02; 55 more call sites would double PHASE03 | Leaving them (a known flake stays); folding into PHASE03 |
| Double-click wait | A click the platform cannot pair, no sleep | Deterministic | Lowering a global double-click time |
| LOG-02 fallback | `logs/early-errors.log`, full exception, capped 1 MiB, guarded | The review's fix; bounded growth; never throws from a handler | Event log (Windows only); unbounded append |
| EXC-01 strategy | Temp + flush + `File.Replace`/`Move`, links followed, Unix mode kept, in-place fallback only when refused | Never truncate the original; never lose the ability to save a file that saved before | Always atomic (breaks saves in non-writable folders / shared files); in place (the finding) |
| EXC-01 test seam | `InternalsVisibleTo` for Core tests + internal write-step overload | A half-way failure cannot be provoked through the public API; Desktop already uses `InternalsVisibleTo` | Filling a disk; a public test hook |
| XAML-02 shape | `EditorTabViewModel.CopyHexSelectionAsync` → result record | The review's fix; the VM already owns `HexSelection` | A new service |
| XAML-01 shape | `MenuChoice` lists; per-value members removed | The finding is the per-value members; keeping both keeps the redundancy | Keeping the bools beside the lists |
| SOLID-02 handler target | Workspace passed to `ExecuteAsync` | Removes the capture and the cycle; `IPaneHost` lacks ~12 needed members | Widening `IPaneHost`; a new workspace interface mirroring the class |
| SOLID-01 window VM | `EditorLifecycle` service | The review's recommendation; 4 services out | Leaving it (finding) |
| SOLID-01/03 workspace | `WorkspaceSession` + `FileIntake`, both acting on the workspace passed in | Size and responsibilities leave; the count drops (12 → 10) instead of rising | Services injected into the workspace for everything (count back to 12) |
| VimEngine split | Three collaborator classes with their own state | The responsibilities the review names; tests through the public API guard it | `partial` files (same class, same reasons to change); a full rewrite |
| VimEngine size target | Named responsibilities out; remainder over 1 000 lines a follow-up | Splitting normal-mode dispatch too is a rewrite beyond the review's fix | Forcing < 1 000 in this item |
| PERF-01 format | v2 with side files, v1 still read | An old session with unsaved text must survive the upgrade; an older editor quarantines a v2 file rather than dropping its text | Optional member on v1 (an older editor would restore without the text and the next start would clean the side files) |
| PERF-01 capture | Version counter per buffer + rope snapshot written off the UI thread | No `Document.Text` copy for unchanged buffers, no string copy at all for changed ones | Hashing the text (still copies it) |
| PERF-02 placement | `Task.Run` in Desktop (`DocumentService`, lifecycle), not in Core | No `Task.Run` inside a library | Async variants in Core |

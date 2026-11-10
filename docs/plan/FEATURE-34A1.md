# FEATURE-34A1 — Restore the previous session

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-34a1-phase01-session-file`
**Branch (PHASE02):** `feature/feature-34a1-phase02-capture-restore`
**Branch (PHASE03):** `feature/feature-34a1-phase03-restore-at-start`
**Branch (PHASE04):** `feature/feature-34a1-phase04-hot-exit`
**Run:** bugfix/2026-11-09-ctrl-y-session-restore

## Objective

Work like Notepad on Windows 11 and VS Code:

- Closing the application stores everything — new files, modified files, the open tabs, the split layout — and the
  next start restores it exactly as it was, unsaved changes included.
- Even with every change saved, the next start reopens the same tabs in the same splits.
- Closing a **tab** still asks whether to save its unsaved changes (unchanged).

## Context & constraints

- Layout: `LayoutTree` (Core) already has `ToSnapshot()` / `FromSnapshot()` / `Restore()` over `LayoutSnapshot`
  (`Pane`, `Axis`, `Ratio`, `First`, `Second`), validated (malformed → `ArgumentException`), tested in
  `LayoutTreeTests`. `WorkspaceViewModel` maps each `PaneId` to an `EditorPaneViewModel` (`_byId`), builds one pane
  in its constructor, and batches layout changes (`Batch`).
- Buffers: `DocumentViewModel` (path or untitled name, `Format`, `Indentation`, chosen `Language`, `Hex`, `IsDirty`
  from the undo stack + `_formatChanged`). `DocumentRegistry` holds them; tabs `Retain`/`Release`. One buffer may be
  shown by tabs in several panes. Tabs (`EditorTabViewModel`) keep `CaretOffset`, scroll offsets and `HexRow`, kept
  up to date continuously by `EditorPaneView` and applied when a tab is shown (`RestoreViewState`, caret clamped).
- Files: `DocumentFile.ReadAsync(path, TextEncoding?)`, `HexDocument.Open`, `TextEncodings`, `IndentationDetector`.
  `DocumentService` reports every failure with a dialog — fine for user actions, wrong for a restore.
- Closing: `MainWindow.OnClosing` cancels, asks through `MainWindowViewModel.ConfirmCloseAsync` →
  `WorkspaceViewModel.ConfirmCloseAllAsync`, then closes again. `:qa` → `IAppShutdown.RequestExit()`, `:qa!` →
  `ExitDiscardingChanges()` (`DiscardChangesOnExit`). `App.OnExit` stops the host inside `ShutdownSequence`.
- Startup: `MainWindowViewModel.StartAsync` → first-run dialog → `Workspace.OpenStartupFilesAsync` (a fresh untitled
  tab gives its place to the first file; with no tab at all, a new untitled one).
- Several processes can run: a launch without files starts another instance (`Program.Main`, `RunningInstance`).
- App data: `AppPaths` (Core) names `settings.json`, `instance.json`, the logs; `ActiveInstanceFile` writes JSON with
  `JsonSerializerDefaults.Web`, temp file + `File.Move(overwrite)`. Unreadable settings are moved aside
  (`settings.invalid.json`).

## Decisions

- **One file, `session.json`,** beside `settings.json`, schema `version: 1`, unsaved text embedded. Written to a temp
  file and moved over the old one, so a failed write leaves the previous session whole.
- **What it holds:** the layout snapshot; per pane its id, tabs in strip order and active tab; the active pane; per
  tab its buffer (an index into the buffer list), caret offset, scroll offsets, hex row; per buffer its path or
  untitled name, hex or text, encoding (code page + BOM), line endings, indentation, the language when the user chose
  it, and — PHASE04 — its unsaved text.
- **Restore rules:** a buffer with no unsaved text is read again from disk (in its stored encoding; indentation and
  chosen language re-applied), so changes made outside the editor while it was closed show. Untitled buffers come back
  only when empty (before PHASE04) or with their kept text (PHASE04). A buffer whose file is gone or unreadable is
  skipped, never asked about; the skipped names go on the status line once. A pane left with no tab is closed, unless
  it is the last. Nothing restored → a new untitled tab, as today.
- **One owner per session:** the first instance takes `session.lock` (an exclusive handle held for the process's
  life, released by the OS even on a crash). Only the owner restores and writes the session; another instance works as
  today.
- **Start:** the session is restored after the first-run dialog and before the command-line files, which then open in
  the active pane.
- **Close (PHASE04):** the window closes without asking; the session keeps the unsaved text. `:qa!` keeps the tabs but
  not the unsaved text. If the session cannot be written and there are unsaved changes, today's questions are asked.
- **The session file is kept after a restore** and replaced at the next close: a crash then restores the last
  close's state rather than nothing.
- **An unreadable session** is moved aside to `session.invalid.json`, logged, and the start goes on without it.

## PHASE01 — Session file and its lock

**Status:** DONE

### Steps

1. Core `Sessions/`: records `SessionState(int Version, LayoutSnapshot Layout, int ActivePane,
   IReadOnlyList<SessionPane> Panes, IReadOnlyList<SessionDocument> Documents)`, `SessionPane(int Id,
   IReadOnlyList<SessionTab> Tabs, int ActiveTab)`, `SessionTab(int Document, int CaretOffset, double
   VerticalScrollOffset, double HorizontalScrollOffset, int HexRow)`, `SessionDocument(string? FilePath, string
   UntitledName, bool IsHex, int CodePage, bool ByteOrderMark, LineEndingStyle LineEndings, bool IndentWithTabs, int
   IndentSize, string? Language, string? UnsavedText)`. `SessionState.CurrentVersion = 1`.
2. `SessionFile.Write(path, state)` — JSON (Web defaults, enums by name), folder created, temp file + move.
   `SessionFile.Read(path)` → a load result: the state, or none when the file is absent, or none plus the failure and
   where the file was moved when it is unreadable or invalid (moved to `session.invalid.json`, replacing an older one).
3. Validation on read: version is 1; the layout builds (`LayoutTree.FromSnapshot`); the panes are exactly the layout's
   leaves; the active pane is one of them; every tab names an existing buffer; every active tab index is in range (or
   -1 for an empty pane); every buffer has a non-blank untitled name; sizes are positive.
4. `SessionLock.TryAcquire(path)` → an `IDisposable` holding the file open with `FileShare.None`, or `null` when
   another process holds it. Disposing releases it.
5. `AppPaths`: `SessionFileName`, `QuarantinedSessionFileName`, `SessionLockFileName`, `SessionFile`,
   `SessionLockFile`.

### Acceptance criteria

- A state with two panes, three buffers (file, untitled, hex) and unsaved text round-trips equal through the file.
- A missing file reads as "no session" with no failure; garbage, a wrong version, a malformed layout, panes not
  matching it, or a tab naming a missing buffer read as "no session" with the failure, and the file is moved aside.
- The write replaces an existing file; no temp file is left behind.
- A second `TryAcquire` on a held lock returns `null`; after the first is disposed it succeeds.
- Zero warnings, whole suite green.

## PHASE02 — Capture and restore tabs and splits

**Status:** DONE

### Steps

1. `WorkspaceViewModel.CaptureSession()` → `SessionState`: the layout snapshot, each pane's tabs and active tab, the
   active pane, the buffers the tabs show (each once, in registry order). File buffers and hex buffers are stored by
   path; an untitled buffer only when it is empty (a buffer with text is not stored before PHASE04).
2. `IDocumentService.RestoreAsync(SessionDocument)` → the buffer or `null`, never a dialog: hex → `HexDocument.Open`;
   file → `DocumentFile.ReadAsync(path, stored encoding)`, stored indentation, chosen language re-applied or the
   file's detected; untitled → `registry.Add` with the stored untitled name. Failures are logged (warning), not shown.
3. `WorkspaceViewModel.RestoreSessionAsync(SessionState)` → the names it could not restore. Only on a workspace with
   no tab. In one batch: `Layout.Restore(state.Layout)`, a pane per leaf (the constructor's pane replaced), buffers
   restored once each, tabs added in order with their caret, scroll and hex row, active tabs and the active pane set;
   panes left with no tab closed (not the last). The skipped names are shown once on the status line
   (`Not restored: …`).
4. Unit tests with `EditorHarness` (real files in its temp folder).

### Acceptance criteria

- Two panes side by side at 0.3, pane 1 with `a.txt` and `b.txt` (b active), pane 2 with `a.txt` again and an empty
  untitled: capture → new harness → restore gives the same layout and ratio, the same tabs in the same order, the same
  active tab per pane and active pane, one buffer for `a.txt` shown by two tabs, the caret offsets and scroll offsets.
- A hex tab comes back as hex; a file opened with *Reopen with Encoding* comes back in that encoding; a chosen
  language comes back.
- A file deleted between capture and restore is skipped, its pane closed when emptied, and the status line names it.
- An untitled buffer with text is not captured (before PHASE04).
- Zero warnings, whole suite green.

## PHASE03 — Restore the session at start

**Status:** TODO

### Steps

1. Desktop `ISessionStore` / `SessionStore` (singleton, disposable): takes the lock on first use and holds it;
   `Load()` → the state or `null` (logs a moved-aside file as a warning); `Save(SessionState)` → whether it was
   written (logs a failure as an error). Neither does anything for an instance that is not the owner.
   Paths from `AppPaths`; the test harness points it at its temp folder.
2. `MainWindowViewModel.StartAsync`: after the first-run dialog, restore the loaded session, then open the startup
   files.
3. `MainWindowViewModel.SaveSession()` called by `MainWindow.OnClosing` when the close goes ahead (after today's
   questions): writes `Workspace.CaptureSession()`.
4. DI registration in `AddShell`; the composition-root test still resolves everything.

### Acceptance criteria

- Harness: a session saved by one window view-model is restored by the next one's `StartAsync`, before command-line
  files, which open in the active pane.
- A second store on the same lock file while the first holds it neither loads nor saves.
- An unreadable session file starts the editor with one untitled tab and is moved aside.
- Closing with every change saved, then starting again, gives the same tabs and splits.
- Zero warnings, whole suite green.

## PHASE04 — Keep unsaved changes on exit

**Status:** TODO

### Steps

1. `CaptureSession(keepUnsaved)`: a dirty text buffer stores its text, format, indentation; an untitled buffer with
   text is stored. Without `keepUnsaved`, as in PHASE02.
2. Restore of a buffer with unsaved text: built from the stored text and format (no disk read), marked unsaved —
   `DocumentViewModel` gets a way to start dirty that stays dirty until saved or reloaded (empty undo history).
   A missing file does not stop it: the buffer keeps its path, and a save creates the file.
3. Closing (owner): no questions; the session is written with `keepUnsaved: !DiscardChangesOnExit`. If it cannot be
   written and there are unsaved changes, today's questions are asked, and the session is written after them without
   the unsaved text. Not the owner: today's behaviour.
4. README: the sentence on quitting with unsaved changes, and a line on the restored session.

### Acceptance criteria

- Harness: an untitled buffer with text and a modified file (CRLF, UTF-16 LE, tabs) close without a question and come
  back dirty with the same text, encoding, line endings and indentation; saving the file writes it and clears the dot.
- `:qa!` closes without a question and the next start shows the file as on disk, without the untitled buffer's text.
- A session that cannot be written (read-only file) with unsaved changes asks the usual questions.
- A non-owner instance with unsaved changes asks the usual questions.
- Closing a tab with unsaved changes still asks.
- Zero warnings, whole suite green.

## Out of scope

- Writing the session while the editor runs (crash recovery between closes) — follow-up.
- A setting to turn the restore off — follow-up.
- Window size, position and state; the explorer's folder; undo history; vim registers and marks.
- Detecting that a file changed on disk under a restored unsaved buffer (the kept text wins).
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Closing with unsaved changes | No question; the text goes into the session | What the draft describes (VS Code's hot exit, Notepad) | Keep asking at exit |
| `:qa!` | Tabs kept, unsaved text discarded | It is vim's "quit, throwing changes away" | Ignore the `!`; drop the session entirely |
| Storage | One JSON file, text embedded, atomic replace | One atomic write; no orphan backup files to clean | A backup file per buffer (VS Code); a database |
| When written | Synchronously while the window closes | Works for every way the window closes, OS logoff included; a failure can still fall back to the questions | In `App.OnExit` (too late to ask); periodically (crash recovery — follow-up) |
| Several instances | First instance owns the session via a lock file | The last one closing would otherwise overwrite another's unsaved text | Last writer wins (data loss); merging sessions |
| Clean buffers | Re-read from disk | Shows edits made while the editor was closed; never overwrites them on save | Storing every buffer's text |
| Failures at restore | Skipped, named once on the status line | A start must not open with a stack of dialogs | `DocumentService`'s error dialog per file |
| Corrupt session | Moved aside, logged, fresh start | Keeps the user's only copy of unsaved text for inspection | Deleting or overwriting it |
| Session after restore | Kept until the next close replaces it | A crash then restores the last close rather than nothing | Deleting it once restored |
| Command-line files | After the restore, in the active pane | VS Code's behaviour; the files are what the user asked for now | Skipping the restore when files are given |
| Untitled buffers | Empty ones restored; ones with text only with their text (PHASE04) | An untitled tab is only worth its text | Restoring every untitled name empty |
| Setting to disable | None | Not asked for | A settings-page toggle (follow-up) |
| Phasing | Core file → capture/restore → wiring → unsaved text | Each phase reviewable alone; tabs and splits ship before hot exit | One phase (too large to review) |

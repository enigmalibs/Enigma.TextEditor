# FEATURE-768C — Explorer folder in the session

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-768c-explorer-folder`
**Run:** feature/2026-11-16-session-explorer-autosave

## Objective

The session (`session.json`, FEATURE-34A1) brings back the tabs, the splits and the unsaved text. Make it bring back
the explorer too:

- the folder the explorer showed is kept with the session, and opened again at the next start;
- if no folder was open, or the folder no longer exists, the editor starts with no folder open.

## Context & constraints

- `FileExplorerViewModel` (Desktop) holds `FolderPath` (full, trimmed path, or `null`) and `IsVisible`.
  `OpenFolderAsync(folder)` clears the tree, sets the folder, **shows the explorer**, and awaits the top-level listing
  (off the UI thread, `DirectoryLister`). Nothing persists either value today; the explorer starts hidden.
- `MainWindow.OnExplorerPropertyChanged`: when `IsVisible` turns true it sizes the column and posts
  `Explorer.FocusContent()` — the explorer takes the keyboard. At start that would take it from the active pane.
- `MainWindowViewModel` owns both the workspace and the explorer, and the session calls: `StartAsync` (first-run
  dialog → `Workspace.RestoreSessionAsync` → `Workspace.OpenStartupFilesAsync`), `KeepSessionOnClose()` and
  `SaveSession()` (both write `Workspace.CaptureSession(…)`). The workspace knows nothing of the explorer.
- `SessionState` (Core) is a positional record, `Version = 1`; `SessionFile.Read` validates it and quarantines what it
  cannot use. JSON uses Web defaults and ignores unknown members, and records' optional constructor parameters take
  their default when the member is missing.

## Decisions

- **Two optional members on `SessionState`:** `string? ExplorerFolder = null` and `bool ExplorerVisible = false`. The
  version stays 1: a file written before has neither and restores as today; an older editor ignores them.
- **Captured by `MainWindowViewModel`:** `Workspace.CaptureSession(keepUnsaved) with { ExplorerFolder, ExplorerVisible }`,
  for both close paths.
- **Restored by `FileExplorerViewModel.RestoreAsync(string? folder, bool visible)`:** sets the visibility as stored;
  opens the folder when it is a fully qualified path to a folder that exists — checked off the UI thread — else leaves
  the explorer with no folder (logged at Information when a folder was named). A folder the user opened while the check
  ran is left alone.
- **Order at start:** session tabs → command-line files → explorer. The documents never wait for a folder listing.
- **The keyboard stays in the active pane at start:** `MainWindowViewModel.IsStarting` is true for the whole of
  `StartAsync`; `MainWindow` does not hand the keyboard to an explorer shown while it is.
- **No validation in `SessionFile`:** a bad explorer path is ignored at restore, never a reason to quarantine a
  session that holds unsaved text.

## Steps

1. Core `SessionState`: add `ExplorerFolder` and `ExplorerVisible` (optional, documented).
2. `FileExplorerViewModel`: `RestoreAsync(folder, visible)`; `OpenFolderAsync` and the restore share one private open
   that takes whether to show the explorer. The class takes an `ILogger<FileExplorerViewModel>` for the Information
   line (the container supplies it; the harness passes `NullLogger`).
3. `MainWindowViewModel`: `CaptureSession(bool keepUnsaved)` (explorer included) used by `KeepSessionOnClose` and
   `SaveSession`; `StartAsync` restores the explorer last; `IsStarting`.
4. `MainWindow.OnExplorerPropertyChanged`: no focus hand-over while `IsStarting`.
5. Tests: Core round-trip and an old file without the members; harness start tests; one headless view test for the
   keyboard.
6. README: the explorer paragraph and the `session.json` line.

## Acceptance criteria

- A session closed with a folder open and the explorer shown comes back with that folder listed and the explorer shown.
- A folder open with the explorer hidden comes back open, the explorer hidden.
- A folder deleted between the close and the start: no folder, no failure, the tabs restored as before.
- No folder at the close: no folder at the start.
- A session file without the new members (written by 1.19) restores its tabs, the explorer hidden with no folder.
- A relative path in `ExplorerFolder` is ignored; the session is not moved aside.
- With a restored explorer shown, the explorer does not have the keyboard after the start.
- Command-line files still open in the active pane after the restore.
- Zero warnings, whole suite green.

## Out of scope

- The explorer's width, its expanded folders and its selection.
- Saving the session while the editor runs (`FEATURE-4158`).
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What of the explorer is kept | Its folder and whether it was shown | "As you left it": a hidden explorer must not pop open at every start | The folder only (opening it would always show the explorer) |
| Folder gone or unusable | No folder, silently; logged | What the draft asks; a start is not the moment for messages | A status-line message; quarantining the session |
| Schema version | Stays 1, members optional | Backward and forward compatible; a bump would move every user's session aside on upgrade | `version: 2` |
| Where existence is checked | Off the UI thread | `Directory.Exists` on an unreachable network share can block for seconds | On the UI thread |
| Order at start | Tabs → command-line files → explorer | Documents first; a large folder's listing never delays them | Explorer before the files |
| Keyboard at start | Stays in the active pane | With vim on, the first keys would otherwise move in the tree | Let the explorer take it as when toggled |
| Visibility when the folder is gone | As stored | The panel and its folder are independent; the user had it shown | Force it hidden |

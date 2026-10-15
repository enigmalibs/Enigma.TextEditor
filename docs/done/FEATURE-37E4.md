# FEATURE-37E4 — Open files by drag and drop

**Item:** FEATURE-37E4 — Open files by drag and drop
**Branch:** `feature/feature-37e4-file-drop`
**Run:** vibe/2026-10-15-drop-icon-help

## Summary

Files dropped on an editor pane now open in that pane. They can come from the file manager or any
application that offers local files.

- **Where.** The whole pane is a drop target: the tab strip, the text, the hex view and the empty
  pane's placeholder. The pane dropped on takes the keyboard, and the files open there as tabs, in
  drop order; the last one is shown.
- **How.** Each file goes through `WorkspaceViewModel.OpenAsync`, the same path as *File → Open*:
  - a binary file gets the binary-file question;
  - a file that is too large gets the size question;
  - a file that cannot be read gets the error dialog;
  - a file that is already open is shown, not read again.
- **What is skipped.** Folders and paths that no longer exist are skipped. They are named once on the
  status line, e.g. `Not opened: notes is a folder; gone.txt does not exist.` Items without a local
  path cannot be opened and are left out.
- **The cursor.** A drag carrying files gets the Copy cursor, and the drop reports a copy, never a
  move: a file manager deletes its original after a reported move. A drag of text is left to the
  editor's own text drag-and-drop. Anywhere else on the pane, a text drag gets the No-drop cursor.
- The files are opened after the drop has returned, so the platform's drag loop is over before any
  dialog appears (FileExplorer's approach).

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Views/DroppedFiles.cs` — the local paths a drag carries, and the
  effect it gets.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileDropTests.cs` — 4 tests.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/FileDropViewTests.cs` — 3 headless window tests.

**Modified**

- `ViewModels/IPaneHost.cs` — `OpenDroppedAsync(pane, paths)`.
- `ViewModels/WorkspaceViewModel.cs` — implements it: activate the pane, open or skip each path,
  report the skipped ones once.
- `ViewModels/EditorPaneViewModel.cs` — `OpenDroppedAsync(paths)`, the pane's door to its host.
- `Views/EditorPaneView.axaml.cs` — allows drops; DragEnter/DragOver set the cursor; Drop reads the
  paths and posts the opening.
- `tests/…/ViewModels/EditorPaneViewModelTests.cs` — its recording `IPaneHost` gains the new member.
- `docs/roadmap.md`, `docs/plan/FEATURE-37E4.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the pane sees drags over the text | Its handlers are registered with `handledEventsToo` | The editor's `TextArea` answers every drag before the pane does | Handlers on the `TextArea` itself (couples the pane to AvaloniaEdit's internals) |
| The cursor for a drag of files | Always Copy | By the time the event reaches the pane, the `TextArea` has overwritten `DragEffects` (the source's allowed effects) with None, and Avalonia 12 keeps no separate copy of them. Every source that offers files allows a copy | Checking against `e.DragEffects` (always None over the text, so files could not be dropped there) |
| A text drag over the empty pane or the hex view | No-drop cursor, unless something inside already handled it | Nothing there takes text; a Copy cursor would promise a drop that does nothing | Leaving the source's default effect |
| When the files open | Posted after the drop returns | A dialog (binary question, read error) must not open inside the platform's drag loop | Opening inside the Drop handler |
| How skipped paths are named | By file or folder name; a drive root by its path | Drops carry full paths, which would crowd the status line | Full paths (the command line uses what was typed) |
| Headless test data | Real `BclStorageFile`s from the headless window's storage provider | Avalonia 12 forbids implementing `IStorageFile` outside Avalonia (CS0535 on an internal marker member) | A fake storage file |

## Deviations & follow-ups

- **The plan's hover rule changed.** The plan said Copy "within what the source allows"; the
  build-time row above explains why the pane cannot see that, and what it does instead.
- **Known limitation: a drag carrying both files and text.** Some applications put both on one
  drag. Over the text, the editor inserts the text *and* the pane opens the files. Windows Explorer
  and the usual file managers carry files only.
- **Fix cycles used: 1 of 3.** The first run failed the two hover/drop tests over the text, which led
  to the Copy-cursor decision above.
- **Not done:** no manual drop from Windows Explorer onto the running Debug build. The drop path is
  exercised headlessly with real storage-file items, and the opening by the view-model tests.
- Line endings: the new and touched files are LF, as `.gitattributes` asks; no CRLF churn.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, and there is no
`CLAUDE.md` or `AGENTS.md`.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2367 passed**, 0 failed, 0 skipped (2360 before,
  plus 7 new). The new tests cover:
  - files open in the pane dropped on, which takes the keyboard, in order, with the last one shown;
  - a folder and a vanished path are skipped and named once;
  - an already-open file shows its buffer, edits included;
  - a binary file gets the binary-file question;
  - in the real window, the hover over the text and over an empty pane is Copy, a text drag over an
    empty pane is None, and the editor still takes a text drag;
  - a drop on the pane without the keyboard opens there, activates it and reports a copy;
  - dropped text, even text that is a path, opens nothing.

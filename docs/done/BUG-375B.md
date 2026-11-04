# BUG-375B — Ctrl+Z in insert mode crashes the editor

**Item:** BUG-375B — Ctrl+Z in insert mode crashes
**Branch:** `bugfix/bug-375b-insert-mode-undo`
**Run:** bugfix/2026-11-04-bug-375b

## Summary

With vim on, `Ctrl+Z` in INSERT no longer crashes ("No undo group should be open at this point"), and the following
`Esc` no longer crashes either ("There are no open undo groups").

- **Cause:** an insert session holds an undo group open until `Esc`. `u` exists only in NORMAL mode, but `Ctrl+Z`
  went straight to AvaloniaEdit, whose `UndoStack.Undo()` throws inside a group and resets the group depth to 0 as it
  does. The engine still thought the group was open, so the `Esc` that closed it threw too. The same applied to redo
  (`Ctrl+Y`, `Ctrl+Shift+Z`), the Edit menu and right-click Undo/Redo, and "Reopen with encoding" (`UndoStack.ClearAll`).
- **Fix:** while vim is on, every undo/redo goes through the engine.
  - `VimEngine` takes `Ctrl+Z` / `Ctrl+Shift+Z` in every mode and `Ctrl+Y` in INSERT. Public `Undo()` / `Redo()` serve
    the menus.
  - In INSERT, the session's group is closed (what was typed becomes its own step), the step is undone or redone, and
    a new group opens: the mode stays INSERT. In NORMAL, the keys are `u` / `Ctrl+R`. In VISUAL, the selection is left
    first.
  - `VimEditorBehavior.Undo/Redo` → `EditorPaneView.Undo/Redo` → the Edit menu (`MainWindow`) and the context menu.
  - `DocumentViewModel.Reloading` is raised before a reload. The pane resets its engine on it, which closes the group.
- The test fake now throws on undo/redo with a group open, like AvaloniaEdit.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`, `src/Enigma.TextEditor.Core/Vim/IVimBuffer.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`, `.../Views/AvaloniaEditVimBuffer.cs`,
  `.../Views/EditorPaneView.axaml.cs`, `.../Views/MainWindow.axaml.cs`, `.../ViewModels/DocumentViewModel.cs`,
  `.../ViewModels/HelpCatalog.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/FakeVimBuffer.cs`, `.../Vim/VimUndoTests.cs`,
  `.../Vim/VimScreenMotionTests.cs`, `tests/Enigma.TextEditor.Desktop.UnitTests/Views/VimEditingTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-375B.md`; created this file

## Deviations & follow-ups

- **Changed contract test:** `VimScreenMotionTests.InInsertMode_TheScrollKeysAreTheEditors` lost its `i<C-y>` case.
  `Ctrl+Y` in INSERT is now the engine's redo, as the plan decided. A new case, `InInsertMode_CtrlYIsTheRedo_NotTheScroll`,
  asserts it is taken and still never scrolls. `CtrlR_TheSixScrollKeys…` now also asserts `Ctrl+Z` is claimed (renamed).
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  ("Pipe is broken"), in the baseline.
- **Pre-existing flake:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` fails intermittently
  under full-suite load: 3 of 6 runs on this branch, and 1 of 4 runs of the untouched baseline (extracted with
  `git archive`). Its `DrainAsync` yields 4 times without awaiting the thread-pool file read. Follow-up: wait on the tab
  instead.
- **Release notes** are left to the next release item.
- **Documentation sweep:** README, RELEASENOTES and `docs/RELEASE.md` do not describe the undo keys, so nothing was
  stale. The Help page gained "Ctrl+Z, Ctrl+Y — Undo, redo without leaving insert mode" (planned).

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Recording `Ctrl+Shift+Z` | Fold it into `<C-Z>` at `HandleKey` | The notation drops Shift from a character: the key would replay as undo | Changing the notation for every Control letter (wider blast radius) |
| Menu route with the `:` line open | `Undo()` returns `false`, the editor undoes | Same as the key: the command line turns Control keys down, and no group is open outside INSERT | Closing the command line first |
| Engine API shape | `bool Undo()` / `bool Redo()` | The caller needs to know when to fall back to the editor | `void` plus a separate query |
| Count on the undo keys | None, one step | `Ctrl+Z` is a GUI key; `3u` remains for counts | Feeding the pending count |
| A menu undo during a recording | Recorded as the key | A replay must reproduce it | Not recorded (replay would diverge) |

## Build/test evidence

- **Baseline** (run branch at `0adff3e`, this run's first dev): `dotnet build Enigma.TextEditor.slnx`: 0 warnings;
  `dotnet test --solution Enigma.TextEditor.slnx`: 3112 total, 3111 passed, 1 failed (`InstanceServerTests…`).
- **Reproduced first:** with the three routes disabled, all 5 new Desktop route tests failed with
  `InvalidOperationException: No undo group should be open at this point`.
- **After:** build: 0 warnings, 0 errors. Tests: 3144 total, 3142 passed, 2 failed (the baseline's and the
  pre-existing flake above). The Desktop suite alone passed 627/627 on 2 of 4 runs.
- **Tests added (+32):**
  - Core `VimUndoTests` (+26): Ctrl+Z in INSERT (5 cases, one group left open), then `Esc u`; redo keys in INSERT (4);
    NORMAL keys = `u` / `Ctrl+R` (6); VISUAL; `Ctrl+Y` still scrolls; `.` and count after an undo (2); macro replay;
    `<C-Z>` recorded; `Undo()`/`Redo()` in INSERT, NORMAL, with the `:` line open, and during a recording.
  - Core `VimScreenMotionTests` (±0): `i<C-y>` moved to its own case.
  - Desktop `VimEditingTests` (+6): Ctrl+Z ×2 then `Esc` and `u`; Ctrl+Y and Ctrl+Shift+Z; Edit menu and context menu
    Undo; Reopen with encoding; vim off.

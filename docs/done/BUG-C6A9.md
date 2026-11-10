# BUG-C6A9 — Ctrl+Y does not redo

## Summary

The Edit menu and the editor's context menu show `Ctrl+Y` for Redo. With vim on, in normal and visual mode, the key
was vim's "scroll one line up" (BUG-375B kept it so), and only insert mode redid with it. `Ctrl+Y` is now the editor's
redo in every mode, exactly like `Ctrl+Shift+Z`: the engine folds it into its redo key, so it redoes in normal mode,
leaves a visual selection and redoes, redoes inside an insert session, and is recorded into a macro as `<C-Z>`. Vim's
line scroll up is given up (`Ctrl+E` still scrolls down). With vim off, AvaloniaEdit already redid on `Ctrl+Y`; a new
headless test pins that.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/VimAction.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/KeySequenceParserTests.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimScreenMotionTests.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimUndoTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/VimEditingTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-C6A9.md`

## Deviations & follow-ups

- No vim-off binding was added: the headless test showed AvaloniaEdit already redoes on `Ctrl+Y` (plan step 4 said
  "bind only if it fails").
- `VimAction.ScrollLineUp` is gone, and `ScrollLines(count, down)` became `ScrollLinesDown(count)`, since only
  `Ctrl+E` reaches it now.
- Tests that encoded the old behaviour were changed to the new one, not weakened: the `<C-y>` scroll rows and
  `CtrlY_PullsACaretThatWouldLeaveOntoTheBottomLine` were removed with the behaviour, and
  `CtrlYInNormalMode_IsStillTheScroll_AndUndoesNothing` became `CtrlYInNormalMode_IsTheRedo`.
- Pre-existing (baseline) failure, unrelated and left alone:
  `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` ("Pipe is broken"). It fails
  every run on this machine.
- Release notes: for the next release item. Upgrading note: `Ctrl+Y` no longer scrolls a line up in normal and
  visual mode.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the redo is read | In `FoldRedoKey`: `Ctrl+Y` (either case) becomes `RedoKey` | One path for every redo key: dispatch, recording and replay all work without new branches |
| Help | *Scrolling*: `Ctrl+E` alone. *Editing*: new row `Ctrl+Z, Ctrl+Y` — "Undo, redo, as the Edit menu does" | The vim row `u, Ctrl+R` stays as vim's own keys |

## Build/test evidence

- Run baseline, taken on the run branch `bugfix/2026-11-09-ctrl-y-session-restore` after the planning commit
  (`c3a6c22`): `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors. `dotnet test --solution
  Enigma.TextEditor.slnx`: 3199 tests, 3198 passed, 1 failed (the pre-existing failure above).
- After this dev: `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors. `dotnet test --solution
  Enigma.TextEditor.slnx`: 3203 tests, 3202 passed, 1 failed (the same pre-existing failure).
- Tests added:
  - Core: `CtrlYInNormalMode_IsTheRedo`, `CtrlYWithNothingToRedo_ChangesNothing`,
    `CtrlYInVisualMode_LeavesTheSelection_ThenRedoes`, `ARecording_WritesCtrlYAsTheRedoItIs_AndReplaysIt` (2 rows),
    `CtrlYInNormalMode_IsNoLongerTheScroll` (2 rows), and three `<C-y>` rows in
    `TheUndoKeysInNormalMode_DoExactlyWhatUAndCtrlRDo`.
  - Desktop headless: `CtrlYInNormalMode_IsTheRedoTheEditMenuNames`, `WithVimOff_CtrlYIsTheEditorsRedo`.
- Docs sweep: README, `docs/RELEASE.md` and SECURITY.md say nothing about `Ctrl+Y` scrolling. README's 1.16 line
  ("`Ctrl+Y` redoes") is still true. No edit.

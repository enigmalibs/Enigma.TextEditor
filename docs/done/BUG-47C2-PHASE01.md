# BUG-47C2-PHASE01 — Click past a line end in normal mode

**Item:** BUG-47C2 — Mouse clicks in vim normal mode
**Branch:** `bugfix/bug-47c2-phase01-click-line-end`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Summary

With vim on, a click past the end of a line in normal mode now puts the block caret on the line's last character,
not after it. `x` then deletes the character the block is on.

- **Cause:** AvaloniaEdit's click puts `CaretOffset` after the last character. The engine never heard about the
  click: it brought the caret back onto the line only when the next command ran (`Execute`'s `SetCaret`). So the
  block was drawn after the line and `x` deleted the character to its left. The desired column also stayed stale
  after a click.
- **Fix:**
  - **Core:** `VimEngine.PlaceCaret()`. Outside insert mode and the command line, it leaves a visual mode
    (remembered for `gv`), clamps the caret to normal mode's rule and forgets the desired column.
  - **View:** `VimEditorBehavior` adds `PointerPressed` and `PointerReleased` handlers on the editor (bubble,
    `handledEventsToo`), attached and detached with the key handlers. The press remembers a left-button press on the
    `TextView` (`IsOnText`, which excludes the margins, scrollbars and find panel), with its click count and Shift. On
    release, a plain single click that left no selection calls `PlaceCaret()`.
- A click now also ends a visual mode, which PHASE02's double-click relies on.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimPointerTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/VimPointerViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-47C2.md`. Created this file.

## Deviations & follow-ups

- **Drags and Shift+click** keep today's behaviour: a normal-mode mouse selection. A drag while in a visual mode
  still leaves the engine's anchor stale (pre-existing; follow-up, as in the plan's *Out of scope*).
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  (run baseline, see `docs/done/BUG-B026-PHASE01.md`).
- **Documentation sweep:** README and Help do not describe clicks with vim on, so nothing was stale. The engine's
  `Execute` comment was corrected as planned.
- **Release notes** are left to the next release item.

## Build/test evidence

- Baseline: see `docs/done/BUG-B026-PHASE01.md`.
- **This dev:** `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` ran 3188 tests: 3187 passed and 1 failed (the baseline test).
- **Tests added (13):**
  - `VimPointerTests` (9 cases):
    - the block lands on the last character, and `x` deletes it;
    - an empty line and a click inside a line are left alone;
    - `v` and `V` are ended by a click, and `gv` still works;
    - the clicked column replaces `$`'s stickiness for `j`;
    - insert mode and an open command line are untouched.
  - `VimPointerViewTests` (4 cases): a real click past the end of line 1 in normal mode gives caret 2, and `x` gives
    `ab`; in insert mode the caret is 3; a click ends `vl`; with vim off the caret is 3.
- **Sanity check:** without the `PlaceCaret()` call, the normal-mode and visual-mode click tests fail.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which button | Left only (`InitialPressMouseButton`) | The right button opens the context menu and the middle button does nothing in the text | Any button |
| What counts as "on the text" | The `TextView` or its descendants | The line-number margin selects lines and the find panel has its own gestures | The whole `TextArea` |
| Pending operator on a click | Kept | Vim would use the click as the motion. Keeping it is closer than dropping it, and it was the behaviour before | Dropping it |

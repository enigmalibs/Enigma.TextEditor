# BUG-47C2-PHASE02 — Double-click selects in visual mode

**Item:** BUG-47C2 — Mouse clicks in vim normal mode
**Branch:** `bugfix/bug-47c2-phase02-double-click-visual`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Summary

With vim on, a double-click on a word in normal mode now selects it in **visual mode**. Operators act on it, `Esc`
or a click ends it, and the status bar shows VISUAL. A triple-click selects the line in linewise visual mode (V-LINE).

- **Cause:** AvaloniaEdit's double-click made the selection, and the engine never heard of it. The mouse selection
  stayed a normal-mode one, so `d` started an operator instead of deleting the word.
- **Fix:**
  - **Core:** `VimEngine.AdoptSelection(anchor, active, linewise)`. Outside insert mode and the command line, for a
    non-empty selection, it drops what was half typed and sets the visual anchor and caret where `viw` would. A
    forward selection gets the anchor on its first character and the caret on its last; a backward one the reverse,
    clamped to normal mode's rule. It then enters `Visual` or `VisualLine` and shows the selection. `SetCaret` now
    goes through a new `OnTheLine` helper, which is shared with the anchor.
  - **View:** `VimEditorBehavior` adopts the editor's selection (`AdoptPointerSelection`) on the press of a gesture
    with `ClickCount >= 2`, and again on its release (a drag after a double-click extends a word at a time). The
    direction is read from the caret's edge. An empty selection is treated as a click (`PlaceCaret`). Linewise
    applies when the gesture is a triple-click and the selection covers whole lines (`CoversWholeLines`).
  - Help → Key Bindings, *Visual mode*: "Double-click — Select the word; a triple-click selects the line".

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimPointerTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/VimPointerViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-47C2.md`. Created this file.

## Deviations & follow-ups

- **Triple-click checked (plan step 3)** with a throwaway headless probe, removed before the commit. AvaloniaEdit 12
  selects the line's text **without** its line break (`[0, 11)` on `foo bar baz\n`). `CoversWholeLines` therefore
  accepts a selection ending at a line's end as well as at the next line's start, and a triple-click is linewise.
- **Help row** says "a triple-click selects the line". This is true only because of that check.
- **Drag selections** are unchanged: they stay normal-mode mouse selections (plan *Out of scope*).
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  (run baseline, see `docs/done/BUG-B026-PHASE01.md`).
- **Documentation sweep:** README does not describe mouse gestures with vim on, so nothing was stale. Help was updated
  as planned.
- **Release notes** are left to the next release item.

## Build/test evidence

- Baseline: see `docs/done/BUG-B026-PHASE01.md`.
- **This dev:** `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` ran 3199 tests: 3198 passed and 1 failed (the baseline test).
- **Tests added (11):**
  - `VimPointerTests` (6 cases): a forward word becomes `foo [ba|r] baz` and `d` deletes it; a backward selection
    gives `foo [|bar] baz`; `linewise` gives VisualLine and `d` deletes the line; a pending `d` is dropped (`y` yanks
    `bar`); an empty selection and insert mode change nothing.
  - `VimPointerViewTests` (5 cases): a real double-click on `bar` gives VISUAL, `bar` selected, caret 6, and `d` gives
    `foo  baz`; `Esc` ends it, a second double-click selects `baz`, and a later click ends visual mode; in insert mode
    the word is selected and the mode stays INSERT; a triple-click gives V-LINE and `d` empties the one-line file;
    with vim off the word is selected and the caret is 7.
- **Sanity check:** with `AdoptPointerSelection` short-circuited, the double-click, Esc/click and triple-click cases
  fail.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| When to adopt | On the press and again on the release | The mode is right while the button is held, and a word-drag after the double-click is taken in full | Release only (the status bar would lag); press only (misses the word-drag) |
| Direction of the selection | The caret at the segment's start means backwards (for more than one character) | AvaloniaEdit puts the caret on the far edge. A one-character selection reads the same either way | Comparing `Selection.StartPosition` locations (more code, same answer) |
| Half-typed command | Dropped | The selection is what the next key acts on, as with `viw` | Keeping it (a pending `d` would delete the selection on the next motion key) |

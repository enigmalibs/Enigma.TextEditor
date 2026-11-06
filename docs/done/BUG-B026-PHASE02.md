# BUG-B026-PHASE02 — The panel's keys are its own

**Item:** BUG-B026 — Find and replace panel with vim on
**Branch:** `bugfix/bug-b026-phase02-panel-keys`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Summary

With vim on, the find and replace panel's fields now take the keyboard in every mode. Typing, `Backspace`, `Enter`
(next match) and `Esc` (close) work as they do with vim off.

- **Cause:** the panel is a child of the text area. `VimEditorBehavior`'s tunnelling `KeyDown` and `TextInput`
  handlers sit on the whole `TextEditor`, so they saw every key typed in the panel first. Outside insert mode they
  swallowed the text and offered `Esc`, `Enter`, `Backspace` and the arrows to the engine, which took them. `Esc` was
  taken in insert mode too, so it never closed the panel.
- **Fix:** a new `IsInSearchPanel(source)` helper, true when the source is `TextEditor.SearchPanel` or a visual
  descendant of it. `OnKeyDown`, `OnTextInput` and the PHASE01 bubbling handler return at once for such a source.
- **`Esc` from the text:** with the panel open, in normal mode with nothing pending (`HasPendingSequence` false), the
  key is left to the text area, whose `CloseSearchPanel` binding closes the panel. In insert mode, a visual mode or
  with something half typed, vim's `Esc` comes first.
- Class remarks on `VimEditorBehavior` gained a paragraph on the panel.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SearchPanelTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-B026.md`. Created this file.

## Deviations & follow-ups

- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  (run baseline, see `BUG-B026-PHASE01.md`). The flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`
  passed this time.
- **Documentation sweep:** no prose doc describes the panel. The Help line added in PHASE01 already says
  "Esc closes it".
- **Release notes** are left to the next release item.

## Build/test evidence

- Baseline: see `docs/done/BUG-B026-PHASE01.md`, which has 0 warnings and 3144 tests, with the failures listed there.
- **This dev:** `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` ran 3171 tests: 3170 passed and 1 failed (the baseline test).
- **Tests added (10 cases in `SearchPanelTests`):**
  - Typing and `Backspace` in the find field, in normal and insert mode: the pattern changes, the text and the mode
    do not.
  - `Enter` selects the next match.
  - `Esc` in the panel closes it in normal, insert and visual mode. Insert stays INSERT, and visual was already left by
    PHASE01.
  - Typing in the replace field in normal mode.
  - `Esc` from the text in normal mode closes the panel.
  - In insert mode the first `Esc` leaves insert and the second closes the panel.
  - Vim keys in the text still work while the panel is open.
- **Sanity check:** with `IsInSearchPanel` forced to false, 4 cases fail: typing in either field, `Enter`, and `Esc`
  in insert mode. The normal and visual `Esc` cases pass either way thanks to the idle-normal `Esc` rule.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to recognise the panel | `ReferenceEquals(source, panel) \|\| panel.IsVisualAncestorOf(source)` | The panel lives in the text area's visual tree, and its fields are template children | Matching on `TextBox` (would also catch any future in-editor field, and nothing more precise) |
| Esc with a normal-mode mouse selection and the panel open | Closes the panel; the selection stays | The rule is "vim would do nothing"; the selection is the panel's last match more often than not | Dropping the selection as well |

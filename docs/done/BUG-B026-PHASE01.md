# BUG-B026-PHASE01 — Ctrl+F and Ctrl+H open in every mode

**Item:** BUG-B026 — Find and replace panel with vim on
**Branch:** `bugfix/bug-b026-phase01-open-panel`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Summary

With vim on, `Ctrl+F` now opens AvaloniaEdit's find panel in normal and visual mode too, not only in insert mode.

- **Cause:** `Ctrl+F` is one of vim's six scroll commands. `VimEditorBehavior` offered it to the engine, which took
  it as a page down in the command modes, so it never reached the text area's `Find` binding. `Ctrl+H` was already
  turned down by the engine and reached the editor. This is confirmed: its tests pass with the fix disabled, and the
  three `Ctrl+F` ones fail.
- **Fix:** `VimEditorBehavior.IsSearchShortcut` matches `Ctrl+F` / `Ctrl+H`, with Control or Meta alone. Those keys
  are never offered to the engine. A bubbling `KeyDown` handler (`handledEventsToo`) runs after the text area. Once
  the panel is open, it calls the new `VimEngine.Interrupt()`.
- **`VimEngine.Interrupt()`:** "something outside vim took the keyboard". It drops a half-typed command or count and
  the `Ctrl+W` prefix, cancels an open `:` / `/` line as `Esc` does (`CancelCommandLine`, now shared with the command
  line's `Esc`), and leaves a visual mode (remembered for `gv`). It does nothing in insert mode and records nothing
  into a macro.
- From visual mode, the panel takes the selected word as its pattern before the selection is left.
- Help → Key Bindings: *Scrolling* lists `PgDn, Ctrl+B, PgUp` and *Search & replace* lists `Ctrl+F, Ctrl+H`.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimInterruptTests.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SearchPanelTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-B026.md`. Created this file.

## Deviations & follow-ups

- **Pending-operator criterion tested by behaviour.** After `d` and `Ctrl+F`, the test closes the panel and types `w`.
  The caret moves to the next word and nothing is deleted. This avoids exposing the engine's `HasPendingSequence` on
  the view.
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  in every baseline run.
- **Pre-existing flake:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`, which failed in 2 of
  4 baseline runs.
- **Documentation sweep:** `README.md` and the Core vim `README.md` never mention `Ctrl+F` or the find panel, so
  nothing was stale. Help was updated as planned.
- **Release notes** are left to the next release item.

## Build/test evidence

- **Run baseline** (run branch `bugfix/2026-11-06-vim-search-mouse` @ `0700c78`, before the first dev branch):
  `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors. `dotnet test --solution Enigma.TextEditor.slnx`
  over 4 runs gave 3144 tests, with `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  failing every time and `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failing in 2 runs.
- **This dev:** `dotnet build` gave 0 warnings and 0 errors. `dotnet test --solution` ran 3161 tests: 3159 passed and
  2 failed, the two baseline tests above.
- **Tests added (17):**
  - `VimInterruptTests` (9 cases): a pending operator, count and `Ctrl+W` are dropped; `:` and `/` lines are
    cancelled with no ex request and the preview cleared; `v` and `V` are left with `gv` still working; insert mode
    and its undo group are kept; idle normal mode is untouched.
  - `SearchPanelTests` (8 cases): `Ctrl+F` / `Ctrl+H` in normal mode (mode kept, no scroll, replace mode for `Ctrl+H`)
    and in insert mode (mode kept); visual mode (pattern = the selected word, then NORMAL); a pending `d` is dropped;
    `PgDn` still pages down; vim off.
- **Sanity check:** with `IsSearchShortcut` disabled, the three `Ctrl+F` normal and visual tests fail. They pass with
  the fix.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which modifiers make a search shortcut | Control alone or Meta alone | AvaloniaEdit's gestures are exactly `Ctrl+F` / `Ctrl+H` (`Cmd` on macOS). `Ctrl+Shift+F` stays vim's page down | Any Control combination |
| Interrupting outside visual mode | `LeaveVisual` only when in a visual mode | A normal-mode mouse selection is left as it is | Always clearing the selection |
| Cancelled `/` line | Restores the view top, as `Esc` does | Shared `CancelCommandLine` with one behaviour | Closing without the scroll-back |

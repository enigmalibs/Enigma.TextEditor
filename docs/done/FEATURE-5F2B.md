# FEATURE-5F2B — Pane focus with gh, gj, gk, gl

**Item:** FEATURE-5F2B — Pane focus with gh, gj, gk, gl
**Branch:** `feature/feature-5f2b-g-pane-focus`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

`gh`, `gj`, `gk` and `gl` now move the keyboard to the pane to the left, below, above and to the right.
They do the same as `Ctrl+W h`, `j`, `k`, `l`.

- The parser completes `g` + `h`/`j`/`k`/`l` as the new `VimAction.FocusPane`, carrying the letter.
  This works in normal mode and both visual modes. Behind an operator (`dgj`) the sequence is dead,
  like `dgv`.
- The engine reads the letter through `VimWindowKeys.Map` and raises `WindowCommandRequested`. The two
  spellings share one mapping, so they cannot drift apart. From there the existing
  `WindowCommandHandler` moves the focus; at an edge nothing happens.
- It is not a change, so `.` does not repeat it. A count in front is accepted and ignored.
- The help's *Panes* section has a new line: `gh, gj, gk, gl` — "The same, without Ctrl+W".

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimAction.cs` — `FocusPane`.
- `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs` — `SecondG` and the `g` remarks.
- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `case VimAction.FocusPane`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the new Panes line.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/KeySequenceParserTests.cs`:
  - the four keys in normal, visual and visual-line mode;
  - `dgj` and `ygl` are dead.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimWindowCommandTests.cs`:
  - each key raises its window command and leaves the text alone, with or without a count;
  - works in visual mode;
  - `.` does not repeat it.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/WindowCommandTests.cs` — typed in the real
  window over a three-pane layout:
  - `gh`, `gl`, `gk`, `gj` move the active pane;
  - `gj` at the bottom edge leaves the keyboard and the text where they were.
- `docs/roadmap.md`, `docs/plan/FEATURE-5F2B.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Help placement | A line of its own under the `Ctrl+W h, j, k, l` line | The Ctrl+W line is already long; the help tests only need every window command documented somewhere | Appending the four keys to the Ctrl+W line |
| Window test layout | Two columns for `gh`/`gl`, then the right column split in rows for `gk`/`gj` | From a full-height left pane, `gl` toward a split column lands on a boundary between two panes; the test checks only moves with one answer | One three-pane layout for all four keys |

## Deviations & follow-ups

- None from the plan.
- Follow-up: Vim's own `gj`/`gk` (move by display line) can no longer be added under those keys. This
  is the user's explicit choice.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the in-app help was updated as part of the dev.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2463 passed**, 0 failed, 0 skipped (2449 before,
  plus 14 new).

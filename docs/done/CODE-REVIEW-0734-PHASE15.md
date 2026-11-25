# CODE-REVIEW-0734-PHASE15 — [Medium] SOLID-03 Visual mode leaves `VimEngine`

## Summary

What a visual selection is made of — the anchor (the end that does not move) and the last selection, kept for `gv`
and the `'<` / `'>` marks — had its state and logic spread across the engine. It is now one internal class:

- `VimVisualSelection` owns `Anchor` and `Last`, and does the work on them:
  - works out the range (`Range`) and hands it to the editor (`Show`, with the line break in a linewise highlight);
  - remembers, clears and forgets the selection (`Remember`, `Clear`, `Forget`);
  - `o` (`Swap`) and `gv` (`Reselect`, which swaps the on-screen selection with the remembered one);
  - for `.`: the extent (`Extent`) and making it again from the caret (`Repeat`).
- `VimRememberedSelection` and `VimVisualExtent` are now namespace-level internal record structs, since the engine's
  `RecordedChange` and insert record carry the extent.
- The engine keeps the mode, the caret and the edits made over a selection (`ApplyVisualOperator`, `FillSelection`,
  `ReplaceSelection`, the puts). Its `EnterVisual`, `LeaveVisual`, `SwapVisualEnds`, `ReselectVisual` and `Reselect`
  are now a few lines each over the class, and `ShowSelection` / `VisualRange` are one-line hand-offs, which keeps the
  many call sites unchanged.
- `VimEngine` goes from 3,128 to 3,017 lines. Its public API is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Vim/VimVisualSelection.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`

## Deviations & follow-ups

- The edits over a selection (`FillSelection`, `ReplaceSelection`, `PutOverCharacters`, `PutOverLines`,
  `ApplyVisualOperator`) stay in the engine, as the plan said ("the engine keeps the edits"). They use its registers,
  operators and undo grouping.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Who sets the caret and the mode | The engine, with the class returning where the caret goes | Mode changes raise the engine's `ModeChanged`, and the caret has normal mode's rules |
| `ShowSelection` / `VisualRange` in the engine | Kept as one-line hand-offs | About a dozen call sites read the same, so the diff stays reviewable |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3413 tests, 3411 passed, 2 skipped (PHASE06's), 0 failed.
- Guard: the Core Vim tests (2,231, through the engine's public API, visual mode, `gv`, `'<,'>` and `.` included)
  pass unchanged.
- Documentation sweep (§7): nothing user-facing changed.

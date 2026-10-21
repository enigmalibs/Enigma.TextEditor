# BUG-09C2 — Space rj/rk move the bar inverted

**Item:** BUG-09C2 — Space rj/rk move the bar inverted
**Branch:** `bugfix/bug-09c2-resize-jk`
**Run:** bugfix/2026-10-21-resize-jk-release

## Summary

`<Space>rj` and `<Space>rk` now move the bar between two stacked panes the way `j` and `k` point in Vim:

- **`<Space>rj`** moves the bar **down**: the top pane grows, typed in the top pane or in the bottom one;
- **`<Space>rk`** moves it **up**: the bottom pane grows.

Before, it was the other way round. FEATURE-216A followed its draft's wording ("k moves the line to the bottom").

- **The fix** is the one switch that holds the direction, `VimEngine.SplitterCommandFor`: `j` → `MoveSplitterDown`,
  `k` → `MoveSplitterUp`.
- **Unchanged:**
  - the parser's leader table, which carries the letter;
  - the window commands, which are named by direction;
  - the handler's `±ResizeStep`.
- `<Space>rh` and `<Space>rl` already went the way they point, and are unchanged. So are `g+ g- g> g<` and
  `Ctrl+W + - > <`.
- **The help** (*Panes*) now reads `Space rj, Space rk` — down, up — and the README says the same.

## Files / modules touched

**Created**

- `docs/done/BUG-09C2.md`

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `SplitterCommandFor`: `j` down, `k` up; its remarks.
- `src/Enigma.TextEditor.Core/Vim/VimAction.cs` — `MoveSplitter`'s summary and remarks name the directions.
- `src/Enigma.TextEditor.Core/Vim/VimWindowCommand.cs` — `MoveSplitterDown` is `rj`, `MoveSplitterUp` is `rk`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the stacked-panes row is `Space rj, Space rk`.
- `README.md` — the *Tabs and panes* sentence.
- Tests:
  - `tests/…Core.UnitTests/Vim/VimWindowCommandTests.cs`:
    - the leader theory maps `rj` to down and `rk` and `3<Space>rk` to up;
    - the dot-repeat test expects `MoveSplitterUp` from `x<Space>rk.`.
  - `tests/…Desktop.UnitTests/ViewModels/WindowCommandTests.cs`:
    - typed in the real window, `Space rj` grows the top pane from both panes and `Space rk` the bottom one;
    - renamed `TypedInTheWindow_SpaceRJAndRK_…`.
- `docs/roadmap.md`, `docs/plan/BUG-09C2.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| `VimAction.MoveSplitter`'s doc | Fixed with the rest | Its summary listed "left, up, down, right" and its remarks repeated the old reason. The plan missed it | Leaving a comment that contradicts the code |
| New tests | None; the existing direction tests were swapped | They already pin each letter to its command and the typed result from both panes | Adding a second test for the same mapping |
| Parser tests | Untouched | They assert the letter carried (`'j'`, `'k'`), which has no direction | — |

## Deviations & follow-ups

- **One addition to the plan:** `VimAction.cs`'s doc comment, as above.
- **Fix budget:** none used. The first build and test run were green.
- **Release notes:** the 1.3.0 section still says `Space rk` is down. That is what 1.3.0 shipped. FEATURE-6622's 1.4.0
  section tells of the swap.
- **Line endings:** every touched file is LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the *Tabs and panes* sentence now names `Space rj` down and `Space rk` up. The 1.3 callout lists the
  four keys without directions and is still true. FEATURE-6622 rewrites it.
- Nothing else names the direction. `docs/RELEASE.md` and `SECURITY.md` are still true.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2676 passed**, 0 failed, 0 skipped. That is the same count as
  before: the cases were swapped, not added.

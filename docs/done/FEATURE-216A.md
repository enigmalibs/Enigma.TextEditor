# FEATURE-216A — Space leader: move the pane splitter

**Item:** FEATURE-216A — Space leader: move the pane splitter
**Branch:** `feature/feature-216a-leader-resize`
**Run:** feature/2026-10-21-leader-resize-release

## Summary

Space is now the **leader key**, and four leader keys move the **bar** between two panes rather than the active
pane's edge. They need no `+`, `-`, `<` or `>`, which are awkward on a Swiss French keyboard.

- **`<Space>rk`** moves the bar between stacked panes **down**: the top pane grows, typed in the top pane or in the
  bottom one. **`<Space>rj`** moves it up.
- **`<Space>rl`** moves the bar between side-by-side panes **right**: the left pane grows, typed in either.
  **`<Space>rh`** moves it left.
- **Which bar:** the nearest split on that axis containing the active pane, the one `Ctrl+W +` / `>` already resize.
  With no such split nothing happens, silently. The ratio stays clamped to 10–90 %.
- **The step** is the *Pane Resize Step* setting (5 % by default), as for every resize key.
- **The leader:**
  - **Where:** Space opens it in normal mode and in both visual modes. Behind an operator it is still Vim's motion,
    so `d<Space>` still deletes a character, wrapping.
  - **How it ends:** a bare Space waits; Esc drops it. A key that no leader mapping starts with makes the sequence
    dead and does nothing (`<Space>x`, `<Space>rx`).
  - **The table:** the mappings are a table in the parser, so another leader mapping is one line.
- **As for `gh` and `g+`:**
  - a count is accepted and ignored;
  - `.` does not repeat them;
  - `"a` in front makes the sequence dead.
- **Unchanged:** `g+ g- g> g<` and `Ctrl+W + - > <` still grow or shrink the active pane.
- **Shape:** a new `VimAction.MoveSplitter` carries the letter. The engine maps it to one of four new window commands,
  `VimWindowCommand.MoveSplitterLeft/Down/Up/Right`. `WindowCommandHandler` carries them out through
  `WorkspaceViewModel.MoveSplitter` and `LayoutTree.MoveSplitter`, which shares the nearest-split lookup with
  `Resize`.

## Files / modules touched

**Created**

- `docs/done/FEATURE-216A.md`

**Modified**

- `src/Enigma.TextEditor.Core/Layout/LayoutTree.cs` — `MoveSplitter`; `NearestSplit`, shared with `Resize`.
- `src/Enigma.TextEditor.Core/Vim/VimWindowCommand.cs` — the four `MoveSplitter*` commands.
- `src/Enigma.TextEditor.Core/Vim/VimAction.cs` — `MoveSplitter`.
- `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs`:
  - the leader: its table, its state, `LeaderKey`;
  - Space opens it when no operator is waiting;
  - the class remarks.
- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `MoveSplitter` → window command (`SplitterCommandFor`);
  `HasPendingSequence` doc.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `MoveSplitter`.
- `src/Enigma.TextEditor.Desktop/ViewModels/WindowCommandHandler.cs` — the four commands, by `±ResizeStep`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`:
  - *Panes*: `Space rh, Space rl` and `Space rk, Space rj`;
  - *Moving*: the `Space, Backspace` row is now `Backspace`.
- `README.md` — the *Tabs and panes* paragraph names the leader keys.
- Tests:
  - `tests/…Core.UnitTests/Layout/LayoutTreeTests.cs`:
    - `MoveSplitter` moves the bar the same way from either side;
    - it picks the nearest split on the axis, refuses without one, and raises `Changed` only when it moved;
    - it is clamped.
  - `tests/…Core.UnitTests/Vim/KeySequenceParserTests.cs`:
    - the four keys in normal and both visual modes;
    - `<Space>`, `<Space>r`, `3<Space>r` pending;
    - dead sequences (`<Space>x`, `<Space>j`, `<Space>rx`, `<Space>rr`, `<Space>r<Esc>`, `"a<Space>rk`);
    - the count rides on the command;
    - `d`/`c`/`y` + Space are still the operator over `RightWrapping`;
    - Space's two plain-motion rows (normal, visual) removed.
  - `tests/…Core.UnitTests/Vim/VimLineMotionTests.cs`:
    - `Space_MovesRightAndCrossesALineBreak` replaced by `Space_IsTheLeader_AndMovesNothingOnItsOwn` and
      `TheLeader_EndsWithEscOrAKeyNoMappingStartsWith`;
    - the visual row `v  d` is now `v<CR>d`;
    - the class remarks.
  - `tests/…Core.UnitTests/Vim/VimWindowCommandTests.cs`:
    - each key raises its window command and leaves the text;
    - with a count;
    - in visual mode;
    - `.` does not repeat it.
  - `tests/…Desktop.UnitTests/ViewModels/WindowCommandTests.cs`:
    - the commands at a step of 4, from either pane, and a no-op without a split on the axis;
    - typed in the real window: `Space rk`/`rj` from the top and bottom panes, `Space rl`/`rh` from the left and right
      panes; the text is untouched; `g>` still widens the active pane.
- `docs/roadmap.md`, `docs/plan/FEATURE-216A.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the `k`→down mapping lives | `VimEngine.SplitterCommandFor`, one switch | One place to swap `j`/`k` if the direction was a slip in the request | Spreading it over the parser and the handler |
| Leader table type | `Dictionary<string, VimCommand>`, prefix-matched | Ordinal lookup and `StartsWith`; four entries | A switch per key (not a table); `FrozenDictionary` (new to the codebase for four entries) |
| Help wording | "Move the bar between side-by-side / stacked panes …, from either pane" | "Bar" is the word `SplitAxis` uses; "from either pane" is the point of the keys | "Splitter", "separator" |
| The visual row `v  d` | Rewritten as `v<CR>d`, same before and after | The theory is about a line motion crossing a break over a selection. Space is not a motion there any more; Enter is | Deleting the row |

## Deviations & follow-ups

- **None from the plan.**
- **Fix cycles: 1.** The visual-mode row `v  d` relied on Space extending a selection, which the plan removes on
  purpose. The row was rewritten with Enter, as above. The product code was green on the first run.
- **Direction of `j`/`k`:** as the request states it twice, `k` moves the bar down and `j` up. This is the opposite
  of Vim's `k`-is-up. If that was a slip, swapping the two letters in `SplitterCommandFor` is the whole change. The
  help, the README and the tests then follow the swap.
- **Behaviour change:** a bare Space no longer moves right in normal and visual mode. `l` (and Backspace leftwards)
  remain, and `d<Space>` is unchanged. FEATURE-3F16's release notes carry it under *Upgrading from 1.2*.
- **Hex view — not tested:** reading `HexView.OnRowsTextInput`, the leader keys should work there too. It hands Space
  to the engine, and every key after it while a sequence is pending. No test covers it. The `g` pane keys do not work
  there, because the hex list takes `g` first. That is unchanged and out of scope.
- **Follow-ups (out of scope):**
  - a configurable leader;
  - user-defined leader mappings;
  - a count multiplying the step.
- **Line endings:** every touched file is LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the *Tabs and panes* paragraph lists the four leader keys and says Space is the leader.
- Nothing else names Space as a motion. `docs/RELEASE.md`, `SECURITY.md` and the Vim engine's provenance README are
  still true. The release notes belong to FEATURE-3F16.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2676 passed**, 0 failed, 0 skipped. That is +26 on 2650: 36 cases
  added, 10 cases of Space's old motion removed or rewritten.

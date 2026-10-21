# FEATURE-216A — Space leader: move the pane splitter

**Status:** DONE — see `docs/done/FEATURE-216A.md`
**Type:** FEATURE
**Branch:** `feature/feature-216a-leader-resize`
**Run:** feature/2026-10-21-leader-resize-release

## Objective

Resize panes without `+`, `-`, `<` and `>`, which are awkward on a Swiss French keyboard:

- a **leader key**, Space;
- **`<Space>rh`, `<Space>rj`, `<Space>rk`, `<Space>rl`** move the bar between two panes.

They move the **bar**, not the active pane's edge. Which pane has the keyboard does not change where the bar goes:

- `<Space>rk` moves the bar **down**: the top pane grows and the bottom one shrinks, typed in either of them;
- `<Space>rj` moves it **up**;
- `<Space>rl` moves it **right**: the left pane grows and the right one shrinks, typed in either of them;
- `<Space>rh` moves it **left**.

## Context & constraints

- **Today's resize keys** grow or shrink the active pane, as Vim's do:
  - `Ctrl+W + - > <` and their twins `g+ g- g> g<` (FEATURE-2860) go through `VimWindowKeys.Map` to
    `VimWindowCommand.Taller/Shorter/Wider/Narrower`;
  - `WindowCommandHandler` calls `WorkspaceViewModel.Resize(axis, ±ResizeStep)`;
  - that calls `LayoutTree.Resize(pane, axis, delta)`, which moves the nearest split on the axis that contains the
    pane, towards the pane's side.
- **The step** is `WorkspaceViewModel.ResizeStep`: the *Pane Resize Step* setting (1–50 %, default 5) / 100.
- **The ratio** is `SplitNode.Ratio`, the first (left or top) side's share, clamped to 0.1–0.9. A positive delta on it
  moves the bar right or down.
- **Space** is Vim's "right, wrapping onto the next line" motion today (`KeySequenceParser.MotionFor`,
  `VimMotion.RightWrapping`):
  - in normal mode, in the visual modes, and behind an operator (`d<Space>`);
  - covered by `VimLineMotionTests.Space_MovesRightAndCrossesALineBreak` and the parser's motion theories.
- **Keys arrive as text input** (`VimEditorBehavior`, `HexView`), so `<Space>` is the character `' '` whatever the
  keyboard layout.
- **The help** (`HelpCatalog`) must have a line for every `VimWindowCommand`. `HelpTests.EveryWindowCommand_HasALine`
  enforces it.

## Decisions

| Decision | Rationale |
|---|---|
| The leader is Space, fixed | The draft's key; Vim's most common `mapleader`. A setting would be gold-plating |
| Space opens the leader in normal and both visual modes when no operator is waiting; behind an operator it is still the motion (`d<Space>`) | A Vim leader mapping is a normal/visual mapping (`nnoremap`/`xnoremap`), and operator-pending mode keeps Space. The pane keys (`gh`, `g+`) also work from a selection |
| A bare Space waits; a key that no leader mapping starts with makes the whole sequence dead | Same rule as every other prefix (`g`, `z`, `Z`). Vim's fallback of replaying Space as a motion would need the parser to emit two commands, or a timeout |
| A table of leader mappings in the parser (`"rh"`, `"rj"`, `"rk"`, `"rl"`), prefix-matched | "Create a leader key": the next leader mapping is one line |
| `k` moves the bar down (top pane grows), `j` up, `l` right (left pane grows), `h` left | Stated twice by the draft for `k` and for `l`; `j` and `h` are their opposites |
| The bar moved is the nearest split on that axis containing the active pane, in the same direction from either side of it | The split `Ctrl+W +` already picks, so the two families act on the same bar |
| No split on that axis: nothing happens, silently | As `Ctrl+W +` and `gj` at the window's edge |
| `g+ g- g> g<` and `Ctrl+W + - > <` are unchanged | The draft's behaviour change is for `<Space>r`. Those keys are Vim's own and stay "grow this pane" |
| A count is accepted and ignored; `.` does not repeat them; `"a` in front is a dead sequence | Same as `gh` and `g+` |
| One action `VimAction.MoveSplitter`, carrying `h`/`j`/`k`/`l` as its argument; four window commands `MoveSplitterLeft/Down/Up/Right` | Same shape as `PaneCommand`; the window commands keep the application side in the existing handler |
| The step is the *Pane Resize Step* setting | One step for every resize key |

## Steps

1. **Core layout** — `Layout/LayoutTree.cs`: `MoveSplitter(PaneId pane, SplitAxis axis, double delta)`.
   - Find the nearest split on `axis` containing the pane and add `delta` to its ratio, whichever side the pane is on.
   - Return `false` when there is no such split.
   - It shares the nearest-split lookup with `Resize`.
2. **Core Vim**:
   - `VimWindowCommand.cs`: `MoveSplitterLeft`, `MoveSplitterDown`, `MoveSplitterUp`, `MoveSplitterRight`;
   - `VimAction.cs`: `MoveSplitter`;
   - `KeySequenceParser.cs`:
     - the leader state and its mapping table;
     - Space opens it when no operator is waiting;
     - `PrefixPending` and `Reset` include it;
     - the class remarks describe it;
   - `VimEngine.cs`: `MoveSplitter` maps its argument to the window command and raises `WindowCommandRequested`.
3. **Desktop**:
   - `WorkspaceViewModel.MoveSplitter(axis, delta)`;
   - `WindowCommandHandler`: the four commands, by `±ResizeStep` (down/right positive).
4. **Help** — `HelpCatalog`:
   - two Panes rows: `Space rh, Space rl` (left, right) and `Space rk, Space rj` (down, up);
   - the Moving row `Space, Backspace` loses Space.
5. **README** — the *Tabs and panes* paragraph names the leader keys and says Space is the leader.
6. **Tests**:
   - core parser:
     - `<Space>rh/rj/rk/rl` complete `MoveSplitter` in normal and both visual modes;
     - `<Space>` and `<Space>r` are pending;
     - `<Space>x` and `<Space>rx` are dead;
     - a count is accepted;
     - `"a<Space>rk` is dead;
     - `d<Space>` is still the operator over `RightWrapping`;
   - core engine:
     - each key raises its window command and leaves the text and the caret alone;
     - `.` does not repeat it;
     - a bare Space no longer moves the caret;
     - the line-motion test for Space moves behind the operator;
   - core layout: `MoveSplitter` from either side moves the ratio the same way; nested splits pick the nearest one;
     clamped; `false` without a split;
   - desktop (typed in the real window):
     - top and bottom pane: `<Space>rk` gives 0.55 from both, `<Space>rj` 0.45;
     - left and right pane: `<Space>rl` gives 0.55 from both, `<Space>rh` 0.45;
     - the text is untouched.

## Acceptance criteria

- `<Space>rk` typed in the top or in the bottom pane makes the top pane taller by the resize step. `<Space>rj` does
  the opposite.
- `<Space>rl` typed in the left or in the right pane makes the left pane wider by the resize step. `<Space>rh` does
  the opposite.
- `g+ g- g> g<` and `Ctrl+W + - > <` behave as before.
- `d<Space>` still deletes one character, wrapping, as before.
- The help lists the four keys. The README mentions them.
- Zero warnings, whole suite green.

## Out of scope

- A configurable leader (`mapleader`), user-defined leader mappings, or a timeout.
- A count multiplying the step.
- Changing `g+ g- g> g<` or `Ctrl+W + - > <`.
- Making the `g` pane keys work in the hex view.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Direction of `k`/`j` | `k` moves the bar down (top pane grows), `j` up | The draft says it twice, for both panes, with its reason ("k means move line to the bottom") | `k` up as in Vim's motions — contradicts the draft's explicit examples; a one-line swap in the engine if wanted |
| A bare Space | The leader prefix in normal and visual mode; dead on an unmapped key; still the motion behind an operator | A leader must own the key. Vim's own leader mappings leave operator-pending mode alone. `l` and Backspace remain | Vim's replay-on-mismatch (two commands from one key); a timeout (timers in the engine); keeping Space a motion and using another leader (the draft names Space) |
| Visual mode | The leader works there too | The pane keys `gh`, `g+` already work from a selection | Normal mode only |
| Which bar in nested layouts | The nearest split on the axis containing the active pane | The one `Ctrl+W +` resizes; predictable | The bar under the pane's edge in the key's direction (two different bars for `rk`/`rj`) |
| Old resize keys | Unchanged | The draft's behaviour change is about `<Space>r`; `Ctrl+W +` is Vim's | Removing `g+ g- g> g<`; giving them the new meaning |
| Leader configurable | No, fixed Space, a mapping table in the parser | No request for it; the table keeps it extensible | A `mapleader` setting or `:let mapleader` |
| Step | *Pane Resize Step* | One step for every resize key | A separate setting |
| Work-item shape | One single-phase FEATURE | Parser, engine, layout and handler are one usable unit; a split would merge dead code | Two phases (window commands, then keys) |

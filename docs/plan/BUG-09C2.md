# BUG-09C2 — Space rj/rk move the bar inverted

**Status:** TODO
**Type:** BUG
**Branch:** `bugfix/bug-09c2-resize-jk`
**Run:** bugfix/2026-10-21-resize-jk-release

## Objective

`<Space>rj` and `<Space>rk` move the bar between two stacked panes the wrong way. In Vim `j` is down and `k` is up,
so:

- `<Space>rj` moves the bar **down**: the top pane grows and the bottom one shrinks, typed in either of them;
- `<Space>rk` moves it **up**: the bottom pane grows.

`<Space>rh` and `<Space>rl` already go the way they point and stay as they are.

## Context & constraints

- **The mapping** is one switch: `VimEngine.SplitterCommandFor` maps the letter after `<Space>r` to a window command.
  Today it sends `j` to `MoveSplitterUp` and `k` to `MoveSplitterDown`. FEATURE-216A followed its draft's wording
  ("k moves the line to the bottom"). Its completion doc names this switch as the whole fix if that was a slip.
- **The parser** (`KeySequenceParser.LeaderMappings`) carries the letter as the command's argument. It knows no
  direction, so it does not change.
- **The window commands** are named by direction (`MoveSplitterDown` / `MoveSplitterUp`), and
  `WindowCommandHandler` moves the bar by `±ResizeStep` from them. Neither changes. Only their doc comments name the
  wrong key.
- **The help** (`HelpCatalog`, *Panes*) and the **README** (*Tabs and panes*) both say `Space rk` is down.
- **Tests that pin today's direction:**
  - `VimWindowCommandTests` — the leader theory and the visual / dot-repeat test;
  - `WindowCommandTests.TypedInTheWindow_SpaceRKAndRJ_…` — typed in the real window.
- `RELEASENOTES.md`'s 1.3.0 section describes what 1.3.0 shipped. FEATURE-6622's 1.4.0 section tells of the change.

## Decisions

| Decision | Rationale |
|---|---|
| `j` → `MoveSplitterDown`, `k` → `MoveSplitterUp`, in `SplitterCommandFor` | The request; the one place the direction lives |
| `h` and `l` unchanged | They already point the way they move; the request names `j`/`k` only |
| The enum keeps its names and its order (Left, Down, Up, Right) | Named by direction, so the names stay true; the order now reads h, j, k, l |
| The help row becomes `Space rj, Space rk` → "down, up" | Reads in the same order as `gh, gj, gk, gl` |
| `Ctrl+W + -` and `g+ g-` unchanged | Out of the request; they grow the active pane, as Vim's do |
| The 1.3.0 release notes stay as they are | They describe the 1.3.0 product |

## Steps

1. `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `SplitterCommandFor`: `j` down, `k` up; its remarks say every
   letter goes the way Vim's motion does.
2. `src/Enigma.TextEditor.Core/Vim/VimWindowCommand.cs` — the `MoveSplitterDown` / `MoveSplitterUp` doc comments name
   `rj` / `rk`.
3. `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the stacked-panes row is `Space rj, Space rk`, down and
   up.
4. `README.md` — the *Tabs and panes* sentence says `Space rj` and `Space rk` move the bar down and up.
5. Tests:
   - `VimWindowCommandTests`: the theory maps `rj` to down and `rk` to up, and the count row and the dot-repeat test
     follow;
   - `WindowCommandTests`: the in-window test types `rj` to grow the top pane and `rk` to grow the bottom one, from
     both panes, and is renamed after them.

## Acceptance criteria

- `<Space>rj`, typed in the top or the bottom pane, makes the top pane taller by the resize step. `<Space>rk` makes
  it shorter.
- `<Space>rh` and `<Space>rl`, `g+ g- g> g<` and `Ctrl+W + - > <` behave as before.
- The help and the README name `Space rj` as down and `Space rk` as up.
- Zero warnings, whole suite green.

## Out of scope

- Any other leader mapping, or a configurable leader.
- Rewording the 1.3.0 release notes or FEATURE-216A's records.
- The release (FEATURE-6622).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Item type | BUG | The keys contradict the stated intent (Vim's `j` down, `k` up) | FEATURE (no new capability) |
| `rh` / `rl` | Unchanged | Already Vim-consistent; not named by the request | Swapping them too |
| Where to swap | `VimEngine.SplitterCommandFor` | The one switch that holds the direction | Swapping the parser's letters (the argument would lie); swapping the handler's signs (the enum names would lie) |
| Enum | Names and order kept, comments fixed | The names are directions and stay true | Renaming the commands after keys |
| Help order | `Space rj, Space rk` — down, up | Same order as the `gj, gk` rows | Keeping `rk` first |
| 1.3.0 notes | Untouched | Historical, true of 1.3.0 | Rewriting them |
| Work-item shape | One single-phase BUG | One reviewable commit | Folding it into the release |

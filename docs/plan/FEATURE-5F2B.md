# FEATURE-5F2B — Pane focus with gh, gj, gk, gl

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5f2b-g-pane-focus`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

In the command modes, `gh`, `gj`, `gk` and `gl` move the keyboard to the pane to the left, below, above
and to the right — the same as `Ctrl+W h`, `j`, `k`, `l`.

## Context & constraints

- Today `g` followed by `h`, `j`, `k` or `l` is a dead sequence: `KeySequenceParser.SecondG` knows only
  `gg`, `gu`/`gU`/`g~`, `gJ` and `gv`.
- Vim's own `gj`/`gk` (move by display line) and `gh` (select mode) are not implemented here, so
  nothing is lost.
- Window commands leave the engine through `VimEngine.WindowCommandRequested`.
  `WindowCommandHandler` turns `FocusLeft`/`FocusDown`/`FocusUp`/`FocusRight` into
  `WorkspaceViewModel.Focus(direction)`. A direction with no pane does nothing.
- `HelpCatalog` documents every window command, and the help tests check it.

## Decisions

| Decision | Rationale |
|---|---|
| The parser completes `gh`/`gj`/`gk`/`gl` as one new `VimAction` that carries its letter | Keeps the parser free of window knowledge; the engine maps the letter through `VimWindowKeys.Map`, so `g{x}` and `Ctrl+W {x}` cannot drift apart |
| Works in normal and both visual modes | `Ctrl+W h` already works in every command mode; the `g` form behaves the same |
| Behind an operator (`dgj`) the sequence does nothing | It is a command, not a motion |
| A count in front is accepted and ignored | Same as `Ctrl+W h` here |
| Not a change: `.` does not repeat it. A macro records the keys | Same as `Ctrl+W` |

## Steps

1. `Core/Vim/VimAction.cs`: add `FocusPane`, with a doc comment; the argument is the `h`/`j`/`k`/`l`.
2. `Core/Vim/KeySequenceParser.cs`: in `SecondG`, after the operator dead end, `h`/`j`/`k`/`l`
   complete `FocusPane` with the letter. Update the class and method remarks.
3. `Core/Vim/VimEngine.cs`: `case VimAction.FocusPane` raises `WindowCommandRequested` with
   `VimWindowKeys.Map` of the letter.
4. `Desktop/ViewModels/HelpCatalog.cs`: the Panes focus line also lists `gh, gj, gk, gl`.
5. Tests:
   - Core parser: `gh`/`gj`/`gk`/`gl` complete `FocusPane` in normal and visual; `dgj` is dead.
   - Core engine: each raises the matching `VimWindowCommand`, the buffer and caret are untouched, and
     visual mode works.
   - Desktop: typed in a split window, `gl`/`gh` move the active pane (view-model level).

## Acceptance criteria

- `gh`, `gj`, `gk`, `gl` focus the neighbouring pane in that direction; nothing happens at an edge.
- They work in normal and visual mode. They do nothing behind an operator, and type text in insert
  mode.
- The help lists them.
- Zero warnings, whole suite green.

## Out of scope

- Display-line motions (`gj`/`gk` in Vim's sense) and select mode (`gh`).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which pane each key goes to | h left, j down, k up, l right | The same as `Ctrl+W h/j/k/l` and Vim's direction keys | — |
| How the key reaches the panes | New `VimAction.FocusPane` → `WindowCommandRequested` via `VimWindowKeys.Map` | One mapping shared with `Ctrl+W`; no new event | A second event; mapping in the Desktop layer |
| Modes | Normal and visual | Matches `Ctrl+W` in this engine | Normal only |

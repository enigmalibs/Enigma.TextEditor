# BUG-C6A9 — Ctrl+Y does not redo

**Status:** DONE
**Type:** BUG
**Branch:** `bugfix/bug-c6a9-ctrl-y-redo`
**Run:** bugfix/2026-11-09-ctrl-y-session-restore

## Objective

Edit → Redo (and the editor's context menu) shows `Ctrl+Y` as its shortcut, but with vim on, in normal and visual
mode, `Ctrl+Y` scrolls the view up a line instead of redoing. Only `Ctrl+R` (and `Ctrl+Shift+Z`) redo there. `Ctrl+Y`
must redo in every mode, vim on or off.

## Context & constraints

- `MainWindow.axaml:184` and `EditorPaneView.axaml:208`: Redo carries `InputGesture="Ctrl+Y"` (display only in
  Avalonia) and `views:VimHint.Keys="Ctrl+R"`.
- With vim on, `VimEditorBehavior.Translate` turns a Control+letter key into a `VimKey`, and `VimEngine.Dispatch`
  decides:
  - insert mode: `IsInsertRedoKey` (`Ctrl+Y`) → `Step(forward: true)` — redo already works there (BUG-375B);
  - the command modes: `KeySequenceParser.IsControlCommand` claims `Ctrl+Y` as `VimAction.ScrollLineUp`
    (`KeySequenceParser.cs:510`). BUG-375B decided "`Ctrl+Y` stays scroll-up" there. That is the bug.
- `Ctrl+Shift+Z` (`IsRedoKey`) is already "the editor's redo in every mode": `Step(forward: true)`. A visual mode is
  left first; the open `:` line turns it down. `FoldRedoKey` writes it into a recording as `RedoKey`.
- Precedent: 1.17 gave vim's `Ctrl+F` (page down) up to the editor's find (BUG-B026); `PgDn` still pages.
- Vim off: AvaloniaEdit's own key bindings. Whether it binds `Ctrl+Y` to redo is to be verified by a headless test.
- Tests encoding the old behaviour: `VimUndoTests.cs:190` (`x<C-y>` is not a redo in normal mode),
  `VimScreenMotionTests.cs` (`<C-y>` scroll rows near lines 277, 301, 320 and 524), `KeySequenceParserTests.cs:498, 545`.

## Decisions

- **`Ctrl+Y` is the editor's redo in every mode**, exactly as `Ctrl+Shift+Z`: the engine takes it as the redo key
  everywhere, not only in insert mode.
- **Vim's `Ctrl+Y` scroll is given up.** It leaves the grammar's control commands. `VimAction.ScrollLineUp` and the
  code only it reaches are removed (`Ctrl+E` keeps scrolling down).
- **Recording:** `Ctrl+Y` is folded into `RedoKey` like `Ctrl+Shift+Z`, so a macro replays a redo.
- **Vim off:** if AvaloniaEdit does not redo on `Ctrl+Y`, the pane view binds it to the editor's redo.

## Steps

1. Core `KeySequenceParser`: drop `'y'` from `ControlActionFor`; the doc comments go from six scroll commands to five.
2. Core `VimEngine`: `Ctrl+Y` folds into `RedoKey` (`FoldRedoKey`) and is handled by the redo-key paths in every mode;
   remove `IsInsertRedoKey`, `VimAction.ScrollLineUp` and code only it reaches; update the remarks on the undo keys.
3. Desktop `VimEditorBehavior` remarks (the scroll keys list); `HelpCatalog`: *Scrolling* lists `Ctrl+E` alone,
   *Editing* lists `Ctrl+Y` with redo.
4. Vim off: a headless test of `Ctrl+Y`; add a binding only if it fails.
5. Tests: update the tests that encode the old behaviour, add the new ones below.

## Acceptance criteria

- Core: `|abcde` + `xu<C-y>` → `|bcde` (normal mode); `<C-y>` in a visual mode leaves it and redoes; insert mode
  unchanged (`iXY<C-z><C-y>` → `aXY|bc`); `<C-y>` with nothing to redo changes nothing and does not scroll; `<C-e>`
  still scrolls; a recording `qa…<C-y>q` replayed with `@a` redoes.
- Desktop headless, vim on: in normal mode, type, `u`, then the `Ctrl+Y` key → the change is back. Vim off: `Ctrl+Y`
  redoes too.
- *Help → Key Bindings* lists `Ctrl+Y` as redo and no longer as a scroll.
- Zero warnings, whole suite green.

## Out of scope

- The menu's displayed gestures and vim hints (unchanged: `Ctrl+Y`, hint `Ctrl+R`).
- Vim's insert-mode `Ctrl+Y` (copy the character above), never supported.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `BUG` | The menu promises `Ctrl+Y` and the key does something else | `FEATURE` |
| `Ctrl+Y` in normal/visual mode | Redo, as `Ctrl+Shift+Z` | The user asked for it; the menu shows it; same trade as `Ctrl+F` in 1.17 | Keep the scroll and relabel the menu (not what was asked); redo only with vim off |
| Vim's scroll-line-up | Removed with its action | No other key reaches it; dead code otherwise | Moving it to another key (vim has none) |
| Recording | Fold into `RedoKey` | One notation for every redo key, as `Ctrl+Shift+Z` already does | Recording `<C-y>` verbatim |
| Vim off | Verify, bind only if missing | AvaloniaEdit may already bind it | Always binding (could double-fire) |

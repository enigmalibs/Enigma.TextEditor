# BUG-47C2 — Mouse clicks in vim normal mode

**Status:** IN PROGRESS
**Type:** BUG
**Branch (PHASE01):** `bugfix/bug-47c2-phase01-click-line-end`
**Branch (PHASE02):** `bugfix/bug-47c2-phase02-double-click-visual`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Objective

Two mouse gestures leave vim's normal mode in the wrong state:

1. **A click past the end of a line** puts the block caret after the last character, as if the line ended with a space.
   `x` from there deletes the last character, the one to the left of the block. In normal mode the block must sit on
   the last character.
2. **A double-click on a word** selects the word but stays in normal mode. It must be in visual mode.

## Context & constraints

- AvaloniaEdit's `SelectionMouseHandler` handles presses on the `TextArea`. A click sets `CaretOffset` (after the last
  character when the click lands past a line's end) and clears the selection. A double-click (`ClickCount == 2`)
  selects the word, with the caret at its end, and a drag after it extends the selection by words.
- The engine never hears about a pointer gesture. `VimEngine.Execute` clamps the caret at the start of every command
  (`SetCaret(_buffer.CaretOffset)`, comment at `VimEngine.cs:678-683`). That is why the block is drawn after the line
  and `x` then deletes the character before it. The desired column (`_desiredColumn`) is also left stale by a click.
- The block caret is AvaloniaEdit's overstrike caret (`VimEditorBehavior.ApplyMode`). It is drawn over whatever
  `CaretOffset` is, so clamping the offset fixes the drawing.
- In visual mode the caret sits on the selection's last character, and the editor's selection is shown from
  `_visualAnchor` to `caret + 1` (`ShowSelection`, `VisualRange`). `SelectTextObject` (`viw`) is the closest existing
  path: anchor at the word start, caret on its last character.
- A drag selection in normal mode deliberately stays in normal mode (`Esc` "also drops a selection made with the mouse
  while in normal mode", `VimEngine.cs:499`).
- Desktop tests can click with `window.MouseDown` / `MouseUp`, twice for a double-click (see
  `FileExplorerViewTests.DoubleClickingAFile_…`). Core tests use `FakeVimBuffer` / `VimScript`.

## Decisions

- **Engine API, view plumbing.** The Core engine gets two public methods. `VimEditorBehavior` calls them from pointer
  handlers it adds to the editor (bubbling, `handledEventsToo: true`, so they run after AvaloniaEdit's), attached and
  detached with the key handlers in `Refresh`. Only presses on the text itself count (not the line-number margin, the
  scrollbars or the search panel).
- **`VimEngine.PlaceCaret()`** (PHASE01): "the pointer put the caret down". Outside insert mode and the command line,
  it leaves a visual mode, clamps the caret to normal mode's rule and forgets the desired column. It is called when a
  left-button single-click gesture is released with no selection.
- **`VimEngine.AdoptSelection(int anchor, int active, bool linewise)`** (PHASE02): "the pointer made this selection".
  Outside insert mode and the command line, it enters charwise (or linewise) visual mode over it, with the caret on the
  selection's last character (or its first, for a backward selection). It is called on the press and on the release of
  a double-click gesture, and of a triple-click gesture when AvaloniaEdit selected whole lines.
- **Insert mode is unchanged** for both gestures. Vim off is unchanged.

## PHASE01 — Click past a line end in normal mode

**Status:** DONE

### Steps

1. Core `VimEngine.PlaceCaret()`: no-op in insert mode and while the command line is open. Otherwise it calls
   `LeaveVisual()` when in a visual mode, then `SetCaret(_buffer.CaretOffset)`, and sets
   `_desiredColumn = NoDesiredColumn`.
2. `VimEditorBehavior`: `PointerPressed` / `PointerReleased` handlers on the editor (bubble, `handledEventsToo`). The
   press remembers the gesture's `ClickCount` for a left button on the text. On release of a single-click gesture
   (`ClickCount == 1`, no Shift), if the editor's selection is empty, it calls `_engine.PlaceCaret()`.
3. A helper deciding "on the text": the event source is the `TextArea`'s `TextView` or inside it, and not inside the
   search panel.
4. Remarks: `VimEditorBehavior` gets a paragraph on pointer gestures. Fix the `Execute` comment in the engine (a click
   is now clamped when it happens; the clamp in `Execute` stays as a safety net).

### Acceptance criteria

- Core: caret past the end of `abc` (offset 3 on a two-line buffer) + `PlaceCaret()` → caret 2, mode NORMAL. Then `x`
  → `ab`. In visual mode `PlaceCaret()` → NORMAL, no selection. On an empty line the caret stays at the line start. In
  insert mode → nothing changes (caret 3 kept). After a click, `j` keeps the clicked column.
- Desktop headless, vim on, normal mode: a click to the right of the end of the first line leaves `CaretOffset` on its
  last character. `x` then deletes that character. In insert mode the same click leaves the caret after the last
  character.
- Zero warnings, whole suite green.

## PHASE02 — Double-click selects in visual mode

**Status:** TODO

### Steps

1. Core `VimEngine.AdoptSelection(int anchor, int active, bool linewise)`: no-op in insert mode, while the command line
   is open, and for an empty selection.
   - Forward selection (`active > anchor`): `_visualAnchor = anchor` and caret `active - 1`.
   - Backward selection: `_visualAnchor = anchor - 1` and caret `active`.
   - Then `SetMode(linewise ? VisualLine : Visual)`, `ShowSelection()`, and the desired column is forgotten.
2. `VimEditorBehavior`: on a press with `ClickCount >= 2` on the text, and again on that gesture's release, read the
   `TextArea.Selection` (`SurroundingSegment`, and whether the caret is at its start, to get the direction). If it is
   not empty, call `AdoptSelection`, linewise when the gesture is a triple-click and the selection covers whole lines.
   Otherwise call `PlaceCaret()`.
3. Check AvaloniaEdit 12's triple-click behaviour in a headless test. If it does not select whole lines, a triple-click
   is adopted charwise like a double-click (record which in the done doc).
4. Help (`HelpCatalog`, *Visual mode* section): add "Double-click — Select the word in visual mode".

### Acceptance criteria

- Core: `AdoptSelection(4, 7, false)` on `foo bar baz` → mode VISUAL, caret 6, editor selection `[4, 7)`. Then `d` →
  `foo  baz`. Backward `AdoptSelection(7, 4, false)` → caret 4, the same selection. `linewise: true` → VISUAL LINE. An
  empty selection or insert mode → no change.
- Desktop headless, vim on, normal mode: a double-click on `bar` → mode VISUAL and the selection is `bar`. `y` yanks
  `bar` and returns to NORMAL. `Esc` leaves visual mode. A single click elsewhere after it leaves visual mode (PHASE01).
- In insert mode a double-click still selects the word and stays INSERT. Vim off is unchanged.
- Zero warnings, whole suite green.

## Out of scope

- A drag selection entering visual mode (it stays a normal-mode mouse selection, as designed). A drag while already in
  a visual mode keeps today's behaviour.
- Clicks on the line-number margin (they select lines in AvaloniaEdit), Shift+click, middle click.
- The hex view's own pointer handling.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `BUG` | Both gestures leave vim in a state its own keys could never reach | `FEATURE` for the double-click |
| Shape | One item, two phases: click, then double-click | Both need the same pointer plumbing in `VimEditorBehavior`. PHASE01's "a click leaves visual mode" is what makes PHASE02's visual mode safe to get out of with the mouse | Two items (shared plumbing split across plans); one phase (two bugs in one commit) |
| Where the fix lives | Two public engine methods + pointer handlers in the view | The engine owns the caret rule, the visual anchor and the mode. The view only reports gestures. Core methods are unit-testable without Avalonia | Clamping `CaretOffset` in the view (duplicates the rule, leaves visual state stale); a caret-changed subscription in the engine (fires for the engine's own moves too) |
| When to clamp | On release of a single click that left no selection | Clamping on press would shift where a drag starts. A drag's selection must not be desynced from its caret | On press; on every caret change |
| A click while in visual mode | Leaves visual mode | Vim (`mouse=a`) does the same. Without it, the anchor of a double-click selection would survive a click elsewhere | Keeping visual mode with a stale anchor |
| Triple-click | Linewise visual if AvaloniaEdit selects whole lines | It is the continuation of the double-click gesture. Not handling it would leave the engine in VISUAL with the word's anchor | Ignoring it |
| Drag selections | Unchanged, follow-up | The draft asks for the double-click only, and the engine deliberately keeps drags in normal mode | Making every mouse selection visual |
| Insert mode | Unchanged | The draft is about normal mode, and insert mode is ordinary typing | gVim's insert-visual mode |

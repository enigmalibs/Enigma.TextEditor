# BUG-375B — Ctrl+Z in insert mode crashes the editor

**Status:** TODO
**Type:** BUG
**Branch:** `bugfix/bug-375b-insert-mode-undo`
**Run:** bugfix/2026-11-04-bug-375b

## Objective

With vim on, `Ctrl+Z` in INSERT mode shows the "Something went wrong" popup
(`InvalidOperationException: No undo group should be open at this point`), and the next `Esc` shows a second one
(`There are no open undo groups`, from `VimEngine.LeaveInsert`). `u` never crashes. Make every undo/redo route go
through the vim engine while vim is on, so that none of them can reach AvaloniaEdit's undo stack while an undo group is
open.

## Root cause

- An insert session holds one undo group open from `i` to `Esc` (`VimEngine.BeginInsertGroup`, `VimEngine.cs:2896`;
  `AvaloniaEditVimBuffer.BeginUndoGroup`), so that the whole session is one `u`.
- `u` is the engine's (`VimEngine.Rewind`) and only exists in NORMAL mode, where the group is closed.
- `Ctrl+Z` is not the engine's. In INSERT, `VimEngine.Dispatch` takes only `Esc`; every other key goes to AvaloniaEdit,
  whose `ExecuteUndo` calls `UndoStack.Undo()` when `CanUndo`. AvaloniaEdit's `ThrowIfUndoGroupOpen` **sets the group
  depth to 0, then throws**. "Sometimes" = only when the undo stack is not empty (anything typed in the session counts).
- The engine still believes the group is open. `Esc` → `EndUndoGroup` → AvaloniaEdit throws on a depth of 0, and
  `LeaveInsert` stops halfway: the mode stays INSERT until a second `Esc`.
- The same crash happens through: redo (`Ctrl+Y`, `Ctrl+Shift+Z`) in INSERT when something can be redone (e.g. `u`,
  `i`, `Ctrl+Y`); the Edit menu and the right-click Undo/Redo (`MainWindow.axaml.cs:326-328`,
  `EditorPaneView.axaml.cs:640-642` call `editor.Undo()/Redo()` directly); and "Reopen with encoding" in INSERT
  (`DocumentService.ReopenWithEncodingAsync` → `DocumentViewModel.ApplyReload` → `UndoStack.ClearAll()`).

## Decisions

- **Ctrl+Z in INSERT: undo, stay in INSERT** (gVim's `mswin.vim` `<C-O>u`). The engine closes the session's group (what
  was typed so far becomes its own step), undoes one step, and opens a new group for what is typed next. Another
  `Ctrl+Z` goes further back. `Ctrl+Y` / `Ctrl+Shift+Z` in INSERT redo the same way.
- **One undo path while vim is on.** NORMAL: `Ctrl+Z` = `u`, `Ctrl+Shift+Z` = `Ctrl+R` (so the caret follows `u`'s
  rule); `Ctrl+Y` stays scroll-up. VISUAL: `Ctrl+Z` / `Ctrl+Shift+Z` leave the selection, then undo / redo. The Edit menu
  and right-click Undo/Redo go through the engine too.
- **Reopen with encoding** while in INSERT: the pane resets its engine (as a tab switch does) before the text is
  replaced, so the group is closed first and the editor ends in NORMAL.
- **No safety net** on the closing `Esc`: no caught exception. The test fake refuses undo/redo while a group is open,
  as AvaloniaEdit does, so a missed route fails in the tests.
- Vim off: unchanged (no group is ever opened).

## Design

1. **Engine (`Core/Vim/VimEngine.cs`)**
   - Public `Undo()` and `Redo()`: what every non-`u` route calls, in any mode.
     - INSERT: `EndInsertGroup()`, `_buffer.Undo()` / `Redo()`, `BeginInsertGroup()`; restart the insert record at the
       caret (`_typed.Begin`). The text typed before the undo has been taken back and is dropped from the record; if a
       recording is open, it is written into the macro first (as `Esc` does), so the macro replays it before the key.
     - NORMAL: what `u` / `Ctrl+R` do (`Execute(new VimCommand(VimAction.Undo/Redo))` or `Rewind(1, …)`), with
       `_parser.Reset()`.
     - VISUAL: `LeaveVisual()`, then as NORMAL.
   - `Dispatch`: in INSERT, besides `Esc`, take `Ctrl+Z` (undo), `Ctrl+Y` and `Ctrl+Shift+Z` (redo) and return `true`.
     In the command modes, take `Ctrl+Z` and `Ctrl+Shift+Z` next to the existing `IsRedo` check (`Ctrl+Y` stays the
     scroll command). Recognized on the letter with Control and without Alt, as `IsRedo` does. Meta is already mapped
     to Control by `VimEditorBehavior.Modifiers`.
   - Update the `HandleKey` remarks ("insert mode says false to everything but Esc").
2. **Behavior (`Desktop/Views/VimEditorBehavior.cs`)**: public `Undo()` / `Redo()` that call the engine when attached
   and `_editor.Undo()` / `Redo()` otherwise. Keys need no change: `Translate` already turns `Ctrl+Z` / `Ctrl+Y` into
   `VimKey`s and `OnKeyDown` marks them handled when the engine takes them.
3. **Menus**: `EditorPaneView` gets `Undo()` / `Redo()` (`_vim` when present, else the editor). `OnMenuUndoClick` /
   `OnMenuRedoClick` and `MainWindow.OnUndoClick` / `OnRedoClick` call them on the active pane instead of
   `editor.Undo()` / `Redo()`. `IsEnabled` (`Editor.CanUndo/CanRedo`) unchanged.
4. **Reload**: `DocumentViewModel` raises a new `Reloading` event at the start of `ApplyReload`. `EditorPaneView`
   subscribes on the document it follows (next to `FollowIndentation`'s `PropertyChanged`) and calls
   `_vim?.OnDocumentChanged()`.
5. **Docs**: XML remarks on `IVimBuffer.Undo/BeginUndoGroup`, `AvaloniaEditVimBuffer.Undo`, `VimEditorBehavior` (class
   remarks on the Control keys). Help (`HelpCatalog`, "Inserting"): add `Ctrl+Z, Ctrl+Y` — "Undo, redo without leaving
   insert mode".

## Tests

- **Fake (`Core.UnitTests/Vim/FakeVimBuffer.cs`)**: `Undo()` / `Redo()` throw `InvalidOperationException` while
  `OpenUndoGroups > 0`, mirroring AvaloniaEdit. Every existing test must stay green.
- **Core (`VimUndoTests`)**, with `VimScript` notation (`<C-z>`, `<C-y>`, `<C-S-z>`):
  - `a|bc` + `iXY<C-z>` → `a|bc`, mode INSERT, one group open; then `Q<Esc>u` → `abc` (only `Q` taken back);
  - `ccXYZ<C-z>` restores the line, mode INSERT;
  - `xiQ<C-z><C-z>` takes back `Q` then the `x`;
  - `xu` then `i<C-y>` redoes the `x`, mode INSERT; `<C-S-z>` does the same;
  - NORMAL `<C-z>` = `u` (text and caret); VISUAL `v<C-z>` leaves VISUAL and undoes;
  - NORMAL `<C-y>` still scrolls (existing scroll tests unchanged);
  - `.` after `ihello<C-z>world<Esc>` repeats `world`; `3iab<C-z>c<Esc>` repeats `c`;
  - a recording `qaiXY<C-z>Z<Esc>q` replays with `@a` to the same text;
  - public `engine.Undo()` / `Redo()` in each mode.
- **Desktop headless (`Views/VimEditingTests`)**, real window and keystrokes (`KeyPressQwerty(PhysicalKey.Z, Control)`):
  - the user's scenario: `x`, `i`, type, `Ctrl+Z` → no exception, text undone, INSERT; `Esc` → NORMAL, no exception;
  - `u`, `i`, `Ctrl+Y` → redone, INSERT, no exception;
  - Edit menu Undo (and the context menu's) while in INSERT → undone, no exception;
  - "Reopen with encoding" while in INSERT → NORMAL, no exception, text reloaded;
  - with vim off, `Ctrl+Z` undoes as before.

## Acceptance criteria

- No route (`Ctrl+Z`, `Ctrl+Y`, `Ctrl+Shift+Z`, Edit menu, context menu, Reopen with encoding) throws while in INSERT.
- `Ctrl+Z` in INSERT takes back what the session typed so far and stays in INSERT; `Esc` then `u` takes back only what
  was typed after it.
- In NORMAL, `Ctrl+Z` leaves the text and caret exactly as `u` does; `Ctrl+Y` still scrolls.
- Vim off: undo/redo behave exactly as today.
- Zero warnings, whole suite green.

## Out of scope

- Catching or tolerating AvaloniaEdit's undo exceptions (decision above).
- Vim's own insert-mode `Ctrl+Y` (copy the character above) and `Ctrl+R` (insert a register).
- `Ctrl+Z` while the `:` / `/` command line is open: unchanged.
- Hex view (`HexVimBuffer` has no undo stack).
- Release notes (written by the next release item).

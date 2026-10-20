# FEATURE-5D83 — Editor context menu

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5d83-editor-context-menu`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

A right-click in the text editor opens a context menu with the editing commands: Undo, Redo, Cut,
Copy, Paste, Delete and Select All.

## Context & constraints

- `EditorPaneView` hosts one AvaloniaEdit `TextEditor` (`Editor`) per pane; it has no context menu
  today.
- The Edit menu (`MainWindow.axaml.cs`) calls the control's own methods (`Undo`, `Redo`, `Cut`,
  `Copy`, `Paste`, `SelectAll`) on the active pane's editor, then gives the focus back to the text.
- AvaloniaEdit's `TextEditor` exposes `CanUndo`, `CanRedo`, `CanCut`, `CanCopy`, `CanPaste`,
  `CanDelete`, `CanSelectAll` and the matching methods, `Delete` included.
- The hex view is a separate, read-only control (out of scope).
- Vim mode: the Edit menu already acts on the editor directly, whatever the Vim mode; the context menu
  does the same.

## Decisions

| Decision | Rationale |
|---|---|
| Items: Undo, Redo · Cut, Copy, Paste, Delete · Select All, with the Edit menu's gestures shown | The standard text-editor menu; the draft asks for cut/copy/paste "plus useful items" |
| Each item is enabled from the matching `Can*` when the menu opens | Correct state without polling; `Can*` are not observable |
| It acts on the editor that was right-clicked, not the active pane's; the focus then goes back to it | Right-clicking a pane that does not have the keyboard must not paste into another one |
| A `ContextMenu` on the `TextEditor` | Opens on right-click and on the keyboard's context-menu key; easy to drive headlessly |
| Right-click does not move the caret (AvaloniaEdit's default) | Same as the Edit menu, which acts at the caret and selection; moving the caret behind the Vim engine would leave its visual-mode state behind |

## Steps

1. `Desktop/Views/EditorPaneView.axaml`: `<edit:TextEditor.ContextMenu>` with the items, named
   (`ContextUndo` …), `InputGesture`s as in the Edit menu, and `Click` handlers.
2. `Desktop/Views/EditorPaneView.axaml.cs`:
   - `Opening`: set each item's `IsEnabled` from `Editor.Can*`;
   - one handler per item: run the action on `Editor`, then focus `Editor.TextArea`.
3. Tests (headless, real window):
   - the editor has a context menu with the seven items in order;
   - with no selection, Cut, Copy and Delete are disabled; with a selection, they are enabled;
   - Undo is disabled on a fresh document;
   - Delete removes the selection;
   - Select All selects all;
   - Copy then Paste duplicates the selection through the clipboard, if the headless clipboard
     supports it; otherwise Cut removes the selection;
   - in a split, the menu of the non-active pane acts on that pane's text.

## Acceptance criteria

- Right-click in the editor opens the menu; every item does what its Edit-menu twin does.
- Items that cannot run are disabled.
- The menu acts on the pane it was opened in.
- Zero warnings, whole suite green.

## Out of scope

- A context menu for the hex view or the tab strip.
- Moving the caret to the right-click position.
- Case-conversion or whitespace commands.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| "Useful items" beyond cut/copy/paste | Undo, Redo, Delete, Select All | The standard set every editor's context menu has; no new behaviour | Change case / trim whitespace / split commands (new features, not asked) |
| Caret on right-click | Unchanged | Consistent with the Edit menu; avoids desynchronising Vim's visual mode | Move caret outside the selection (follow-up) |
| Target editor | The right-clicked one | Correct in splits | The active pane's |

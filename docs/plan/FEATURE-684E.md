# FEATURE-684E — :q on the last tab keeps the app

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-684e-last-tab-keeps-app`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

Closing the last tab with `:q` no longer quits the application. The tab closes and the pane shows its
placeholder, *No file is open. Use File → Open… or File → New.*

## Context & constraints

- `ExCommandHandler.QuitAsync(pane, tab, discard)` closes the tab, then calls
  `_shutdown.RequestExit()` when no pane has a tab left (`!_workspace.HasTabs`). `:q`, `:q!`, `:wq`,
  `:x` and `Ctrl+W q` all end there. `ZZ`/`ZQ` map to them in the engine.
- Closing a tab from the menu, Ctrl+F4, the tab's × or a middle click never quits. The last pane is
  then left empty with the placeholder (`EditorPaneView`).
- `:qa`, `:qa!` and File → Exit quit.
- The workspace remarks and the help describe `:q` on the last tab as quitting.

## Decisions

| Decision | Rationale |
|---|---|
| Every `:q` form (`:q`, `:q!`, `:wq`, `:x`, `ZZ`, `ZQ`, `Ctrl+W q`) closes the tab and never quits | The draft; the same rule as the other ways of closing a tab |
| `:qa`, `:qa!`, File → Exit and the window's close button still quit | Quitting stays one command away |
| The placeholder text is kept as it is | It is the message the draft quotes |

## Steps

1. `ExCommandHandler.QuitAsync`: drop the shutdown call; class remarks updated.
2. `WorkspaceViewModel` remarks: `:q` on the last tab leaves an empty pane.
3. `HelpCatalog`: the `:q` line says it closes the tab (the app stays open) and points to `:qa`.
4. Tests:
   - updated: tests that expected `:q` on the last tab to request an exit now expect none and an
     empty pane;
   - new: `:wq` and `:x` on the last tab save, close it and do not quit;
   - new: `Ctrl+W q` on the last tab does not quit;
   - new: `:qa` still quits;
   - headless: after `:q` on the last tab the placeholder shows.

## Acceptance criteria

- `:q` (and its variants) on the last tab leaves the window open with the empty-pane message.
- `:qa` still quits.
- Zero warnings, whole suite green.

## Out of scope

- Typing ex commands in an empty pane (the editor is hidden there).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which commands change | Every `:q` variant, `ZZ`/`ZQ`, `Ctrl+W q` | They are all "close this tab"; one rule | Only the bare `:q` |
| How to quit from the keyboard now | `:qa`, Alt+F4 | Unchanged, already documented | A second `:q` in an empty pane |

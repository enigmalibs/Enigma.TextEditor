# FEATURE-4EAA — :enew and :enew!

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-4eaa-enew`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

Add Vim's `:ene[w]` and `:ene[w]!`:
- `:enew` opens a new untitled buffer in the pane.
- `:enew!` puts a new untitled buffer in place of the current one, throwing away that buffer's unsaved
  changes.

## Context & constraints

- Ex requests that leave the engine are listed in `ExCommandParser.Definitions`, raise a `VimExCommand`,
  and run in `ExCommandHandler.ExecuteAsync`.
- In this editor a pane's tabs are its buffer list: `:e file` adds or shows a tab rather than replacing
  the current one. This is Vim with `hidden` set, which is Neovim's default.
- Vim: `:enew` edits a new unnamed buffer. `:enew!` does the same and discards any changes to the
  current buffer.
- `EditorPaneViewModel.Replace(tab, document, out created)` already swaps the buffer a tab shows in
  place (used by reopen as hex/text). `DocumentRegistry.ViewCount` tells whether another tab still
  shows a buffer.
- `HelpCatalog` must document every `VimExCommand` (the help tests check it).

## Decisions

| Decision | Rationale |
|---|---|
| `:enew` = File → New: a new untitled tab in the active pane; the current tab stays | Tabs are the buffer list. This is Vim with `hidden`, and it matches what `:e file` already does here |
| `:enew!` replaces the current tab with a new untitled buffer in its place, without asking | Vim's "discard any changes to the current buffer"; the bang is the user saying "don't ask" |
| If another tab or pane still shows the replaced buffer, nothing is discarded | The buffer stays open there, as with `:q!` on a shared buffer |
| `:enew!` with no tab in the pane is `:enew` | Nothing to replace |
| A hex tab is replaced the same way | A hex view has no changes to lose |
| `ene` is the shortest spelling; `:en` is refused | Vim's `:ene[w]`. `edit` keeps `:e` (`ene` is not a start of `edit`, and `e` is shorter than `enew`'s minimum) |
| `:enew {anything}` is refused | Vim's `:enew` takes no argument |

## Steps

1. `Core/Vim/VimExCommand.cs`: add `EditNew` (`:ene[w]`) and `EditNewDiscard` (`:ene[w]!`), with doc
   comments.
2. `Core/Vim/ExCommandParser.cs`: `new("enew", 3, VimExCommand.EditNew, BangCommand: VimExCommand.EditNewDiscard)`.
3. `Desktop/ViewModels/WorkspaceViewModel.cs`: a public
   `ReplaceWithNewDocument(EditorPaneViewModel pane, EditorTabViewModel tab)`. It creates an untitled
   buffer, swaps it into the tab (`pane.Replace`), retains the new buffer and releases the old one,
   without asking. It returns the tab.
4. `Desktop/ViewModels/ExCommandHandler.cs`: `EditNew` → `NewDocument()`. `EditNewDiscard` → replace
   the pane's active tab, or `NewDocument()` when there is none. Update the class remarks.
5. `Desktop/ViewModels/HelpCatalog.cs`: two lines in *Files & tabs*.
6. Tests:
   - Core parser: `ene`, `enew` → `EditNew`; `ene!`, `enew!` → `EditNewDiscard`; `en`, `enew x` → null;
     `e`, `ed` still read as `Edit`.
   - Desktop ex handler: `:enew` adds a second, untitled tab and keeps the first one, unprompted.
   - `:enew!` on a dirty buffer: the tab now shows an untitled buffer, with no question, the old buffer
     is out of the registry, and the tab count is unchanged.
   - `:enew!` on a buffer another pane still shows: that buffer stays open there.
   - `:enew!` with no tab creates one.

## Acceptance criteria

- `:ene`, `:enew` open a new untitled tab in the pane; the previous tab stays open.
- `:ene!`, `:enew!` replace the current tab's buffer with a new untitled one, never asking. Its
  changes are gone unless another view still shows it.
- `:en` and `:enew foo` are not commands.
- The help lists both.
- Zero warnings, whole suite green.

## Out of scope

- `:e!` (revert to disk) and `:enew` options such as `fileformats`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| `:enew` replaces or adds a tab | Adds a tab (File → New) | Tabs are the buffer list; this is Vim with `hidden` and matches `:e file` here | Replace the tab and ask about unsaved changes (closes the user's file for a command meant to open one) |
| `:enew!` meaning | Replace the current tab, discarding its unsaved changes | Vim's documented meaning; the only way the bang means something | Same as `:enew` (bang ignored) |
| Shared buffer under `:enew!` | Kept open in the other view | Same rule as `:q!` / `CloseTabAsync`: only the last view loses changes | Discard everywhere |

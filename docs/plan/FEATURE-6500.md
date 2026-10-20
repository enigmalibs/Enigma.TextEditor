# FEATURE-6500 — :e opens the file dialog

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-6500-edit-dialog`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

`:e` (any spelling, `:e` to `:edit`) with no file opens the same open-file dialog as File → Open….
`:e {file}` is unchanged.

## Context & constraints

- `ExCommandParser.Definitions` declares `edit` with `VimExArgument.Required`. The comment says a bare
  `:e` (Vim: reload) and `:e!` (revert) are refused because neither exists here.
- File → Open is `WorkspaceViewModel.OpenCommand` → private `OnOpenAsync`. It asks
  `IFilePrompts.AskFilesToOpenAsync(ActiveFolder())` and opens each chosen file in the active pane.
- `ExCommandParserTests` currently asserts `e` is refused (the near-miss list), and the help line says
  `:e[dit] {file}`.

## Decisions

| Decision | Rationale |
|---|---|
| `edit` takes an optional argument; without one it opens the picker | The user's request; Vim's reload has no meaning in an editor that does not reload buffers |
| The picker is File → Open's, starting in the active buffer's folder, several files allowed | One way in, as every ex command here goes through the menus' commands |
| `:e!` stays refused, with or without a file | Revert-to-disk is not implemented; reading it as something else would be a near miss that costs work |
| Cancelling the dialog does nothing | Same as File → Open |

## Steps

1. `Core/Vim/ExCommandParser.cs`: `edit` → `VimExArgument.Optional`, with the comment rewritten.
2. `Core/Vim/VimExCommand.cs`: the `Edit` doc says that without a file it opens the picker.
3. `Desktop/ViewModels/WorkspaceViewModel.cs`: make the open-from-picker flow a public
   `OpenFromPromptAsync()` used by `OpenCommand`.
4. `Desktop/ViewModels/ExCommandHandler.cs`: `case VimExCommand.Edit` without an argument →
   `OpenFromPromptAsync()`.
5. `Desktop/ViewModels/HelpCatalog.cs`: `Ex("Ctrl+O", "Open a file in this pane; without one, pick it", VimExCommand.Edit, " [file]")`.
6. Tests:
   - Core parser: `e`, `ed`, `edit` with nothing parse to `Edit` with a null argument. `e!` is still
     refused. `e` moves out of the near-miss list.
   - Desktop: `:e` with no argument asks the fake picker (with the active folder) and opens what it
     returns.
   - A cancelled pick opens nothing.

## Acceptance criteria

- `:e`, `:ed`, `:edi`, `:edit` alone open the file dialog. The chosen files open in the active pane.
- `:e file` behaves as before; `:e!` is refused.
- The help shows `:e[dit] [file]`.
- Zero warnings, whole suite green.

## Out of scope

- `:e!` (revert) and reloading the current buffer.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which dialog | File → Open's picker (multi-select, active folder) | One path for opening; consistent with the menu | A single-file picker |
| `:e!` | Still refused | Revert is not implemented; a near miss must not do something else | Open the dialog too |

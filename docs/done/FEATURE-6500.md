# FEATURE-6500 — :e opens the file dialog

**Item:** FEATURE-6500 — :e opens the file dialog
**Branch:** `feature/feature-6500-edit-dialog`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

`:e`, `:ed`, `:edi` and `:edit` with no file now open File → Open's picker.
- The picker starts in the active buffer's folder and accepts several files.
- The chosen files open in the active pane.
- Cancelling opens nothing.

`:e {file}` is unchanged. `:e!` is still refused: revert-to-disk does not exist here.

Implementation:
- The parser now treats `edit`'s argument as optional, and the comment explains why.
- File → Open's flow became the public `WorkspaceViewModel.OpenFromPromptAsync()`, which `OpenCommand`
  and the ex handler share.
- The help shows `:e[dit] [file]` — "Open a file in this pane; without one, pick it".

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Vim/ExCommandParser.cs` — `edit` takes an optional argument.
- `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs` — `Edit` remarks.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `OpenFromPromptAsync` replaces
  the private `OnOpenAsync`.
- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — `case VimExCommand.Edit` without an
  argument.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the `:e` line.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/ExCommandParserTests.cs`:
  - `e`, `ed`, `edi`, `edit`, ` e ` read as `Edit` with no argument;
  - `e` left the near-miss list;
  - `ed` in the not-a-command list became `ed!`.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimQuitTests.cs` — `:e<CR>` moved from "refused" to a new
  "asked for with no argument" theory, with `:edit<CR>`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/HelpTests.cs` — the expected spelling
  `:e[dit] [file]`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/ExCommandHandlerTests.cs`:
  - `:e` alone asks once, from the active buffer's folder, and opens both picked files in order;
  - a cancelled pick opens nothing.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeFilePrompts.cs` — counts open
  questions and records their start folder.
- `docs/roadmap.md`, `docs/plan/FEATURE-6500.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The tests asserting `:e` alone is refused (`ExCommandParserTests`, `VimQuitTests`) | Changed to assert the new request | They pinned the behaviour this item deliberately replaces; the refusal of `:e!` stays asserted | Keeping them (contradicts the feature) |
| Sharing the picker flow | Public `OpenFromPromptAsync` used by both `OpenCommand` and `:e` | One door, as for every other ex command here | Executing `OpenCommand` from the handler (hides the task) |

## Deviations & follow-ups

- None from the plan.
- Fix cycles: 1. The first run failed `VimQuitTests.TheExCommandsStillOut_AreStillRefusedByName(":e<CR>")`,
  a test that pinned the old refusal. It was moved to the new contract; green after.
- Line endings: LF, per `.gitattributes`; no churn.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the in-app help was updated as part of the dev.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2483 passed**, 0 failed, 0 skipped (2476 before;
  net +7).

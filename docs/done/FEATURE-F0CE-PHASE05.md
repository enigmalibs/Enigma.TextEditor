# FEATURE-F0CE-PHASE05 — Delete files and folders

**Item:** FEATURE-F0CE — File explorer on the files page
**Branch:** `feature/feature-f0ce-phase05-delete-entries`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

The explorer deletes the selected file or folder, from **Delete** in the context menu or with the Delete key.

- **Where it goes:** after a confirmation, to the Recycle Bin (Windows) or the trash (Linux). Where there is no trash,
  or it declines the entry, a second confirmation offers a permanent delete.
- **Tabs:** clean tabs of the deleted files close, as does a pane left without tabs. Tabs with unsaved changes stay
  open, and a save writes the file again.
- **Core** (ported from Enigma.MarkdownEditor):
  - `ITrashService` (`false` means "untouched");
  - `WindowsTrashService` (`SHFileOperationW` with `FOF_ALLOWUNDO`, silent);
  - `LinuxTrashService` (`gio trash`, else the freedesktop home trash);
  - `UnsupportedTrashService`, and `TrashService.ForCurrentPlatform()`;
  - `FileOperations.DeleteAsync`: recursive and permanent; an entry already gone is not an error.
- **The questions:** `IFileExplorerPrompts.ConfirmMoveToTrashAsync` / `ConfirmPermanentDeleteAsync`, in the content
  dialog:
  - "Delete 'x'?" / "Delete 'x' permanently?";
  - *Recycle Bin* on Windows, *trash* elsewhere, and "the folder and everything in it" for a folder;
  - Cancel is the default button, and only the confirming button confirms.
- **`WorkspaceViewModel.CloseTabsOfDeletedAsync(path)`** closes the clean tabs of buffers at or under the path, using
  `DocumentRegistry.FindUnder`.
- **`FileExplorerViewModel.DeleteCommand`** (needs a file or folder selected):
  - confirm, trash, else confirm again and delete for good;
  - then close the clean tabs and list the parent again;
  - IO and permission failures are reported ("The file/folder could not be deleted").
  - A selection whose row leaves the tree, after any listing, is cleared.
- **View:** **Delete** (`Trash`, Delete) in the context menu; Delete on the tree runs it.
- **DI:** `ITrashService` = `TrashService.ForCurrentPlatform()`.
- **Help:** *Files & tabs*: "Delete in the explorer — Move the file or folder to the trash, after asking".
- **README:** the explorer paragraph now covers the right-click menu: create, rename (F2, tabs follow) and delete
  (Delete, the trash or for good after asking again; the tabs of deleted files close unless they hold unsaved
  changes).

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Files/ITrashService.cs`, `TrashService.cs`, `UnsupportedTrashService.cs`,
  `WindowsTrashService.cs`, `LinuxTrashService.cs`
- Modified: `src/Enigma.TextEditor.Core/Files/FileOperations.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/FileExplorerViewModel.cs`, `WorkspaceViewModel.cs`,
  `HelpCatalog.cs`; `Services/IFileExplorerPrompts.cs`, `FileExplorerPrompts.cs`; `Views/FileExplorerView.axaml(.cs)`;
  `Extensions/ServiceCollectionExtensions.cs`; `README.md`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Files/TrashServiceTests.cs`;
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileExplorerDeleteTests.cs`,
  `Infrastructure/FakeTrashService.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Files/FileOperationsTests.cs`;
  `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeFileExplorerPrompts.cs`, `EditorHarness.cs`,
  `Views/FileExplorerViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-F0CE.md`; created this file

## Deviations & follow-ups

- **A side effect, ported deliberately:**
  - `TrashServiceTests.OnAPlatformWithATrash_AFileReallyGoesIntoIt` really trashes one small file per run, named
    `enigma-texteditor-trash-test-<guid>.md`. On Windows it lands in the developer's Recycle Bin;
  - it is the only proof that the native marshalling is right, as in Enigma.MarkdownEditor;
  - the Desktop tests use `FakeTrashService` and leave nothing behind.
- **Not seen in a real shell:** the recycle-bin and freedesktop paths run in the Core test above. The dialogs' wording
  and the whole flow are checked headless. Worth one manual delete on each platform.
- **Pre-existing flaky test:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed again in
  the full run. See `docs/done/FEATURE-F0CE-PHASE04.md`: it fails 5 in 10 at the run's starting commit.
- **Baseline failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md`.
- **Release notes** are left to the next release item.
- **Documentation sweep:** the README explorer paragraph and the help row were edited as planned. Nothing else
  describes the explorer.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Default button of the delete questions | Cancel | The app's other destructive question (`ConfirmationPrompt`) does the same. With `DialogKeyboard`, an Enter typed too soon cancels |
| When tabs close | After the entry is gone, not before | A refused or cancelled delete must leave the tabs as they were. Hex tabs hold the file with delete sharing, so they do not block it |
| Clearing a stale selection | After every listing, when the selected row is no longer in the tree | Without a view (and before the tree catches up), a deleted row stayed selected and the commands still offered to act on it |
| Desktop tests' trash | `FakeTrashService`, which removes the entry | One real round trip in Core is enough; the explorer's behaviour does not need the Recycle Bin |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3109 total, 3107 passed, 2 failed — the baseline's
  `InstanceServerTests` failure and the pre-existing flaky `FileDropViewTests` test.
- **Tests added (+20):**
  - Core `FileOperationsTests` (+6, ported): a file deleted; a folder and everything under it; an entry already gone
    is not a failure; nameless paths rejected;
  - Core `TrashServiceTests` (5, ported): the platform's implementation is chosen; an unsupported platform declines
    and leaves the entry; every implementation checks its path; a real round trip to the platform's trash; an entry
    already gone counts as trashed;
  - `FileExplorerDeleteTests` (7):
    - delete needs a selection;
    - a confirmed delete trashes and the row goes, the selection with it;
    - a declined confirmation deletes nothing;
    - with no trash, one question and a permanent delete;
    - a declining trash asks again, and a declined second question keeps the file;
    - a clean tab of the deleted file closes and a dirty one stays;
    - a deleted folder closes the clean tabs under it and the pane they leave empty;
  - `FileExplorerViewTests` (+2, real window): Delete on the tree deletes the selected row (the menu item is the
    command, with Delete). In the real dialogs, the trash question opens on Cancel and Enter answers no, and Escape
    answers no to the permanent one.

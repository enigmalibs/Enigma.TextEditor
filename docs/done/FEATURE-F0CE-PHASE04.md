# FEATURE-F0CE-PHASE04 — Rename files and folders

**Item:** FEATURE-F0CE — File explorer on the files page
**Branch:** `feature/feature-f0ce-phase04-rename-entries`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

The explorer renames the selected file or folder, from **Rename…** in the context menu or with F2. Open tabs of the
file, or of every file under a renamed folder, follow the new path, so a save never brings the old name back.

- **Core:** `FileOperations.RenameAsync` (ported):
  - a case-only rename goes through a staging name, which Windows needs;
  - keeping the name is a no-op. A taken name, an entry that is gone, or an IO failure raise a
    `FileOperationException`;
  - after a failed second half of a case-only rename, the entry is put back under its original name.
- **Buffers follow:**
  - `DocumentViewModel.Relocate(path)` changes only the path, so the title and tooltip follow while the text and its
    unsaved changes stay;
  - `DocumentRegistry.FindUnder(path)` and `Relocate(old, new)` move every buffer at or under a path. A sibling that
    only shares the prefix (`docs2` for `docs`, `notes.txt.bak` for `notes.txt`) is not moved;
  - `WorkspaceViewModel.RelocateDocuments` exposes the move to the explorer.
- **The dialog:** `IFileExplorerPrompts.AskNewNameAsync(path, isFolder)` shows *Rename file* / *Rename folder*. It
  starts on the current name with the stem selected, and accepts keeping the name or changing only its case.
- **`FileExplorerViewModel.RenameCommand`** (needs a file or folder selected, not a reason row):
  - asks, renames, moves the buffers, lists the parent again, and selects the renamed entry;
  - a renamed folder that was open stays open, and is listed under its new name;
  - failures are reported as "The file/folder could not be renamed", and the parent is listed again.
- **View:** **Rename…** (`PencilSimple`, F2) in the context menu; F2 on the tree runs it.
- **Help:** *Files & tabs*: "F2 in the explorer — Rename the file or folder; its open tabs follow it".

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Files/FileOperations.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/DocumentViewModel.cs`, `WorkspaceViewModel.cs`,
  `FileExplorerViewModel.cs`, `HelpCatalog.cs`; `Services/DocumentRegistry.cs`, `IFileExplorerPrompts.cs`,
  `FileExplorerPrompts.cs`; `Views/FileExplorerView.axaml(.cs)`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Files/FileOperationsTests.cs`;
  `tests/Enigma.TextEditor.Desktop.UnitTests/Services/DocumentRegistryTests.cs`,
  `Infrastructure/FakeFileExplorerPrompts.cs`, `Views/FileExplorerViewTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileExplorerRenameTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-F0CE.md`; created this file

## Deviations & follow-ups

- **Pre-existing flaky test:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed in both
  full runs of this phase. It is not caused by this run:
  - built from `git archive`, it fails 5 times in 10 at the run's starting commit (`develop` @ 469c01a) and 3 in 10
    at the run-branch tip before this phase (`997aa36`);
  - the test drops a file and then drains the dispatcher a fixed 4 times, while the file is read on the thread pool,
    so it races the disk (and any on-access scanning of the new temporary file);
  - it passed in the baseline run by chance;
  - follow-up: wait for the tab (as `FileExplorerViewTests.UntilAsync` does) instead of a fixed drain. This run does
    not touch unrelated tests.
- **Known edge, not handled:** suppose a buffer is open for a file that does not exist yet (a name typed at
  `:e new.txt`), and another file is renamed to that same name. Two buffers then share the path. The rename passes
  because validation looks at the disk, and the not-yet-saved file is not there.
- **Baseline failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md`.
- **Documentation sweep:** the help row was added as planned. The README's explorer sentence is still true, and
  PHASE05 extends it to create, rename and delete, as planned.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Path comparison when moving buffers | `DocumentRegistry.PathComparison` | The registry's own rule for "same file", so a buffer found by `Find` is the one moved |
| A renamed open folder | Expanded again under its new name (listed afresh) | The rows under it are new paths. Keeping the folder open is what the user saw before |
| Testing the menu's bindings | Open the menu first | A `ContextMenu`'s items bind once it is opened. Before that their `Command` is null |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`, twice: 3089 total, 3087 passed, 2 failed — the baseline's
  `InstanceServerTests` failure and the pre-existing flaky `FileDropViewTests` test above. Run alone, that test failed
  once in 8.
- **Tests added (+17):**
  - Core `FileOperationsTests` (+6, ported): rename, keep the name, case-only for a file and a folder, a taken name
    refused (the other file kept), an entry that is gone refused;
  - `DocumentRegistryTests` (+2): a folder's buffers move but not a prefix sibling's; a file's buffer moves but not a
    longer name's;
  - `FileExplorerRenameTests` (7):
    - rename needs a file or folder selected;
    - the renamed file shows selected;
    - an open, edited tab follows, stays dirty and saves under the new name, and the old file is not recreated;
    - an open folder's tabs move and it stays open;
    - a case-only rename works and the tab follows;
    - keeping the name changes nothing;
    - a name taken in between is reported, and both files are left alone;
  - `FileExplorerViewTests` (+2, real window): F2 renames the selected row (the menu item is the command, with F2);
    the real rename dialog starts on the name with its stem selected, and typing over it then Enter answers the new
    name.

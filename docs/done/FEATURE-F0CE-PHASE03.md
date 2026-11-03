# FEATURE-F0CE-PHASE03 — New file and new folder

**Item:** FEATURE-F0CE — File explorer on the files page
**Branch:** `feature/feature-f0ce-phase03-new-entries`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

The explorer's tree has a context menu with **New File…** and **New Folder…**. A name dialog checks the name as it is
typed. The new entry is created, revealed and selected, and a new file opens in the active pane.

- **Core `Files/`** (ported from Enigma.MarkdownEditor):
  - `FileNameValidation`: empty, separators, the platform's forbidden characters, `.`/`..`, a trailing space or dot,
    Windows device names, length over 255, and a name taken in the folder (case-insensitive on Windows, and a rename
    may keep its own name);
  - `FileOperationException`;
  - `FileOperations.CreateFileAsync` (`FileMode.CreateNew`, so it never overwrites) and `CreateDirectoryAsync` (refuses
    a name taken), off the UI thread, with failures as sentences;
  - new `FilePaths` holds the path comparer and `NameOf`.
- **The name dialog** (ported):
  - `FileNamePromptViewModel` (validates on every keystroke; Enter commits only a valid name) and `FileNamePromptView`
    (a box with the error under it, focused, the stem selected);
  - `IFileExplorerPrompts` / `FileExplorerPrompts`: *New file* / *New folder* in the content dialog. *Create* is off
    while the name is invalid, Enter answers like *Create*, and Escape like *Cancel*.
- **`FileExplorerViewModel`:**
  - `NewFileCommand` / `NewFolderCommand`, off with no folder;
  - `TargetFolder`: the selected folder, the folder of a selected file (or of a reason row), else the open folder;
  - after a create, the folder is listed again (expanded, and listed the first time if need be) and the new entry is
    selected. A new file opens through `File → Open`'s door;
  - a `FileOperationException` goes to `IMessageService.ShowErrorAsync` ("The file could not be created"), and the
    folder is listed again;
  - `FileOpened` moved here from the view, so a created file hands the keyboard to the pane as an opened one does.
- **`FileExplorerView`:**
  - the tree's `ContextMenu` holds New File… (`FilePlus`) and New Folder… (`FolderPlus`);
  - a right-click selects the row under it, or nothing below the rows, so the menu acts on the open folder.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Files/FileNameValidation.cs`, `FileOperationException.cs`, `FileOperations.cs`,
  `FilePaths.cs`
- Created: `src/Enigma.TextEditor.Desktop/ViewModels/FileNamePromptViewModel.cs`,
  `Views/FileNamePromptView.axaml(.cs)`, `Services/IFileExplorerPrompts.cs`, `Services/FileExplorerPrompts.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/FileExplorerViewModel.cs`, `Views/FileExplorerView.axaml(.cs)`,
  `Views/MainWindow.axaml.cs`, `Extensions/ServiceCollectionExtensions.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Files/FileNameValidationTests.cs`, `FileOperationsTests.cs`;
  `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileNamePromptViewModelTests.cs`, `FileExplorerCreateTests.cs`,
  `Infrastructure/FakeFileExplorerPrompts.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs`, `Views/FileExplorerViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-F0CE.md`; created this file

## Deviations & follow-ups

- **A new file's dialog opens empty.** MarkdownEditor pre-fills `.md`; a text editor has no default extension.
- **`FileOperations` holds only the creates for now.** Rename and delete land in PHASE04 and PHASE05, with their tests.
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md` (the run's baseline).
- **Documentation sweep:** the README's explorer sentence is still true (it does not list the menu yet). PHASE05 adds
  create, rename and delete to it, as planned. Nothing was made stale.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Right-click selection | A tunnelling `ContextRequested` handler (`handledEventsToo`) | The tree's menu marks the request handled as it opens, before a bubbling handler runs. A diagnostic showed the empty-area request never reached it |
| Target folder for a reason row | The folder it describes | The row stands for that folder's contents |
| Where `FileOpened` lives | The view-model | Both an opened and a created file need the keyboard handed to the pane, and only the view-model knows of both |
| A failed create | Error dialog, then the folder is listed again | Whatever made it fail (a name taken outside the editor) is then on screen |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3072 total, 3071 passed, 1 failed (the baseline's).
- **Tests added (+75):**
  - Core `FileNameValidationTests` (ported, every rule) and `FileOperationsTests` (creates: empty file, folder, refused
    names never touch the disk, a taken name leaves the file or folder alone, nameless folder): 55;
  - Desktop `FileNamePromptViewModelTests` (ported): 10;
  - `FileExplorerCreateTests` (7): nothing to create with no folder; a new file in the root is selected and opened
    (`FileOpened` raised); in a selected folder, which expands; beside a selected file; a new folder is selected and
    opens nothing; a cancelled name creates nothing; a name taken in between is reported, the file left as it was;
  - `FileExplorerViewTests` (+3, real window): a right-click selects the row, and below the rows selects nothing (the
    menu's items are the commands); the real name dialog keeps *Create* off on empty and taken names, and Enter
    answers the trimmed name; Escape answers nothing.

# FEATURE-F0CE — File explorer on the files page

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-f0ce-phase01-folder-tree-model`
**Branch (PHASE02):** `feature/feature-f0ce-phase02-explorer-panel`
**Branch (PHASE03):** `feature/feature-f0ce-phase03-new-entries`
**Branch (PHASE04):** `feature/feature-f0ce-phase04-rename-entries`
**Branch (PHASE05):** `feature/feature-f0ce-phase05-delete-entries`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Objective

Give the files page a file explorer, to open the files of a folder quickly:

- a resizable panel on the left of the panes, collapsed by default, shown and hidden by a toggle button;
- in it, an **Open Folder** button (a folder picker) and a **Close Folder** button;
- with a folder open, a tree of its files and folders, as Enigma.MarkdownEditor's, from which the user opens files and
  creates, renames and deletes files and folders.

## Context & constraints

- **The files page** (`Views/MainWindow.axaml`): `Panel x:Name="FilesPage"` (shown while `Shell.IsFilesPage`) holds
  `views:SplitHost x:Name="Splits"`. The rail (`nav:NavigationView`, 44 px) stands left of it; the status bar
  (`Border x:Name="StatusBar"`, a `Grid` `Auto,*,Auto`: Vim mode chip, message line, file buttons) below.
- **Opening files:** `WorkspaceViewModel.OpenAsync(path)` (the *File → Open* door: binary prompt, size limit, already
  open → shown) puts the file in the active pane and raises `DocumentShown`; `SplitHost` then focuses the active pane.
- **Pickers:** `IFolderDialogService` (Enigma.Avalonia.Desktop) is registered (`AddEnigmaServices`), unused so far.
  `IFilePrompts`/`FilePrompts` wrap `IFileDialogService`; `FakeFilePrompts` stands in for tests.
- **Dialogs:** `IConfirmationPrompt` (ContentDialog, primary = the action, default = Cancel), `IMessageService.ShowErrorAsync`.
  `DialogKeyboard` / `DialogFit` give dialogs the keyboard and a size.
- **Buffers:** `DocumentViewModel.FilePath` has a private setter, changed by `MarkSaved`. `DocumentRegistry.Find(path)`
  / `FindHex(path)`, `Documents`, `PathComparison` (case-insensitive on Windows). A hex tab's `FileByteSource` opens
  the file with `FileShare.ReadWrite | FileShare.Delete`, so it does not block a rename or a delete.
- **Closing tabs:** `IPaneHost.CloseTabAsync(pane, tab, discard)`; a pane left without tabs closes.
- **The source to port** — Enigma.MarkdownEditor (same author, MIT), `src/Enigma.MarkdownEditor.Core/Files/`:
  `FileSystemEntry`, `DirectoryLister`/`DirectoryListing`/`DirectoryListingStatus`, `NaturalNameComparer`,
  `FileSystemEntryComparer`, `FileNameValidation`, `FileOperations`, `UniqueName`, `FileOperationException`,
  `ITrashService` + `WindowsTrashService` / `LinuxTrashService` / `UnsupportedTrashService` / `TrashService`; Desktop:
  `FileTreeViewModel`, `FileTreeNodeViewModel`, `Views/FileTreeView.axaml(.cs)`, `Services/FileOperationPrompts`;
  and their tests. Its multi-root workspaces, folder colours, filtering, drag-and-drop moves and watchers are not
  ported.
- **Conventions:** explicit usings, `field` + `SetProperty`, explicit commands, XML docs, warnings as errors; Core never
  references Avalonia; icons through `ei:Icon` / Phosphor.
- **Tests:** Core: `tests/Enigma.TextEditor.Core.UnitTests` (`Documents/TempDocumentFolder`); Desktop:
  `tests/Enigma.TextEditor.Desktop.UnitTests` (`Infrastructure/EditorHarness`, `HeadlessAvaloniaFixture`, `TempFolder`,
  fakes; real window tests under `Views/`).
- **Docs:** README *Files* paragraph and *The status bar* list; `HelpCatalog` *Files & tabs* and *Application*.

## Decisions

- **One folder at a time.** Opening a folder replaces the open one; closing it empties the tree.
- **The panel:**
  - inside the files page, left of the panes, a splitter between them; 260 px by default, at least 160, the panes keep
    at least 240;
  - collapsed at every start; neither its state, its width nor the folder is remembered across runs;
  - the header: the folder's name (its path as the tooltip), or *Explorer* with none; **Open Folder**, **Refresh** and
    **Close Folder** buttons (the last two only with a folder open);
  - with no folder: *No folder is open.* and an **Open Folder…** button.
- **The toggle:** a toggle button at the left end of the status bar (`SidebarSimple` icon, *Explorer*), on the files
  page only, not focusable like the bar's other buttons; and **View → Explorer** (checkable, Ctrl+Shift+E).
- **File menu:** **Open Folder…** and **Close Folder** under **Open as Hex…**. Opening a folder shows the panel.
- **The picker** starts in the open folder, else the active file's folder.
- **The tree:**
  - top-level rows are the folder's entries (the folder itself is the header, so it is never renamed or deleted from
    the tree);
  - folders first, then files, each in natural order, ignoring case; nothing hidden (dot files, hidden attribute);
  - a folder is listed the first time it is expanded, off the UI thread; a folder that cannot be read shows one
    *Cannot be read* row; an empty folder has no children;
  - single selection.
- **Opening:** double-click or Enter on a file opens it through `WorkspaceViewModel.OpenAsync` in the active pane, which
  takes the keyboard; on a folder, the same gestures expand or collapse it.
- **Keeping current:** no file-system watcher. **Refresh** lists every loaded folder again, keeping expansion and
  selection where the entries survive; each operation lists its folder again.
- **Create** (context menu, on a row or the empty tree): **New File…**, **New Folder…** in the selected folder, a
  selected file's folder, or the root. A name dialog validates as the name is typed: empty, `.`/`..`, separators,
  invalid characters, Windows reserved names, a trailing dot or space on Windows, a name already taken (ignoring case
  where the platform does). A new file is created empty, revealed, selected and opened; a new folder revealed and
  selected.
- **Rename** (context menu, F2): the dialog starts with the current name, the stem selected; case-only renames work.
  Open buffers of the renamed file, or of files under a renamed folder, follow the new path (title and tooltip change,
  unsaved changes kept), so a save never brings back the old name.
- **Delete** (context menu, Delete): after a confirmation, to the Recycle Bin / Trash; when the platform has none or
  refuses the entry, a second confirmation for a permanent delete. Tabs of deleted files close when they have no
  unsaved changes; with unsaved changes they stay open, and a save writes the file again.
- **Failures** (IO, permissions, a name taken in between) are reported with `IMessageService.ShowErrorAsync`, and the
  folder is listed again.
- **Layers:**
  - Core `Files/`: listing, ordering, name validation, operations, trash — no Avalonia;
  - Desktop: `FileExplorerViewModel` (folder, tree, commands), `FileTreeNodeViewModel`, `IFileExplorerPrompts` for the
    name and delete questions, `IFilePrompts.AskFolderToOpenAsync`; view `FileExplorerView`.

## PHASE01 — Folder tree model

**Status:** DONE

### Steps

1. **Core `Files/`** (namespace `Enigma.TextEditor.Core.Files`), ported and trimmed:
   - `FileSystemEntry` (path, name, kind folder/file), `NaturalNameComparer`, `FileSystemEntryComparer` (folders
     first, then natural order, ignoring case);
   - `DirectoryLister.ListAsync(directory, cancellationToken)` → `DirectoryListing` (sorted entries, or a status: not
     found, no access, failed), listing every entry, hidden ones included, off the calling thread.
2. **Desktop `ViewModels/`:**
   - `FileTreeNodeViewModel`: full path, name, kind (folder, file, *Cannot be read*), `Children`, `IsExpanded` (the
     first expansion asks for the listing; a folder starts with one placeholder child so it can be expanded),
     `IsLoaded`, icon kind (`Folder`/`FolderOpen`, `File`, `Warning`), tooltip;
   - `FileExplorerViewModel`: `FolderPath`, `HasFolder`, `Header`, `Roots`, `SelectedNode`, `IsVisible`;
     `OpenFolderAsync(path)` (lists the folder, shows the panel), `CloseFolder()`, `RefreshAsync()` (lists loaded
     folders again, keeping expanded folders and the selection whose paths survive), `OpenAsync(node)` (a file →
     `WorkspaceViewModel.OpenAsync`; a folder → toggles it); `ToggleCommand`, `RefreshCommand`, `CloseFolderCommand`,
     `OpenCommand`; a listing in flight is cancelled when the folder changes or closes, and its result dropped;
   - registered as a singleton; reachable as `MainWindowViewModel.Explorer`.
3. **Tests:**
   - Core: ordering (folders first, `file2` before `file10`, case ignored), hidden entries listed, a missing folder,
     cancellation; ported comparer tests;
   - Desktop (temp folders): opening lists the top level; expanding a folder lists it once; *Cannot be read*; closing
     empties the tree and cancels a listing; opening another folder replaces it; refresh shows a file created outside
     and drops a deleted one, keeping an expanded folder expanded; opening a file node opens a tab in the active pane;
     opening a folder node toggles it.

### Acceptance criteria

- The model lists a folder lazily, folders first in natural order, and reports unreadable folders as a row.
- Open, close, refresh and open-a-file work without a window.
- Zero warnings, whole suite green.

## PHASE02 — Explorer panel and its toggle

**Status:** TODO

### Steps

1. **`IFilePrompts.AskFolderToOpenAsync(startDirectory)`** over `IFolderDialogService`; `FakeFilePrompts` follows.
   `FileExplorerViewModel.OpenFolderCommand` asks (starting in the open folder, else the active file's folder) and
   opens the choice.
2. **`Views/FileExplorerView.axaml(.cs)`:** the header (name, Open Folder, Refresh, Close Folder), the no-folder
   placeholder, and the `TreeView` (`TreeDataTemplate` over `Children`, `IsExpanded` two-way, icon + name rows with a
   transparent background); double-tap and Enter on a row call `OpenCommand`.
3. **`MainWindow.axaml`:** the files page becomes a `Grid` — explorer column (260, min 160), a `GridSplitter`, the panes
   (min 240). The explorer column collapses to 0 when hidden and comes back at its last width (code-behind, as
   Enigma.MarkdownEditor's `ApplyPaneWidth`).
4. **Toggle:** a `ToggleButton` at the left of the status bar (`SidebarSimple`, *Explorer*, `Focusable="False"`,
   visible on the files page, `IsChecked` bound to `Explorer.IsVisible`); **View → Explorer** (checkable,
   `Ctrl+Shift+E`) and the window key binding; **File → Open Folder…** and **Close Folder**.
5. **Keyboard:** opening a file gives the keyboard to the pane; hiding the panel while the tree has the keyboard gives
   it to the active pane.
6. **Help:** *Application*: `Keys("Ctrl+Shift+E", "Show or hide the explorer")`; *Files & tabs*:
   `Keys("Double-click or Enter in the explorer", "Open the file")`.
7. **README:** the *Files* paragraph describes the explorer; *The status bar* list gains the explorer toggle first.
8. **Tests** (real window, headless): the panel starts hidden; the toggle, the menu item and Ctrl+Shift+E (from the
   editor, Vim on) show and hide it; dragging the splitter widens it, and hiding then showing keeps the width; Open
   Folder with a fake picker fills the tree and shows the panel; Close Folder empties it; double-clicking a file opens
   it in the active pane with the keyboard; the toggle is hidden on the settings page.

### Acceptance criteria

- The files page has a resizable explorer, hidden at start, shown and hidden by the status-bar toggle, View → Explorer
  and Ctrl+Shift+E.
- Open Folder asks with a folder picker and shows the folder's tree; Close Folder empties it; a double-click or Enter
  opens a file in the active pane.
- The help and the README say so. Zero warnings, whole suite green.

## PHASE03 — New file and new folder

**Status:** TODO

### Steps

1. **Core:** port `FileNameValidation` (the rules in *Decisions*), `FileOperationException`, and
   `FileOperations.CreateFileAsync` / `CreateDirectoryAsync` (`FileMode.CreateNew`: never overwrites).
2. **Desktop:** `IFileExplorerPrompts.AskNewFileNameAsync(directory)` / `AskNewFolderNameAsync(directory)` and
   `FileExplorerPrompts` (a ContentDialog with a `TextBox`, the validation message under it, the primary button
   disabled while the name is invalid); a fake for tests.
3. **`FileExplorerViewModel`:** `NewFileCommand`, `NewFolderCommand` (target: the selected folder, a selected file's
   folder, else the root; disabled with no folder open); after creating: list the folder again, expand it, select the
   new entry; a new file opens in the active pane; failures reported through `IMessageService`.
4. **View:** a `ContextMenu` on the tree with **New File…** and **New Folder…** (`FilePlus`, `FolderPlus`); a right
   click on a row selects it first; a right click on the empty tree clears the selection (target: the root).
5. **Tests:** Core: every validation rule, create never overwrites; Desktop: a file created in the root, in a selected
   folder and beside a selected file; it is selected and opened; a folder is created and selected; a cancelled prompt
   creates nothing; a name taken in between is reported and nothing is overwritten. View: the context menu's items run
   the commands.

### Acceptance criteria

- New File… and New Folder… create an entry in the right folder, with a validated name, reveal and select it; a new
  file opens in the active pane.
- Nothing is ever overwritten. Zero warnings, whole suite green.

## PHASE04 — Rename files and folders

**Status:** TODO

### Steps

1. **Core:** port `FileOperations.RenameAsync(path, newName)` (files and folders; a case-only rename through a
   temporary name where the platform needs it; a taken name refused).
2. **Buffers follow:** `DocumentViewModel.Relocate(newPath)` (path only: dirty state and text untouched);
   `DocumentRegistry.Relocate(oldPath, newPath)` moves every buffer whose file is the path or lies under it.
3. **Desktop:** `IFileExplorerPrompts.AskNewNameAsync(path, isFolder)` (prefilled, the stem selected, validated, the
   unchanged name is a no-op); `FileExplorerViewModel.RenameCommand`: renames, relocates buffers, lists the parent
   again, keeps a renamed folder's expansion, selects the renamed entry; failures reported.
4. **View:** **Rename…** (`PencilSimple`, F2) in the context menu; F2 on the tree runs it.
5. **Help:** *Files & tabs*: `Keys("F2 in the explorer", "Rename the file or folder")`.
6. **Tests:** Core: file and folder rename, case-only rename, taken name refused; Desktop: the tree shows the new name,
   selected; an open tab of the file shows the new name and saves there, unsaved changes kept; renaming a folder moves
   the paths of tabs under it; the old path is not recreated by a save.

### Acceptance criteria

- Rename… and F2 rename the selected file or folder with a validated name; case-only renames work.
- Open tabs of renamed files follow the new path. Zero warnings, whole suite green.

## PHASE05 — Delete files and folders

**Status:** TODO

### Steps

1. **Core:** port `ITrashService`, `WindowsTrashService` (`SHFileOperationW`, `FOF_ALLOWUNDO`), `LinuxTrashService`
   (freedesktop trash), `UnsupportedTrashService`, `TrashService.ForCurrentPlatform()`; `FileOperations.DeleteAsync`
   (permanent, recursive). Registered in the Desktop container.
2. **Desktop:** `IFileExplorerPrompts.ConfirmMoveToTrashAsync(name, isFolder)` and `ConfirmPermanentDeleteAsync(name,
   isFolder)` over `IConfirmationPrompt`; `FileExplorerViewModel.DeleteCommand`: confirm, trash, else confirm and
   delete permanently; then close the clean tabs of the deleted files (and of files under a deleted folder), keep the
   dirty ones; list the parent again; failures reported.
3. **View:** **Delete** (`Trash`, Delete) in the context menu; Delete on the tree runs it.
4. **Help:** *Files & tabs*: `Keys("Delete in the explorer", "Move the file or folder to the trash")`. **README:**
   the explorer sentence mentions create, rename and delete.
5. **Tests:** Core: permanent delete of a file and a folder tree; the Linux trash with a temporary home (ported);
   Desktop with a fake trash: a confirmed delete trashes and the row goes; a declined confirmation deletes nothing; an
   unavailable trash asks again and deletes permanently; a clean tab of the file closes, a dirty one stays; a folder
   delete closes the clean tabs under it.

### Acceptance criteria

- Delete and the context menu's Delete move the selected entry to the trash after a confirmation, or delete it for
  good after a second one when there is no trash.
- Clean tabs of deleted files close; tabs with unsaved changes stay. Zero warnings, whole suite green.

## Out of scope

- Several folders at once (workspaces); remembering the panel, its width or the folder across runs.
- A file-system watcher (external changes need Refresh).
- Moving entries by drag and drop; dragging a file from the tree onto a pane; multi-selection.
- Filtering or hiding entries (`.git`, a pattern list).
- Opening a folder named on the command line or dropped on the window.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `FEATURE` | A new panel | — |
| Folders at once | One | The draft's open and close buttons are singular | Multi-root workspaces (MarkdownEditor's) |
| Panel state across runs | Not remembered | "Collapsed by default" read as at every start; persistence touches the settings schema for little gain now | Persisting visibility, width and folder |
| Toggle placement | Status bar, far left, files page only; plus View → Explorer, Ctrl+Shift+E | Always visible, next to the panel, without a toolbar the app does not have; the menu gives a keyboard path | A toolbar row; the rail (selects pages); the menu bar's end |
| Shortcut | Ctrl+Shift+E | VS Code's explorer key; Ctrl+B is Vim's page up | Ctrl+B |
| Root row | The folder is the header; its entries are the top rows | Saves a level; the folder cannot be renamed or deleted from under the panel | A single root node |
| Hidden entries | Shown | A text editor edits dot files; no filter to configure yet | Hiding `.git` / hidden attribute |
| Listing | Lazy, per folder, off the UI thread | Large trees stay quick; same as MarkdownEditor | Listing the whole tree up front |
| External changes | Refresh button, no watcher | The watcher machinery (watchers, coalescing, merges) is a feature of its own | Porting the watchers now |
| Opening a file | Double-click or Enter, in the active pane, through File → Open's door | One way files open: binary prompt, size limit, already-open tabs | Single click (preview tabs) |
| Name validation | Ported from MarkdownEditor, checked as typed | Proven rules; the dialog never offers an invalid name | Validating only on confirm |
| Rename and open tabs | Buffers follow the new path | Otherwise a save recreates the old file beside the renamed one | Leaving tabs on the old path |
| Delete target | Recycle Bin / Trash, permanent only after a second confirmation | A delete from an editor's tree should be recoverable | Permanent delete only |
| Tabs of deleted files | Clean ones close, dirty ones stay | Clean ones show a file that is gone (and in the trash); dirty ones hold work | Closing all; keeping all |
| Failures | `IMessageService.ShowErrorAsync`, folder listed again | The app's channel for what went wrong | The status line (too easy to miss) |
| Selection | Single | Enough for the asked operations; simpler prompts | Multi-select |
| Shape | Five phases: model, panel, create, rename, delete | Each is about 30 min to review and testable on its own | Fewer, larger phases |
| Release notes | Left to the next release item | Release items write them | Editing `RELEASENOTES.md` now |

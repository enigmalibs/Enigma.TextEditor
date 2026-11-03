# FEATURE-F0CE-PHASE02 — Explorer panel and its toggle

**Item:** FEATURE-F0CE — File explorer on the files page
**Branch:** `feature/feature-f0ce-phase02-explorer-panel`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

The files page has an explorer: a resizable panel left of the panes, hidden at start, that shows a folder's tree and
opens its files.

- **Picker:** `IFilePrompts.AskFolderToOpenAsync(start)` over `IFolderDialogService`. `FilePrompts` now also takes the
  folder service. `FileExplorerViewModel.OpenFolderCommand` asks, starting in the open folder, else in the active
  file's folder.
- **`Views/FileExplorerView`:**
  - the header shows the folder's name with its path as the tooltip, then *Open Folder*, *Refresh* and *Close Folder*
    (the last two only with a folder);
  - with no folder: *No folder is open.* and an *Open Folder…* button;
  - the `TreeView`: lazy rows bound two-way to `IsExpanded`, an icon and a name per row;
  - a double-click or Enter opens a file (a folder expands or collapses), and Esc hands the keyboard back;
  - a row selected while the tree itself has the keyboard takes it, so the arrow keys work from the keyboard alone.
- **`MainWindow`:**
  - the files page is a grid: explorer, `GridSplitter`, panes (at least 240 px). The explorer's column is 0 while
    hidden; shown, it comes back at its last width (260 by default, at least 160);
  - showing the explorer gives it the keyboard. Hiding it while it has the keyboard, or opening a file from it,
    gives the keyboard to the active pane.
- **Toggle and menus:**
  - a flat `ToggleButton` (`SidebarSimple`) first on the status bar, files page only, not focusable;
  - **View → Explorer** (checkable, Ctrl+Shift+E) and the window key binding;
  - **File → Open Folder…** and **Close Folder**;
  - showing the explorer, or opening a folder, from the settings page brings the files page back
    (`MainWindowViewModel`).
- **Help:** *Application*: Ctrl+Shift+E, and Esc from the explorer. *Files & tabs*: double-click or Enter in the
  explorer.
- **README:** the *Files* paragraph describes the explorer; *The status bar* list starts with its button.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/FileExplorerView.axaml`, `FileExplorerView.axaml.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml`, `MainWindow.axaml.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Services/IFilePrompts.cs`, `FilePrompts.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/FileExplorerViewModel.cs`, `MainWindowViewModel.cs`,
  `HelpCatalog.cs`; `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeFilePrompts.cs`, `EditorHarness.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/FileExplorerViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-F0CE.md`; created this file

## Deviations & follow-ups

- **Additions to the plan:**
  - Esc in the tree gives the keyboard back to the text, and has its help row. Without it, a keyboard user could
    reach the tree (Ctrl+Shift+E) but not leave it short of hiding the panel;
  - showing the explorer from the settings page brings the files page back.
- **Not seen in a real window:** the toggle's checked look and the splitter's hairline are checked by layout in
  headless tests, not by eye. Worth a glance in both themes.
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md` (the run's baseline).
- **Release notes** are left to the next release item.
- **Documentation sweep:** README *Files* and *The status bar* were edited as planned. The README's "rail on the left
  of the window has two icons" is still true. Nothing else describes the files page.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Keyboard on showing the explorer | The tree, or the *Open Folder…* button with no folder | Ctrl+Shift+E is the keyboard's way in; showing a panel that cannot be reached by keys would leave it mouse-only |
| A focusable `TreeView` | `Focusable="True"`, and a row selected from the tree itself takes the keyboard | The tree needs the keyboard while its folder is still being listed. Avalonia's tree moves between rows only from a focused row |
| Keyboard after opening a file | Posted to the active pane, unless a dialog is open | A dialog (binary file) hands the keyboard back itself when it closes |
| Remembering the width | Read from the column before it is emptied, kept for the session | The `GridSplitter` writes the dragged width into the column; nothing else to store |
| Status-bar toggle look | Flat, the hover wash when checked | The theme's accent fill is too loud for a status bar |
| The toggle's place | Flush left (`Margin -8`), under the rail | It stands next to the panel it toggles |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2997 total, 2996 passed, 1 failed (the baseline's).
- **Tests added (+12, `FileExplorerViewTests`, real window, headless input):**
  - hidden at start with a 0-width column;
  - the status-bar toggle shows it at 260 and hides it;
  - View → Explorer is checkable with Ctrl+Shift+E;
  - Ctrl+Shift+E from the editor with Vim on shows it (keyboard in the explorer, text untouched) and hides it
    (keyboard back to the text);
  - dragging the edge 120 px widens it, and hide/show keeps the width;
  - Open Folder (fake picker) fills the tree and the header, and Close Folder brings the placeholder back;
  - File → Open Folder… / Close Folder are the explorer's commands;
  - a double-click on a file opens it in the active pane, with the keyboard;
  - Esc gives the text the keyboard, and Enter opens the selected file;
  - Down, Down, Enter opens a file from the keyboard alone;
  - no toggle on the settings page;
  - showing the explorer from the settings page brings the files back.

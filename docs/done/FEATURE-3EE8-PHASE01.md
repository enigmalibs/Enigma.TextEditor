# FEATURE-3EE8-PHASE01 — Editor pane & tabs

**Item:** FEATURE-3EE8 — Editor panes, tabs & files
**Branch:** `feature/feature-3ee8-phase01-editor-pane-tabs`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The shell now edits files. The window shows one editor pane with a tab strip.

**File menu**

- New (Ctrl+N), Open… (Ctrl+O, multi-select), Save (Ctrl+S), Save As… (Ctrl+Shift+S), Save All.
- Close Tab (Ctrl+F4), Exit.

**Edit menu**

- Undo, Redo, Cut, Copy, Paste, Select All. The gestures are shown in the menu only; the editor
  handles the keys itself.

**Tabs and status**

- Ctrl+Tab / Ctrl+Shift+Tab cycle tabs.
- Each tab keeps its own caret and scroll.
- The status bar shows `Ln x, Col y` of the active tab.
- The window title reads `name[ •] — Enigma Text Editor`.

**Model**

- **`DocumentViewModel`** is one buffer: `TextDocument`, file path or `Untitled-N`, `TextFileFormat`,
  `Indentation`, `IsDirty`. Dirty tracking (undo stack + first keystroke + group-close correction) is
  ported from MarkdownEditor.
- **`DocumentRegistry`** keeps one buffer per file, matched with the platform's path comparison. It
  counts how many tabs show each buffer, drops a buffer with its last tab, and numbers untitled
  buffers from the lowest free number.
- **`DocumentService`**:
  - opens through `DocumentFile`, detects indentation (settings fallback) and dedups;
  - saves in the buffer's own format, or asks where to save an untitled buffer;
  - Save As refuses a target that another buffer has open;
  - reports every failure (missing file, unrepresentable text, I/O) with a dialog and a log entry,
    and returns `null`/`false`.
- **`EditorTabViewModel`** holds the per-tab view state (caret offset, line, column, scroll).
- **`EditorPaneViewModel`** holds the tabs and the active tab, shows a buffer (dedup), removes a tab
  (the right neighbour becomes active, else the left), and moves to the next/previous tab.
  `CloseTabCommand` delegates to the workspace.
- **`WorkspaceViewModel`** has the active pane and the file commands. Panes are a list, ready for
  FEATURE-04BC's splits.
- **`StatusBarViewModel`** and **`MainWindowViewModel`** follow the active tab and buffer.

**Views**

- `EditorPaneView`: the tab strip (dirty dot, close button, middle-click close), one shared
  AvaloniaEdit `TextEditor`, and a placeholder when there is no tab.
  - The indentation options follow the buffer.
  - Theme-aware chrome comes from `EditorAppearance`, ported from MarkdownEditor without TextMate.
- `MainWindow`: menus, key bindings, and `ActiveEditor`, which the Edit menu uses.

## Files / modules touched

**Created — `src/Enigma.TextEditor.Desktop`**

- `ViewModels/`: `DocumentViewModel`, `EditorTabViewModel`, `EditorPaneViewModel`, `EditorPreferences`,
  `WorkspaceViewModel`, `StatusBarViewModel`.
- `Services/`: `DocumentRegistry`, `IDocumentService`, `DocumentService`, `IFilePrompts`, `FilePrompts`,
  `IMessageService`, `MessageService`.
- `Views/`: `EditorAppearance`, `EditorPaneView.axaml(.cs)`.

**Modified — `src/Enigma.TextEditor.Desktop`**

- `ViewModels/MainWindowViewModel.cs`: workspace, status bar, live title.
- `Views/MainWindow.axaml(.cs)`: File and Edit menus, key bindings, pane, status bar.
- `Extensions/ServiceCollectionExtensions.cs`: the new registrations.

**Tests**

- Created:
  - `Infrastructure/{EditorHarness,FakeFilePrompts,FakeMessageService}`;
  - `ViewModels/{DocumentViewModelTests,EditorPaneViewModelTests,WorkspaceViewModelTests}`;
  - `Services/{DocumentRegistryTests,DocumentServiceTests}`;
  - `Views/MainWindowEditorTests` (headless).
- Modified: `CompositionRootTests`, which now resolves the new services and builds the window
  view-model from the container.

## Deviations & follow-ups

- **No unsaved-changes prompt yet.** In this phase, Close Tab on a dirty buffer discards it. PHASE02
  adds the Save / Don't save / Cancel prompt, which is the very next dev of this run.
- **Name clash with `AvaloniaEdit.TextEditor`.** Inside `Enigma.TextEditor.*` namespaces the simple
  name `TextEditor` resolves to the namespace. View code therefore uses the file-scope alias
  `AvaloniaTextEditor = AvaloniaEdit.TextEditor`, and `EditorPaneView` exposes the control as
  `EditorControl`.
- **Edit shortcuts are not window key bindings.** They are AvaloniaEdit's own. Redo's `Ctrl+Y` is
  Vim's scroll-up in normal mode once Vim is attached (PHASE03); Vim users redo with `Ctrl+R`.
- **New tabs open at the end of the strip,** not next to the active tab.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the Edit menu reaches the editor | `MainWindow.ActiveEditor` finds the `EditorPaneView` whose DataContext is the active pane | Works unchanged once there are several panes (FEATURE-04BC) | An editor reference in the view-model (UI leaking into the VM) |
| Where a tab's close goes | `EditorPaneViewModel` hands it to a workspace callback | Closing the last tab of a buffer is a cross-pane question (unsaved changes, registry) | The pane closes tabs itself (would need the registry and prompts) |
| Where the editor's font, size and wrap live | An `EditorPreferences` singleton read from the settings | Every pane follows one instance; FEATURE-6BB4 changes it live | Each pane reading the settings itself |
| Save As onto a file open in another buffer | Refused with a message | Two buffers for one file is the exact state the registry exists to prevent | Overwrite and orphan the other buffer |
| Where a new tab is added | The end of the strip | Predictable; matches the MarkdownEditor strip | Right of the active tab |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2101 passed**, 0 failed, 0 skipped. That includes
  the headless window tests: active tab text, Ln/Col updates, per-tab caret, Ctrl+Tab, empty-pane
  placeholder.
- Manual smoke: the Debug build launches, with the title "Enigma Text Editor".

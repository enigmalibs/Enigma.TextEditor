# FEATURE-3EE8 — Editor panes, tabs & files

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-3ee8-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

Make the shell an editor:

- open, edit, create and save text files in a pane that holds several documents as tabs;
- never lose work silently;
- open the files named on the command line;
- drive the editor with the ported Vim engine, with mode and command line in the status bar;
- colour each document by its language, with a way to change the language.

## Context & constraints

- Builds on FEATURE-55D7 (shell, Vim engine, `Core/Documents`).
- MarkdownEditor's editor side is the reference, ported and generalised:
  - `Views/AvaloniaEditVimBuffer`, `Views/VimEditorBehavior`;
  - `Services/IVimSession`, `Services/VimSession`;
  - `Views/EditorAppearance` (TextMate);
  - the `EditorPaneView` tab strip (a horizontal `ListBox`).
- The pane must be designed for FEATURE-04BC's splits:
  - a document (buffer) can be shown by several panes at once;
  - each pane keeps its own caret and scroll per tab.
- App layout, top to bottom: native title bar · menu · content (pane) · status bar.

## Decisions

| Decision | Rationale |
|---|---|
| `DocumentViewModel` = one buffer: `TextDocument`, path or untitled name, `TextFileFormat`, `Indentation`, language, dirty state from the undo stack | One buffer per file, shared by every tab and pane showing it (Vim buffer semantics) |
| `DocumentRegistry` (singleton) owns open buffers, dedups by full path, and knows how many tabs show each buffer | Opening an already open file reuses its buffer, so two divergent copies cannot exist |
| `EditorTabViewModel` = (document, caret offset, scroll offsets); `EditorPaneViewModel` = tabs + active tab | Per-pane view state, ready for splits |
| One `TextEditor` per pane, swapping the bound `TextDocument` on tab switch | MarkdownEditor's proven approach; cheap tabs |
| Unsaved changes: Save / Don't save / Cancel when the **last** tab showing a dirty buffer closes, and on window close for every dirty buffer | A buffer still shown elsewhere is not lost |
| Startup args: each path opens as a tab, the last one active. A missing file is a new buffer bound to that path (written on save). Folders are skipped with a status message | Matches `vim file` and VS Code; nothing is created on disk until a save |
| File menu: New (Ctrl+N), Open… (Ctrl+O), Save (Ctrl+S), Save As… (Ctrl+Shift+S), Save All, Close tab (Ctrl+F4), Exit | Ctrl+W is kept free for Vim's window prefix |
| Edit menu: Undo (Ctrl+Z), Redo (Ctrl+Y), Cut, Copy, Paste, Select All | Plain GUI editing; search is Vim's `/` |
| Ex requests carry an argument (`VimExRequest(Command, Argument)`). New ex commands: `:e[dit] {path}`, `:bn[ext]`, `:bp[revious]`, `:wa[ll]`, `:qa[ll]`, `:qa!` | `:e` needs a path; tabs need a Vim way through |
| `:q` closes the active tab; the last tab of the last pane quits the app | Vim and FileExplorer semantics; `:q` is Vim's way out |
| Language: TextMate grammar from the extension; "Plain Text" when there is none. Tools → *Set language…* opens a filterable picker; the choice is per buffer and not persisted | "Set language color" read as the highlighting language |
| Status bar: Vim mode chip · command line or message · Ln x, Col y · language | Later phases add Spaces, encoding and EOL |

## PHASE01 — Editor pane & tabs

**Status:** DONE — see `docs/done/FEATURE-3EE8-PHASE01.md`
**Branch:** `feature/feature-3ee8-phase01-editor-pane-tabs`

### Steps

1. `DocumentViewModel`:
   - the buffer, format, indentation (detected), `IsDirty` via `UndoStack.IsOriginalFile`, `Title`
     (file name or `Untitled-N`), `FilePath?`;
   - `MarkSaved` / `ApplyReload`.
2. `DocumentRegistry`: open by path (dedup), create untitled, track tab count per buffer, release.
3. `IDocumentService`: `OpenAsync(path)`, `SaveAsync(doc)`, `SaveAsAsync(doc, path)`. Errors (I/O,
   unauthorized, too large, unrepresentable) are reported through an `IMessageService`
   (ContentDialog) and logged.
4. `EditorTabViewModel`, `EditorPaneViewModel`: `Tabs`, `ActiveTab`, open/activate/close tab, next and
   previous tab.
5. `EditorPaneView`:
   - a tab strip (title, dirty dot, close button, middle-click close);
   - one `TextEditor` (line numbers, current-line highlight, theme-aware chrome from a ported
     `EditorAppearance`, without a grammar yet);
   - restores caret and scroll on tab switch;
   - a placeholder when there is no tab.
6. Menus: File and Edit as decided; Ctrl+Tab and Ctrl+Shift+Tab switch tabs.
7. Status bar: *Ln x, Col y* of the active tab.
8. Window title: `<title>[ •] — Enigma Text Editor`.
9. Tests:
   - registry dedup and untitled numbering;
   - pane tab add/activate/close/next/previous;
   - document dirty tracking;
   - document service open/save/save-as with temp files, and error reporting through a fake message
     service;
   - headless: the pane shows the active document's text and the status bar shows Ln/Col.

### Acceptance criteria

- New / Open / Save / Save As / Save All / Close tab / Exit work from the menu and their gestures.
- Several files open as tabs; switching restores each tab's caret and scroll.
- Opening an already open file activates its tab instead of opening a second copy.
- Ln/Col follow the caret; the title reflects the active tab and its dirty state.
- Zero warnings, whole suite green.

## PHASE02 — Unsaved changes & startup args

**Status:** DONE — see `docs/done/FEATURE-3EE8-PHASE02.md`
**Branch:** `feature/feature-3ee8-phase02-unsaved-changes-args`

### Steps

1. `IUnsavedChangesPrompt` (ContentDialog): Save (primary, default) / Don't save (secondary) / Cancel.
2. Closing a tab that is the last view of a dirty buffer asks first:
   - *Save* runs Save (Save As for an untitled buffer), and a cancelled or failed save keeps the tab;
   - *Cancel* keeps it.
3. Window close and *File → Exit* walk every dirty buffer, asking once per buffer. Any *Cancel*
   aborts the close: `Closing` is cancelled, and the window closes again once the walk completes.
4. `StartupFiles.Parse(args, fileExists, directoryExists)` (Core): returns the files to open and the
   paths skipped. Desktop opens them after the window is shown; a missing file becomes a new buffer
   bound to that path.
5. Tests:
   - startup-args parsing (files, missing files, folders, relative paths resolved against the
     working directory);
   - the close flows with a scripted prompt (save, don't save, cancel, failed save).

### Acceptance criteria

- `Enigma.TextEditor.Desktop a.txt b.txt` opens both, with `b.txt` active.
- A missing `new.txt` opens empty and is created by the first save.
- Closing a dirty tab, the window or Exit never discards changes without the user choosing *Don't
  save*.
- Zero warnings, whole suite green.

## PHASE03 — Vim integration & ex requests

**Status:** DONE — see `docs/done/FEATURE-3EE8-PHASE03.md`
**Branch:** `feature/feature-3ee8-phase03-vim-integration`

### Steps

1. Core:
   - change `VimEngine.ExCommandRequested` to `EventHandler<VimExRequest>`, where
     `VimExRequest(VimExCommand Command, string? Argument)`;
   - extend `ExCommandParser.FileCommand` with `Edit` (`:e`, `:edit`, argument required), `BufferNext`
     (`:bn`, `:bnext`), `BufferPrevious` (`:bp`, `:bprevious`), `WriteAll` (`:wa`, `:wall`), `QuitAll`
     (`:qa`, `:qall`) and `QuitAllDiscard` (`:qa!`);
   - `:w {path}` writes to that path (Save As). Ranges remain refused.
2. Desktop:
   - port `AvaloniaEditVimBuffer`, `VimEditorBehavior` and `IVimSession`/`VimSession`;
   - the pane attaches one behavior to its editor;
   - `VimSession.IsEnabled` comes from `AppSettings.VimEnabled`.
3. `IExCommandHandler` runs the requests against the workspace:
   - write / quit / write-quit / quit-discard on the active tab (`:q` semantics as decided);
   - `:e` resolves relative to the active document's folder, else the working directory;
   - `:bn` and `:bp` cycle the pane's tabs;
   - `:wa`, `:qa` and `:qa!`.
4. Status bar: the mode chip (NORMAL / INSERT / VISUAL / V-LINE; quiet / green / amber), then the
   command line while one is open, else the last message.
5. Tests:
   - Core: the parser's new commands and the arguments they carry;
   - Desktop: ex handler behaviour against fakes;
   - headless: typing `i`, text and `Esc` edits the buffer, and `:w` saves it.

### Acceptance criteria

- With Vim on, the editor starts in NORMAL and the full ported command set works.
- `/`, `:` and messages show in the status bar.
- `:w`, `:w path`, `:q`, `:q!`, `:wq`, `:x`, `ZZ`, `ZQ`, `:e path`, `:bn`, `:bp`, `:wa`, `:qa` and
  `:qa!` behave as decided, through the unsaved-changes prompt where applicable.
- Ctrl-key menu gestures (Ctrl+S, Ctrl+O…) work in every Vim mode.
- Zero warnings, whole suite green.

## PHASE04 — Syntax highlighting & language

**Status:** TODO
**Branch:** `feature/feature-3ee8-phase04-syntax-highlighting`

### Steps

1. `LanguageCatalog` (Desktop, over `TextMateSharp.Grammars.RegistryOptions`):
   - available languages (id, display name, extensions), plus Plain Text;
   - the language for a file name.
2. `EditorAppearance` applies the active document's grammar: none for Plain Text. It re-applies on
   tab switch and on a language change, and follows the light/dark variant (DarkPlus / LightPlus).
3. `DocumentViewModel.Language`: set on open or on Save As from the extension, and changeable.
4. Tools menu: *Set language…* opens `LanguagePickerView` (filter box + list, Enter/double-click picks)
   in the ContentDialog.
5. Status bar: the language name, clickable (opens the same picker).
6. Tests:
   - catalog lookups (`.cs` → C#, `.json` → JSON, unknown → Plain Text);
   - picker view-model filtering;
   - headless: changing the language updates the status bar.

### Acceptance criteria

- `.cs`, `.json`, `.xml`, `.md`, `.py` files open highlighted; an unknown extension is Plain Text.
- *Tools → Set language…* and the status-bar item change the highlighting of the active buffer in
  every tab showing it.
- Zero warnings, whole suite green.

## Out of scope

- Session restore, external-change detection, recent files.
- A non-Vim find UI (search is Vim's `/`).
- The system-clipboard register `"+`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Tabs given multi-split | A tab strip per pane (editor groups), buffers shared across panes | Discoverable GUI model that composes with splits; reuses MarkdownEditor's tab strip | No tabs, Vim buffers only (hidden state for GUI users); global tabs each holding a layout (Vim tabpages, heavier and unfamiliar) |
| One editor control per pane or per tab | Per pane, swapping documents | Proven in MarkdownEditor, constant cost per pane | Per tab (memory and TextMate installs grow with tabs) |
| `:q` on the last tab of the last pane | Quit the application | Vim semantics and FileExplorer's `:q` | Leave an empty window (surprising for Vim users) |
| Missing file on the command line | New buffer bound to the path | `vim new.txt` behaviour; nothing is written until a save | Error dialog (blocks a common workflow) |
| Close-tab gesture | Ctrl+F4 | Ctrl+W is Vim's window prefix | Ctrl+W (conflicts with Vim) |
| Meaning of "Set language color" | Choose the highlighting language per buffer | The status bar shows "language"; colouring follows the language | A colour-theme editor (not asked elsewhere, no anchor in the draft) |
| Language picker UX | Filterable list in a ContentDialog | Dozens of grammars; filtering is the fast path | Nested submenu (unusable at that length) |
| Status-bar items | Clickable, opening the same tools | Same commands, no new behaviour | Display only (discoverability loss) |
| Find without Vim | Out of scope | Ctrl+F is Vim's page-down in normal mode, and a second search system would compete | AvaloniaEdit SearchPanel (gesture conflict) |

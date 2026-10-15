# FEATURE-3EE8-PHASE04 — Syntax highlighting & language

**Item:** FEATURE-3EE8 — Editor panes, tabs & files
**Branch:** `feature/feature-3ee8-phase04-syntax-highlighting`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

Buffers are coloured by their language, and the language can be changed.

- **`LanguageCatalog`** reads TextMateSharp's bundled grammars:
  - it offers only languages that have a grammar, by display name (C#, JSON, Python…), with
    Plain Text first;
  - `ForFile(path)` maps an extension, case-insensitively, else Plain Text.
- **`DocumentViewModel.Language`** works as follows:
  - set from the file on open, on creating a buffer for a missing file, and again after a
    Save As to another name;
  - a language the user chose (`ChooseLanguage`) sticks for the life of the buffer;
  - `DetectLanguage` does nothing once a language was chosen.
- **`EditorAppearance`** (now with TextMate, ported from MarkdownEditor):
  - installs TextMate on each pane's editor and sets the grammar of the shown buffer on every tab
    switch and every language change;
  - Plain Text removes the grammar;
  - token colours follow the variant (Dark+ / Light+);
  - a tokenizer failure is reported on the status line;
  - it is disposed when the pane unloads.
- **Tools → Set Language…** and the new **language item in the status bar** open the picker:
  - a filter box (name, id or extension) above the list;
  - Enter or a double click picks; *Select* / *Cancel* buttons do the same.
  - The choice applies to the buffer, so every tab showing it is recoloured.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Services/`: `LanguageInfo.cs`, `LanguageCatalog.cs`,
  `ILanguagePicker.cs`, `LanguagePicker.cs`.
- `src/Enigma.TextEditor.Desktop/ViewModels/`: `LanguagePickerViewModel.cs`, `ToolsViewModel.cs`.
- `src/Enigma.TextEditor.Desktop/Views/LanguagePickerView.axaml(.cs)`.
- Tests: `Services/LanguageCatalogTests.cs`, `ViewModels/LanguageTests.cs`,
  `Infrastructure/FakeLanguagePicker.cs`.

**Modified**

- `Views/EditorAppearance.cs`: TextMate installation, `SetLanguage`, `IDisposable`.
- `Views/EditorPaneView.axaml.cs`: builds the appearance with a failure route, applies the language
  on swap and on change, disposes on unload.
- `ViewModels/DocumentViewModel.cs`: `Language`, `IsLanguageChosen`, `ChooseLanguage`,
  `DetectLanguage`.
- `Services/DocumentService.cs`: detects the language on open, on create and on save.
- `ViewModels/IPaneHost.cs`, `WorkspaceViewModel.cs`: `ShowStatus`.
  `ViewModels/EditorPaneViewModel.cs`: `ReportHighlightingFailure`.
- `ViewModels/StatusBarViewModel.cs`: `LanguageText`, following the active buffer.
- `ViewModels/MainWindowViewModel.cs`: `Tools`.
- `Views/MainWindow.axaml`: Tools menu, language status button.
- `Extensions/ServiceCollectionExtensions.cs`: catalog, picker, tools.
- Tests: `Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`, `EditorPaneViewModelTests.cs`
  (the host fake).

## Deviations & follow-ups

- **No lookup by exact file name.** `Dockerfile`, `Makefile` and similar are not recognised:
  TextMateSharp.Grammars 2.0.3's `Language` exposes no file-name list, only extensions. They open as
  Plain Text and can be set by hand. Follow-up: a small built-in name table.
- **The chosen language is not persisted** across restarts, as planned (no session restore).
- **Tokenizer failures are not logged.** They go to the status line only: views have no logger.
  Follow-up if failures are ever seen in practice.
- **Fix cycles used: 1 of 3.** `TextBox.Watermark` is obsolete in Avalonia 12 (AVLN5001); replaced
  by `PlaceholderText`.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| A language's display name | The first alias of the grammar package (C#, JSON, Python), else its id | These are the names VS Code shows, from the same packages | Title-casing the id (`Csharp`) |
| Where the Tools commands live | A `ToolsViewModel`, which the status-bar items share | FEATURE-316E adds encoding/EOL/indent tools to the same place; the workspace stays about files and panes | Putting them on the workspace |
| Closing the picker with Enter or a double click | The view raises `Picked`; the picker hides the dialog and reads `Committed` | The Enigma dialog has no API to close with a given result | Requiring the *Select* button |
| One TextMate registry per pane | Yes, as in MarkdownEditor | The registry carries the theme; panes repaint independently | One shared registry (theme coupling) |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (XAML included).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2180 passed**, 0 failed, 0 skipped. That includes:
  - catalog lookups for `.cs`, `.json`, `.xml`, `.md`, `.py`, unknown and none;
  - picker filtering and commit;
  - detection on open and on Save As;
  - a chosen language surviving a rename;
  - cancel changing nothing;
  - a headless check that the editor's grammar scope and the status-bar item follow the buffer
    (C# → Plain Text with no grammar → JSON on a tab switch).
- Manual smoke: the Debug build opened a `.cs` file from the command line.

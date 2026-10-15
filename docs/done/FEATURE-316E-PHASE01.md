# FEATURE-316E-PHASE01 — Encoding tools

**Item:** FEATURE-316E — Encoding, EOL & indent tools
**Branch:** `feature/feature-316e-phase01-encoding-tools`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The two encoding tools the draft asks for are in the Tools menu, and in a flyout on the status bar's
new encoding item.

- **Reopen with Encoding…**
  - Picks an encoding, then reads the file from disk again, decoding its bytes that way.
  - The text and the undo history are replaced and the buffer is clean.
  - If the buffer has unsaved changes, *Discard unsaved changes?* asks first; declining keeps
    everything.
  - Disabled for untitled buffers.
- **Change File Encoding…**
  - Picks an encoding; the buffer's format takes it, with that encoding's BOM rule.
  - The buffer becomes unsaved, and the next save writes the new encoding.
  - A format change keeps the buffer unsaved even when its text is undone back to the saved text.
  - Choosing the encoding the buffer already has changes nothing.
- **Unrepresentable characters.** A save into an encoding that cannot hold the text is refused, with
  the dialog naming the first such character and its line and column. The file is untouched and the
  buffer stays unsaved (FEATURE-55D7 PHASE03's `UnrepresentableTextException`, surfaced by the
  document service).
- **Status bar.** The encoding item shows the active buffer's label (`UTF-8`, `UTF-8 with BOM`,
  `UTF-16 LE`, `Windows-1252` …) and follows tab switches and changes.

## Files / modules touched

**Created**

- `Services/IEncodingPicker.cs`, `Services/EncodingPicker.cs`.
- `Services/IConfirmationPrompt.cs`, `Services/ConfirmationPrompt.cs`.
- `ViewModels/EncodingPickerViewModel.cs`, `Views/EncodingPickerView.axaml(.cs)`.
- Tests: `ViewModels/EncodingToolsTests.cs` (9), `Infrastructure/FakeEncodingPicker.cs`,
  `Infrastructure/FakeConfirmationPrompt.cs`.

**Modified**

- `ViewModels/DocumentViewModel.cs`:
  - `Encoding`, `ChangeEncoding`, `ChangeLineEndings` (ready for PHASE02);
  - format-change dirty tracking;
  - `MarkSaved(…, writtenFormat)`.
- `Services/IDocumentService.cs`, `DocumentService.cs`: `ReopenWithEncodingAsync`; the written format
  travels with the save.
- `ViewModels/ToolsViewModel.cs`: the two commands.
- `ViewModels/StatusBarViewModel.cs`: `EncodingText`.
- `Views/MainWindow.axaml`: Tools menu entries and the encoding status button with its flyout.
- `Extensions/ServiceCollectionExtensions.cs`: the picker and the confirmation prompt.
- Tests: `Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`.

## Deviations & follow-ups

- **`ChangeLineEndings` is already on `DocumentViewModel`.** It shares the format-change path with
  `ChangeEncoding`. PHASE02 adds its menu, status item and tests.
- **Indentation is kept on Reopen with Encoding.** It is not re-detected: the encoding does not
  change where lines start.
- **Fix cycles used: 0.**

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How a format change keeps the buffer unsaved | A `_formatChanged` flag ORed into the undo-stack answer, cleared by a save of that same format | The undo stack only knows text; undoing an edit must not make a re-encoded buffer look saved | Pushing a fake undo entry |
| The confirmation's default button | Cancel | Discarding work should never be the Enter default | Defaulting to the destructive action |
| Picker UX | A plain list (9 entries) with Enter / double-click / Select | Short list; no filter needed | The filtered list of the language picker |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2301 passed**, 0 failed, 0 skipped. That
  includes:
  - UTF-8 reopened as Windows-1252 (`é` → `Ã©`);
  - a dirty reopen asks, and declining or confirming each behaves as described;
  - untitled buffers cannot be reopened;
  - a change to UTF-16 LE writes `FF FE 68 00 69 00`;
  - a re-encoded buffer stays unsaved through an undo;
  - the same encoding is a no-op;
  - Windows-1252 refuses "日本" with the message naming line and column, and the file is untouched;
  - headless: the status item follows changes and tab switches, and its flyout carries both commands.

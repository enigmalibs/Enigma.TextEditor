# FEATURE-3AE6-PHASE02 — Binary prompt & reopen as hex

**Item:** FEATURE-3AE6 — Read-only hex viewer
**Branch:** `feature/feature-3ae6-phase02-binary-prompt`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

Opening a binary file now suggests the hex viewer, as the draft asks.

- **On open,** `DocumentService.OpenAsync` probes the first 8000 bytes before reading:
  - **Looks binary** (a NUL byte, with no UTF-16/32 BOM) → *Open a binary file?* with **Open in hex
    viewer** (default) / **Open as text** / **Cancel**.
  - **Over 32 MB** → *Too large to edit*, offering **Open in hex viewer** / **Cancel**.
  - **Otherwise** text, as before. A UTF-16 file with its BOM opens without a question.
- **One open path.** File → Open, the command-line arguments, `:e` and `:sp file` all go through
  `Workspace.OpenAsync` → `DocumentService.OpenAsync`, so they all ask.
- **No second question.** A file already open only in the hex viewer is shown there again without
  asking.
- **Tools → Reopen as Hex / Reopen as Text** swaps the active tab's view of its file in place, in the
  same strip position.
  - Leaving a text view whose buffer has unsaved changes, with no other tab showing it, goes through
    the Save / Don't save / Cancel question.
  - *Reopen as Text* opens as text without the binary question; the size cap still applies.

## Files / modules touched

**Created**

- `Services/IBinaryFilePrompt.cs` (with `BinaryFileChoice`), `Services/BinaryFilePrompt.cs`.
- Tests: `ViewModels/BinaryOpenTests.cs` (10), `Infrastructure/FakeBinaryFilePrompt.cs`.

**Modified**

- `Services/IDocumentService.cs`, `DocumentService.cs`: `OpenAsync(path, forceText)` with the probe
  and the two questions.
- `ViewModels/EditorPaneViewModel.cs`: `Replace(tab, document)`.
- `ViewModels/WorkspaceViewModel.cs`: `ReopenActiveAsync(asHex)`.
- `ViewModels/ToolsViewModel.cs`: `ReopenAsHexCommand`, `ReopenAsTextCommand`.
- `Views/MainWindow.axaml`: the two Tools entries.
- `Extensions/ServiceCollectionExtensions.cs`: the prompt.
- Tests: `Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`.

## Deviations & follow-ups

- **The binary question explains its default.** It says "as text it would read as garbage, and saving
  it could damage it" — the reason the hex viewer is suggested.
- **Fix cycles used: 1 of 3.** A test expected 12 characters from a 12-byte PNG signature opened as
  text. The signature contains CR LF, which the buffer holds as one line feed, so the answer is 11.
  The behaviour is correct; the test was fixed.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the decision is made | In `DocumentService.OpenAsync`, before reading | Every route gets it for free; reading a 2 GB file only to refuse it would be wasted I/O | In each caller |
| A file already in the hex viewer, opened again | Shown there, no question | The user answered already | Asking every time |
| Reopen with unsaved text | The unsaved-changes question, only for the buffer's last view | Same rule as closing a tab; nothing is lost if another tab still shows it | A plain discard confirmation |
| The too-large question for *Open as text* | Not offered: text over 32 MB is refused | The cap protects the UI from freezing | Allowing it anyway |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2347 passed**, 0 failed, 0 skipped. That
  includes:
  - binary → hex by default, → text when chosen, cancel opens nothing;
  - over 32 MB → hex when accepted, nothing when declined;
  - UTF-16 with BOM → no question;
  - every route (`:e`, `:sp file`, File → Open, command line) asks;
  - a file already in hex is not asked about again;
  - reopen as hex and back keeps the tab's position and drops the other buffer;
  - a dirty buffer asks, and cancel keeps it;
  - reopen as text forces text for a binary file.
- Manual smoke: launching the Debug build with a DLL as argument shows the window waiting on the
  binary-file question, with no tab opened until it is answered.

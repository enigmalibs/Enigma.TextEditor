# FEATURE-34A1-PHASE02 — Capture and restore tabs and splits

## Summary

The editor area can now be captured as a `SessionState` and brought back in another one. Nothing calls this at
start or close yet; PHASE03 wires it in.

- `WorkspaceViewModel.CaptureSession()` stores:
  - the layout snapshot, each pane's tabs in strip order with the one showing, and the active pane;
  - each tab's caret, scroll offsets and hex row, clamped to what `SessionFile` accepts;
  - each buffer once: file and hex buffers by path, untitled buffers only when empty.
- `WorkspaceViewModel.RestoreSessionAsync(state)` runs only on an editor area with no tab. Its steps:
  - It reads the buffers the tabs show (each once) through `IDocumentService.RestoreAsync`.
  - It rebuilds the layout in one batch, with a pane per leaf and the tabs adopted with their positions.
  - It shows the saved tab, or the nearest one before it that came back.
  - It closes panes left empty (not the last one) and activates the saved pane.
  - Skipped buffers are named once on the status line.
  - If a tab appears while files are being read, it keeps what the user opened and releases the restored buffers.
- `IDocumentService.RestoreAsync(SessionDocument)` never shows a question or a dialog:
  - a file is read again in the stored encoding, with the stored indentation and chosen language;
  - a hex view reopens its file;
  - an untitled buffer comes back empty under its name;
  - a failure is logged as a warning and returns `null`.
- `DocumentViewModel.FileFormat` is the file's own format, as last read or saved. `ToSession()` stores it rather
  than a change of encoding or line endings still waiting for a save.
- `TextEncodings.ForCodePage(codePage, bom)` maps a stored code page back to an encoding entry, or `null` for a code
  page the machine lacks; detection then takes over.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Documents/TextEncodings.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Services/IDocumentService.cs`, `DocumentService.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/DocumentViewModel.cs`, `WorkspaceViewModel.cs`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Documents/EncodingRoundTripTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SessionRestoreTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-34A1.md`

## Deviations & follow-ups

- Added `DocumentViewModel.FileFormat`, which the plan did not list. Without it, a buffer whose encoding was changed
  but not saved would be read back in an encoding its file was never written in.
- Restoring a file in its stored encoding means a file rewritten in another encoding while the editor was closed is
  read in the old one. "Reopen with Encoding" fixes it. Recording whether the encoding was chosen or detected is a
  possible follow-up.
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where a buffer becomes a `SessionDocument` | `DocumentViewModel.ToSession()` | The buffer knows its file, formats, indentation and language |
| Where a buffer comes back | `IDocumentService.RestoreAsync` | Beside `OpenAsync`: same registry, language and indentation rules, but quiet |
| Which tab shows when the saved one is gone | The nearest restored tab before it, else the first after | What closing that tab would have shown |
| A tab opened while files are read | The restore steps aside and releases its buffers | It must never replace what the user opened |
| Status line text | `Not restored from the previous session: a.txt, b.txt.` | One line, the names only; the reasons are in the log |

## Build/test evidence

- Baseline: see `docs/done/BUG-C6A9.md` (0 warnings; 3199 tests, 1 pre-existing failure).
- `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3245 tests, 3244 passed, 1 failed (the pre-existing failure).
- Tests added:
  - Core: `EveryOfferedEncoding_IsFoundAgainByItsCodePageAndMark` and
    `ACodePageNotOffered_StillHasAnEntry_AndOneThatDoesNotExistHasNone`.
  - Desktop (`SessionRestoreTests`, 7 tests):
    - layout, ratio, tabs, active tabs and pane, the shared buffer and positions, through the real session file;
    - hex view and hex row, the chosen encoding, language and indentation;
    - an encoding change not saved is not kept;
    - a deleted file is skipped, its pane closed and the file named;
    - nothing restorable leaves one empty pane;
    - untitled buffers with and without text;
    - an editor area that already has a tab is left alone.
- Docs sweep: nothing user-facing changed in this phase, so no doc is stale.

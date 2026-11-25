# CODE-REVIEW-0734-PHASE07 — [Medium] XAML-02 The hex view's copy moves into the tab's view-model

## Summary

`HexView.CopyBytes` used to do everything in the view: it checked the size limit, read the selected bytes from disk
on the UI thread, formatted them and reported failures. The rule could only be tested through a headless window. Now
the tab owns it:

- `EditorTabViewModel.CopyHexSelectionAsync(HexColumn)` returns a `HexCopyResult`, which holds either the text or a
  status message.
  - It checks the size limit first; over the limit, nothing is read.
  - The read and the formatting run together on the thread pool (`HexDocument.Read` uses `RandomAccess`, so reads
    from the rows being painted do not interfere).
  - An `IOException`, `UnauthorizedAccessException` or `ObjectDisposedException` (tab closed meanwhile) becomes the
    message, not a failure.
- `EditorTabViewModel.CopyHexRange()` gives the address range the same way, so the view builds no clipboard text at
  all.
- `HexView` keeps only the call, the clipboard and the status line, in one `ToClipboardAsync`, started with
  `Forget()` as before. The focus hand-back to the rows after a menu copy is unchanged.
- The clipboard texts and status messages are the same as before.

This also takes care of PERF-02's `HexView.axaml.cs:299` location (the read on the UI thread).

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/ViewModels/HexCopyResult.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/EditorTabViewModel.cs`, `src/Enigma.TextEditor.Desktop/Views/HexView.axaml.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/HexCopyTests.cs`

## Deviations & follow-ups

- The copy range moved too (`CopyHexRange`). The plan named only the bytes, but leaving the one other clipboard text
  in the view would have kept two homes for the same rule.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Result shape | `HexCopyResult(Text, Message)` with `Nothing` / `Copied` / `Refused` | The view decides only where each part goes |
| What runs on the thread pool | The read and the formatting together | Formatting a megabyte builds a 3 MB string, which is as much a UI stall as the read |
| A closed document | `ObjectDisposedException` becomes a message | The tab can close while the read is in flight |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3395 tests, 3393 passed, 2 skipped (PHASE06's platform skips),
    0 failed.
- Tests added (6) in `HexCopyTests`, over an in-memory `IByteSource`:
  - both formats and the range;
  - the read happens off the UI thread (the source records its thread);
  - a selection over the limit is refused without being read;
  - a read failure is a message;
  - a disposed document is a message;
  - no selection, or a text tab, copies nothing.
- `HexCopyMenuTests` and `HexViewTests` (through the window) pass unchanged.
- Documentation sweep (§7): nothing user-facing changed.

# FEATURE-BE30-PHASE01 — Selection model and copy formats

## Summary

The Core side of byte selection in the hex viewer, with no UI yet:

- `HexColumn` (`Hex`, `Text`) and `HexSelection` (anchor, active end, column; normalized `Start`/`End` inclusive,
  `Length`, `Contains`, `At`, `ExtendedTo`). Never empty; negative offsets and unknown columns are refused.
- `HexFormatter` now publishes the layout it writes: `HexCharIndex` / `TextCharIndex` (where a byte's pair and
  character stand in a row) and `ByteAt` (which column and byte a character belongs to — every character answers,
  gaps go to the nearest byte of their column). The copy formats: `FormatHexString` (`48 65 6C`),
  `FormatText` (the text column's rendering), `FormatAddressRange` (`0x00000001 - 0x00000004`), and
  `FormatOffset`, which `FormatRow` now uses too. `FormatRow`'s output is unchanged.
- `HexDocument.Read(start, count)` (short at the end), `HexDocument.OffsetDigits`, `HexDocument.MaxCopyBytes` (1 MiB).

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Hex/HexColumn.cs`, `src/Enigma.TextEditor.Core/Hex/HexSelection.cs`
- Modified: `src/Enigma.TextEditor.Core/Hex/HexFormatter.cs`, `src/Enigma.TextEditor.Core/Hex/HexDocument.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Hex/HexSelectionTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-BE30.md`

## Deviations & follow-ups

- The plan's `HexSelection.ClampedTo(length)` was not added: nothing needs it — hit-testing clamps an offset to the
  file before it makes a selection (PHASE03). `At` and `ExtendedTo` were added instead, which the mouse needs.
- Added beyond the plan: `HexFormatter.FormatOffset` (one place writes offsets for the rows and the range) and
  `HexDocument.OffsetDigits` (the view needs the width without recomputing it).
- `StatusBarViewModel.CaretText` still formats the offset itself (`X8`/`X16`); it could use `FormatOffset`. Left as is
  (out of scope).
- `FormatRow` writes its hex digits from a lookup table rather than `byte.ToString("X2")`: same output, and the same
  table writes `FormatHexString`.
- Pre-existing failures, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` (flaky, noted in FEATURE-34A1-PHASE04).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Shape of a hit-test answer | `(HexColumn Column, int ByteInRow)` tuple from `HexFormatter.ByteAt` | Two values, one caller kind; no type worth naming |
| A character left of the row (negative index) | The first hex pair, not an exception | A captured drag can leave the row on the left; the caller should not have to clamp first |
| Where the copy limit lives | `HexDocument.MaxCopyBytes` | The document owns the reads a copy makes |

## Build/test evidence

- Run baseline (taken on `feature/2026-11-10-hex-selection-copy` after the planning commit, before this branch):
  `dotnet build Enigma.TextEditor.slnx` — 0 warnings, 0 errors; `dotnet test --solution Enigma.TextEditor.slnx` —
  3257 tests, 3255 passed, 2 failed (the two pre-existing failures above).
- This dev: build 0 warnings, 0 errors; 3271 tests, 3269 passed, 2 failed (the same two).
- Tests added (`HexSelectionTests`, 14 cases): normalization both ways, one byte, extending, refusals; every pair and
  character of a row at 8 and 16 digits maps both ways; gutter, group gap, text gap, past the end; hex string, text,
  address range (8 and 16 digits, refused reversed range), offsets; `Read` across rows and at the end.

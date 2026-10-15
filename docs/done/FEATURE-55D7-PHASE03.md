# FEATURE-55D7-PHASE03 — Text file I/O

**Item:** FEATURE-55D7 — Text editor foundation
**Branch:** `feature/feature-55d7-phase03-text-file-io`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

`Enigma.TextEditor.Core.Documents` is now the layer every later feature reads and writes files
through.

- **Ported from MarkdownEditor:** `LineEndings`, `TextFileFormat`, `DocumentContent` and
  `DocumentFile`. The buffer holds LF-only text; the file's style is applied again on write.
- **Encodings.** `TextEncoding` and `TextEncodings` define the curated set: UTF-8, UTF-8 with BOM,
  UTF-16 LE/BE, Windows-1252, ISO-8859-1/-15, Windows-1250/-1251.
  - The code-page provider is registered lazily, once.
  - `TextEncodings.For(format)` maps any read format back to an entry. UTF-32, which is not offered,
    gets an ad-hoc entry.
- **`ContentSniffer`:**
  - `IsBinary`: a NUL byte in the first 8000 bytes, unless a UTF-16/32 BOM is present.
  - `DetectEncoding`: BOM, then strict UTF-8 (`Utf8.IsValid`), then Windows-1252.
- **`DocumentFile`:**
  - `ProbeAsync` reads 8000 bytes: size, plus whether the file looks binary.
  - `ReadAsync(path, encoding?)` detects the encoding, or forces one for *Reopen with encoding*. A
    forced read drops a BOM of the same code page.
  - `Decode` and `Encode` are pure.
  - `WriteAsync` refuses text the encoding cannot hold. It throws `UnrepresentableTextException`
    (the character, line and column) before writing a byte.
  - Files over 32 MB raise `FileTooLargeException`, an `IOException` carrying the size and the
    limit.
- **Indentation.** `Indentation(UseTabs, Size)` carries the status-bar label (`Spaces: 4` /
  `Tab Size: 4`). `IndentationDetector` decides:
  - tabs vs spaces by majority;
  - the space size from the indent steps between non-blank lines (2, or 4 with 8 counted as two
    levels);
  - the fallback when the file says nothing;
  - only the first 10,000 lines are read.

## Files / modules touched

**Created — `src/Enigma.TextEditor.Core/Documents/`**

- Ported: `LineEndings.cs`, `TextFileFormat.cs` (plus `With(TextEncoding)` and a reworded
  `DetectEncoding` remark), `DocumentContent.cs`.
- `DocumentFile.cs`: ported, then reworked.
- New: `TextEncoding.cs`, `TextEncodings.cs`, `ContentSniffer.cs`, `FileProbe.cs`,
  `FileTooLargeException.cs`, `UnrepresentableTextException.cs`, `Indentation.cs`,
  `IndentationDetector.cs`.

**Created — `tests/Enigma.TextEditor.Core.UnitTests/Documents/`**

- Ported: `LineEndingsTests.cs`, `TextFileFormatTests.cs`, `TempDocumentFolder.cs`.
- `DocumentFileTests.cs`: ported, then adapted to the new signature and the new exception.
- New: `EncodingRoundTripTests.cs`, `ContentSnifferTests.cs`, `IndentationDetectorTests.cs`.

## Deviations & follow-ups

- **`DocumentFile` builds the bytes itself.** It writes them with `File.WriteAllBytesAsync`
  (preamble + body) instead of `File.WriteAllTextAsync`. That is what lets the
  unrepresentable-character check run before anything touches the disk.
- **Writes are in place, not atomic, as in MarkdownEditor.** A crash mid-write can truncate the file.
  Follow-up: write to a temp file and replace.
- **A forced decode can lose bytes on save.** Reopening a legacy file as UTF-8 turns invalid bytes
  into U+FFFD, and saving then writes them. This is the user's explicit choice and matches other
  editors. Saving through the *detected* encoding never loses bytes.
- **A UTF-16 file without a BOM sniffs as binary.** Nothing distinguishes it. The hex prompt's *Open
  as text* plus *Reopen with encoding* covers that case later (FEATURE-3AE6, FEATURE-316E).
- **Fix cycles.** One cycle was used: an xUnit analyzer rule (xUnit2017) rejected
  `Assert.True(bytes.Contains(...))`, replaced by `Assert.Contains`.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to validate UTF-8 | `System.Text.Unicode.Utf8.IsValid` | Allocation-free and exception-free | Decoding with `throwOnInvalidBytes` and catching |
| Where the line and column of an unrepresentable character come from | A second scan over the normalized text, only on failure | Positions match what the editor shows (LF-only); no cost on a successful save | The exception's index in the CRLF-applied text (wrong columns on CRLF files) |
| A BOM-less file reopened as UTF-16 | Recorded with a BOM, which is written on save | UTF-16 without a BOM is indistinguishable from binary for every later reader | Keep it unmarked (the next open would offer the hex viewer) |
| Tab size of a tab-indented file | The fallback's size (setting, default 4) | A tab's width is a display preference, not something the file says | Guessing from alignment |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2062 passed**, 0 failed, 0 skipped.
- The acceptance criteria are covered by the new tests:
  - byte-for-byte round trips for UTF-8, UTF-8 BOM, UTF-16 LE/BE and Windows-1252, with CRLF;
  - invalid UTF-8 read as Windows-1252;
  - NUL means binary, UTF-16 with a BOM does not;
  - "日本" into Windows-1252 is refused and the file is left untouched;
  - 2-space, 4-space, tab and fallback indentation.

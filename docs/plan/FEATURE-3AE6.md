# FEATURE-3AE6 — Read-only hex viewer

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-3ae6-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

Open binary files in a **read-only hex viewer**, offered automatically when a binary file is opened,
and available for any file on demand.

## Context & constraints

- FEATURE-55D7 PHASE03 provides:
  - `ContentSniffer.IsBinary` (NUL in the first 8000 bytes, BOM exemption);
  - the 32 MB text cap (`FileTooLargeException`).
- FEATURE-3EE8 / FEATURE-04BC provide panes with tabs over `DocumentViewModel` buffers and the
  `DocumentRegistry`.
- Binary files can be far larger than 32 MB. A hex dump as text is about 5 characters per byte, so
  rendering through AvaloniaEdit is not an option.

## Decisions

| Decision | Rationale |
|---|---|
| Core `IByteSource` (`Length`, `Read(offset, Span<byte>)`, `IDisposable`); `FileByteSource` over `File.OpenHandle` (read, `FileShare.ReadWrite \| FileShare.Delete`) + `RandomAccess.Read` | Reads only the rows on screen; no size limit; does not lock the file against other writers |
| Core `HexFormatter.FormatRow(offset, bytes)` → `"00000010  48 65 6C 6C 6F 20 77 6F  72 6C 64 0A 00 01 02 03  Hello world....."` (16 bytes, 8+8 grouping, printable ASCII else `.`, offset width 8, or 16 hex digits above 4 GB) | The classic `hexdump -C` layout, readable by anyone |
| `HexDocumentViewModel` is a second buffer kind next to `DocumentViewModel`, behind a common `IBufferViewModel` (title, path, `IsReadOnly`, `IsDirty` = false). Tabs hold an `IBufferViewModel` | Tabs, splits, registry dedup and `:q` work unchanged; a file can be open as text and as hex at once (registry key: path + kind) |
| `HexView`: a `ListBox` with `VirtualizingStackPanel` over `HexRowList : IReadOnlyList<string>, IList` (rows formatted on demand, a small LRU of 64-row pages), monospace font, the selected row highlighted | Avalonia's virtualization handles millions of rows; no custom scrolling code |
| Keys in the hex view: arrows, PageUp/PageDown, Home/End, plus `j`/`k`/`gg`/`G`. No editing keys | The app is Vim-first; read-only |
| Status bar for a hex tab: `HEX` in the mode chip, `Offset 0x0000A0` instead of Ln/Col, the file size instead of encoding / EOL / Spaces, the language hidden | The text-only items do not apply |
| Binary detected on open → ContentDialog "*name* looks like a binary file." with **Open in hex viewer** (primary, default) / **Open as text** / Cancel | The draft: hex view suggested for binary files |
| A text file above 32 MB → "too large to edit as text" dialog offering **Open in hex viewer** / Cancel | The same fallback for the same reason |
| *Tools → Reopen as hex* / *Reopen as text* swaps the active tab's buffer kind in place (text side: through the unsaved-changes prompt) | A text file can be inspected as bytes and back |
| Startup args and `:e` go through the same detection | One open path |

## PHASE01 — Hex view

**Status:** DONE — see `docs/done/FEATURE-3AE6-PHASE01.md`
**Branch:** `feature/feature-3ae6-phase01-hex-view`

### Steps

1. Core: `IByteSource`, `FileByteSource`, `HexFormatter`, `HexRows` (row count, row text through a
   page cache).
2. Desktop:
   - `IBufferViewModel` extracted from `DocumentViewModel`; `HexDocumentViewModel`;
   - tabs and the registry hold `IBufferViewModel` (key path + kind);
   - the pane shows `HexView` for a hex tab and the editor for a text tab.
3. `IDocumentService.OpenHexAsync(path)`, reachable from *File → Open as hex…* in this phase.
4. Status-bar adaptation for hex tabs.
5. Tests:
   - formatter rows (full, partial last row, non-printables, offsets past 4 GB width);
   - `FileByteSource` reads at offsets and at the end;
   - `HexRows` count and paging;
   - pane switching between text and hex tabs;
   - headless: a 1 MB file shows only a screenful of realized rows and `G` reaches the last row.

### Acceptance criteria

- *File → Open as hex…* opens any file read-only in hex, of any size, with smooth scrolling.
- The text-editing commands are disabled on a hex tab; `:q`, `:bn`/`:bp`, splits and tabs work.
- Zero warnings, whole suite green.

## PHASE02 — Binary prompt & reopen as hex

**Status:** TODO
**Branch:** `feature/feature-3ae6-phase02-binary-prompt`

### Steps

1. `IDocumentService.OpenAsync(path)` sniffs the first 8000 bytes:
   - binary → the binary prompt;
   - over 32 MB → the too-large prompt;
   - otherwise text as before.

   *Open as text* forces the text path; above the cap it is still refused.
2. *Tools → Reopen as hex* / *Reopen as text* (enabled on file-backed tabs), swapping the tab's
   buffer in place.
3. Every open path uses the sniff: File → Open, startup args, `:e`, `:sp file`.
4. Tests (scripted prompt):
   - binary → hex;
   - binary + "Open as text" → text;
   - cancel opens nothing;
   - large text → hex;
   - reopen as hex/text round trip;
   - a dirty text buffer asks before reopening as hex.

### Acceptance criteria

- Opening a `.png`, `.exe` or `.zip` asks and, by default, opens the hex viewer.
- Opening a UTF-16 text file does not ask.
- Every open route behaves the same.
- Zero warnings, whole suite green.

## Out of scope

- Editing bytes, searching bytes, go-to-offset, selection and copy of hex ranges.
- Data inspector panels.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Rendering approach | Virtualized `ListBox` over on-demand rows | Built-in virtualization; constant memory | Hex dump in AvaloniaEdit (about 5× the file size in memory); custom scrolling control (more code to own) |
| File access | `RandomAccess` on a shared read handle | No size cap, no full load, does not block other writers | `File.ReadAllBytes` with a cap (binaries are often large) |
| Prompt default | Open in hex viewer | The draft says hex is suggested for binaries | Open as text by default |
| Navigation keys | Standard keys + `j`/`k`/`gg`/`G` | Vim-first app; read-only so no conflicts | Standard keys only |
| Reopen as hex/text | Included | Makes the binary prompt's "Open as text" reversible without closing the tab | Close and reopen by hand |

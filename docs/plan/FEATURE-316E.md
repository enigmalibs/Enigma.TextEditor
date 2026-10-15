# FEATURE-316E — Encoding, EOL & indent tools

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-316e-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

The Tools menu and status-bar items the draft asks for, beyond *Set language*:

- **Reopen with encoding**, **Change file encoding**, **Change line endings**;
- an indentation choice behind the status bar's *Spaces* item;
- the status bar complete: Vim mode · Ln/Col · Spaces · encoding · language · LF/CRLF.

## Context & constraints

- FEATURE-55D7 PHASE03 provides:
  - `TextEncodings` (the curated list);
  - `DocumentFile.ReadAsync(path, encodingOverride)`;
  - writes that refuse unrepresentable text;
  - `LineEndings`;
  - `IndentationDetector`.
- FEATURE-3EE8 provides `DocumentViewModel` (format, indentation, dirty), the Tools menu with *Set
  language…*, the ContentDialog picker pattern and the clickable status bar.
- The buffer always holds normalized text. The line-ending style is applied on save, so changing it
  is a format change, not a text edit.

## Decisions

| Decision | Rationale |
|---|---|
| *Reopen with encoding…*: picker → re-read the file from disk with that encoding. If the buffer is dirty, confirm first ("Discard unsaved changes and reopen?"). Disabled for untitled buffers. The caret is clamped and kept | It re-decodes bytes that were misread; unsaved edits would be lost, so the user decides |
| *Change file encoding…*: picker → the buffer's format gets the new encoding and BOM flag and is marked unsaved; the next save writes it | Notepad++ "Convert to" semantics: the user decides when to write |
| A save into an encoding that cannot hold the text is refused with a dialog naming the first unrepresentable character and its line; the buffer stays dirty | No silent data loss |
| Line endings: *Tools → Line endings ▸ LF / CRLF* (radio) → format change, marked unsaved | Same model as the encoding change |
| Indentation: *Tools → Indentation ▸ Spaces: 2 / Spaces: 4 / Tabs* (radio) → `DocumentViewModel.Indentation`, applied to the editor options (`ConvertTabsToSpaces`, `IndentationSize`) and to Vim (`expandtab`, `shiftwidth`) of every pane showing the buffer. Existing text is not converted | The draft's "Spaces (2/4 spaces, tabs)"; converting text is a different command |
| The buffer is not marked dirty by an indentation change | Nothing to write |
| Status-bar labels: `Spaces: 4` / `Tab Size: 4`, the encoding label (`UTF-8`, `UTF-8 with BOM`, `UTF-16 LE`, `Windows-1252` …), `LF` / `CRLF` | VS Code's wording, familiar |
| Status-bar items are buttons: encoding → a flyout with *Reopen with encoding…* / *Change file encoding…*; EOL and Spaces → flyouts with the radio items | Same commands, one click away |

## PHASE01 — Encoding tools

**Status:** DONE — see `docs/done/FEATURE-316E-PHASE01.md`
**Branch:** `feature/feature-316e-phase01-encoding-tools`

### Steps

1. `EncodingPickerViewModel` / `EncodingPickerView`: the list from `TextEncodings`, with the current
   one selected. Enter, double-click or OK picks.
2. `IDocumentService.ReopenWithEncodingAsync(document, encoding)` and
   `DocumentViewModel.ChangeEncoding(entry)`.
3. Tools menu entries *Reopen with encoding…* and *Change file encoding…*, enabled as decided.
4. Status-bar encoding button and flyout.
5. The unrepresentable-save dialog (through the existing message service).
6. Tests:
   - reopen with Windows-1252 over a UTF-8 file changes the text as expected;
   - dirty reopen asks first, and cancel keeps the buffer;
   - change to UTF-16 LE marks dirty and the save writes a UTF-16 LE BOM file;
   - change to Windows-1252 with "日本" refuses the save and keeps the buffer dirty;
   - headless: the status bar shows the encoding label.

### Acceptance criteria

- Both commands work from the Tools menu and the status bar.
- The status bar always shows the active buffer's encoding.
- No save ever writes replacement characters for text the encoding cannot hold.
- Zero warnings, whole suite green.

## PHASE02 — Line-ending & indentation tools

**Status:** TODO
**Branch:** `feature/feature-316e-phase02-eol-indent-tools`

### Steps

1. `DocumentViewModel.ChangeLineEndings(style)` (marks dirty) and `SetIndentation(indentation)` (raises
   a change).
2. The pane applies the indentation to its editor options and its Vim options on tab switch and on
   change.
3. Tools submenus *Line endings ▸* and *Indentation ▸*, as radio items reflecting the active buffer.
4. Status-bar Spaces and EOL buttons with flyouts.
5. Tests:
   - CRLF change then save writes CRLF bytes;
   - indentation Tabs sets `ConvertTabsToSpaces=false` and Vim `expandtab` off, so `>>` inserts a tab;
   - Spaces: 2 makes `>>` insert two spaces;
   - headless: the status bar shows `Spaces: 2` / `Tab Size: 4` and `LF` / `CRLF`.

### Acceptance criteria

- Line endings and indentation change from the Tools menu and the status bar.
- The status bar shows every draft item: Vim mode · Ln, Col · Spaces · encoding · language · LF/CRLF.
- Zero warnings, whole suite green.

## Out of scope

- Converting existing indentation (tabs ↔ spaces) in the text.
- Old-Mac CR-only line endings (read as LF, as in the ported layer).
- Encoding auto-detection beyond BOM / UTF-8 / Windows-1252.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Change-encoding semantics | Convert in memory, write on the next save | The user stays in control of when bytes change; one model for encoding and line endings | VS Code "Save with encoding" (writes immediately; surprising when the buffer has other unsaved edits) |
| Unrepresentable characters | Refuse the save with an explanation | Data integrity | Replace with `?` (silent loss) |
| Reopen over unsaved edits | Confirm first | Reopen discards edits | Refuse while dirty (needless friction) |
| Indentation change converts text? | No | The draft asks for the setting shown in the status bar; conversion is a separate tool | Convert automatically (destructive) |
| Encodings offered | UTF-8, UTF-8 BOM, UTF-16 LE/BE, Windows-1252, ISO-8859-1, ISO-8859-15, Windows-1250, Windows-1251 | Covers Unicode and the common Western/Central/Cyrillic legacy files | Every code page .NET knows (an unusable list) |

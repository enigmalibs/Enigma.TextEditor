# FEATURE-BE30 — Hex selection and copy menu

**Status:** DONE
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-be30-phase01-selection-model`
**Branch (PHASE02):** `feature/feature-be30-phase02-paint-selection`
**Branch (PHASE03):** `feature/feature-be30-phase03-mouse-selection`
**Branch (PHASE04):** `feature/feature-be30-phase04-copy-menu`
**Run:** feature/2026-11-10-hex-selection-copy

## Objective

Today the hex viewer selects whole rows only. Make it select **bytes**:

- A press and drag in the **hex** column selects bytes there only; the same in the **text** column selects there only.
  The two selections are dissociated: a drag never switches column.
- The bytes selected in one column are highlighted in the other (hex → their characters, text → their hex pairs).
- A context menu acts on the selection: **Copy Hex String**, **Copy Text**, **Copy Address Range**
  (`0x00000001 - 0x00000004`).

## Context & constraints

- Core (`src/Enigma.TextEditor.Core/Hex`, no Avalonia): `IByteSource` (`Length`, `Read(offset, Span<byte>)`),
  `FileByteSource`, `MemoryByteSource`, `HexDocument` (`Length`, `Rows`, owns the source), `HexFormatter`
  (`BytesPerRow` = 16, `OffsetDigits(length)` = 8 or 16, `FormatRow`), `HexRows` (virtual `IReadOnlyList<string>`,
  64-row pages, `OffsetOf(row)`, count capped at `int.MaxValue`).
- Row layout (`FormatRow`), with `d` offset digits: offset `[0, d)`, two spaces, byte `i` at
  `d + 2 + 3i (+1 when i ≥ 8)` as two hex digits and a space, one extra space, then the text column at `d + 52 + i`.
  A short last row pads the hex column so the text column stays aligned.
- Desktop: `HexView` (`Views/HexView.axaml[.cs]`) is a `ListBox` named `Rows` over `Document.Hex.Rows`, a `TextBlock`
  per row (class `row`, the pane's monospace font inherited), `SelectedIndex` bound to `EditorTabViewModel.HexRow`.
  The selected row is painted `EnigmaSelectionBrush` (BUG-78F9 PHASE02, test
  `HexViewTests.TheSelectedRow_IsMarkedInTheSelectionColour_NotTheSystemAccent`). Its keys: list navigation, plus
  `j`/`k`/`gg`/`G` and the Vim engine (`HexVimBuffer`) with Vim on. `Attach(pane, session)` gives it an
  `IVimSession` whose `ReportStatus` writes the status line.
- View state lives on the tab (`EditorTabViewModel`): two panes on one buffer are two places in it. The session stores
  `HexRow` only.
- Context-menu convention: `EditorPaneView.axaml` declares a `ContextMenu` with named `MenuItem`s, an `Opening`
  handler that enables them, and code-behind `Click` handlers that return the focus to the view. Clipboard:
  `TopLevel.GetTopLevel(control)?.Clipboard` + `SetTextAsync` (Avalonia 12 extension); fire-and-forget work goes
  through `TaskExtensions.Forget()` (failures reach the safety net).
- Build: `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, explicit usings, nullable. Tests: xunit.v3 on MTP;
  the Desktop suite drives the real `MainWindow` headless (`HeadlessAvaloniaFixture`, `EditorHarness`).

## Decisions

- **Selection model (Core).** `HexColumn { Hex, Text }`; `readonly record struct HexSelection(long Anchor, long Active,
  HexColumn Column)` with `Start`, `End` (inclusive), `Length`, `Contains(offset)`, and `ClampedTo(length)`. A
  selection is never empty: a press selects one byte.
- **Layout geometry (Core).** `HexFormatter` gains the positions it already lays out — the character index of a byte's
  hex pair and of its text character, and the reverse: which column and which byte of the row a character index falls
  on (offset gutter → hex byte 0; the gap between groups → the byte before it; the gap before the text → text byte 0;
  past the end → byte 15). One class owns the layout, so painting, hit-testing and `FormatRow` cannot drift apart.
- **Copy formats (Core).** `HexFormatter.FormatHexString(bytes)` → `48 65 6C 6C 6F` (upper case, single spaces, one
  line); `FormatText(bytes)` → the text column's own rendering (printable ASCII, else `.`);
  `FormatAddressRange(start, end, digits)` → `0x00000001 - 0x00000004` (inclusive end, offset width; one byte is
  `0x… - 0x…` too). `HexDocument.Read(start, count)` reads a range; `HexDocument.MaxCopyBytes` = 1 MiB.
- **Where the selection lives.** `EditorTabViewModel.HexSelection` (`HexSelection?`, observable). Kept across tab
  switches, independent per pane, not persisted.
- **Rendering.** The virtualized `ListBox` stays. The row `TextBlock` becomes `HexRowBlock` (a `TextBlock` subclass
  if its text rendering can be overridden in Avalonia 12, else a `Control` drawing its own `TextLayout`), told by
  `HexView` which bytes of its row are selected and in which column, painting them under its text: the selection's
  column in `EnigmaSelectionBrush`, the other column in the same brush at half opacity. A band is continuous across
  the spaces between selected pairs.
- **Current row.** Painted `EnigmaHoverBrush` (the editor's current-line colour) instead of the selection colour, and
  the pointer-over band is removed: the selection colour now belongs to the byte selection, and a band following the
  pointer would flicker during a drag.
- **Mouse.** Left press: select the byte under the pointer in the column pressed, capture the pointer, move the current
  row (`HexRow`) there, focus the rows. Drag: extend in the column the drag started in, clamping to that column's edge
  bytes when the pointer crosses into the other one. Above or below the list: scroll a row per move and extend.
  Shift+press: extend from the anchor in the selection's column. The press is handled before the list's own row
  selection.
- **Context menu.** On the rows: `Copy _Hex String`, `Copy _Text`, `Copy _Address Range`, enabled with a selection.
  A right-click outside the selection first selects the byte under the pointer; inside, it keeps the selection.
  Ctrl+C copies in the selection's own column format; that item shows `Ctrl+C`. A selection over `MaxCopyBytes`
  refuses hex and text copies with a status-line message; the address range is always copied. A read failure
  (`IOException`, `UnauthorizedAccessException`) is reported on the status line.

## PHASE01 — Selection model and copy formats

**Status:** DONE

### Steps

1. `Core/Hex/HexColumn.cs`, `Core/Hex/HexSelection.cs` (normalized `Start`/`End`/`Length`, `Contains`, `ClampedTo`).
2. `HexFormatter`: layout positions (hex pair start of a byte, text character of a byte, character index → column and
   byte), `FormatHexString`, `FormatText`, `FormatAddressRange`. `FormatRow` reuses the positions/text rendering
   where that keeps one source of truth, with its output unchanged.
3. `HexDocument.Read(long start, int count)` (reads what there is; short at the end) and `MaxCopyBytes`.
4. Tests (`tests/Enigma.TextEditor.Core.UnitTests/Hex`): selection normalization both directions, single byte,
   clamping; positions agree with `FormatRow` for every byte of a full row at 8 and 16 digits; hit-test of the gutter,
   both group gaps, the text gap and past the end; the three formats (incl. non-printables, 16-digit range);
   `Read` across a row boundary and at the end.

### Acceptance criteria

- Every hex pair and text character `FormatRow` writes maps back to its own byte and column.
- `FormatAddressRange(1, 4, 8)` is `0x00000001 - 0x00000004`.
- `FormatRow`'s output is unchanged (existing tests pass untouched).
- Zero new warnings, the whole suite green against the baseline.

## PHASE02 — Paint the selection in both columns

**Status:** DONE

### Steps

1. `EditorTabViewModel.HexSelection` (observable).
2. `Views/HexRowBlock.cs`: the row control; properties for the row's selected byte range, the selection's column and
   the selection brush; paints the primary band and the half-opacity mirror band under the text, using the PHASE01
   positions.
3. `HexView`: template uses `HexRowBlock`; on `ContainerPrepared` / index changes and on the tab's `HexSelection`
   changes (and on DataContext changes), give each realized row its slice of the selection.
4. Styles: current row `EnigmaHoverBrush`; no pointer-over band.
5. Tests (headless): a selection set on the tab reaches exactly the realized rows it spans with the right in-row
   ranges and column (hex and text, across rows, ending on a short last row); switching tabs and back keeps it; two
   panes on one buffer keep independent selections; the current-row test updated to `EnigmaHoverBrush`.

### Acceptance criteria

- A selection on the tab is painted in its column and mirrored in the other, on screen rows only.
- The current row no longer uses the selection colour.
- Zero new warnings, the whole suite green against the baseline.

## PHASE03 — Select bytes with the mouse

**Status:** DONE

### Steps

1. `HexView`: tunnel `PointerPressed`/`PointerMoved`/`PointerReleased` on the rows; hit-test the row under the pointer
   (realized container → index; `HexRowBlock` text layout → character index → PHASE01 mapping); clamp to the file.
2. Press, drag (column locked), Shift+press, capture, scroll-and-extend beyond the list's edges; the press sets
   `HexRow` and focuses the rows; the list no longer selects rows itself on a press.
3. Tests (headless, real pointer input on the window): press in hex selects one byte in hex; drag across rows in hex
   stays hex even when the pointer crosses into the text column; the same from the text column; Shift+press extends;
   a press on a short last row past its bytes clamps to the last byte; keys still move the current row.

### Acceptance criteria

- Dragging in the hex column selects only hex, in the text column only text; each mirrors the other.
- Zero new warnings, the whole suite green against the baseline.

## PHASE04 — Copy context menu

**Status:** DONE

### Steps

1. `HexView.axaml`: `ContextMenu` on the rows with `MenuCopyHex`, `MenuCopyText`, `MenuCopyRange`; `Opening`
   enables them and puts `Ctrl+C` on the item matching the selection's column.
2. Right-press outside the selection selects the byte under the pointer; inside, keeps it.
3. Handlers: read the selection through `HexDocument.Read`, format, `SetTextAsync` (via `Forget()`), focus the rows;
   the cap and read failures report on the status line. Ctrl+C on the rows copies in the selection's column format.
4. README *Hex viewer* paragraph: selecting bytes, the mirror, the menu and Ctrl+C.
5. Tests (headless): the menu's items and their enabled state with and without a selection; each item puts the
   expected text on the clipboard (hex, text, range; 16-digit range for a > 4 GB stand-in source if practical, else
   covered in Core); right-click outside moves the selection, inside keeps it; Ctrl+C in each column; a selection
   over the cap reports and leaves the clipboard alone while the range still copies.

### Acceptance criteria

- The three items copy `48 65 6C …`, the text as shown, and `0x… - 0x…` for the selection.
- Zero new warnings, the whole suite green against the baseline.

## Out of scope

- Keyboard selection (Shift+arrows), Select All, a byte caret.
- Selection length or offset of the active byte in the status bar.
- Window *Edit → Copy* on a hex tab.
- Persisting the selection in `session.json`.
- Editing bytes, searching, go-to-offset.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Rendering approach | Keep the virtualized `ListBox`; a row control paints the selection under its text | Keeps virtualization, the keys and their tests; the change stays local to a row | A custom virtualizing control (much more code to own); inline `Run` backgrounds (rebuilt per change, no continuous bands) |
| What a press does | Selects the byte under the pointer; drag extends; Shift+press extends | The menu always has something to act on; standard selection gestures | A caret with empty selections (needs a caret the viewer does not have) |
| Column dissociation | The column is fixed by the press; a drag clamps to that column's edge bytes | The draft: a drag in one column selects only that column | Switching column mid-drag |
| Where the selection lives | `EditorTabViewModel.HexSelection` | View state belongs to the tab; survives tab switches; panes independent | View-only field (lost on tab switch); the buffer (shared across panes) |
| Mirror highlight | Selection brush at half opacity | Reads as the same selection, follows the theme with no new colour | Outline rectangles; a new per-variant brush in `App.axaml` |
| Current-row marker | `EnigmaHoverBrush`, no pointer-over band | The selection colour must stay the byte selection's; matches the editor's current line | Keep the selection colour (hides the byte selection) |
| Hex string format | `48 65 6C 6C 6F` | What the column shows; pastes into most hex tools | `48656C6C6F`; `0x48, 0x65, …` |
| Text format | As the text column shows it | What you see is what you copy; no NULs or control characters on the clipboard | Latin-1 / UTF-8 decoding of the raw bytes |
| Address range format | `0x{start} - 0x{end}`, inclusive, offset width; same shape for one byte | The draft's example; one shape is easy to parse | Exclusive end; `start + length` |
| Large selections | Hex/text copy capped at 1 MiB with a status message; range never capped | A whole-file selection of a multi-GB file would exhaust memory | No cap; disabling items silently |
| Keyboard copy | Ctrl+C copies in the selection's column format | The universal copy key; cheap | Context menu only |
| Right-click | Outside the selection selects the byte; inside keeps it | The menu acts on what was clicked, without losing a selection made for it | Always keep; always reselect |
| Persist the selection | No | Session format unchanged; a selection is transient | New session field |

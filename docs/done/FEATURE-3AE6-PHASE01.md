# FEATURE-3AE6-PHASE01 — Hex view

**Item:** FEATURE-3AE6 — Read-only hex viewer
**Branch:** `feature/feature-3ae6-phase01-hex-view`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

**File → Open as Hex…** opens any file, of any size, in a read-only hex viewer tab.

**Core: `Enigma.TextEditor.Core.Hex`**

- `IByteSource`, with two implementations:
  - `FileByteSource`: `File.OpenHandle` (read; shares write and delete) plus `RandomAccess.Read`;
    nothing is loaded whole.
  - `MemoryByteSource`.
- `HexFormatter`: `hexdump -C` rows — 16 bytes in two groups, printable ASCII, offsets 8 digits, or 16
  above 4 GB.
- `HexRows`, a read-only `IReadOnlyList<string>` + `IList`:
  - rows are formatted on demand, 64 per page, the last 16 pages kept;
  - `IndexOf` reads the offset at the start of the row, so it is O(1).
- `HexDocument`, plus `FormatSize`.

**Desktop**

- **A hex buffer is a `DocumentViewModel` carrying a `HexDocument`.** It is read-only, never dirty,
  and its text is empty. Its title reads `name (hex)`. The registry keeps text and hex buffers of the
  same file apart (`FindHex`), and disposes the byte source with the buffer's last tab.
- **`HexView`**:
  - a virtualizing `ListBox` over the rows, monospace, at the editor's font size;
  - arrows, Page and Home/End come from the list;
  - with Vim on, `j` / `k` / `gg` / `G` move through the rows;
  - every other key goes to a Vim engine over an empty buffer (`HexVimBuffer`), so `:q`, `:bn`,
    `:sp`, `Ctrl+W …` work in a hex tab;
  - `:w` there says "The hex view is read-only", and `:wq` / `:x` just close.
- **The pane** shows the hex view or the editor according to the active tab, and focuses the right
  one.
- **Disabled on a hex tab:** Save / Save As, every Tools command, and the Edit menu.
- **The status bar on a hex tab:**
  - the chip reads HEX;
  - the caret item shows `Offset 0x000FFFF0`;
  - the file size replaces Spaces, encoding, language and EOL.

## Files / modules touched

**Created**

- Core: `Hex/IByteSource.cs`, `FileByteSource.cs`, `MemoryByteSource.cs`, `HexFormatter.cs`,
  `HexRows.cs`, `HexDocument.cs`.
- Desktop: `Views/HexView.axaml(.cs)`, `Views/HexVimBuffer.cs`.
- Tests: `tests/.../Core.UnitTests/Hex/HexTests.cs` (22 cases),
  `tests/.../Desktop.UnitTests/Views/HexViewTests.cs` (7).

**Modified**

- `ViewModels/DocumentViewModel.cs`: optional `HexDocument`, `IsHex`, title; a hex buffer refuses
  format changes.
- `ViewModels/EditorTabViewModel.cs`: `HexRow`.
- `Services/DocumentRegistry.cs`: kind-aware lookup; dispose on the last release.
- `Services/IDocumentService.cs`, `DocumentService.cs`: `OpenHexAsync`; saves refuse hex.
- `ViewModels/WorkspaceViewModel.cs`: `OpenHexAsync`, `OpenHexCommand`; Save/Save As disabled on hex.
- `ViewModels/ToolsViewModel.cs`: the tools are text-only.
- `ViewModels/ExCommandHandler.cs`: hex is read-only for `:w`; `:wq` = `:q`.
- `ViewModels/EditorPaneViewModel.cs`: `IsTextActive`, `IsHexActive`.
- `ViewModels/StatusBarViewModel.cs`: `IsHexTab`, `IsTextTab`, `IsModeChipVisible`, `SizeText`, the
  hex offset, HEX mode.
- `Views/EditorPaneView.axaml(.cs)`: hosts the hex view; focus and restore per kind.
- `Views/VimEditorBehavior.cs`: `Translate` is `internal`, shared with the hex view.
- `Views/MainWindow.axaml(.cs)`: *Open as Hex…*, the hex status items; the Edit menu ignores hex.

## Deviations & follow-ups

- **No `IBufferViewModel` interface.** The plan called for one, with tabs holding it. Hex is a mode
  of `DocumentViewModel` instead. The interface would have changed the type of every tab across
  about 150 call sites and tests, for no behavioural gain: tabs, splits, the registry, `:q` and the
  unsaved-changes rule all work unchanged with a buffer that is never dirty.
- **Over 32 GB.** Past 2^31 rows, the end of the file is not reachable: a list indexes with an int.
- **Not implemented, as planned out of scope:** selection/copy of byte ranges, go-to-offset, byte
  search.
- **Fix cycles used: 3 of 3.**
  1. A compiled-binding error: the visibility binding on the hex view resolved against its own
     DataContext, the tab. It now sits on a parent panel.
  2. The keystroke tests: the keys never reached the rows. A throwaway probe test, run alone and
     deleted, showed the cause: Avalonia 12's `ListBox` is not focusable by default.
  3. `Focusable="True"` on the rows; green.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How a hex tab is represented | A `DocumentViewModel` with a `HexDocument` | Smallest change that keeps every tab, pane and registry path working | A buffer interface and a second tab type |
| Vim in the hex view | Navigation keys handled by the view; everything else to a Vim engine over an empty buffer | `:q`, `:bn`, `:sp`, `Ctrl+W` behave as in text tabs; no second command-line implementation | No Vim in hex (menus only) |
| Byte-source lifetime | Disposed by the registry when the buffer's last tab closes | The file handle lives exactly as long as some tab shows the file | Disposing per tab |
| Hex tab font size | Inherited from the pane's editor font size | One zoom for both views | A separate setting |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2336 passed**, 0 failed, 0 skipped.
- Core:
  - full and short rows, printable mapping, 16-digit offsets, row counts;
  - reads at offsets and at the end;
  - the file stays writable and deletable while open;
  - on-demand paging, O(1) `IndexOf`, read-only list, sizes.
- Desktop:
  - read-only hex tab: commands disabled, HEX and size in the status bar;
  - text and hex of one file side by side, deduped;
  - `:w` refused, `:wq` closes, and the file can be deleted afterwards;
  - a hex tab splits;
  - a 1 MB file realizes under 200 rows, reads under 10 pages, `G` / `gg` / `j` / `k` move, with
    `Offset 0x000FFFF0` at the end;
  - `:q` typed in the hex view closes it;
  - the Edit menu targets nothing.
- Manual smoke: the Debug build launches normally.

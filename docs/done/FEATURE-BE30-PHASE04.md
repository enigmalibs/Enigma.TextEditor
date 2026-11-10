# FEATURE-BE30-PHASE04 — Copy context menu

## Summary

The hex view's rows have a context menu that acts on the selected bytes, and Ctrl+C copies them:

- **Copy Hex String** (`2C 20 68 65 78`), **Copy Text** (as the text column shows it), **Copy Address Range**
  (`0x00000005 - 0x0000000D`, as wide as the rows write offsets). Disabled with no selection; Ctrl+C is shown on
  the item for the selection's column.
- A right press inside the selection keeps it; elsewhere on a row it selects the byte under the pointer, in its
  column. Either way the rows take the keyboard and the press is handled before the list's own row selection; the
  release opens the menu as before. A click on an item gives the keyboard back to the rows.
- Ctrl+C on the rows copies the selection in its own column's format, with Vim on or off (a pending `:` line or
  Ctrl+W still takes the key first).
- Hex and text copies of more than `HexDocument.MaxCopyBytes` (1 MiB) are refused with a status-line message; the
  range is always copied. A read failure (`IOException`, `UnauthorizedAccessException`) is reported on the status
  line; a clipboard failure goes through `Forget()` to the safety net.
- README *Hex viewer*: selecting bytes, the mirror, the menu, Ctrl+C and the limit.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/HexView.axaml`, `src/Enigma.TextEditor.Desktop/Views/HexView.axaml.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HexCopyMenuTests.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-BE30.md`

## Deviations & follow-ups

- The 16-digit address range is covered in Core (`HexSelectionTests.AnAddressRange_…`) rather than headless: a
  stand-in file over 4 GB is not practical in a test, as the plan allowed.
- The keyboard opening of the menu is tested by raising `ContextRequested` on the rows, which is what the platform's
  Shift+F10 / Menu key gesture raises; the key itself is not simulated.
- Follow-ups left out of scope by the plan: *Edit → Copy* on a hex tab, Select All, keyboard selection, the selection
  length in the status bar. The release item should mention the feature and the current row's new colour (PHASE02).
- Pre-existing failures, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` (flaky).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Right-click on the other column's mirror of a selected byte | Inside the selection: kept | The byte is selected; the mirror is the same selection |
| Message over the limit | `The selection is too large to copy as hex: the limit is 1 MB` | The selection's own size reads as "1 MB" just over the limit, which says nothing |
| Ctrl+C with Vim on | Copies, unless a `:` line or Ctrl+W is pending | Ctrl+C had no meaning in the hex view; a pending sequence keeps every key |
| Focus after a menu click | Back to the rows | As the editor's menu returns it to the text |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BE30-PHASE01.md` (0 warnings; 3257 tests, 2 pre-existing failures).
- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3292 tests, 3290 passed, 2 failed (the two pre-existing failures).
- Tests added (`HexCopyMenuTests`, 8 cases): the items and their order; Ctrl+C shown per column; opened with no
  selection, all disabled; each item's clipboard text with the selection kept and the focus back; a right-click
  outside the selection; Ctrl+C in each column with Vim on and off; over the limit refused with the message and the
  clipboard untouched, the range still copied, exactly at the limit copied.
- One test was corrected before the first full run: its second right-click landed on the mirror of the selected
  byte, which rightly kept the selection; it now right-clicks an unselected byte.

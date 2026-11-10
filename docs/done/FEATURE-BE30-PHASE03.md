# FEATURE-BE30-PHASE03 — Select bytes with the mouse

## Summary

The mouse selects bytes in the hex view, in one column at a time:

- A left press on a row selects the byte under the pointer in the column it is over (the offset gutter counts as the
  hex column, the gap before the text as the text column), makes its row the current one (`HexRow`), gives the rows
  the keyboard and captures the pointer. The press is handled in the tunnel phase, before the list would select the
  row.
- A drag extends the selection in the column it started in. Over the other column the pointer stands on this column's
  nearest edge byte (the hex column's last, the text column's first). Above or below the rows on screen, each move
  scrolls one row and extends to it.
- Shift with a press extends the existing selection from its anchor, in its own column.
- A press past the last byte of a short last row selects that row's last byte. Presses off the rows (the scroll bar,
  below a short file's last row) are left to the list.
- The keys still move the current row and leave the selection as it is.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Views/HexView.axaml.cs`, `src/Enigma.TextEditor.Desktop/Views/HexRowBlock.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HexMouseSelectionTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-BE30.md`

## Deviations & follow-ups

- None against the plan's steps. A press below a short file's last row is left to the list rather than selecting the
  last byte: the plan did not say, and the area belongs to no row.
- Scrolling past the edges happens on pointer moves only: holding the pointer still below the rows does not keep
  scrolling. A timer-driven auto-scroll would be a follow-up if wanted.
- Pre-existing failures, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` (flaky).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Finding the row under the pointer | The lowest realized row starting at or above the pointer's height (clamped to the list) | Works for a pointer outside the list during a captured drag, and for the space below a short last row |
| Finding the byte across a row | `TextLayout.HitTestPoint` → character → `HexFormatter.ByteAt` | Exact for the font in use; the layout comes from the class that writes rows |
| Shift with a press in the other column | Same rule as a drag: that column's nearest edge byte | One rule for "extend to here"; the selection never changes column |
| The current row during a drag | Follows the active end | It is where the keys continue from, as a caret would be |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BE30-PHASE01.md` (0 warnings; 3257 tests, 2 pre-existing failures).
- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3284 tests, 3282 passed, 2 failed (the two pre-existing failures).
- Tests added (`HexMouseSelectionTests`, 8, real pointer input on the window): a press in each column and the current
  row; either digit or the trailing space of a pair; a drag from hex over the text and back up; a drag from text over
  the hex; Shift with a press, both directions; a press past a short last row in each column; the keys keep the
  selection; dragging below the rows scrolls and extends.

# FEATURE-BE30-PHASE02 — Paint the selection in both columns

## Summary

A byte selection held by the tab is painted on the hex rows on screen: in the column it was made in with the
selection colour, and in the other column with the same colour at half opacity.

- `EditorTabViewModel.HexSelection` (`HexSelection?`, observable; not stored in the session).
- `HexRowBlock`, a `TextBlock` subclass, replaces the row `TextBlock`. It reads its row's offset and byte count from
  its own text (a row starts with its offset), takes the tab's selection and the offset width through bindings, and
  paints one continuous band per column, just before its text (`RenderTextLayout`; `TextBlock.Render` is sealed in
  Avalonia 12). Recycled rows need nothing from code-behind: their text says which bytes they show.
- The current row (the list's selected item, where the keys are) is marked with `EnigmaHoverBrush`, the editor's
  current-line colour, instead of `EnigmaSelectionBrush`; the pointer-over band is gone.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/HexRowBlock.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/HexView.axaml`, `src/Enigma.TextEditor.Desktop/ViewModels/EditorTabViewModel.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HexSelectionViewTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HexViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-BE30.md`

## Deviations & follow-ups

- The plan had `HexView` push the selection and the row index to each realized row (`ContainerPrepared`). Bindings in
  the row template (`$parent[ListBox]` → the tab) plus the offset read from the row's text do the same with no
  code-behind and no recycling bookkeeping, so `HexView.axaml.cs` is unchanged.
- `HexRowBlock.RowOffset` parses the row's leading offset as `HexRows.IndexOfRow` already does; the two could share
  a `HexFormatter` helper. Left as two short parses.
- `HexViewTests.TheSelectedRow_IsMarkedInTheSelectionColour_NotTheSystemAccent` became
  `TheCurrentRow_IsMarkedInTheCurrentLineColour_NotTheSelectionColourOrTheSystemAccent`: the planned colour change,
  asserting the new brush and that the selection brush is not used — no assertion weakened.
- The README's 1.7 history line ("the hex view's current row in the editor's selection colour") describes what 1.7
  shipped and is left as history; the release item should mention the new colour.
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
  The flaky `FileDropViewTests.ThePaneWithNoTab_…` passed in this run.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How a row learns the selection and its own bytes | Template bindings to the tab; offset parsed from the row text | Survives recycling with no bookkeeping; no code-behind |
| Where the bands are drawn | `TextBlock.RenderTextLayout` override | `Render` is sealed in Avalonia 12; this hook runs right before the text |
| Band height | The row block's full height | Adjacent selected rows join into one block, as an editor's selection does |
| Proving the paint | A frame captured headless: selected vs plain pixels, full vs mirror | Geometry alone would not show that anything was drawn |

## Build/test evidence

- Baseline: see `docs/done/FEATURE-BE30-PHASE01.md` (0 warnings; 3257 tests, 2 pre-existing failures).
- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3276 tests, 3275 passed, 1 failed (pre-existing).
- Tests added (`HexSelectionViewTests`, 5): rows spanned (across rows, ending on a short last row); band geometry over
  pairs, the group gap and characters; painted pixels (full vs mirror, in either column); tab switch keeps the
  selection; two panes on one file select independently. Updated: the current-row colour test.

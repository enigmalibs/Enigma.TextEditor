# FEATURE-3546-PHASE01 — Minimap beside the editor

**Item:** FEATURE-3546 — Editor minimap (PHASE01 of 2)
**Branch:** `feature/feature-3546-phase01-minimap`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

Every text tab now has a minimap at the right of the text, in place of the vertical scrollbar. It is
Enigma.GitClient's diff minimap, adapted to an editor.

- **What it draws:** a 36 px strip in the surface colour. A window (wash + outline, GitClient's colours,
  light and dark) covers the part of the document on screen. A mark in the accent colour shows the
  caret's line. The window is not drawn when the whole document fits.
- **Scrolling:**
  - press or drag on it, and the document scrolls so the pointer is in the middle of the view;
  - the wheel over it scrolls the text by what the wheel over the text does (the text view's own
    50 px logical step).
- **Where:** text tabs only. A hex tab has no map; the hex view scrolls its own rows. The editor's
  vertical scrollbar is `Hidden`, so the wheel and the keys still scroll it. The horizontal scrollbar
  is unchanged.
- **How it knows:**
  - `EditorPaneView` finds the editor's `PART_ScrollViewer` when its template is applied, and reports
    offset / extent / viewport as fractions on every `ScrollChanged`;
  - it reports the caret's visual top (`TextView.GetVisualTopByDocumentLine`, so wrapped lines count
    at their real height) on caret and document changes;
  - it scrolls by setting the viewer's `Offset`, as GitClient does.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Views/EditorMinimap.cs` — the control:
  - `ViewportStart`/`ViewportEnd`/`CaretPosition` and four brushes;
  - `ScrollRequested`, `StartFor`, press/drag with capture;
  - `Render`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/EditorMinimapTests.cs` — 14 tests:
  - `StartFor` cases;
  - a press asks for the centred view;
  - text tab shows the map with the scrollbar hidden; hex tab has none;
  - a short document is all on screen;
  - pressing at the bottom scrolls to the end and the window follows;
  - the wheel over the map scrolls the text;
  - the caret mark.

**Modified**

- `src/Enigma.TextEditor.Desktop/App.axaml` — the minimap's `ControlTheme` (width 36, Hand cursor,
  brushes) and the theme-scoped viewport brushes.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml` — the editor and the map share row 1
  (`*,Auto`); `VerticalScrollBarVisibility="Hidden"`.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`:
  - scroll viewer lookup;
  - viewport and caret reporting;
  - scroll and wheel handlers;
  - `MinimapControl` accessor for tests.
- `docs/roadmap.md`, `docs/plan/FEATURE-3546.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What the fractions are measured against | The scroll viewer's extent, which includes AvaloniaEdit's room to scroll below the document | The caret mark and the window share one scale, so the mark sits inside the window around it | The document height (the mark would drift out of the window near the end) |
| How the view is scrolled | The scroll viewer's `Offset` | `TextEditor.ScrollToVerticalOffset` is a no-op in AvaloniaEdit 12 (decompiled: it only applies the template) | `ScrollToVerticalOffset` |
| Wheel step over the map | The text view's `ILogicalScrollable.ScrollSize.Height` | Same distance as the wheel over the text | A fixed number of lines |
| Where the brushes live | A `ControlTheme` + `ThemeDictionaries` in `App.axaml` | GitClient's shape; the pane XAML stays lean | Brushes set on the instance |

## Deviations & follow-ups

- Fix cycles: 2.
  - Cycle 1: a missing `using` in the new test file.
  - Cycle 2: the caret test assumed fractions of the document height. The implementation measures
    against the scroll extent, as decided above, so the test now asserts that formula. This is the
    defined behaviour, not a weakened assertion.
- **Found, not fixed (out of scope):** `EditorPaneView.RestoreViewState` calls
  `Editor.ScrollToVerticalOffset` / `ScrollToHorizontalOffset` to put a tab's scroll back on a tab
  switch. In AvaloniaEdit 12 those calls do nothing, so a tab probably comes back scrolled to its
  caret rather than where it was left. Suggest a BUG item: restore through the scroll viewer's
  `Offset`.
- Follow-up suggestions: search hits on the map; a map for the hex view.
- Visual check: the real window was rendered headlessly to a PNG (temporary scratch test, deleted).
  The strip, window and caret mark appear as in GitClient.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong. The README does not describe the scrollbar. The Settings list is updated in
PHASE02, which adds the setting.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2524 passed**, 0 failed, 0 skipped (2510 before,
  +14).

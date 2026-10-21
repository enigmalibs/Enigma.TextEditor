# FEATURE-3546 — Editor minimap

**Status:** DONE — see `docs/done/FEATURE-3546-PHASE01.md`, `docs/done/FEATURE-3546-PHASE02.md`
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-3546-phase01-minimap`
**Branch (PHASE02):** `feature/feature-3546-phase02-minimap-setting`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

A minimap beside the text, like the one in Enigma.GitClient's diff view. It replaces the thin vertical
scrollbar, so the document is easy to scroll with the mouse. A setting brings the scrollbar back; the
minimap is the default.

## Context & constraints

- Enigma.GitClient's `Controls/Diff/DiffMinimap.cs`:
  - a `Control` drawn in `Render`: a track, coloured runs of rows, and a window over the visible part
    (fill + outline);
  - it is told `ViewportStart`/`ViewportEnd` as fractions and raises `ScrollRequested(start)` on press
    and drag (pointer capture);
  - `StartFor(y, height, viewportFraction)` centres the window on the pointer;
  - 36 px wide (trackpad-friendly), cursor Hand; its brushes come from a `ControlTheme`;
  - `DiffViewerView` sets the editor's `VerticalScrollBarVisibility="Hidden"`. It finds the editor's
    `PART_ScrollViewer` and drives its `Offset`; the comment there says `ScrollToVerticalOffset` does
    not move the view in AvaloniaEdit 12.
- Here, `EditorPaneView` shows one AvaloniaEdit `TextEditor` per pane (row 1 of its grid), or the
  `HexView` for a hex tab. The editor has `VerticalScrollBarVisibility="Auto"`.
- `EditorPreferences` carries the live editor preferences (font size, word wrap) seeded from
  `AppSettings`. `SettingsMenuViewModel` toggles them and writes the settings (e.g. Word Wrap).

## Decisions

| Decision | Rationale |
|---|---|
| New `Views/EditorMinimap` control, ported from `DiffMinimap`: track, caret-line mark, viewport window, press/drag to scroll, wheel forwarded to the editor | "Like the diffs view"; the caret mark is the editor's equivalent of "where am I" |
| No text rendering and no search-hit marks | GitClient's reasoning (a picture of the code at 2 px says less than a mark); search hits are a follow-up |
| 36 px wide, same viewport colours as GitClient (theme-scoped resources) | Same look and same reach as the app the user points to |
| Text tabs only; the hex view keeps its own scrolling | The hex rows are virtualised by the hex view; out of scope |
| The editor's vertical scrollbar is hidden while the minimap shows; the horizontal one stays | The map is the vertical scrollbar |
| PHASE02: `AppSettings.ShowMinimap` (default `true`), *Settings → Minimap* check item, live through `EditorPreferences.ShowMinimap`; off shows the scrollbar again and hides the map | The draft: configurable, minimap by default |

## PHASE01 — Minimap beside the editor

**Status:** DONE — see `docs/done/FEATURE-3546-PHASE01.md`

### Steps

1. `Views/EditorMinimap.cs`:
   - styled properties: `LineCount`, `CaretLine`, `ViewportStart`, `ViewportEnd`, `TrackBrush`,
     `CaretBrush`, `ViewportBrush`, `ViewportBorderBrush`;
   - `ScrollRequested` (start fraction) and `StartFor`, as in `DiffMinimap`;
   - the wheel is not the map's: it raises `WheelRequested` (or bubbles) for the view to scroll the
     editor by;
   - `Render`: track, caret mark (minimum height as GitClient's), viewport window (none when everything
     is on screen).
2. Theme: a `ControlTheme` for it (width 36, Hand cursor, brushes). The viewport colours are
   theme-scoped (`ThemeDictionaries`, light and dark) in `App.axaml`.
3. `EditorPaneView`:
   - row 1 becomes the editor and the minimap (`*,Auto`); the map shows with a text tab;
   - the editor's vertical scrollbar is `Hidden`;
   - find the editor's `PART_ScrollViewer` (template applied), report offset/extent/viewport to the
     map on `ScrollChanged`, scroll on `ScrollRequested`;
   - report the caret line and line count on caret/document changes.
4. Tests:
   - control: `StartFor` maths; a press raises `ScrollRequested` at the pointer's fraction; render does
     not throw with no lines;
   - headless pane: the map shows beside a text tab and not for a hex tab; scrolling the editor moves
     the window; a press near the bottom scrolls the editor to its end; the caret line is reported.

### Acceptance criteria

- Every text tab shows a minimap at the right of the text instead of the vertical scrollbar.
- Clicking or dragging on it scrolls the document there; the wheel over it scrolls the text.
- It shows which part is on screen and the caret's line.
- Zero warnings, whole suite green.

## PHASE02 — Minimap or scrollbar setting

**Status:** DONE — see `docs/done/FEATURE-3546-PHASE02.md`

### Steps

1. `Core/Settings/AppSettings.cs`: `ShowMinimap` (default `true`).
2. `EditorPreferences.ShowMinimap`, seeded from the settings.
3. `SettingsMenuViewModel`: `ToggleMinimapCommand`, `IsMinimap`, writes the settings.
4. `MainWindow.axaml`: *Settings → Minimap* check item next to *Word Wrap*.
5. `EditorPaneView`: the map's visibility and the editor's `VerticalScrollBarVisibility` follow
   `Preferences.ShowMinimap` (Hidden with the map, Auto without).
6. Tests: settings default and round-trip; the toggle flips the preference and writes the settings;
   headless: off hides the map and shows the scrollbar, on again restores it.

### Acceptance criteria

- *Settings → Minimap* switches every pane between the minimap and the thin scrollbar at once, and is
  remembered.
- A settings file without the key shows the minimap.
- Zero warnings, whole suite green.

## Out of scope

- Search hits, selections or diagnostics on the map; a rendered picture of the code.
- A minimap for the hex view.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What the map draws | Viewport window + caret line | The diff map draws "where things are"; for an editor that is the caret | Rendered code (VS Code style): costly, and not what GitClient does |
| Width | 36 px | GitClient's, chosen there for trackpads | A narrower 14–20 px strip |
| Setting shape | One boolean, Settings → Minimap | Two states asked for; matches Word Wrap | An enum with a third "both" state |
| Wheel over the map | Scrolls the text | The point of the feature is mouse scrolling | Ignore the wheel there (GitClient's behaviour) |

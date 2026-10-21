# FEATURE-3546-PHASE02 — Minimap or scrollbar setting

**Item:** FEATURE-3546 — Editor minimap (PHASE02 of 2)
**Branch:** `feature/feature-3546-phase02-minimap-setting`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

*Settings → Minimap* switches every pane between the minimap and the editor's own thin vertical
scrollbar, at once, and the choice is remembered. The minimap is the default.

- **Setting:** `AppSettings.ShowMinimap`, default `true`. A settings file written before this key
  existed shows the minimap.
- **Live value:** `EditorPreferences.ShowMinimap`, seeded from the settings and set by the toggle,
  as Word Wrap is.
- **Menu:** *Settings → Minimap*, a check item under *Word Wrap*. `SettingsMenuViewModel` gains
  `ToggleMinimapCommand` and `IsMinimap`, and writes the settings.
- **Pane:**
  - the map shows when the tab is text *and* the setting is on (`MultiBinding` with
    `BoolConverters.And`);
  - the editor's `VerticalScrollBarVisibility` is `Hidden` with the map and `Auto` without, through
    `EditorPaneView.VerticalScrollBarFor`.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Settings/AppSettings.cs` — `ShowMinimap`.
- `src/Enigma.TextEditor.Desktop/ViewModels/EditorPreferences.cs` — `ShowMinimap`.
- `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs` — `ToggleMinimapCommand`,
  `IsMinimap`, `OnToggleMinimapAsync`.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — *Settings → Minimap*.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml` — the map's visibility and the
  scrollbar follow the preference.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs` — the `VerticalScrollBarFor`
  converter.
- `README.md` — the Settings sentence names the minimap.
- Tests:
  - `tests/…Core.UnitTests/Settings/AppSettingsTests.cs` — the default, the round trip, and a file
    without the key;
  - `tests/…Desktop.UnitTests/ViewModels/SettingsMenuTests.cs`:
    - the toggle applies and is written;
    - the Settings menu lists *Minimap*, checked;
  - `tests/…Desktop.UnitTests/Views/EditorMinimapTests.cs`:
    - the toggle switches both panes of a split between map and scrollbar, and back;
    - a pane opened with the setting off has the scrollbar.
- `docs/roadmap.md`, `docs/plan/FEATURE-3546.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the scrollbar follows | A static `FuncValueConverter` on the view, used in a binding | Declarative, beside the map's own binding | A `PropertyChanged` handler in code-behind |
| Where the menu item goes | Right under *Word Wrap* | Both say how the text is shown | A *View* menu item |

## Deviations & follow-ups

- None from the plan. Fix cycles: 0.
- The menu-structure test lists the Settings items exactly, so it now includes *Minimap*. This is the
  behaviour change itself.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the *Settings* sentence listed every setting and now needed the minimap. Updated to
  name it and say what turning it off gives.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2527 passed**, 0 failed, 0 skipped (2524 before,
  +3).

# FEATURE-2860-PHASE02 — Configurable resize step

**Item:** FEATURE-2860 — Quick pane and tab keys (PHASE02 of 2)
**Branch:** `feature/feature-2860-phase02-resize-step`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

How far the resize keys move a splitter is now a setting, in percent of the split. At 4, one
`Ctrl+W >` (or `g>`) takes a 50/50 split to 54/46, as the draft's example.

- **Setting:** `AppSettings.PaneResizeStep`, 1–50, default 5 (the step the keys always had). A value
  outside the range makes the file invalid, as every other range does: it is moved aside and the
  defaults load.
- **Live value:** `EditorPreferences.PaneResizeStep`. `WorkspaceViewModel.ResizeStep` (was a constant
  0.05) is now that value / 100, read on every key, so a change applies at once.
- **Menu:** *Settings → Pane Resize Step* with 2%, 5% and 10% as radio items, written to the
  settings. Any other step can be set in `settings.json`; the menu then checks none of its three.
- **Keys:** every resize key goes through it — `Ctrl+W + - > <` and PHASE01's `g+ g- g> g<`.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Settings/AppSettings.cs` — `PaneResizeStep` and its three constants.
- `src/Enigma.TextEditor.Desktop/ViewModels/EditorPreferences.cs` — `PaneResizeStep`.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `ResizeStep` from the preference.
- `src/Enigma.TextEditor.Desktop/ViewModels/WindowCommandHandler.cs` — reads the workspace's step.
- `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs` — three commands, three radio
  flags.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — the submenu.
- `README.md` — the Settings sentence names the step.
- Tests:
  - `tests/…Core.UnitTests/Settings/AppSettingsTests.cs` — default 5, round trip;
  - `tests/…Core.UnitTests/Settings/SettingsLoaderTests.cs` — 0 and 51 are refused, the file is
    quarantined;
  - `tests/…Desktop.UnitTests/ViewModels/WindowCommandTests.cs` — at 4, Wider gives 46/54; a live
    change to 10 applies at once;
  - `tests/…Desktop.UnitTests/ViewModels/SettingsMenuTests.cs`:
    - the menu commands apply and write;
    - an unlisted step checks nothing;
    - the Settings menu lists the submenu with 5% checked;
  - `tests/…Desktop.UnitTests/ViewModels/SplitPaneTests.cs` — the step is read from the workspace
    instance.
- `docs/roadmap.md`, `docs/plan/FEATURE-2860.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the menu item goes | Under *Minimap*, before the "new document" defaults | It is about the view, like Word Wrap and Minimap | A *View* menu entry |
| Menu labels | `2%`, `5%`, `10%` | Short and unambiguous | "Fine / Normal / Coarse" |

## Deviations & follow-ups

- None from the plan. Fix cycles: 0.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the Settings sentence now lists the resize step and its range.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2635 passed**, 0 failed, 0 skipped (2630 before,
  +5).

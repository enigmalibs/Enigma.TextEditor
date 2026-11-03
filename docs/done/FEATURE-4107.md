# FEATURE-4107 — New tab on a tab strip double-click

**Item:** FEATURE-4107 — New tab on a tab strip double-click
**Branch:** `feature/feature-4107-new-tab-double-click`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

A double-click on the empty part of a pane's tab strip opens a new untitled buffer in a new tab of that pane. That is
the area right of the last tab, or below the tabs. A double-click on a tab, its close button or the scrollbar opens
nothing.

- **Workspace:** `NewDocument(EditorPaneViewModel pane)` activates the pane and opens the buffer there (File → New's
  `NewDocument()`). A pane outside the workspace is refused.
- **Host seam:** `IPaneHost.NewDocument(pane)`, reached through `EditorPaneViewModel.NewTab()`.
- **View:**
  - `EditorPaneView` listens for `DoubleTapped` on the strip (`handledEventsToo`, since the list takes the presses);
  - the source must have no `ListBoxItem` or `ScrollBar` ancestor;
  - the pane's editor takes the focus once it shows the buffer.
- **Help:** *Files & tabs* gains "Double-click the tab strip — New untitled buffer in a new tab of that pane".
- **README:** the *Tabs and panes* paragraph mentions it.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/IPaneHost.cs`, `EditorPaneViewModel.cs`, `WorkspaceViewModel.cs`,
  `HelpCatalog.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`, `README.md`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/WorkspaceViewModelTests.cs`,
  `EditorPaneViewModelTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/TabStripViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-4107.md`; created this file

## Deviations & follow-ups

- **The plan's `Button` check is not separate:** the close button sits inside its `ListBoxItem`, so the `ListBoxItem`
  check already covers it.
- **Pre-existing failure** (baseline, not touched):
  `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` fails with
  `IOException: Pipe is broken` on this machine, before and after the change.
- **Release notes** are left to the next release item.
- **Documentation sweep:** README *Tabs and panes* and the help row were edited as planned. Nothing else describes
  the strip.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Focus after the new tab | Posted `FocusEditor()` at `Loaded` priority | When the pane already had the keyboard, `SplitHost` does not move focus (no `ActivePane` change). The editor can only take the new buffer's focus once it shows it |
| Where the handler lives | `Strip` (the border), not `Tabs` | It also covers the strip's band below the list, which `Tabs` does not span |

## Build/test evidence

- **Baseline** (run branch `feature/2026-11-02-tab-strip-file-explorer` at `3d25b13`, before the first dev):
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2945 total, 2944 passed, 1 failed
    (`InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, "Pipe is broken",
    reproduced twice).
- **After:**
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2951 total, 2950 passed, 1 failed (the baseline's).
- **Tests added (+6):**
  - workspace: a new document in the pane without the keyboard lands at its end and activates it. A pane outside
    the workspace is refused, and nothing changes;
  - pane: `NewTab()` asks its host;
  - real window (`TabStripViewTests`): a double-click right of the last tab opens `Untitled-1` there, active and with
    the keyboard. One on a tab opens nothing. One on the other pane's strip opens the tab there and activates that
    pane.

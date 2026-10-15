# FEATURE-04BC-PHASE01 — Layout tree & split host

**Item:** FEATURE-04BC — Multi-split panes
**Branch:** `feature/feature-04bc-phase01-split-host`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The content area now splits into any arrangement of editor panes, using Enigma.FileExplorer's split
model.

**Ported from FileExplorer (`ce34411`)**

- `LayoutTree` and its nodes, verbatim in `Enigma.TextEditor.Core.Layout`, with their 22 tests.
- `SplitHost`:
  - nested grids with 3 px splitters;
  - the ratio is written back when a drag ends;
  - pane views are kept across rebuilds;
  - a pointer press or focus inside a pane activates it;
  - focus follows the active pane.

**`WorkspaceViewModel`, reworked around the tree**

- One `EditorPaneViewModel` per leaf, plus `ActivePaneId` / `ActivePane`, `PaneFor`, `IdOf` and
  `Activate`.
- `Split(axis)`: the new pane shows the active buffer at the same caret and scroll, and takes the
  keyboard.
- `ClosePaneAsync`: closes every tab through the unsaved-changes rule, so a buffer still shown
  elsewhere is not asked about. The last pane never closes.
- `OnlyAsync`, `Equalize`, `Focus(direction)`, `FocusNext/Previous`, `Resize` (0.05 step).
- `LayoutChanged` with FileExplorer's batch semantics.
- A pane that loses its last tab closes, unless it is the only pane. `:q` in a split therefore
  closes the split, and `:q` on the last tab of the last pane quits.

**Shell**

- The active pane gets an accent frame when there are several. `IsActive` and `IsFramed` live on the
  pane view-model.
- File, Edit and Tools act on the active pane, and the status bar follows it.
- **View menu:** Split Right, Split Down, Close Pane, Only This Pane, Equalize Panes, Focus Next Pane,
  Focus Previous Pane.
- **One Vim engine per pane.** Only the active pane's engine reports to the status bar, through a
  `PaneVimSession` wrapper. A pane that takes the keyboard announces its own mode again.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Core/Layout/`: `FocusDirection`, `LayoutNode`, `LayoutRect`, `LayoutSnapshot`,
  `LayoutTree`, `PaneId`, `PaneNode`, `SplitAxis`, `SplitNode` (ported).
- `src/Enigma.TextEditor.Desktop/Views/SplitHost.cs` (ported), `Views/PaneVimSession.cs`.
- Tests:
  - `tests/Enigma.TextEditor.Core.UnitTests/Layout/LayoutTreeTests.cs` (ported);
  - `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SplitPaneTests.cs` (10);
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SplitHostTests.cs` (3).

**Modified**

- `ViewModels/WorkspaceViewModel.cs`: rewritten around `LayoutTree`, with the View commands.
- `ViewModels/EditorPaneViewModel.cs`: `IsActive`, `IsFramed`.
- `Views/EditorPaneView.axaml(.cs)`:
  - pane frame;
  - per-pane Vim session wrapper and mode announce on activation;
  - null-variant guard (below).
- `Views/VimEditorBehavior.cs`: public `Announce()`.
- `Views/MainWindow.axaml`: `SplitHost` instead of a single pane, the View menu, the splitter style.

## Deviations & follow-ups

- **Not ported: FileExplorer's `ViewMode.cs`.** It is a file-list concept with no use here.
- **No key gestures on the View menu.** The plan left them out. The Vim `Ctrl+W` keys arrive in
  PHASE02. Avalonia's `KeyGesture` cannot express a chord such as `Ctrl+W, V` for display.
- **A pane whose last tab is closed with the tab's ✕ closes too,** not just with `:q`. Same rule as
  VS Code's editor groups; consistent with the plan's `:q` semantics.
- **Bug found by the split tests and fixed.** A pane view detached during a layout rebuild raised
  `ActualThemeVariantChanged` with a null variant, and `EditorAppearance.Apply` threw. The handler
  now ignores the null.
- **Fix cycles used: 3 of 3.**
  1. xUnit analyzer rule in a new test (`Assert.Contains`).
  2. The null-variant crash above.
  3. A test that assumed a click leaves the caret alone.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which engine speaks to the status bar | Only the active pane's (`PaneVimSession` filter) | One mode chip; inactive engines still reset on tab switches | Shared engine across panes (per-pane state like visual marks would clash) |
| Closing a pane | Each tab goes through the normal close rule; a cancel leaves the pane with its remaining tabs | One rule for every way a buffer can be lost | All-or-nothing pre-check |
| What a split shows when the active pane is empty | An empty pane | Nothing to duplicate | Refusing to split |
| The split ratio | Written back on drag end (FileExplorer) | The layout model stays the source of truth | Live binding of grid lengths |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2215 passed**, 0 failed, 0 skipped. That includes:
  - 22 ported layout tests;
  - split / shared buffer / close-with-shared-dirty / close-cancel / last pane / `:q` in a split /
    only / focus / resize-equalize / file commands on the active pane;
  - headless: two editors side by side over one document with the frame on the active one; a click
    activating a pane, with the status bar following; closing a pane removes its editor.
- Manual smoke: the Debug build launches normally.

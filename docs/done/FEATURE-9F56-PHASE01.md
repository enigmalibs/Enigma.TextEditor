# FEATURE-9F56-PHASE01 — Space m keys move the tab

**Item:** FEATURE-9F56 — Move tabs between panes
**Branch:** `feature/feature-9f56-phase01-move-tab-keys`
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Summary

`<Space>mh`, `<Space>mj`, `<Space>mk` and `<Space>ml` move the active tab to the pane on the left, below, above or on
the right — the pane `gh`, `gj`, `gk`, `gl` would reach — and that pane takes the keyboard.

- **The tab itself moves**, so its caret, scroll and hex row come with it; it goes to the end of the target's strip.
- **A buffer the target already shows** is not given a second tab there: the target's tab is shown, and the moved
  one is dropped (one view released in the registry). Nothing is asked: the buffer stays open.
- **The pane the tab leaves** closes when it has no tab left, as closing that tab would have closed it.
- **No pane on that side**, one pane, no tab: nothing happens, silently, as `gh` at the window's edge.
- **Keys:** two entries more in the leader table (`m` and a direction → `VimAction.MoveTab`), four window commands
  `MoveTabLeft/Down/Up/Right`, mapped by the engine and carried out by `WindowCommandHandler` through
  `WorkspaceViewModel.MoveTab(FocusDirection)`. Like every leader key: works over a selection, a count is ignored,
  `.` never repeats it, `"a` in front is dead. The hex view's engine reports to the same handler, so the keys work
  from a hex tab too.
- **Help:** a Panes row `Space mh, mj, mk, ml`. **README:** the *Tabs and panes* paragraph names the keys.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs`, `src/Enigma.TextEditor.Core/Vim/VimAction.cs`,
  `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`, `src/Enigma.TextEditor.Core/Vim/VimWindowCommand.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/EditorPaneViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/WindowCommandHandler.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-9F56.md`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/KeySequenceParserTests.cs`,
  `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimWindowCommandTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/MoveTabTests.cs`, this file

## Deviations & follow-ups

- None from the plan. `MoveTab(source, tab, target)` is public on the workspace already, ready for the drop in
  PHASE02.
- Baseline: no pre-existing warning or failing test.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Order of the move | Off the source strip first, then onto the target | The same tab instance never sits in two bound strips at once; the source shows its neighbour before the tab leaves (`Remove`'s rule) |
| Registry on a merge | `Release` once, no `Retain` | The target's tab already holds the buffer; one view went away |
| Events | `DocumentShown` raised after the move | A move from the settings page's menu path brings the files back, as every other way of showing a buffer does |

## Build/test evidence

- **Run baseline** (run branch at `df9d687`, before this dev): `dotnet build Enigma.TextEditor.slnx` — 0 warnings,
  0 errors; `dotnet test --solution Enigma.TextEditor.slnx` — 2780 passed, 0 failed, 0 skipped. Later devs of this
  run cite this record.
- **This dev:** `dotnet build Enigma.TextEditor.slnx` — 0 warnings, 0 errors; `dotnet test --solution
  Enigma.TextEditor.slnx` — 2806 passed, 0 failed, 0 skipped (+26).
- **Tests added:**
  - core parser: `<Space>mh/mj/mk/ml` → `MoveTab` in normal, visual and line-visual mode; `<Space>m`, `2<Space>m`
    pending; `<Space>mx`, `<Space>mm`, `<Space>m<Esc>`, `"a<Space>ml` dead;
  - core engine: each key raises its window command and leaves text, mode and pending state alone; works in visual
    mode; `.` does not repeat it;
  - desktop (`MoveTabTests`): the tab instance moves with caret and scroll and the target takes the keyboard; the last
    tab's pane closes; a buffer already in the target is shown, not doubled, without a question; a dirty sole view
    moves without a question; no neighbour, one pane, own pane, wrong source → nothing; the four window commands
    there and back on both axes; typed in the real window, `Space ml` then `Space mh` move the tab and its caret.
- `HelpTests.EveryWindowCommand_HasALine` covers the new commands.

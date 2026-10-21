# FEATURE-684E — :q on the last tab keeps the app

**Item:** FEATURE-684E — :q on the last tab keeps the app
**Branch:** `feature/feature-684e-last-tab-keeps-app`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

`:q` on the last tab no longer quits the application. The tab closes and the pane shows its
placeholder, *No file is open. Use File → Open… or File → New.*, as when the tab is closed any other
way.

- **Every "close this tab" spelling** follows: `:q`, `:q!`, `:wq`, `:x`, `ZZ`, `ZQ` and `Ctrl+W q`.
  They all end in `ExCommandHandler.QuitAsync`, which now only closes the tab.
- **Quitting** is `:qa` / `:qa!`, File → Exit (Alt+F4) or the window's close button, all unchanged.
- **Splits:** `:q` on a split pane's last tab still closes that pane. Only the last pane is left
  standing, empty.
- **Help:** the `:q` line says "the app stays open". The workspace and handler remarks say the same.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — `QuitAsync` closes the tab only;
  remarks; the shutdown is for `:qa`.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — remarks, `shutdown` parameter doc.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the `:q` line.
- Tests:
  - `tests/…/ViewModels/ExCommandHandlerTests.cs` — `:q`, `:q!` and `:wq` on the last tab close it
    and request no exit;
  - `tests/…/ViewModels/SplitPaneTests.cs` — the last pane stays, empty;
  - `tests/…/ViewModels/WindowCommandTests.cs` — `Ctrl+W q` on the last tab requests no exit;
  - `tests/…/Views/MainWindowEditorTests.cs` (new test) — `:q` typed on the last tab leaves the window
    open with the placeholder text;
  - `:qa` / `:qa!` tests unchanged and green.
- `docs/roadmap.md`, `docs/plan/FEATURE-684E.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| The four tests that asserted an exit after the last `:q` | Rewritten to the new contract (tab closed, no exit, pane empty) | The behaviour change is the feature | Deleting them |

## Deviations & follow-ups

- None from the plan. Fix cycles: 0.
- Follow-up suggestion: an empty pane takes no keys, so a Vim user cannot type `:qa` there. Alt+F4 and
  File → Exit work. If this is missed, the empty pane could accept `:` for ex commands.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong. The README lists `:q` among the file commands without saying it quits.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2607 passed**, 0 failed, 0 skipped (2606 before,
  +1).

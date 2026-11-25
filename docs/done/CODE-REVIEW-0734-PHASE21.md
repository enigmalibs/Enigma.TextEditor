# CODE-REVIEW-0734-PHASE21 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 3)

## Summary

Four test classes still had a private `DrainAsync` that pumped the dispatcher a fixed four rounds of `Task.Yield`.
That is the shape that made `FileDropViewTests` flaky (PHASE02). The 55 call sites now go through `UiWait`, as in
PHASE02 and PHASE03, and the four helpers are gone:

| File | Calls | Now `UntilAsync` | Now `SettleAsync` |
|---|---|---|---|
| `Views/HelpViewTests.cs` | 9 | 9 | 0 |
| `Views/SettingsPageTests.cs` | 20 | 18 | 2 |
| `Views/TabDragViewTests.cs` | 23 | 11 | 12 |
| `Views/TabStripViewTests.cs` | 3 | 2 | 1 |

- **A call followed by a check that something happened** waits on what the test asserts next. Examples: the help's
  scroll area has the keyboard, the dialog has closed, the editor has the keyboard back, the settings page has the
  keyboard, the tab has moved, the pane has split, Vim is back in normal mode, the reset flyout is open.
- **A call followed by a check that nothing happened** settles the dispatcher instead. Examples: a tab let go over its
  own pane or off the panes, a drag cancelled with Escape, a double-click on a tab, Select All on the settings page.
- **Every assertion is still made**, after its wait.
- **Failure messages are unchanged**, kept by the "open = IsOpen; if open, hide" guard in the help tests (as in
  `FirstRunDialogTests`).

With this phase, every High and Medium finding of the 2026-11-19 review is fixed, and `CODE-REVIEW-0734` is `DONE`.

## Files/modules touched

- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/HelpViewTests.cs`, `Views/SettingsPageTests.cs`,
  `Views/TabDragViewTests.cs`, `Views/TabStripViewTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/CODE-REVIEW-0734.md` (statuses)

## Deviations & follow-ups

- `SettingsPageTests.ShowAsync` settles rather than waiting on the editor's focus. Its tests check different things
  next, and several open no file, so their pane has no tab to take the keyboard.
- Single `Dispatcher.UIThread.RunJobs()` calls between synchronous key presses and wheel notches are left as they
  are. They are not fixed-round helpers.
- `HexMouseSelectionTests` has a `RunJobs` inside a loop, but the loop plays mouse moves that drive the auto-scroll.
  It is not a pump helper, so it is left as it is.
- The session that built PHASE21 had died after cutting this branch and before flipping the status. The run was
  resumed per vibe-workflow §9 (dev branch, clean tree, row `TODO`, `HEAD` = run-branch tip → §6 step 3).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The draft "Continue the TODO roadmap items for the last review" | Resume run `review/2026-11-19-high-medium-review-fixes`, not plan a new item | It names existing rows; a new item would duplicate PHASE21. The Lows stay out of scope as the plan says |
| What a "settings shown" wait waits on | The page holding the keyboard | That is the last thing showing the page does (posted at `Background`); Esc on the page needs it |
| Wait after the page's controls change | The last change (`DefaultLineEnding`) reaching the settings | Each change is applied before its write; the last one shows they all went through |
| `InsertModeAsync`'s first pump | `SettleAsync` | It lets what the window posted on opening run out, so none of it takes the keyboard back later |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3436 tests, 3434 passed, 2 skipped (PHASE06's), 0 failed.
  - Fix cycles used: 0.
- The four classes on their own (59 tests), three runs in a row: 59/59 each time.
- `grep` finds no `DrainAsync`, `Task.Yield`, `Task.Delay` or `Thread.Sleep` in the Desktop test project outside
  `Infrastructure/`.
- No tests added: this phase changes how tests wait, not what they check.
- Documentation sweep (§7): only tests changed, so no user-facing document is affected.

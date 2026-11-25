# CODE-REVIEW-0734-PHASE02 — [Medium] TEST-02 UI tests wait on conditions, not fixed rounds (part 1)

## Summary

Many headless UI tests waited a fixed number of dispatcher rounds, five rounds of `RunJobs` with `Task.Delay(10)`
between them. That passes on an idle machine and fails on a loaded one. The same private helper existed in each file
under four names. This phase adds one shared helper and moves the part-1 files onto it.

- New `tests/…Desktop.UnitTests/Infrastructure/UiWait.cs`:
  - `UntilAsync(condition, because)` pumps the dispatcher (`RunJobs`, then `Task.Yield`) until the condition holds. It
    fails after 10 s and names what it was waiting for. It replaces the old `FileExplorerViewTests.UntilAsync`.
  - `SettleAsync()` pumps until `Dispatcher.UIThread.HasJobsWithPriority(SystemIdle)` has been false for three rounds
    in a row. It is used only where the test then asserts that something did *not* happen.
- In each part-1 file, every wait now waits on the condition the test asserts next. Examples: a pane becoming active,
  a split ratio, a dialog open with its default button focused, a question answered, the explorer listed, a menu open.
  The assertions themselves are all still there, unchanged, after the waits.
- `FileDropViewTests` was added to this phase. Its fixed four-round `DrainAsync` raced the thread-pool file reads of a
  drop, and in this phase's first full run that was the known flake
  `ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`. Run alone, the class failed in 3 of 5 runs before the change and
  passed in 8 of 8 after it.

## Files/modules touched

- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/UiWait.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/MoveTabTests.cs`, `ViewModels/WindowCommandTests.cs`,
  `Views/AboutDialogTests.cs`, `Views/DialogKeyboardTests.cs`, `Views/EditorContextMenuTests.cs`,
  `Views/FileDropViewTests.cs`, `Views/FileExplorerViewTests.cs`, `Views/FirstRunDialogTests.cs`
- Modified: `docs/plan/CODE-REVIEW-0734.md` (scope adaptation below), `docs/roadmap.md`

## Deviations & follow-ups

- Scope widened, and recorded in the plan:
  - `FileDropViewTests` joined this phase because its flake failed the phase's suite.
  - Four more files pump a fixed four rounds of `Task.Yield` but were not in the review's list: `HelpViewTests`,
    `SettingsPageTests`, `TabDragViewTests` and `TabStripViewTests`, about 55 call sites. They became the new
    **PHASE21** rather than doubling PHASE03.
- `SettleAsync` stands where a test asserts an absence. Examples: `gj` with no pane below it, an existing name keeping
  *Create* disabled, the selection set before the menu opens, text dropped as text.
- Fix cycles used: 1. The first full run failed only on the flake above. Converting `FileDropViewTests` was the fix,
  and the second full run was green.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| "Settled" | `HasJobsWithPriority(SystemIdle)` false for 3 rounds | Public Avalonia API. It waits for the dispatcher to be quiet rather than for a count of rounds |
| Wait between rounds | `Task.Yield` (the fixture loop sleeps 1 ms) | No wall-clock guess. Thread-pool work still gets time to finish |
| Keep the assertions after the waits | Yes | A wait that times out names the condition, and the assertion still gives its exact diff |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md` (0 warnings; 3371 tests, 1 failure, which PHASE01 fixed).
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - First `dotnet test --solution Enigma.TextEditor.slnx`: 3372 tests, 1 failed (the `FileDropViewTests` flake).
  - After converting `FileDropViewTests`: 3372 tests, 3372 passed.
- The converted classes on their own: `WindowCommandTests` and `MoveTabTests` 33/33; `FileExplorerViewTests`,
  `AboutDialogTests`, `DialogKeyboardTests`, `EditorContextMenuTests` and `FirstRunDialogTests` 35/35;
  `FileDropViewTests` 8/8 in each of 8 runs.
- No `Task.Delay` and no private pump helper are left in the part-1 files (checked with grep).
- Documentation sweep (§7): only tests changed, so no user-facing document is affected.

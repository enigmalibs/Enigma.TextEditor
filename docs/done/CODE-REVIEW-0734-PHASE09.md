# CODE-REVIEW-0734-PHASE09 — [Medium] SOLID-02 The workspace no longer builds its command handlers

## Summary

`WorkspaceViewModel` used to `new` an `ExCommandHandler` and a `WindowCommandHandler` in its constructor, passing
`this` to both. As a result:

- four of its constructor parameters were only passed through: `IAppShutdown`, `IHelpDialogService`,
  `LanguageCatalog` and `ILanguagePicker`;
- the handlers could not be built or replaced without a whole workspace;
- each handler held the workspace that held it.

The handlers no longer hold the workspace, so the cycle is gone:

- `ExCommandHandler.ExecuteAsync`, `QuitAsync` and `ClosePaneAsync` take the `WorkspaceViewModel` as their first
  parameter, and so does `WindowCommandHandler.ExecuteAsync`. Neither keeps a reference to it.
- Both handlers are registered as singletons in `AddShell`.
- `WorkspaceViewModel` takes the two handlers instead of the four services it never used, going from 12 to 10
  parameters. Its `ExecuteExAsync` and `ExecuteWindowAsync` hand `this` to the handlers.
- The ex and window commands behave exactly as before.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs`, `ViewModels/WindowCommandHandler.cs`,
  `ViewModels/WorkspaceViewModel.cs`, `Extensions/ServiceCollectionExtensions.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs` (builds the handlers and exposes
  them), `CompositionRootTests.cs` (resolves both), `ViewModels/ExCommandHandlerTests.cs`

## Deviations & follow-ups

- No deviation from the plan.
- `WindowCommandHandler` still takes `StatusMessages`, which it does not use. The parameter was already unused
  before this phase, so it was left alone (scope); its removal is a follow-up.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| What the handlers receive | The concrete `WorkspaceViewModel`, per call | `IPaneHost` lacks about 12 members the handlers use, and widening it would put them on every pane's contract |
| `Move` helper of the window handler | Inlined to `workspace.Focus(…)` | It only forwarded, and the comment about silent directions now sits on the switch |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3403 tests, 3401 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (3):
  - `CompositionRootTests` resolves `ExCommandHandler` and `WindowCommandHandler` (2 new theory rows).
  - `ExCommandHandlerTests.TheHandlers_ActOnTheEditorAreaTheyAreHanded_AndHoldNone`: one harness's handlers act on
    another harness's workspace and leave their own alone.
- `grep` finds no `new ExCommandHandler` or `new WindowCommandHandler` left under `src`.
- Documentation sweep (§7): nothing user-facing changed.

# FEATURE-452D-PHASE03 — Fire-and-forget tasks reach the net

**Item:** FEATURE-452D — Catch unhandled exceptions
**Branch:** `feature/feature-452d-phase03-fire-and-forget`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

Work the application starts and does not await no longer loses its failures.

- **`Task.Forget()`** (C# 14 extension block, `Extensions/TaskExtensions.cs`, as in Enigma.GitClient) observes the
  task.
  - Each failure is handed to `UnhandledExceptionHooks.Report`, as `FireAndForget`. That happens at once for a task
    already complete, or in a synchronous continuation when it ends.
  - A success or a cancellation says nothing.
  - An overload takes the source: `App`'s posted `StartAsync` reports as `Startup`.
- **The 22 discards in `src/` became `.Forget()`.** These are the 19 `_ = …Async(…)` of the plan, plus PHASE02's three
  `_ = PresentAsync()` in the reporter:

  | Where | How many |
  |---|---|
  | `App` | 1 |
  | `EncodingPicker`, `LanguagePicker`, `HelpDialogService` | 3 |
  | `PreferencesViewModel` | 10 |
  | `EditorPaneView` | 3 |
  | `HexView` | 2 |
  | `MainWindow` | 1 |
  | `UnhandledErrorReporter` | 3 |

  Before, such a failure surfaced only if the finaliser found the task unobserved. Now it is logged and shown as it
  happens.

## Files/modules touched

- Created:
  - `src/Enigma.TextEditor.Desktop/Extensions/TaskExtensions.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Extensions/TaskExtensionsTests.cs`
  - `docs/done/FEATURE-452D-PHASE03.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/App.axaml.cs`
  - `src/Enigma.TextEditor.Desktop/Services/EncodingPicker.cs`
  - `src/Enigma.TextEditor.Desktop/Services/HelpDialogService.cs`
  - `src/Enigma.TextEditor.Desktop/Services/LanguagePicker.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.TextEditor.Desktop/ViewModels/PreferencesViewModel.cs`
  - `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
  - `src/Enigma.TextEditor.Desktop/Views/HexView.axaml.cs`
  - `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-452D.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The `Startup` source | A `Forget(UnhandledErrorSource)` overload | One extension, no second entry point; `Forget()` delegates to it |
| The continuation's state | The source passed as `state`, not captured | No closure allocated per task |
| Location | `Extensions/`, next to `ServiceCollectionExtensions` | The plan; this codebase's home for extension classes |
| Tests' collection | The headless collection | Every test that attaches a reporter to the process-wide hooks runs serially there |

## Deviations & follow-ups

- **One more replacement than the plan listed:** PHASE02's three `_ = PresentAsync()` became `PresentAsync().Forget()`.
  `PresentAsync` catches the dialog's failure itself, so it cannot report itself in a loop.
- **The `null`-conditional calls** (`_pane?.ExecuteExAsync(request).Forget()`) skip `Forget` along with the call when
  there is no pane, as the discard did.

## Documentation sweep

- Nothing user-facing changed, so no edit was made.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2780 total, 2780 passed (5 new).
- `grep -rn "_ = .*Async(" src --include=*.cs`: nothing.
- Fix cycles: 1 of 3. `EveryFailureOfTheTask_IsReported` built its two-failure task with
  `Task.FromException(new AggregateException(a, b))`, which wraps the aggregate as a single failure. It now uses
  `Task.WhenAll`, which really has two. The code was right; the scenario was not.

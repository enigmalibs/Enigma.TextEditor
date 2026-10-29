# FEATURE-452D-PHASE01 — Error policy and process hooks

**Item:** FEATURE-452D — Catch unhandled exceptions
**Branch:** `feature/feature-452d-phase01-error-policy-hooks`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

This phase adds the windowless half of the safety net, ported from Enigma.GitClient so the Enigma apps fail alike.

- **`UnhandledErrorPolicy`** decides what is shown:
  - one batch at a time, with later errors merged into it;
  - an error closed less than a minute ago is suppressed;
  - a requested cancellation, or any cancellation while closing, is ignored;
  - the same exception offered twice counts once.
- **The texts:**
  - `Summarise` unwraps single-inner wrappers;
  - `BuildDetails` gives type, message and stack, follows inner exceptions to depth 8, and never reads `Data` or
    `ToString()`.
- **`UnhandledErrorBatch`** holds what one dialog shows: the first error, the count of the rest, and the details.
- **`IUnhandledErrorReporter`**: the contract PHASE02 implements.
- **`UnhandledExceptionHooks`** (static) adapts the process events: AppDomain, unobserved tasks and the UI dispatcher.
  - Reports made before a reporter is attached are kept (at most 32) and replayed on `Attach`.
  - `Detach` takes the reporter away before the container goes.
- **`Program.Main`** calls `UnhandledExceptionHooks.Install()` before anything else.

Nothing is shown yet: until PHASE02 attaches a reporter, a caught exception is traced and kept.

## Files/modules touched

- Created:
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorSource.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorDecision.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorBatch.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorPolicy.cs`
  - `src/Enigma.TextEditor.Desktop/Services/IUnhandledErrorReporter.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledExceptionHooks.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/ManualTimeProvider.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/RecordingErrorReporter.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/UnhandledExceptionHooksCollection.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Services/UnhandledErrorPolicyTests.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Services/UnhandledExceptionHooksTests.cs`
  - `docs/done/FEATURE-452D-PHASE01.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/Program.cs`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-452D.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| File layout | One type per file | This codebase's convention (GitClient puts the enums, batch and policy in one file) |
| Doc wording | "Initializes a new instance of the … class" | This codebase's wording, not GitClient's "Initialises" |
| Test seam for the static hooks | `internal ResetForTests()`, plus a dedicated test collection | The hooks' state is the process's; the collection keeps the hook tests (and PHASE03's) serial |
| `Install()` in tests | Never called | Hooking the test host's unobserved tasks would make reports land in unrelated assertions |
| `ManualTimeProvider` | Timestamps only, no timers | Nothing under test starts a timer; GitClient's timer support would be dead code here |

## Deviations & follow-ups

- No `ArgumentRedactor`: this app handles no credentials. The rule that the details never read `Data` or
  `ToString()` still holds, and is tested.

## Documentation sweep

- Nothing user-facing changed, so no edit was made.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2765 total, 2765 passed (22 new: 18 policy, 4 hooks).
- Fix cycles: 0 of 3.

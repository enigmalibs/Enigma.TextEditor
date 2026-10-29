# FEATURE-452D-PHASE02 — Error dialog and wiring

**Item:** FEATURE-452D — Catch unhandled exceptions
**Branch:** `feature/feature-452d-phase02-error-dialog`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

An unhandled exception now shows Enigma.GitClient's error dialog in the window, and the application keeps running.

- **`UnhandledErrorReporter`** (`IUnhandledErrorReporter`):
  - **Logging:** each error is logged at Error with its source, on the reporting thread. An `AppDomain` error then
    flushes the log and stops there: the process is about to end, so no dialog can come.
  - **The dialog:** the batch is shown on the window's dialog host. The title is "Something went wrong", with
    *Close*, the Phosphor warning icon and the `#E8A33D` brush.
  - **Waiting:** while a question is open it waits for that question's `Closed`. Before the window hands over its
    host (`RegisterHost`), it waits for it.
  - **Failure:** a dialog that cannot be shown is logged once, and only logging is done from then on.
- **`UnhandledErrorViewModel` and `UnhandledErrorView`:** GitClient's layout, sizes and brushes:
  - the lead, the message in semi-bold, and "N more errors occurred.";
  - a collapsed *Details* expander with a monospace read-only box and *Copy details*.

  GitClient's `MonospaceFontFamily` and `Button.toolbar` styles are the view's own here.
- **The wiring:**
  - DI registers `TimeProvider.System` and one reporter under both its names.
  - `App` installs the dispatcher hook first, attaches the reporter right after the host starts, and calls
    `ShutdownStarted` on `ShutdownRequested`.
  - `RegisterHosts` hands the reporter `HostDialog`.
  - `OnExit` calls `ShutdownStarted` and `Detach` before the host is disposed.

## Files/modules touched

- Created:
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledErrorReporter.cs`
  - `src/Enigma.TextEditor.Desktop/ViewModels/UnhandledErrorViewModel.cs`
  - `src/Enigma.TextEditor.Desktop/Views/UnhandledErrorView.axaml`
  - `src/Enigma.TextEditor.Desktop/Views/UnhandledErrorView.axaml.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/FakeLoggerHandle.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Views/UnhandledErrorDialogTests.cs`
  - `docs/done/FEATURE-452D-PHASE02.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/App.axaml.cs`
  - `src/Enigma.TextEditor.Desktop/Extensions/ServiceCollectionExtensions.cs`
  - `src/Enigma.TextEditor.Desktop/Services/UnhandledExceptionHooks.cs` (`ResetForTests` also unhooks the
    dispatcher)
  - `tests/Enigma.TextEditor.Desktop.UnitTests/CompositionRootTests.cs`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/Services/UnhandledExceptionHooksTests.cs`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-452D.md`
- Deleted: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/UnhandledExceptionHooksCollection.cs`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the reporter gets the host | `RegisterHost(ContentDialog)`, called from `App.RegisterHosts` | The Enigma services' own pattern; no dependency on `MainWindow`, and an error before the window waits for it |
| DI shape | Concrete singleton plus an interface forwarding to it | `RegisterHosts` needs the concrete `RegisterHost`; everything else reports through the interface |
| Copy details | A `Func<string, Task>` over `TopLevel.GetTopLevel(host).Clipboard` | No clipboard service exists; the VM stays testable without a platform (plan) |
| Toolbar button and monospace font | GitClient's styles, scoped to the view | Same look; nothing else in this app uses them, so nothing app-wide changes |
| Hook tests' collection | The headless collection; the separate one from PHASE01 is deleted | The new dispatcher-hook test needs the UI thread. A dispatcher hook live during another UI test would mark that test's failures handled, so `ResetForTests` unhooks it and the tests run serially |
| `AppDomain` errors | Log, flush, no dialog | .NET ends the process after the event (plan) |

## Deviations & follow-ups

- **The plan said to wait on `ContentDialog.IsOpen` / `Closed` for open questions.** That is done, and tested with a
  real question on the real host.
- **The `_ = PresentAsync()` discards in the reporter** are left for PHASE03, which replaces every `_ =` discard with
  `Forget()`.
- **Smoke run:** the built `Enigma.TextEditor.Desktop.exe` was started and was still running after 8 s, then
  force-killed. Not checked by hand: the dialog in a real window, which needs a deliberate failure.

## Documentation sweep

- The README and `docs/RELEASE.md` say nothing about error handling that became wrong, so no edit was made.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2775 total, 2775 passed. 10 are new:
  - 7 dialog tests;
  - 1 dispatcher-hook test;
  - 2 composition tests.
- Fix cycles: 1 of 3. The first run did not compile: the fake missed `ILoggerHandle`'s members. After the suite was
  green, one more run followed to add the dispatcher-hook test; it was not a fix.

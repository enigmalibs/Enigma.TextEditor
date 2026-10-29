# FEATURE-452D — Catch unhandled exceptions

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-452d-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Every exception that nothing else handles reaches one safety net. The net logs the exception and shows an error
content dialog, which says the application is still running. The dialog has the error, a count of further errors,
collapsed details and *Copy details*. This is the same net as Enigma.GitClient's, so the Enigma apps fail alike.

## Context & constraints

- **The reference** is Enigma.GitClient, in `src/Enigma.GitClient.Desktop/`:
  - `Services/UnhandledErrorPolicy.cs`: the source and decision enums, `UnhandledErrorBatch` and
    `UnhandledErrorPolicy`;
  - `Services/UnhandledExceptionHooks.cs`;
  - `Services/UnhandledErrorReporter.cs`;
  - `ViewModels/Dialogs/UnhandledErrorViewModel.cs`;
  - `Views/Dialogs/UnhandledErrorView.axaml`;
  - `Services/TaskExtensions.cs` (`Forget`).
- **What does not carry over** from GitClient:
  - several windows (`IAppWindows`, `CurrentWindowChanged`);
  - the token reset (`ITokenRecovery`);
  - `ArgumentRedactor`.

  This app has one `MainWindow`, whose `HostDialog` every question shares (`IContentDialogService`), and no
  credentials.
- **Today nothing is caught:**
  - `Program.Main` starts the lifetime;
  - `App.OnFrameworkInitializationCompleted` builds the host, posts `viewModel.StartAsync(startup)` with `_ =`, and
    wires `OnExit`;
  - 19 `_ = …Async()` discards in `src/` lose their faults until the finaliser finds them, if it ever does.
- **Conventions:**
  - CommunityToolkit.Mvvm comes in through Enigma.Avalonia.Desktop, and ViewModels derive from `ObservableObject`;
  - Desktop services live in `Services/`, ViewModels in `ViewModels/`, Views in `Views/` (flat, no `Dialogs/`
    folder);
  - tests run headless, through `HeadlessAvaloniaFixture` and its `[Collection(HeadlessCollection.Name)]`;
  - each service has a fake in `tests/…/Infrastructure/`.
- **Logging:** NLog through `ILogger<T>`; `IFlushableLoggerHandle` flushes it (see `App.OnExit`).
- There is no clipboard service yet. Copying goes through the window's `TopLevel.Clipboard`.

## PHASE01 — Error policy and process hooks

**Status:** DONE

### Steps

1. `Services/UnhandledErrorPolicy.cs`: GitClient's `UnhandledErrorSource` (`Dispatcher`, `UnobservedTask`,
   `AppDomain`, `FireAndForget`, `Startup`), `UnhandledErrorDecision`, `UnhandledErrorBatch` and
   `UnhandledErrorPolicy`:
   - one batch at a time, with later errors merged into it;
   - the same error is suppressed for `RepeatWindow` (1 min) after it was closed;
   - a requested cancellation, or any cancellation while shutting down, is ignored;
   - `Summarise`: the first meaningful message, unwrapping a single-inner `AggregateException` and
     `TargetInvocationException`;
   - `BuildDetails`: type, message and stack, recursing into inner exceptions up to depth 8. Never `Data` and never
     `ToString()`, so no redaction is needed.
2. `Services/IUnhandledErrorReporter.cs`: `Report(Exception, UnhandledErrorSource)` and `ShutdownStarted()`.
3. `Services/UnhandledExceptionHooks.cs` (static):
   - `Install` hooks AppDomain and `TaskScheduler.UnobservedTaskException` (`SetObserved`);
   - `InstallDispatcher` hooks `Dispatcher.UIThread.UnhandledException` (`Handled = true`);
   - `Attach` replays the early reports (at most 32);
   - `Detach`;
   - `Report`, which traces the type and message while no reporter is attached.

   For testability, the static state gets an `internal` reset seam used only by the tests.
4. `Program.Main`: call `UnhandledExceptionHooks.Install()` first, before the lifetime starts.
5. Tests (Desktop project, no platform needed):
   - **policy:** ignore, queue, merge, suppress within the window and show again after it (a fake `TimeProvider`),
     the same exception offered twice, `Summarise` unwrapping, details with inner exceptions;
   - **batch:** `More`, `Details` of several errors, `Changed`;
   - **hooks:** early reports replayed on `Attach` (bounded), reports after `Attach` delivered, nothing after
     `Detach`.

### Acceptance criteria

- The policy and the hooks are covered by unit tests; build with zero warnings, whole suite green.
- `Program.Main` installs the process hooks before anything else.

## PHASE02 — Error dialog and wiring

**Status:** DONE

### Steps

1. `ViewModels/UnhandledErrorViewModel.cs` (`ObservableObject`):
   - `Lead`, `Message`, `HasMore`, `MoreText`, `Details`;
   - `CopyDetailsCommand`, through an injected copy delegate (`Func<string, Task>`);
   - it follows `batch.Changed`.
2. `Views/UnhandledErrorView.axaml(.cs)`: GitClient's layout verbatim:
   - lead, message (semi-bold), "N more errors" in the secondary brush;
   - a collapsed *Details* expander with a read-only monospace `TextBox` (max 220 px high), and *Copy details*;
   - `MinWidth` 480, `MaxWidth` 560 on the texts, the same Enigma brushes and automation names.

   No remedy line: there is no token reset here.
3. `Services/UnhandledErrorReporter.cs` (`IUnhandledErrorReporter`):
   - **Logging:** logs on the reporting thread, at Error, with the source. An `AppDomain` report also flushes the
     log (`IFlushableLoggerHandle`), because the process is about to end.
   - **Decisions:** taken on the UI thread through the policy.
   - **The dialog:** shown on `IContentDialogService`. The title is "Something went wrong", with *Close* (default)
     and GitClient's warning icon and `#E8A33D` brush.
   - **Waiting:** while the host has a question open (`ContentDialog.IsOpen`), it waits for that question's
     `Closed`, then tries again at Background priority.
   - **Failure:** if the dialog itself fails, the failure is logged and only logging is done from then on.
   - **Clipboard:** *Copy details* uses `TopLevel.GetTopLevel(host)?.Clipboard`.
4. DI: `AddShell` registers `TimeProvider.System` (`TryAdd`) and the reporter as a singleton.
5. `App.OnFrameworkInitializationCompleted`:
   - `UnhandledExceptionHooks.InstallDispatcher()` first;
   - `Attach(reporter)` as soon as the host is built and the hosts are registered;
   - `desktop.ShutdownRequested` → `ShutdownStarted()`.

   `OnExit` calls `ShutdownStarted()` and `Detach()` before the host is disposed.
6. Tests (headless, in the real `MainWindow`, as `AboutDialogTests`):
   - a reported error opens the dialog with its title, message and *Close*;
   - a second error while it is open shows "1 more error occurred.";
   - an error raised while another question is open waits for it, then shows;
   - a cancellation is not shown;
   - *Copy details* calls the copy delegate with the details;
   - the composition root resolves the reporter (`CompositionRootTests`).

### Acceptance criteria

- An exception thrown by a command or a posted callback on the UI thread shows the dialog, and the app keeps
  running.
- The dialog never replaces a question on screen, and a burst of errors opens one dialog.
- Build with zero warnings, whole suite green.

## PHASE03 — Fire-and-forget tasks reach the net

**Status:** DONE

### Steps

1. `Extensions/TaskExtensions.cs`: `Forget(this Task)`, a C# 14 `extension` block in the house style of
   `ServiceCollectionExtensions`. It observes the task. A fault that is not an ignored cancellation goes to
   `UnhandledExceptionHooks.Report(…, FireAndForget)`. A completed task costs nothing.
2. Replace every `_ = …Async(…)` discard in `src/` with `.Forget()`. That includes `App`'s posted `StartAsync`,
   which reports as `Startup` through a small overload or a dedicated call.
3. Tests:
   - a faulted task reaches the reporter as `FireAndForget`;
   - a completed task and a cancelled one report nothing;
   - a task that faults later still reports.

### Acceptance criteria

- `grep -rn "_ = .*Async(" src` finds nothing.
- Build with zero warnings, whole suite green.

## Out of scope

- A crash dialog for `AppDomain.UnhandledException`: .NET ends the process after the event, so the error is only
  logged.
- Restart or "send report" buttons; any change to the existing error messages, which handle known failures where
  they happen.
- Unifying the existing `IMessageService` with the net.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Design | Port GitClient's safety net (policy, hooks, reporter, dialog) | "Uniform" Enigma apps; it is tested and handles bursts and open questions | A bare `try/catch` in `Main`; a `MessageService` call per hook |
| Multi-window logic | Dropped | One `MainWindow` here; the host is always the same | Porting `IAppWindows` |
| Redaction | None; details never use `Data`/`ToString()` | No credentials in this app; GitClient's rule on what the details read still holds | Porting `ArgumentRedactor` |
| AppDomain errors | Logged, then the log is flushed | The process ends right after; an unflushed NLog buffer would lose the one record | Trying to show a dialog |
| Copying details | A delegate over `TopLevel.Clipboard`, injected into the VM | No clipboard service exists; the VM stays testable without a platform | A new `IClipboardService` (more surface for one call) |
| Discarded tasks | A `Forget()` extension replacing all `_ =` discards (PHASE03) | "Catch **all** unhandled exceptions": a discarded fault is otherwise seen at best when the GC finalises it | Leaving them to `UnobservedTaskException` |
| Phasing | 3 phases: logic, dialog, discards | Each is one reviewable commit; the logic is reviewable without UI | One large dev |
| Folders | Flat `Services/`, `ViewModels/`, `Views/` | This codebase has no `Dialogs/` sub-folders | GitClient's `Dialogs/` folders |

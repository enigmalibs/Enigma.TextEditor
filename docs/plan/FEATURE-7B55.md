# FEATURE-7B55 — Open files in the running instance

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-7b55-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Objective

Several instances of the editor can run side by side, and that stays. But a file opened from the system — a
double-click on an associated file in Explorer, *Open with*, the Linux launcher — opens **as a new tab in the
instance already running** instead of starting another one. Started without a file, the editor still opens a new
instance.

## Context & constraints

- **Today every launch is a new process:**
  - `Program.Main` → `StartWithClassicDesktopLifetime(args)`;
  - `App.OnFrameworkInitializationCompleted` parses `desktop.Args` with `StartupFiles.Parse` (full paths against the
    working directory, folders skipped, duplicates once) and posts `MainWindowViewModel.StartAsync(startup)`;
  - `WorkspaceViewModel.OpenStartupFilesAsync` opens each in the active pane, a missing file in an existing folder as
    an empty buffer, and reports the rest on the status line.
- **Both platforms pass files as arguments:** the Windows association runs the `.exe` with the file;
  `packaging/linux/enigma-text-editor.desktop.in` has `Exec=@EXEC@ %F`. Nothing in packaging needs to change.
- **No IPC exists.** Avalonia 12 has no single-instance support of its own.
- **Per-user state** lives under `ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, …)` (`AppPaths`).
- **Conventions:** Core has no Avalonia dependency; file helpers are static with the path as a parameter
  (`SettingsLoader`, `FirstRunState.Detect`) and tested against temp folders; `UnhandledExceptionHooks` and
  `ShutdownSequence` show how process-edge code is kept out of the container; logging through `ILogger<T>`.
- **A fresh tab** (untitled, empty, unchanged, shown once) is replaced by the first dropped file
  (`OpenDroppedAsync`, `OpenInPlaceOfAsync`, FEATURE-4217).

## PHASE01 — Instance pipe and pointer file

**Status:** DONE

### Steps

1. `Core/Instances/InstanceEndpoint.cs`: `record InstanceEndpoint(string PipeName, int ProcessId)`;
   `InstanceEndpoint.ForCurrentProcess()` names the pipe after the application and the process id.
2. `Core/Instances/ActiveInstanceFile.cs` — the pointer to the instance that was active last, as JSON:
   - `Write(path, endpoint)`: atomic (temp file + replace), creating the folder;
   - `TryRead(path)`: the endpoint, or `null` when missing or unreadable;
   - `Clear(path, endpoint)`: deletes it only while it still names that endpoint.
3. `Core/Instances/InstanceProtocol.cs` — one request, one reply, on a stream:
   - request: a version tag and the paths (System.Text.Json, length-prefixed, capped in size);
   - reply: accepted or not;
   - a wrong version, an oversized or malformed message is rejected, never thrown past the server.
4. `Core/Instances/InstanceServer.cs` — `IAsyncDisposable`:
   - `Start(pipeName, onRequest, onError)`: a background loop over a `NamedPipeServerStream` with
     `PipeOptions.CurrentUserOnly | Asynchronous`, one connection at a time;
   - each request is read, handed to `onRequest`, and answered; a fault is reported through `onError` and the loop
     carries on; disposal stops it.
5. `Core/Instances/InstanceClient.cs` — `TryForwardAsync(endpoint, paths, connectTimeout, replyTimeout,
   cancellationToken)`: `true` only when the server answered that it accepted them; every failure is `false`.
6. `AppPaths.ActiveInstanceFile`: `instance.json` in the application-data folder.
7. **Tests** (Core):
   - the protocol round-trips paths with spaces, non-ASCII and very long names; rejects a wrong version, an
     oversized length and garbage;
   - the pointer file round-trips; a missing or corrupt file reads `null`; `Clear` leaves another instance's
     pointer alone;
   - a server and a client on a unique pipe name: the paths arrive and `true` comes back; no server → `false` within
     the timeout; a server whose handler refuses → `false`; two requests in a row both arrive; disposing stops it.

### Acceptance criteria

- A client hands a list of paths to a server in another task over a current-user pipe and learns whether it was
  accepted; every failure is a `false`, never an exception.
- The pointer file is written atomically and cleared only by its owner.
- Zero warnings, whole suite green, on Linux (Windows pipes share the code path).

## PHASE02 — Forward launches to the open window

**Status:** DONE

### Steps

1. **Program.Main**, before Avalonia starts: when `StartupFiles.Parse(args, …)` finds at least one file, the last
   active instance is read from the pointer file. If its process is alive and is this application, the full paths are
   forwarded; on success the launch ends (exit code 0). On Windows the launch first lets that process take the
   foreground (`AllowSetForegroundWindow`). Any failure starts a new instance as today.
2. **App** (composition root): after the window is built,
   - starts an `InstanceServer` on this process's endpoint and writes the pointer;
   - a request is posted to the UI thread: the window is restored if minimised and activated, and the files open
     through `MainWindowViewModel.OpenForwardedAsync(StartupFiles)` (fire-and-forget through the safety net);
   - the server is disposed and the pointer cleared on exit.
3. **MainWindow:** `Activated` writes the pointer (off the UI thread), so the last window used is the one files go
   to; `BringToFront()` restores and activates.
4. **Workspace:** forwarded files open like command-line ones, in the active pane; a fresh tab is replaced by the
   first, as a drop does; the files page is shown (`DocumentShown`).
5. **Logging:** the receiving instance logs each request (file count) and any server fault; a failure to start the
   server is a warning, and that instance simply does not receive files.
6. **README:** the *Files* paragraph says a file opened from the system goes to the open window; a new window is
   started without a file.
7. **Tests:**
   - workspace: forwarded files open in the active pane; a fresh tab is replaced; a missing file is a new buffer;
     folders are reported;
   - the forwarding decision (no files → no forward; no pointer, dead or foreign process → no forward) through a
     seam that takes the pointer path and a process check;
   - an end-to-end forward to an `InstanceServer` in the test process.

### Acceptance criteria

- With an instance open, launching the editor with a file opens it as a tab in that instance's active pane, brings
  that window forward, and starts no new window.
- Several instances open: the file goes to the one used last.
- Launching without a file opens a new instance.
- No instance running, or the last one gone or unresponsive: a new instance opens the file, as today.
- Zero warnings, whole suite green.

## Out of scope

- A `--new-window` switch or a setting to turn forwarding off.
- Forwarding anything but files (a line number, a mode), or to an instance of another user.
- macOS (`Apple events`) and installer or association changes.
- Queuing forwarded files behind an open dialog.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| BUG or FEATURE | FEATURE | Nothing ever claimed to reuse an instance; it is a new capability | BUG (no broken intent to restore) |
| When a launch forwards | Only with at least one file, and only when an instance answers | The draft keeps several instances; a launch without a file is the way to get one | Always single-instance (contradicts the draft) |
| Which instance | The one whose window was activated last | "The opened instance" with several open is the one in use; VS Code does the same | The first started (an arbitrary old window); the last started |
| Mechanism | One named pipe per instance (`CurrentUserOnly`) plus a pointer file naming the last active one | Works on Windows and Linux with the BCL; per-instance names avoid two servers fighting over one name (Unix sockets) | A well-known pipe name (no "last active", stale sockets on Linux); a mutex plus window messages (Windows-only) |
| Where the launch decides | `Program.Main`, before Avalonia | No window flashes; nothing to tear down | After the app starts (a window would appear) |
| Bringing the window forward | Restore and activate; on Windows the launch grants `AllowSetForegroundWindow` to the receiver | Otherwise Windows only flashes the taskbar, and the file seems not to open | Leaving it in the background |
| Where the files open | Active pane, as command-line files; a fresh tab is replaced | The same files the command line would open; FEATURE-4217's rule for a new tab | A new pane; always a new tab beside a blank one |
| Timeouts and failure | Connect 1 s, reply 2 s; any failure starts a new instance | A file must never be lost to a stuck instance | Longer waits; failing with a message |
| Message format | Length-prefixed UTF-8 JSON with a version, capped | Any path character survives; versioned for later | Newline-separated text (Linux names may hold newlines) |
| Opt-out | None | Not asked; starting without a file gives a new instance | A switch or a setting |
| Shape | Two phases: the transport in Core, then the wiring | The transport is testable on its own; each is ~30 min of review | One phase |

# FEATURE-3EE8-PHASE03 — Vim integration & ex requests

**Item:** FEATURE-3EE8 — Editor panes, tabs & files
**Branch:** `feature/feature-3ee8-phase03-vim-integration`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The editor is now driven by the ported Vim engine. It starts in NORMAL, and the whole MarkdownEditor
command set works in every pane.

**Status bar**

- The mode chip shows NORMAL (quiet), INSERT (green) or VISUAL / V-LINE (amber).
- While `:` or `/` is being typed, the command line takes the message slot.
- Engine messages share the message line with the app's own messages.

**Ex requests**

Ex requests now carry an argument: `VimExRequest(Command, Argument)`.

| Command | What it does |
|---|---|
| `:w` | Save |
| `:w {file}` | Save As, relative to the buffer's folder |
| `:q`, `:q!` | Close the tab (asking, or discarding). The last tab anywhere quits the app |
| `:wq`, `:x`, `ZZ` | Save, then close |
| `ZQ` | `:q!` |
| `:e {file}` | Open or show a file in the pane, relative to the buffer's folder |
| `:bn`, `:bnext` / `:bp`, `:bprevious` | Cycle the pane's tabs |
| `:wa`, `:wall` | Save every dirty buffer |
| `:qa`, `:qall` | Exit through the window's unsaved-changes questions |
| `:qa!` | Exit without asking |

Ranges on these are still refused. `:w!`, `:e`, `:e!` and `:wqa` are still reported as "Not an
editor command".

Ctrl-key gestures pass through Vim's command modes to the window, so Ctrl+S, Ctrl+O and the others
work in NORMAL.

## Files / modules touched

**Core**

- Created `Vim/VimExRequest.cs`.
- Modified:
  - `Vim/VimExCommand.cs`: new requests;
  - `Vim/ExCommandParser.cs`: `Parse`/`FileCommand` return `VimExRequest?` and read the new commands;
  - `Vim/VimEngine.cs`: the event carries `VimExRequest`.

**Desktop — created**

- Ported from MarkdownEditor: `Views/AvaloniaEditVimBuffer.cs`, `Views/VimEditorBehavior.cs`,
  `Services/IVimSession.cs`, `Services/VimSession.cs`.
- New: `ViewModels/IPaneHost.cs`, `ViewModels/ExCommandHandler.cs`.

**Desktop — modified**

- `ViewModels/EditorPaneViewModel.cs`: takes an `IPaneHost` instead of a close callback; exposes `Vim`
  and `ExecuteExAsync`.
- `ViewModels/WorkspaceViewModel.cs`: implements `IPaneHost`, owns the `ExCommandHandler`, `HasTabs`,
  `SaveAsAsync(document, path)`.
- `ViewModels/StatusBarViewModel.cs`: mode, chip classes, command line.
- `ViewModels/MainWindowViewModel.cs`: `MustConfirmClose`; honours a discard-on-exit request.
- `Services/IAppShutdown.cs`, `AppShutdown.cs`: `ExitDiscardingChanges` / `DiscardChangesOnExit`.
- `Views/EditorPaneView.axaml.cs`: attaches a `VimEditorBehavior` and follows the session.
- `Views/MainWindow.axaml`: chip, message and command-line slots. `Views/MainWindow.axaml.cs`: the
  close guard uses `MustConfirmClose`.
- `Extensions/ServiceCollectionExtensions.cs`: `IVimSession`.

**Tests**

- Core:
  - `ExCommandParserTests`: the six original spellings now compare requests; new commands with their
    arguments; new refusals.
  - `VimQuitTests`: `:wa`/`:qa` moved from "refused" to "requested"; `:w`/`:e` arguments.
  - `VimCommandLineTests`: `:wa` replaced by `:xa` as the still-unknown example.
  - `VimScript`: `VimRun` exposes `ExRequests`, and `ExCommands` as a projection.
- Desktop:
  - created `ViewModels/ExCommandHandlerTests` (12) and `Views/VimEditingTests` (6, headless
    keystrokes);
  - modified `Infrastructure/{EditorHarness,FakeAppShutdown}` and `ViewModels/EditorPaneViewModelTests`.

## Deviations & follow-ups

- **Behaviour changes to ported tests.** The ported suite asserted that `:wa` and `:qa` are refused.
  The plan makes them requests, so those cases now assert the requests. The refusal cases keep
  commands that remain unsupported.
- **Ported adapter fix: an empty pane.** `AvaloniaEditVimBuffer` returned a `default` `VimLine`
  (null text) when the editor had no document. Resetting the engine in an empty pane then threw. It
  now answers one empty line. In MarkdownEditor the pane hid the editor before it could be reset;
  here an empty pane is a normal state.
- **`:x` writes even when unchanged,** exactly as `:wq` does. Ported behaviour, documented in the
  parser.
- **No `"+` clipboard register.** Yank and put stay inside the editor; Ctrl+C/V use the system
  clipboard. Follow-up.
- **Fix cycles used: 2 of 3.**
  1. A ported test expected `:wa` to be refused (intended behaviour change).
  2. The empty-pane null line above.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How a pane reaches the ex handler | `IPaneHost` (implemented by the workspace) passed to each pane | One seam for close-tab, ex commands and the Vim session; no service locator in views | More constructor callbacks on the pane |
| Where Vim messages go | The shared `StatusMessages` line | One message slot for the app and for Vim, as in Vim | A second message slot |
| `:qa!` with dirty buffers | `IAppShutdown.ExitDiscardingChanges` sets a flag the window's close check honours | Explicit, and the buffers stay as they are rather than being falsely marked clean | Marking every buffer saved |
| Whether `:e`, `:bn` and `:wa` accept full spellings | `:edit`, `:bnext`, `:bprevious`, `:wall`, `:qall` accepted; `:write`/`:quit` stay refused as ported | Matches the plan; the ported strictness on w/q is kept | Accepting every abbreviation (vim's own minimum-prefix rules, a larger change) |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2160 passed**, 0 failed, 0 skipped. The headless
  tests type real keystrokes into the window:
  - `x` deletes in NORMAL;
  - `i`, text and Esc insert and return to NORMAL;
  - `:w` + Enter saves, and the command line is shown while typed;
  - Ctrl+S saves in NORMAL;
  - an unknown ex command shows its message;
  - with Vim off, keys type and the chip is hidden.
- Manual smoke: the Debug build starts on `Untitled-1 — Enigma Text Editor`.

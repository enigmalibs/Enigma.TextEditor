# FEATURE-04BC-PHASE02 — Vim window commands & shared state

**Item:** FEATURE-04BC — Multi-split panes
**Branch:** `feature/feature-04bc-phase02-vim-window-commands`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

Panes are now driven from Vim as well as from the View menu, and every pane's engine shares Vim's
editor-wide state.

**Shared state**

- `VimSharedState` holds the registers, the last search and the last find.
- `VimEngine(buffer, options, shared)` uses it. Without one, an engine gets a private instance, so
  the existing tests are unchanged.
- The app registers one singleton and hands it to every pane's engine. A line yanked in one split is
  put in another, and `n` or `;` in any pane repeats the last search or find typed in any pane.

**Window commands**

`Ctrl+W` in normal and visual modes is a prefix. The next key maps to a `VimWindowCommand` raised on
`WindowCommandRequested`:

| Key after `Ctrl+W` | Command |
|---|---|
| `h` `j` `k` `l`, arrows, `Ctrl+`letter | Focus by direction |
| `w` / `Ctrl+W` | Next pane |
| `W` | Previous pane |
| `s` `S` / `v` | Split down / split right |
| `c` | Close pane ("Cannot close last window" on the last one) |
| `q` | `:q` |
| `o` | Only this pane |
| `=` | Equalize |
| `+` `-` `>` `<` | Resize by 0.05 |

- Any other key cancels silently and is swallowed.
- The prefix drops a half-typed command.
- Insert mode leaves `Ctrl+W` to the editor.

**Ex commands**

| Command | What it does |
|---|---|
| `:sp[lit]`, `:vs[plit]` | Split showing the same buffer |
| `:sp file`, `:vs file` | Open that file alone in the new pane |
| `:new`, `:vne[w]` | Split onto a new untitled buffer |
| `:on[ly]`, `:clo[se]` | Act on panes |

**Desktop**

- `WindowCommandHandler` maps window commands onto the workspace, the same operations as the View
  menu. `Ctrl+W q` and `Ctrl+W c` go through the ex handler's `:q` / close-pane logic.
- `Workspace.Split(axis, showActiveBuffer)` can start the new pane empty.
- Each pane's `VimEditorBehavior` is built with the pane, from its host's shared state.

## Files / modules touched

**Created**

- Core: `Vim/VimSharedState.cs`, `Vim/VimLastSearch.cs`, `Vim/VimWindowCommand.cs`,
  `Vim/VimWindowKeys.cs`.
- Desktop: `ViewModels/WindowCommandHandler.cs`.
- Tests:
  - `tests/.../Core.UnitTests/Vim/VimWindowCommandTests.cs` (45 cases);
  - `tests/.../Desktop.UnitTests/ViewModels/WindowCommandTests.cs` (10, including a keystroke-level
    cross-pane yank/put).

**Modified**

- Core:
  - `Vim/VimEngine.cs`: shared state replaces `_lastSearch` / `_lastFind*` / `Registers`; window
    prefix; `WindowCommandRequested`; `HasPendingSequence`; `Reset`.
  - `Vim/VimExCommand.cs`, `Vim/ExCommandParser.cs`: `Split`, `VerticalSplit`, `New`, `VerticalNew`,
    `Only`, `Close`.
- Desktop:
  - `Views/VimEditorBehavior.cs`: shared state and window-command callback.
  - `Views/EditorPaneView.axaml.cs`: behaviour built on the first pane.
  - `ViewModels/IPaneHost.cs`, `EditorPaneViewModel.cs`, `WorkspaceViewModel.cs`,
    `ExCommandHandler.cs`.
  - `Extensions/ServiceCollectionExtensions.cs`: `VimSharedState` singleton.
- Tests:
  - `Vim/VimScript.cs`: `VimRun.WindowCommands`.
  - `Vim/VimUndoTests.cs`, `Vim/VimModeTests.cs`: Ctrl+W is now claimed.
  - `Infrastructure/EditorHarness.cs`, `ViewModels/EditorPaneViewModelTests.cs`.

## Deviations & follow-ups

- **Two ported tests changed with the behaviour.** In MarkdownEditor, `Ctrl+W` belonged to the
  window. The plan makes it Vim's window prefix, so `VimUndoTests` now asserts it is claimed, and
  `VimModeTests` uses `Ctrl+O` as its example of a window shortcut left alone.
- **No counts on window commands.** `3 Ctrl+W +` resizes once, not three times.
- **Not implemented:** `Ctrl+W x` / `r` (exchange/rotate) and `Ctrl+W T`. Out of scope, as planned.
- **Fix cycles used: 1 of 3.** The two ported tests above (an intended behaviour change).

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where `Ctrl+W` is decoded | In `VimEngine.Dispatch`, before the grammar, as a one-key pending state | Testable in Core; cancels cleanly; no second pending-key mechanism in the view | In `VimEditorBehavior` (untestable, splits the key arbitration in two) |
| How the engine gets the shared state | Constructor parameter; the pane view builds the behaviour when its pane arrives | The state is fixed for an engine's life, and nothing is mutable after construction | A settable property on the engine (could be swapped mid-session) |
| `:sp file` | Split empty, then open the file in the new pane | Vim's window shows only that file; a stray tab of the old buffer would be noise | Split with the buffer, then add the file as a second tab |
| Closing the last pane | Refused with Vim's own message, "Cannot close last window" | Matches E444; `:q` is the way out | Quitting the app |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2269 passed**, 0 failed, 0 skipped. That includes:
  - every `Ctrl+W` mapping, pending/cancel/drop, visual-mode prefix, the insert-mode pass-through;
  - the window ex commands with their files;
  - yank/put, search and find shared across engines, and private when no state is shared;
  - workspace handling of every window command and window ex command;
  - a headless run that types `Ctrl+W v`, `yy`, `Ctrl+W h`, then `p` in another file in the other
    pane.

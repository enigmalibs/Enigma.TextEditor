# CODE-REVIEW-0734-PHASE14 — [Medium] SOLID-03 The Vim command line and ex commands leave `VimEngine`

## Summary

`VimEngine` was 3,709 lines. Its command line (typing it, plus `incsearch`), the ex lines it runs, and the search
motions it lands were three reasons to change living in one class. Each now has its own internal class beside
`VimSearch` and `VimSubstitute`:

- `VimCommandLine` holds the line as it is typed, the command that opened it, the view top to return to, and the
  `incsearch` preview. It covers `Open`, `Close`, `Cancel` and `HandleKey`; on Enter it hands back
  `(Line, Opener)`.
- `VimExCommands` runs an ex line: a line jump, `:s`, `:set`, `:noh`, the file requests, range resolution and the
  `'<` / `'>` marks. It also holds the `NoRangeAllowed` and `MarkNotSet` messages.
- `VimSearchMotion` works out where `/`, `?`, `n`, `N`, `*` and `#` land, and writes the last search. It holds the
  "search hit BOTTOM/TOP" messages and `NoPreviousSearch`.
- `IVimEngineContext` (internal) lists what the parts may ask of the engine: `Fail`, `StopMacro`, `MoveCaret`,
  `LastSelection`, `Request` (raises `ExCommandRequested`) and `RaiseOptionsChanged`. `VimEngine` implements it
  explicitly, so none of it appears on its public surface.
- The engine keeps the mode, the caret, the events and `RunCommandLine`. `RunCommandLine` runs a search as the
  motion it is (through `Execute`) and hands an ex line to `VimExCommands`. `IncSearch` now reads from the command
  line.
- `VimEngine` goes from 3,709 to 3,128 lines. Its public API is unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Vim/IVimEngineContext.cs`, `Vim/VimCommandLine.cs`, `Vim/VimExCommands.cs`,
  `Vim/VimSearchMotion.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/VimEngine.cs`

## Deviations & follow-ups

- No deviation from the plan.
- The Vim README says the engine is a port of Enigma.MarkdownEditor's and that a fix applying to both belongs in both.
  The split means such a fix now sits in a different file here than there. The README stays correct, so it was not
  edited (§7). It would be worth a line if the two engines are ever reconciled.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the parts reach the engine | A narrow internal interface, implemented explicitly | What each part may touch is written down, and the engine's public surface does not grow |
| Where `RunCommandLine` lives | In the engine | A search on the line runs through `Execute` like every motion (highlight, `.`, operators) |
| How Enter hands back the line | An `out (Line, Opener)?` from `HandleKey` | The line part never calls back into the engine to run anything |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3413 tests, 3411 passed, 2 skipped (PHASE06's), 0 failed.
- Guard: the Core Vim tests (2,231, all through the engine's public API) pass unchanged. No test was added, because
  the behaviour is unchanged and the parts are internal.
- Documentation sweep (§7): nothing user-facing changed.

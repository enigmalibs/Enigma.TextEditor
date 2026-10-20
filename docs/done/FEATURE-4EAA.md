# FEATURE-4EAA — :enew and :enew!

**Item:** FEATURE-4EAA — :enew and :enew!
**Branch:** `feature/feature-4eaa-enew`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

Two new ex commands.

**`:ene[w]`** opens a new untitled buffer in a new tab of the pane, like File → New.
- The tab that was showing stays open. This is Vim's `:enew` with `hidden` set: the tabs are this
  editor's buffer list, and `:e file` already adds a tab rather than replacing one.

**`:ene[w]!`** puts a new untitled buffer in the current tab's place.
- It does not ask about the replaced buffer's unsaved changes: the `!` is the answer.
- If another tab or pane still shows that buffer, it stays open there with its changes, the same rule
  as closing a tab.
- In a pane with no tab it does what `:enew` does.

**Parser:** `ene` is the shortest spelling, as in Vim. `:en` and `:enew {file}` are not commands.

**Help:** two lines in *Files & tabs*. `Ctrl+N` moved onto the `:enew` line, the way `Ctrl+S` sits on
the `:w` line and `Ctrl+O` on `:e`.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs` — `EditNew`, `EditNewDiscard`.
- `src/Enigma.TextEditor.Core/Vim/ExCommandParser.cs` — the `enew` definition.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — `ReplaceWithNewDocument(pane,
  tab)`.
- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — the two cases and a remarks
  paragraph.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — two new lines; the *Application*
  section's separate `Ctrl+N` line was removed.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/ExCommandParserTests.cs`:
  - `ene`/`enew` and their bang forms;
  - `en`, `enews`, `enew file` and `ene! file` are refused;
  - the bracketed spelling `:ene[w]!`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/ExCommandHandlerTests.cs`:
  - `:enew` adds a tab and keeps the dirty one, without asking;
  - `:enew!` replaces the middle tab in place: no question, the buffer leaves the registry, the file on
    disk is untouched;
  - `:enew!` on a buffer another pane shows keeps it, with its changes, in that pane;
  - `:enew!` in an empty pane opens a tab.
- `docs/roadmap.md`, `docs/plan/FEATURE-4EAA.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| `Ctrl+N` in the help | On the `:enew` line; the *Application* line removed | One line per command, as with `Ctrl+S`/`:w` and `Ctrl+O`/`:e` | Listing `Ctrl+N` twice |
| How `:enew!` swaps the buffer | `EditorPaneViewModel.Replace` + registry retain/release, the same steps as reopening as hex | Keeps the tab's place on the strip; the registry counts views exactly as for every other swap | Close then new (the tab would move to the end of the strip) |
| Shared buffer in the test | Built explicitly with `Show(document)` in the new pane | Holds once FEATURE-4A9A changes what a split shows | Relying on the split to share the buffer |

## Deviations & follow-ups

- None from the plan.
- Line endings: LF, per `.gitattributes`; no churn.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the in-app help was updated as part of the dev.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2476 passed**, 0 failed, 0 skipped (2463 before,
  plus 13 new).

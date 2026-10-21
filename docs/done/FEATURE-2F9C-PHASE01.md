# FEATURE-2F9C-PHASE01 — Indentation options

**Item:** FEATURE-2F9C — :set for the status bar items (PHASE01 of 2)
**Branch:** `feature/feature-2f9c-phase01-set-indentation`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

The status bar's *Spaces* item can now be set with `:set`: `expandtab`/`et`, `shiftwidth`/`sw` and
the new `tabstop`/`ts` change the buffer's indentation. The status bar, Tools → Indentation, the Tab
key and every pane showing the buffer follow.

- **Before:** these options lived only in the pane's Vim engine. The status bar kept saying
  *Spaces: 4* after `:set sw=2`, and the next tab switch quietly re-seeded the engine from the editor.
- **Core:**
  - `VimSet` knows `tabstop`/`ts`, the same value as `shiftwidth` (the editor has one indentation
    size). A bare `:set` lists it.
  - `VimEngine.OptionsChanged` is raised after a `:set` that changed `shiftwidth` or `expandtab`. It
    also fires when a later argument was refused, because what came before the refusal was applied,
    as in Vim.
- **Desktop:**
  - `VimEditorBehavior` hands the event on with the engine's options;
  - `EditorPaneView` writes `Spaces(sw)` or `Tabs(sw)` into the current text buffer's `Indentation`;
  - the existing indentation path then re-seeds the editor and the engine.
- **Help:** the two Options lines say the file's indentation follows, and list `tabstop, ts`.

## Files / modules touched

**Created**

- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SetCommandTests.cs` — typed in the real window:
  - `:set noet sw=8` gives *Tab Size: 8*, Tools → Tabs, and the editor's options;
  - `:set ts=2` then `>>` gives *Spaces: 2* and a 2-space indent;
  - `:set ic` leaves the indentation alone;
  - a second pane on the buffer follows.

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimSet.cs` — `tabstop`/`ts`, remarks.
- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `OptionsChanged`; `Set` compares before/after.
- `src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs` — `optionsChanged` constructor parameter.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs` — `OnVimOptionsChanged`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — the Options lines.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimSetTests.cs`:
  - `tabstop` sets the size;
  - `ts` and `sw` read one value;
  - the event fires once per changing `:set`, not for `ic`, `sw?` or a no-op, and despite a later
    refusal;
  - the bare `:set` listing gains `tabstop=4`.
- `README.md` — the `:set` bullet.
- `docs/roadmap.md`, `docs/plan/FEATURE-2F9C.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the engine knows an indentation option changed | Compare `ShiftWidth`/`ExpandTab` before and after `TryApply` | No change to `VimSet`'s signature; a `:set` that writes the same value announces nothing | An out flag from `VimSet` |
| Event name | `OptionsChanged` (the plan's), documented as the options the editor holds too | Leaves room for more shared options | `IndentationChanged` |
| A refused `:set` that changed something first | Still announced | Vim applies what precedes the refusal; the buffer must agree with the engine | Announce only on success |

## Deviations & follow-ups

- None from the plan. Fix cycles: 0.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the `:set` bullet listed the options and now needed `tabstop`, and to say the last three
  are the file's indentation.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2545 passed**, 0 failed, 0 skipped (2527 on the
  run branch before, +18).

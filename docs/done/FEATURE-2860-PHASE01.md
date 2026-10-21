# FEATURE-2860-PHASE01 — g=, g+, g-, g>, g<, gt, gT

**Item:** FEATURE-2860 — Quick pane and tab keys (PHASE01 of 2)
**Branch:** `feature/feature-2860-phase01-g-pane-keys`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

Quicker keys than `Ctrl+W …` for the panes, and Vim's tab keys for a pane's tabs.

- **`g=`** makes the panes the same size, as `Ctrl+W =`.
- **`g+`, `g-`, `g>`, `g<`** make the active pane taller, shorter, wider, narrower, as `Ctrl+W` with
  the same key.
- **The rule** is "`g` and the `Ctrl+W` key", which `gh` `gj` `gk` `gl` already followed. The parser
  completes all nine as one action, renamed `VimAction.PaneCommand` (was `FocusPane`), carrying the key.
  The engine maps the key through `VimWindowKeys.Map`, so the two spellings cannot drift apart.
- **`gt`, `gT`** show the next and previous tab of the active pane, wrapping round, as Ctrl+Tab and
  `:bn` / `:bp`. These are Vim's own keys for the next and previous tab page. They complete as
  `VimAction.NextTab` / `PreviousTab`, which raise the existing `BufferNext` / `BufferPrevious` ex
  requests.
- **As for `gh`:** they work in normal and both visual modes, are dead behind an operator (`dg=`,
  `dgt`), and accept a count but ignore it. They are not changes, so `.` does not repeat them.
- **Help and README:** the keys sit beside their `Ctrl+W` / Ctrl+Tab twins.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimAction.cs` — `FocusPane` → `PaneCommand` (doc widened); `NextTab`,
  `PreviousTab`.
- `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs` — `SecondG` and the `g` remarks.
- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `PaneCommand`, `NextTab`, `PreviousTab`.
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — `gt`/`gT` beside Ctrl+Tab; `g=`, `g+ g-`,
  `g> g<` beside their `Ctrl+W` keys.
- `README.md` — the *Tabs and panes* paragraph (keys added, paragraph re-wrapped at 120 columns).
- Tests:
  - `tests/…Core.UnitTests/Vim/KeySequenceParserTests.cs`:
    - the five size keys and `gt`/`gT` in normal and visual mode;
    - dead behind an operator;
    - `PaneCommand` rename.
  - `tests/…Core.UnitTests/Vim/VimWindowCommandTests.cs`:
    - each size key raises its window command and leaves the text;
    - `gt`/`gT` raise `BufferNext`/`BufferPrevious`, also in visual mode;
    - `.` does not repeat them.
  - `tests/…Desktop.UnitTests/ViewModels/WindowCommandTests.cs` — typed in the real window:
    - `g>`, `g<g<` and `g=` move the splitter (0.55, 0.45, 0.5);
    - `gt`/`gT` cycle a pane's three tabs, wrapping.
- `docs/roadmap.md`, `docs/plan/FEATURE-2860.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How `gt`/`gT` reach the pane | The existing `BufferNext`/`BufferPrevious` ex requests | One path with `:bn`/`:bp`, already handled and tested | A new window command |
| Help placement | On the existing rows, beside their twins | One line per action, as the help already does for `gh` | Separate rows for every `g` key |

## Deviations & follow-ups

- None from the plan.
- Fix cycles: 3, all compile errors in the new test files. The edit scripts wrote `\n` inside C#
  string literals as real newlines. The product code compiled and passed on the first green run.
- Follow-ups, out of scope as planned: a count multiplying the resize step (`3g>`), and `{count}gt`
  going to tab N as in Vim.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the *Tabs and panes* paragraph lists the new keys.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2630 passed**, 0 failed, 0 skipped (2607 before,
  +23).

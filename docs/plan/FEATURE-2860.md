# FEATURE-2860 — Quick pane and tab keys

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-2860-phase01-g-pane-keys`
**Branch (PHASE02):** `feature/feature-2860-phase02-resize-step`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

Quicker keys than `Ctrl+W …` to equalize, resize and move between tabs of the active pane, and a
configurable resize step.

## Context & constraints

- `gh`/`gj`/`gk`/`gl` already move between panes (FEATURE-5F2B):
  - `KeySequenceParser.SecondG` completes them as `VimAction.FocusPane`, carrying the letter;
  - the engine maps the letter through `VimWindowKeys.Map` and raises `WindowCommandRequested`.

  So `g{x}` and `Ctrl+W {x}` share one mapping.
- `VimWindowKeys.Map` already maps `=` (Equalize), `+`/`-` (Taller/Shorter) and `>`/`<`
  (Wider/Narrower). `WindowCommandHandler` resizes by `WorkspaceViewModel.ResizeStep`, a constant
  0.05 of the split.
- Tabs of a pane: `:bn`/`:bp` → `VimExRequest(BufferNext/BufferPrevious)` → `pane.NextTab()` /
  `PreviousTab()` (wrapping); Ctrl+Tab / Ctrl+Shift+Tab do the same.
- In Vim: `gt`/`gT` go to the next/previous tab page. `gn` selects the next search match, and `cgn`
  then `.` is a well-known idiom. `g=` is unused. `g+`/`g-` walk the undo tree in time and `g<` shows
  the last message; neither is implemented here.
- Settings live in `AppSettings` (validated by data annotations) and are edited from the Settings
  menu (`SettingsMenuViewModel`). Live values reach the view models through `EditorPreferences`.

## Decisions

| Decision | Rationale |
|---|---|
| `g=`, `g+`, `g-`, `g>`, `g<` do what `Ctrl+W` does with the same key | One rule: `g` + the `Ctrl+W` key, as `gh`…`gl` already are; `g=` is the draft's own suggestion |
| `gt` / `gT` go to the next / previous tab of the active pane, wrapping | Vim's own keys for exactly this |
| **Not** `gn` / `gN` | They would block Vim's `gn` text object (`cgn`, `dgn`), which the engine may want one day |
| `VimAction.FocusPane` becomes `VimAction.PaneCommand` (it now resizes and equalizes too); `gt`/`gT` complete as new actions that raise the `BufferNext`/`BufferPrevious` ex requests | Names say what they do; reuse the existing `:bn`/`:bp` path |
| A count is accepted and ignored, behind an operator the keys are dead, they work in visual mode, and `.` does not repeat them | Same as `gh` and `Ctrl+W` |
| PHASE02: `AppSettings.PaneResizeStep`, a percentage, 1–50, default 5 (today's step); live through `EditorPreferences.PaneResizeStep`; *Settings → Pane Resize Step* with 2 % / 5 % / 10 % | "Configurable percentage"; the default keeps today's behaviour; any other value in `settings.json` |

## PHASE01 — g=, g+, g-, g>, g<, gt, gT

**Status:** DONE — see `docs/done/FEATURE-2860-PHASE01.md`

### Steps

1. `Core/Vim/VimAction.cs`: rename `FocusPane` → `PaneCommand` (doc comment); add `NextTab`,
   `PreviousTab`.
2. `Core/Vim/KeySequenceParser.cs` (`SecondG` + remarks): `=`, `+`, `-`, `>`, `<` complete
   `PaneCommand` with the character; `t`/`T` complete `NextTab`/`PreviousTab`.
3. `Core/Vim/VimEngine.cs`: `PaneCommand` as before (via `VimWindowKeys.Map`); `NextTab`/`PreviousTab`
   raise `ExCommandRequested` with `BufferNext`/`BufferPrevious`.
4. `HelpCatalog`: lines for `g=`, `g+ g- g> g<`, `gt gT`.
5. README: the panes paragraph mentions the new keys.
6. Tests:
   - core parser: each key in normal and visual mode, dead behind an operator;
   - core engine: the window command or ex request raised, text untouched, `.` does not repeat;
   - desktop (typed in the real window): `g=` equalizes after a resize, `g>` widens the active pane,
     `gt`/`gT` cycle the pane's tabs.

### Acceptance criteria

- `g=`, `g+`, `g-`, `g>`, `g<` act like `Ctrl+W =`, `+`, `-`, `>`, `<`.
- `gt`/`gT` show the next/previous tab of the active pane, wrapping.
- Help lists them. Zero warnings, whole suite green.

## PHASE02 — Configurable resize step

**Status:** TODO

### Steps

1. `Core/Settings/AppSettings.cs`: `PaneResizeStep` (`[Range(1, 50)]`, default 5) with constants.
2. `EditorPreferences.PaneResizeStep`, seeded from the settings.
3. `WorkspaceViewModel`: the `ResizeStep` constant goes; the step is
   `Preferences.PaneResizeStep / 100.0`. `WindowCommandHandler` reads it from there.
4. `SettingsMenuViewModel`: three commands and `IsResizeStep2/5/10`; `MainWindow.axaml`:
   *Settings → Pane Resize Step* radio submenu.
5. README: the Settings sentence mentions the step.
6. Tests:
   - settings: default, validation (0 and 51 refused, the file falls back to defaults as other invalid
     values do);
   - resize: with a step of 4, `Ctrl+W >` takes 50/50 to 54/46;
   - the menu commands set the preference and write the settings.

### Acceptance criteria

- The resize keys move the splitter by the configured percentage; 5 % when unset.
- The Settings menu offers 2, 5 and 10 %, and remembers the choice.
- Zero warnings, whole suite green.

## Out of scope

- A count multiplying the step (`3g>`); `{count}gt` going to tab N; `:resize`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Equalize key | `g=` | The draft's suggestion; free in Vim; fits `g` + `Ctrl+W` key | A new chord; Alt shortcuts |
| Resize keys | `g+ g- g> g<` | Same rule; no conflict with anything implemented | `gH/gJ/gK/gL` (`gJ` is join); Alt+arrows |
| Tab keys | `gt` / `gT` | Vim's own; keeps `gn` free for `cgn` | `gn` / `gN` (the draft's guess) |
| Default step | 5 % (unchanged) | No surprise for current users; 4 % is one settings value away | 4 % (the draft's example) |
| Where the step is set | `settings.json` + Settings menu presets | Discoverable and still free-form | `settings.json` only; a `:set` option |

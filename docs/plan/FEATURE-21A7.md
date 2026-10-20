# FEATURE-21A7 — Vim shortcuts in menus

**Status:** DONE — see `docs/done/FEATURE-21A7.md`
**Type:** FEATURE
**Branch:** `feature/feature-21a7-menu-vim-hints`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Objective

While Vim mode is on, menu items and editor context-menu items that have a Vim equivalent show it on
the right: `:w`, `Ctrl+W v`, `u`, and so on.

## Context & constraints

- The Fluent `MenuItem` template has a header column (`*`, stretched) and, to its right, the
  `InputGesture` column (shared size). `InputGesture` holds a `KeyGesture`, not free text.
- Menus live in `MainWindow.axaml`; the editor context menu is in `EditorPaneView.axaml`
  (FEATURE-5D83). Both get their DataContext and inherited properties through the logical tree.
- Vim mode is `SettingsMenuViewModel.IsVimEnabled`, and `IVimSession.IsEnabled` / `Changed`.
- The help dialog's key notation is `Ctrl+W v`, `Ctrl+R`, `:w`.

## Decisions

| Decision | Rationale |
|---|---|
| Attached property `VimHint.Keys` (string) on a `MenuItem`. Setting it adds the class `vimHint` | One attribute per item in XAML |
| An app-wide style `MenuItem.vimHint` sets a `HeaderTemplate`: the header as `AccessText` (mnemonics kept), plus a right-aligned hint `TextBlock` in the shortcut text's colour | The hint column is right-aligned in the stretched header, just left of the shortcut column, in every menu and popup |
| Inherited attached property `VimHint.IsShown`, bound to Vim mode on the window; it reaches menus and the context menu through the logical tree | Hints only while Vim mode is on: `u` or `p` mean nothing otherwise |
| Hints only where Vim does the same thing | Cut/Copy/Paste get none: Vim's `d`/`y`/`p` use registers, not the system clipboard |
| The help dialog's notation | One vocabulary across the app |

**Hints**

| Menu | Item | Hint |
|---|---|---|
| File | New | `:enew` |
| File | Open… | `:e` |
| File | Save | `:w` |
| File | Save As… | `:w {file}` |
| File | Save All | `:wa` |
| File | Close Tab | `:q` |
| File | Exit | `:qa` |
| Edit / context | Undo | `u` |
| Edit / context | Redo | `Ctrl+R` |
| Context | Delete | `x` |
| Edit / context | Select All | `ggVG` |
| View | Split Right | `Ctrl+W v` |
| View | Split Down | `Ctrl+W s` |
| View | Close Pane | `Ctrl+W c` |
| View | Only This Pane | `Ctrl+W o` |
| View | Equalize Panes | `Ctrl+W =` |
| View | Focus Next Pane | `Ctrl+W w` |
| View | Focus Previous Pane | `Ctrl+W W` |
| Help | Key Bindings | `:help` |

## Steps

1. `Desktop/Views/VimHint.cs`: the two attached properties and the class toggle.
2. `Desktop/App.axaml`: the `MenuItem.vimHint` style with its `HeaderTemplate` and the hint text style.
3. `Desktop/Views/MainWindow.axaml`:
   - `VimHint.IsShown="{Binding Settings.IsVimEnabled}"` on the window;
   - `VimHint.Keys` on the items in the table.
4. `Desktop/Views/EditorPaneView.axaml`: `VimHint.Keys` on the context-menu items in the table.
5. Tests:
   - every `:`-hint in both views parses with `ExCommandParser.Parse` (`{file}` replaced by a name);
   - the `Ctrl+W` hints map through `VimWindowKeys`;
   - headless: an open File menu shows `:w` beside Save while Vim is on, and hides it when Vim is
     switched off;
   - the header keeps its access key (`_Save` shows "Save");
   - the context menu shows `u` beside Undo with Vim on.

## Acceptance criteria

- With Vim mode on, the listed items show their hint, right-aligned before the shortcut column; with it
  off, no hint shows.
- Mnemonics and gestures still work as before.
- No hint names a command the parser or the window keys do not accept.
- Zero warnings, whole suite green.

## Out of scope

- Hints for Tools and Settings items (no Vim equivalent).
- Rebinding menus to Vim keys.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Show hints with Vim off | No | They would be wrong: `u` types a "u" | Always show |
| Where on the row | Right-aligned header column, before the gesture | "In the right" without replacing the Ctrl gestures; no theme template copy | Replace the gesture text; copy the Fluent template |
| Cut/Copy/Paste hints | None | Vim's equivalents work on registers, not the clipboard | `d`/`y`/`p` (misleading) |
| Mechanism | Attached property + class + one style | One attribute per item; menus and context menus share it | A custom header control per item (verbose) |

# FEATURE-21A7 — Vim shortcuts in menus

**Item:** FEATURE-21A7 — Vim shortcuts in menus
**Branch:** `feature/feature-21a7-menu-vim-hints`
**Run:** vibe/2026-10-20-menus-enew-pane-focus

## Summary

While Vim mode is on, menu and editor context-menu items with a Vim twin show its keys on the right.
The keys sit in a right-aligned column just before the existing shortcut column, in the shortcut's
colour (and its disabled colour). With Vim mode off, no hint shows.

**Mechanism:** `Views/VimHint.cs` has two attached properties.
- `VimHint.Keys` (on a `MenuItem`) holds the keys in the help dialog's notation. Setting it gives the
  item the `vimHint` class.
- `VimHint.IsShown` is inherited. It is set once on the window, bound to `Settings.IsVimEnabled`, and
  reaches the main menu's popups and the editor's context menu.

One app-wide style in `App.axaml` (`MenuItem.vimHint`) sets a `HeaderTemplate`.
- The header is an `AccessText`, so mnemonics keep working.
- The keys are a `TextBlock` right-aligned in the header's stretched column.
- Items without keys keep the theme's own header.

**Hints:**
- File: New `:enew`, Open… `:e`, Save `:w`, Save As… `:w {file}`, Save All `:wa`, Close Tab `:q`,
  Exit `:qa`.
- Edit: Undo `u`, Redo `Ctrl+R`, Select All `ggVG`.
- View: Split Right `Ctrl+W v`, Split Down `Ctrl+W s`, Close Pane `Ctrl+W c`, Only This Pane
  `Ctrl+W o`, Equalize `Ctrl+W =`, Focus Next/Previous `Ctrl+W w`/`W`.
- Help: Key Bindings `:help`.
- Context menu: Undo `u`, Redo `Ctrl+R`, Delete `x`, Select All `ggVG`.
- No hint on Cut/Copy/Paste: Vim's `d`/`y`/`p` use registers, not the system clipboard.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Views/VimHint.cs` — the attached properties and the class toggle.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/MenuVimHintTests.cs`:
  - the main menu and the context menu carry exactly the expected hints, and none on the clipboard
    items;
  - every hint is something the editor reads: ex hints parse; `Ctrl+W` hints map through
    `VimWindowKeys`; `u`, `x`, `ggVG` complete in the grammar;
  - an open File menu shows `:w` beside Save; the header keeps its mnemonic; every hint ends at the
    same x, before the shortcut text;
  - switching Vim off hides the hints;
  - the context menu shows `u` beside Undo.

**Modified**

- `src/Enigma.TextEditor.Desktop/App.axaml` — the `views` namespace, the `MenuItem.vimHint` style and
  the hint text styles.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — `VimHint.IsShown` on the window;
  `VimHint.Keys` on 18 items.
- `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml` — `VimHint.Keys` on four context-menu
  items.
- `docs/roadmap.md`, `docs/plan/FEATURE-21A7.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Owner type of the attached properties | `sealed class VimHint : AvaloniaObject` with a private constructor | A static class cannot be the owner's type argument; this is Avalonia's own pattern | A static holder (does not compile) |
| Hint colour | A style on `TextBlock.vimHintKeys` with Fluent's `MenuFlyoutItemKeyboardAcceleratorTextForeground`, and its `…Disabled` twin under `:disabled` | The same colours as the shortcut text. An attribute in the template would outrank the disabled style | Hard-coded brush |
| Font | Fluent's `CaptionTextBlockStyle` class, as the shortcut text uses | Same size as the gesture next to it | Monospace (would not match the gesture) |
| Checking normal-mode hints | Feed them to `KeySequenceParser`, following the mode `V`/`v` enter | `ggVG` is three commands across two grammars; this is how the engine reads it | Only a fixed table |

## Deviations & follow-ups

- None from the plan.
- Not done: no look at the running Debug build. Layout, visibility and alignment are measured
  headlessly in the real window with the application's theme.
- Fix cycles: 3, all compile errors in the new test file:
  - `AccessText` needs `Avalonia.Controls.Primitives`;
  - `AccessText.AccessKey` is not a `char`;
  - it is nullable.

  Green on the third. The feature code did not change across the cycles.
- Line endings: LF; no churn.

## Documentation sweep

Nothing became wrong. README and RELEASENOTES are empty; the help dialog does not describe the menus.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (no AVLN warnings).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2503 passed**, 0 failed, 0 skipped (2497 before,
  plus 6 new).

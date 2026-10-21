# FEATURE-1FBE — Dialog keyboard focus

**Item:** FEATURE-1FBE — Dialog keyboard focus
**Branch:** `feature/feature-1fbe-dialog-focus`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

A dialog now takes the keyboard when it opens and gives it back when it closes.

- **On open:** unless its content took the focus itself, the dialog's default button gets the
  keyboard, or its first button when it declares none. "Save changes?" opens on Save, which is its
  first button and its default.
- **Tab and Shift+Tab** walk the buttons and wrap round (`KeyboardNavigation.TabNavigation="Cycle"`
  on the host). Enter or Space answers with the focused button.
- **Esc** answers Cancel. The library's host already closed on Esc with `DialogResult.None`, which
  every prompt reads as Cancel, but the key never reached it because the keyboard stayed in the editor
  behind the dialog.
- **Dialogs that focus their own content** keep it there: the language picker's filter box, the
  encoding list, the help's scroll area.
- **On close:** the element that had the keyboard before gets it back if it is still on screen;
  otherwise the active pane's editor (or hex rows) does.
- **How:** `Views/DialogKeyboard` follows the host's `IsOpen`. It focuses the button at Background
  priority, after the content's own focus, and restores the focus the same way once the host has
  hidden. `MainWindow` attaches it, with `FocusActivePane()` as the fallback.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/Views/DialogKeyboard.cs`.
- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/DialogKeyboardTests.cs` — real window, real prompts:
  - "Save changes?" opens on Save; Tab gives Don't save, Cancel, Save; Shift+Tab goes back;
  - Esc answers Cancel and the editor has the keyboard again;
  - Tab then Enter answers Don't save;
  - a confirmation with Cancel as default opens on Cancel;
  - the language picker keeps its filter box focused.

**Modified**

- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — `KeyboardNavigation.TabNavigation="Cycle"`
  on `HostDialog`.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs` — attaches `DialogKeyboard`;
  `FocusActivePane()`.
- `docs/roadmap.md`, `docs/plan/FEATURE-1FBE.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the buttons are found | By their template part names, among the host's visual descendants | The library's own names; no change to the package | Subclassing the host |
| Focus visual | `NavigationMethod.Tab`, so the focused button shows its focus ring | The user has to see which button Enter answers | A pointer-style focus with no ring |
| A dialog opened again before the restore runs | Keeps the keyboard | The restore is posted, and must not pull focus out of the new dialog | Restoring regardless |

## Deviations & follow-ups

- None from the plan. Fix cycles: 0.
- Follow-up suggestion: move this behaviour into Enigma.Avalonia.Desktop's `ContentDialog`, so every
  Enigma app gets it.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong: the README does not describe dialog keyboard handling.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2640 passed**, 0 failed, 0 skipped (2635 before,
  +5).

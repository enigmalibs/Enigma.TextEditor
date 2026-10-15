# FEATURE-6BB4 — Settings & Help menus

**Item:** FEATURE-6BB4 — Settings & Help menus
**Branch:** `feature/feature-6bb4-settings-help-menus`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The menu bar now reads **File · Edit · View · Tools · Settings · Help**.

- **Settings menu.** Each item shows its current value as a check or a radio mark, applies at once,
  and is written to the settings file at once:
  - *Theme ▸ System / Light / Dark* — repaints through `IThemeService`.
  - *Vim Mode* ✓ — every pane attaches or detaches its engine. With Vim off, keys type and the mode
    chip hides.
  - *Word Wrap* ✓ — every pane's editor.
  - *Default Indentation ▸ Spaces: 2 / Spaces: 4 / Tabs* and *Default Line Endings ▸ LF / CRLF* — used
    by new buffers, and as the fallback when a file's own indentation does not say.
- **View menu zoom.** *Zoom In* (Ctrl+= / Ctrl+numpad +), *Zoom Out* (Ctrl+- / Ctrl+numpad -),
  *Reset Zoom* (Ctrl+0). One point per step, within 6–72, default 14. Persisted, and applied to every
  pane, the hex viewer included.
- **A failed write** is logged every time, and shown to the user once per session (*Settings could
  not be saved*). The change still applies until the editor closes.
- **Help → About Enigma Text Editor** opens a content dialog with one *Close* button. It shows the
  product name, the version, the build revision (only when the build recorded one) and the copyright.

## Files / modules touched

**Created**

- `Services/ISettingsWriter.cs`, `Services/SettingsWriter.cs` — writes the settings, one write at a
  time, and reports a failure once.
- `ViewModels/SettingsMenuViewModel.cs` — the Settings menu's checks, radio marks and commands, and
  the zoom commands.
- `ViewModels/AboutViewModel.cs`, `Views/AboutView.axaml(.cs)`, `Services/IAboutDialogService.cs`,
  `Services/AboutDialogService.cs`.
- Tests: `ViewModels/SettingsMenuTests.cs` (10), `Infrastructure/FakeThemeService.cs`,
  `Infrastructure/FakeAboutDialogService.cs`.

**Modified**

- `ViewModels/MainWindowViewModel.cs`: `Settings` and `AboutCommand`.
- `Views/MainWindow.axaml`: the Settings and Help menus, the zoom items in View, the zoom key
  bindings.
- `Extensions/ServiceCollectionExtensions.cs`: the writer, the Settings menu and the About dialog.
- Tests: `Infrastructure/EditorHarness.cs`, `CompositionRootTests.cs`.

**Unchanged, already in place:** `DocumentService` already took the default indentation and line
ending from the settings for new buffers and for the indentation detector's fallback (FEATURE-316E),
so plan step 5 needed no code.

## Deviations & follow-ups

- **No app icon in the About dialog.** The repository has no icon yet; the dialog shows text only.
  Follow-up: ship `Assets/app.ico` and add it to the window, the executable and the About dialog.
- **Fix cycles used: 0 of 3.**
- **Not done:** no manual smoke of the Debug build. The window, its menus, the About view and the
  toggles reaching the open editors are exercised headlessly.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the menu's state lives | A separate `SettingsMenuViewModel`, exposed as `MainWindowViewModel.Settings` | `MainWindowViewModel` stays small; the menu needs five services of its own | Members on `MainWindowViewModel` (over-injection) |
| Zoom step | One point per step | Fine control within a 6–72 range | Percent steps (uneven at small sizes) |
| Zoom gestures | Ctrl+OemPlus and Ctrl+Add; Ctrl+OemMinus and Ctrl+Subtract; Ctrl+D0 | Ctrl+= is the OemPlus key on most layouts; the numpad keys behave the same | One gesture each |
| *Tabs* as the default indentation | Tabs, size 4 | A tab's displayed width; four is the editor's default | Asking for a size (no dialog in this menu) |
| Overlapping commands | Commands allow concurrent runs; the writer serializes the writes | A held Ctrl+= would otherwise drop steps and grey the menu item out while a write runs; two writes at once would race on the same file | The toolkit's default (one run at a time per command) |
| The failure message | Shown after the write lock is released | An open dialog must not hold up later writes | Showing it under the lock |
| Write failures caught | `IOException`, `UnauthorizedAccessException`, `EnigmaConfigurationException` | What a write to an unusable folder or an invalid value raises; anything else is a bug and surfaces | Catching every exception |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2360 passed**, 0 failed, 0 skipped. That
  includes:
  - every toggle applies and is read back from the settings file;
  - the radio marks follow the choice;
  - zoom steps by one, stops at 72 and 6, persists, and resets to 14;
  - a new buffer takes the default indentation and line endings;
  - a failed write (a folder where the file should be) is reported once and the change still applies;
  - turning Vim off in an open window lets keys type and hides the mode chip;
  - wrap and zoom reach the open editor;
  - the menu bar order, the Settings and View items, and a check mark following a toggle;
  - Help → About asks for the dialog; the About view shows the name, version and copyright.

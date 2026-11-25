# CODE-REVIEW-0734-PHASE08 — [Medium] XAML-01 Radio menu choices are lists of choice items

## Summary

Each radio group used to be one command and one bool per value in the view-model, plus two or three hand-copied
`MenuItem`s in `MainWindow.axaml` that had to stay in step. The indentation group existed three times and the
line-ending group three times. Every group is now one list of `MenuChoice`, bound through one item theme.

- New `ViewModels/MenuChoice` (`ObservableObject`) holds:
  - `Header`, with an access-key underscore;
  - `Group`, the radio group's name;
  - `Command`;
  - `IsCurrent`, read from the state through a predicate and refreshed in place by `MenuChoice.Refresh(list)`.
  The lists are built once, so a flyout, which follows the collection it first showed, keeps working. This is the
  same rule `FontSizeChoices` already followed.
- `SettingsMenuViewModel` has `ThemeChoices`, `PaneResizeStepChoices`, `DefaultIndentationChoices` and
  `DefaultLineEndingChoices`. The eleven `Is…` properties and eleven `…Command`s they replace are removed. The
  `Set…Async` methods stay, and refresh their group. `ResetAsync` and `ApplyFirstRunAsync` refresh all four.
- `ToolsViewModel` has `LineEndingChoices` and `IndentationChoices`. The five `Is…` properties and five commands are
  removed. The choices act on the active buffer and are disabled on a hex tab, as before.
- `MainWindow.axaml` has one `MenuChoiceItem` container theme (header, radio, group, `IsChecked` one-way, command).
  The six submenus and the two status-bar flyouts are each one element bound to their list. The headers, access keys,
  order, radio marks and enabling are unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/ViewModels/MenuChoice.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/SettingsMenuViewModel.cs`, `ViewModels/ToolsViewModel.cs`,
  `Views/MainWindow.axaml`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/MenuChoiceExtensions.cs` (`Choice`,
  `ChooseAsync`), `Infrastructure/MenuItems.cs` (what an opened submenu shows),
  `ViewModels/MenuChoiceTests.cs`
- Modified (moved to the lists): `tests/…/ViewModels/FirstRunTests.cs`, `LineEndingAndIndentationToolsTests.cs`,
  `PreferencesTests.cs`, `SettingsMenuTests.cs`, `Views/HexViewTests.cs`, `SetCommandTests.cs`,
  `SettingsPageTests.cs`, `StatusBarFocusTests.cs`

## Deviations & follow-ups

- On a hex tab, the Tools choices still show the hex buffer's default format as checked, but disabled, as the old
  bools did. Not changed.
- The two menu-structure tests read a list-bound submenu by opening it, since its items have no controls until then.
  Their assertions are unchanged: the same headers, in the same order, with the same marks. The default line ending
  is now compared with the setting instead of being assumed to be LF.
- Fix cycles used: 0. Two of the new tests had wrong expectations during development, before the first full run:
  hex-tab checks and the default step being 10.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Choice type | A small observable class with a predicate | `IsCurrent` must change in place. A record would mean replacing list entries, which a flyout does not follow |
| Command type | `ICommand` (async for settings, sync for tools) | Each group keeps the command kind it had |
| Tests | Extension blocks `Choice` / `ChooseAsync` by header | Tests read the menu as a user does. The `extension` block follows the house C# 14 style |
| Group names | One per list, set at construction | Menu and flyout sit in separate popup roots, so a shared list is safe |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3400 tests, 3398 passed, 2 skipped (PHASE06's), 0 failed.
- Tests added (5) in `MenuChoiceTests`:
  - every theme checks exactly its own choice;
  - choosing sets the value and moves the check;
  - the lists and their entries are the same objects for the life of the menu, and `IsCurrent` raises its change;
  - the Tools choices follow the active buffer and are disabled on a hex tab;
  - each group has a radio-group name of its own.
- Every existing menu, settings, preferences, first-run, `:set` and status-bar test passes, moved to the lists.
- Documentation sweep (§7): nothing user-facing changed.

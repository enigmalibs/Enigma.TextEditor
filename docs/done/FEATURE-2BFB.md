# FEATURE-2BFB — :set lang opens the language picker

## Summary

`:set lang` (or `:set language`) typed on the Vim command line opens the language list, the same one the status bar's
language button and **Tools → Set Language…** open, for the buffer the command was typed in. A pick changes the
buffer's language; a cancel changes nothing. `:set lang?` shows `language=<id>` and `:set lang=<name>` sets the language,
as `ft?` / `ft=<name>` do. In the hex view the command is refused with the existing message, and the list does not
open.

The engine forwards `language`/`lang` as file options (`VimSet.FileOptionNames`). `FileOptions.Apply` gained an
overload that reports a bare `language` through `out bool pickLanguage` (false after any refusal). `ExCommandHandler`
now takes `ILanguagePicker` (through `WorkspaceViewModel`'s constructor), shows the status message, and then awaits
the picker.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Core/Vim/VimSet.cs`, `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/FileOptions.cs`, `ExCommandHandler.cs`, `WorkspaceViewModel.cs`,
  `HelpCatalog.cs`
- Modified: `README.md`
- Modified: `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimSetFileOptionsTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs`,
  `ViewModels/FileOptionsTests.cs`, `Views/SetCommandTests.cs`

## Deviations & follow-ups

- The plan had `FileOptions.Apply` return a `FileOptionsResult(Message, PickLanguage)`. It got an overload with
  `out bool pickLanguage` instead, and the existing `string?` overload is kept. Changing the return type would have
  rewritten every existing `FileOptionsTests` assertion, which goes against the plan's own "existing `:set` tests pass
  unchanged".
- Pre-existing failures, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`,
  `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`.
- Documentation sweep (§7): README's Vim `:set` paragraph now mentions `:set lang`; the help's *Options* section gained
  the row *Pick the language from the list* — `:set language, lang`.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Shape of the pick request | `out bool pickLanguage` overload | Keeps the existing API and tests; the parser stays synchronous |
| `lang` given twice in one `:set` | The list opens once | It is one request for the list, not two |
| Picker target | The buffer the command was typed in (captured before awaiting) | Matches the plan; the active pane is that buffer anyway |

## Build/test evidence

- Run baseline (on `feature/2026-11-10-set-lang-zoom-gestures` after the planning commit `bc2026a`, before this
  branch): `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors. `dotnet test --solution Enigma.TextEditor.slnx`:
  3292 tests, 3290 passed, 2 failed (the two pre-existing failures above).
- This dev: build 0 warnings, 0 errors; 3308 tests, 3306 passed, 2 failed (the same two).
- Tests added (16):
  - Core: `:set lang`, `language`, `lang=python`, `lang?` forwarded; `nolang` unknown.
  - `FileOptionsTests`: bare `lang`/`language` asks for the list; `?`/`=` show and set it; `lang!` and an unknown name
    are refused; a refusal after `lang` asks for nothing; hex refused.
  - Headless `SetCommandTests`: the list opens with the buffer's language and the pick reaches the buffer and the status
    bar (both spellings); a cancel changes nothing; `lang ff=dos lang` opens it once with `ff` applied; refused in the
    hex view.

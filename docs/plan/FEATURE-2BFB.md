# FEATURE-2BFB — :set lang opens the language picker

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-2bfb-set-lang`
**Run:** feature/2026-11-10-set-lang-zoom-gestures

## Objective

The status bar's language button (and **Tools → Set Language…**) opens the language picker. Let the Vim command line
open the same picker: `:set lang`.

## Context & constraints

- Core: `VimSet` (`src/Enigma.TextEditor.Core/Vim/VimSet.cs`) keeps the engine's options. The words of the file
  options (`fileformat`/`ff`, `fileencoding`/`fenc`, `bomb`, `filetype`/`ft`) are listed in `FileOptionNames`; they
  are not kept by the engine but forwarded, in order, as one `VimExRequest(VimExCommand.SetFileOptions, words)`.
- Desktop: `ExCommandHandler.ExecuteAsync` passes those words to `FileOptions.Apply(words, document, languages)`
  (static, synchronous). That returns the message for the status line, or `null`. In the hex view it refuses every word
  (`Not available in the hex view: <word>`).
- The picker: `ILanguagePicker.PickAsync(current)` → the picked `LanguageInfo` or `null` (cancelled), then
  `DocumentViewModel.ChooseLanguage`, exactly as `ToolsViewModel.OnSetLanguageAsync` does. The dialog gives the
  keyboard back to the current page when it closes (`DialogKeyboard.Attach`).
- `ExCommandHandler` is built by `WorkspaceViewModel`'s constructor (DI singleton). `EditorHarness` builds
  `WorkspaceViewModel` directly, with `FakeLanguagePicker` (`Answers`, `OpenedWith`).
- Docs: README (Vim section, `:set` options paragraph), `HelpCatalog` (*Options* section).

## Decisions

- Spellings: `language` and its short form `lang`, a file option like the others (added to `FileOptionNames`).
- `:set lang` with no value opens the picker on the buffer the command was typed in. Picked → `ChooseLanguage`;
  cancelled → nothing changes, no message.
- `:set lang?` shows `language=<id>` (as `ft?` shows `filetype=<id>`). `:set lang=<value>` sets the language exactly
  like `ft=<value>`. `lang!` and `nolang` are refused like the other valued options.
- Words apply in order. A bare `lang` asks for the picker, which opens once, after the other words are applied.
  If a later word is refused, the words before it have already applied but the picker does not open (Vim's
  first-error rule).
- `FileOptions.Apply` stays synchronous and reports the request: it returns a small result (`Message`,
  `PickLanguage`) instead of a bare string. `ExCommandHandler` gains `ILanguagePicker` (through `WorkspaceViewModel`'s
  constructor) and awaits the picker.
- In the hex view: refused with the existing message.

## Steps

1. `VimSet.FileOptionNames` += `language`, `lang`; update the remarks/doc comments that list the file options
   (`VimSet`, `VimExCommand.SetFileOptions`).
2. `FileOptions`: `case "language" or "lang"`: bare → request the picker; `?` → `language=<id>`; `=` → the existing
   `SetLanguage`; `!` → refused. Return type → `FileOptionsResult(string? Message, bool PickLanguage)`.
3. `ExCommandHandler`: take `ILanguagePicker`; on `SetFileOptions`, show the message, and if `PickLanguage` (and still
   on the same document) await `PickAsync(document.Language)` → `ChooseLanguage`.
4. `WorkspaceViewModel` constructor: an `ILanguagePicker` parameter, passed through to `ExCommandHandler`. Update
   `EditorHarness`.
5. `HelpCatalog`: a row *Pick the language from a list* — `:set language, lang`. README: mention `:set lang` beside
   `filetype`.
6. Tests:
   - Core `VimSetFileOptionsTests`: `:set lang`, `:set language`, `:set lang=python`, `:set lang?` are forwarded.
   - Desktop `FileOptionsTests`: bare → `PickLanguage` with no message; `?` → `language=<id>`; `=` sets; `!` refused;
     hex refused.
   - Desktop headless (`SetCommandTests` or `LanguageTests`): `:set lang` in the window opens the picker with the
     buffer's language; a scripted answer changes the buffer's language and the status bar's text; a cancel changes
     nothing; `:set lang` in the hex view does not open it.

## Acceptance criteria

- `:set lang` (and `:set language`) typed in a text tab opens the language picker. The choice applies to that buffer,
  as the status bar's button does.
- `:set lang=python` / `:set lang?` behave as `ft=python` / `ft?` (under the name `language`).
- `:set lang` in the hex view does not open the picker and says why.
- Existing `:set` tests pass unchanged; zero new warnings; the whole suite green against the baseline.

## Out of scope

- Making a bare `:set ft` open the picker (Vim shows the value; unchanged).
- A separate `:lang[uage]` ex command (Vim's `:language` sets the message locale).
- The encoding and line-ending pickers from the command line.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which spellings open the picker? | `:set lang` and `:set language` | The draft's `:set lang`, plus its long form, as each option has one | Only `lang` (inconsistent with the long/short pairs) |
| What do `lang?` / `lang=x` do? | Show / set the language, like `ft?` / `ft=x` | A user who learned `:set lang` will try `lang=python`; refusing it would be a narrow reading | Refuse every form but the bare word |
| Should bare `:set ft` open the picker too? | No | Vim shows the value; existing behaviour and tests | Repurpose `ft` |
| How does the handler reach the picker? | Inject `ILanguagePicker` into `ExCommandHandler` via `WorkspaceViewModel` | Plain constructor DI; the picker is already a singleton service | An event that `ToolsViewModel` answers (indirect request/response); passing `ToolsViewModel` (circular) |
| `FileOptions.Apply` signature | Returns `FileOptionsResult(Message, PickLanguage)` | Keeps the parser synchronous and testable; the async dialog stays in the handler | Making `FileOptions` async with the picker inside |
| Hex view | Refused with the existing message | The rule `FileOptions` already applies to every file option | Opening the picker and ignoring the pick |
| A word after `lang` is refused | The picker does not open | Vim stops at the first error; a dialog after an error message would hide it | Opening it anyway |

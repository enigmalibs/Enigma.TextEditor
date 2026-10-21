# FEATURE-2F9C — :set for the status bar items

**Status:** DONE — see `docs/done/FEATURE-2F9C-PHASE01.md`, `docs/done/FEATURE-2F9C-PHASE02.md`
**Type:** FEATURE
**Branch (PHASE01):** `feature/feature-2f9c-phase01-set-indentation`
**Branch (PHASE02):** `feature/feature-2f9c-phase02-set-file-options`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

Every item the status bar can change — indentation (*Spaces*), encoding, language and line
endings — can also be set with `:set`, under Vim's own option names.

## Context & constraints

- `:set` is the engine's (`VimEngine.Set` → `VimSet.TryApply`). It knows `ignorecase`, `smartcase`,
  `wrapscan`, `expandtab`, `hlsearch`, `incsearch` and `shiftwidth`. An unknown name fails with
  `Unknown option: …`, a bad value with `Invalid argument: …`. A failure stops at the first bad
  argument, with what came before it done (Vim's rule).
- `expandtab`/`shiftwidth` today change only the engine's `VimOptions`. The buffer's `Indentation`
  (status bar *Spaces: 4*, Tools → Indentation, the editor's Tab key) does not follow. The next
  `VimEditorBehavior.Refresh` re-seeds the engine from the editor and undoes the `:set`.
- The buffer's format is changed by `DocumentViewModel`:
  - `ChangeLineEndings(LineEndingStyle)`;
  - `ChangeEncoding(TextEncoding)` (written on the next save);
  - `ChooseLanguage(LanguageInfo)`;
  - `Indentation { set; }`.

  `ToolsViewModel` and the status bar call these.
- `TextEncodings.All`: UTF-8, UTF-8 with BOM, UTF-16 LE/BE (with BOM), Windows-1252, ISO-8859-1,
  ISO-8859-15, Windows-1250, Windows-1251. `LanguageCatalog`: TextMate ids (`csharp`, `python`,
  `shellscript`…), display names and extensions; `plaintext` for Plain Text.
- Requests that need the application leave the engine as `VimExRequest`s through
  `ExCommandRequested`, handled by `ExCommandHandler`.

## Decisions

| Decision | Rationale |
|---|---|
| Vim's names: `expandtab`/`et`, `shiftwidth`/`sw`, `tabstop`/`ts`, `fileformat`/`ff`, `fileencoding`/`fenc`, `bomb`, `filetype`/`ft` | The ones a Vim user's fingers know |
| `shiftwidth` and `tabstop` are one value here: the buffer's indentation size | The editor has one size; documented in help |
| After a `:set` that writes `et`/`sw`/`ts`, the engine raises `OptionsChanged`; the pane writes the buffer's `Indentation` from it (`Spaces(sw)` or `Tabs(sw)`); not on a hex tab | One indentation, shown everywhere; nothing undoes the `:set` any more |
| `ff`, `fenc`, `bomb`, `ft` are known to the engine as file options and forwarded in one `VimExRequest(VimExCommand.SetFileOptions, "<words>")`; the app validates and applies them | The values (encodings, languages) are the application's |
| `ff=unix` → LF, `ff=dos` → CRLF, `ff=mac` → `Invalid argument` | CR-only files are not supported |
| `fenc` = Change File Encoding (next save), Vim names and aliases (`utf-8`, `utf-16le`, `utf-16`/`utf-16be`, `latin1`/`iso-8859-1`, `iso-8859-15`/`latin9`, `cp1252`, `cp1250`, `cp1251`, plus the `windows-125x` spellings); `fenc=utf-8` keeps the current BOM state | Vim's `fileencoding` + `bomb` model |
| `bomb`/`nobomb` toggles UTF-8's BOM; UTF-16 always has one here; other encodings answer `Invalid argument` | What the encoding list can represent |
| `ft=` accepts the TextMate id, the display name or an extension (case-insensitive); `ft=` or `ft=text` is Plain Text | Covers Vim's names (`python`, `cs`, `sh`…) without a mapping table |
| `name?` and bare `name` show the value (`fileformat=unix`, `fileencoding=utf-8`, `nobomb`, `filetype=python`) | Vim's spelling |
| On a hex tab the file options answer `Not available in the hex view` | The hex view has no text format |
| A bare `:set` keeps showing the engine's options only | It is synchronous in the engine; the app values are one `?` away |

## PHASE01 — Indentation options

**Status:** DONE — see `docs/done/FEATURE-2F9C-PHASE01.md`

### Steps

1. `Core/Vim/VimSet.cs`: `tabstop`/`ts` (same value as `shiftwidth`); `TryApply` reports whether an
   indentation option was written.
2. `Core/Vim/VimEngine.cs`: `event EventHandler? OptionsChanged`, raised after a `:set` that wrote
   `expandtab`, `shiftwidth` or `tabstop`.
3. `Desktop/Views/VimEditorBehavior.cs`: forward the event to a callback; `EditorPaneView` writes the
   current text buffer's `Indentation`.
4. `HelpCatalog`: the `:set` line lists `tabstop` and says the indentation follows.
5. Tests:
   - core: `:set ts=2` sets the width, `:set ts?` shows it, the event fires for `et`/`sw`/`ts` and not
     for `ic`;
   - desktop: `:set noet sw=8` gives the buffer `Tab Size: 8` (status bar text), `:set et sw=2` gives
     `Spaces: 2`; `>>` then indents by the new size; other panes on the buffer follow.

### Acceptance criteria

- `:set et`, `:set noet`, `:set sw=N`, `:set ts=N` change the buffer's indentation, and the status bar
  and Tools menu show it.
- Zero warnings, whole suite green.

## PHASE02 — ff, fenc, bomb and ft

**Status:** DONE — see `docs/done/FEATURE-2F9C-PHASE02.md`

### Steps

1. `Core/Vim/VimExCommand.cs`: `SetFileOptions`.
2. `Core/Vim/VimSet.cs`: the four file option names (long and short) are recognised and collected
   rather than refused; the engine raises one `SetFileOptions` request with the collected words, in
   order, and still fails on an unknown name after them.
3. `Desktop/ViewModels/FileOptions.cs` (new, internal): parses and applies the words to a
   `DocumentViewModel` with the `LanguageCatalog`, and returns the message to show (values or the
   first error).
4. `ExCommandHandler`: `case VimExCommand.SetFileOptions` → `FileOptions`, then the status line.
5. `LanguageCatalog`: `Lookup(name)` by id, display name or extension.
6. `HelpCatalog` and README: the new options.
7. Tests:
   - core: forwarding (single, several, mixed with engine options, failure after a file option);
   - desktop: each option, its `?` form, aliases, invalid values, the hex refusal, and that the status
     bar text changes.

### Acceptance criteria

- `:set ff=dos`, `:set fenc=latin1`, `:set bomb`, `:set ft=python` change what the status bar shows,
  as its buttons do; `:set ff?` etc. answer on the status line.
- Invalid values are refused with `Invalid argument: …`; nothing changes.
- Zero warnings, whole suite green.

## Out of scope

- `:e ++enc=` (reopen with an encoding), `:set all`, `softtabstop`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Option names | Vim's (`ff`, `fenc`, `bomb`, `ft`, `et`, `sw`, `ts`) | Vim users already know them; the app is Vim-first | Invented names (`:set encoding=`, `:set language=`) |
| Who validates file options | The app, after the engine forwards them | Encodings and languages live in the app | Teach the Core engine the encoding and language lists |
| `fenc` semantics | Change File Encoding (next save) | Vim's `fileencoding` | Reopen with encoding |
| `tabstop` | Same value as `shiftwidth` | One indentation size in this editor | A separate tab width |

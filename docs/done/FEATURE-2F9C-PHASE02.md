# FEATURE-2F9C-PHASE02 — ff, fenc, bomb and ft

**Item:** FEATURE-2F9C — :set for the status bar items (PHASE02 of 2)
**Branch:** `feature/feature-2f9c-phase02-set-file-options`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

The status bar's encoding, language and line-ending items are now `:set` options under Vim's names, so
all four of its buttons have a `:set` (indentation came in PHASE01).

- **`:set fileformat=unix|dos`** (`ff`) — the line endings, as *Line Endings → LF / CRLF*. `mac` is
  refused: the editor does not write CR-only files.
- **`:set fileencoding=…`** (`fenc`) — the encoding the next save writes, as *Change File Encoding…*
  (Vim's `fileencoding`, not a re-read). Accepted names:
  - `utf-8` / `utf8`;
  - `utf-16le`;
  - `utf-16` / `utf-16be`;
  - `latin1` / `iso-8859-1`;
  - `iso-8859-15` / `latin9`;
  - `cp1252`, `cp1250`, `cp1251`, and their `windows-125x` spellings.

  `fenc=utf-8` keeps the current byte-order mark, as Vim keeps `bomb`.
- **`:set bomb` / `nobomb` / `bomb!`** — UTF-8's byte-order mark. UTF-16 is always written with one
  here, and the single-byte encodings have none, so a change there is refused.
- **`:set filetype=…`** (`ft`) — the language, as *Set Language…*. It accepts the grammar id, the
  display name or an extension (`python`, `C#`, `cs`), ignoring case. `ft=` and `ft=text` give Plain
  Text.
- **Showing and refusing:**
  - `name?` or a bare `name` shows the value under Vim's name (`fileformat=dos`,
    `fileencoding=latin1`, `nobomb`, `filetype=python`);
  - a bad value answers `Invalid argument: …` and stops the rest, with what came before it done;
  - on a hex tab: `Not available in the hex view: …`.
- **How:**
  - the engine knows the seven names but does not keep them (`VimSet`);
  - it hands the words on, as typed and in order, in one `VimExRequest(SetFileOptions, …)`, after
    applying its own options in the same `:set`;
  - `ExCommandHandler` applies them through the new `FileOptions`, using the buffer's own
    `ChangeLineEndings` / `ChangeEncoding` / `ChooseLanguage`, and shows its answer on the status line.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Desktop/ViewModels/FileOptions.cs` — parse, apply, show.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimSetFileOptionsTests.cs` — each spelling handed on;
  several together; mixed with the engine's own options; a refusal after one; none handed on without
  one; `noff` unknown.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileOptionsTests.cs` — every option, its
  aliases, the `?` forms, BOM rules, refusals, the stop-at-first-bad-word rule, the hex refusal.

**Modified**

- `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs` — `SetFileOptions`.
- `src/Enigma.TextEditor.Core/Vim/VimSet.cs` — the file option names, collected into `forwarded`.
- `src/Enigma.TextEditor.Core/Vim/VimEngine.cs` — `Set` raises the request.
- `src/Enigma.TextEditor.Desktop/Services/LanguageCatalog.cs` — `Lookup(name)`.
- `src/Enigma.TextEditor.Desktop/ViewModels/ExCommandHandler.cs` — the `SetFileOptions` case; takes
  the `LanguageCatalog`.
- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs` — takes the `LanguageCatalog` (DI
  resolves it; it was already a singleton).
- `src/Enigma.TextEditor.Desktop/ViewModels/HelpCatalog.cs` — four Options lines.
- `README.md` — the `:set` bullet.
- `tests/…Desktop.UnitTests/Infrastructure/EditorHarness.cs` — passes the catalog.
- `tests/…Desktop.UnitTests/Views/SetCommandTests.cs` — typed `:set ff=dos fenc=latin1 ft=python`
  changes the status bar; `ff?` answers; `ff=mac` is refused.
- `tests/…Desktop.UnitTests/ViewModels/HelpTests.cs`, `tests/…Core.UnitTests/Vim/ExCommandParserTests.cs`
  — see *Deviations*.
- `docs/roadmap.md`, `docs/plan/FEATURE-2F9C.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where the encoding names live | A table in `FileOptions` (Desktop) | Vim's spellings are a user-interface concern; Core's `TextEncodings` stays as it is | Names on `TextEncoding` in Core |
| How `ft` reads names | Id, then display name, then extension | Covers Vim's usual names without a mapping table | A Vim filetype → TextMate id table |
| What `ft?` shows | The language id (`plaintext` for Plain Text) | An id round-trips through `ft=`; Vim's empty value would not | The display name; empty for Plain Text |
| How the catalog reaches the handler | Constructor parameter on `WorkspaceViewModel` → `ExCommandHandler` | Constructor injection, as everything else there | A service locator; static access |
| Messages from both halves of one `:set` (`:set ic? ff?`) | Both write the one status line; the application's comes last | Rare, and each half is still correct | Merging the two answers through the request |

## Deviations & follow-ups

- **Two test invariants assumed that every `VimExCommand` comes from `ExCommandParser`'s table:**
  - `ExCommandParserTests.EveryRequest_HasADefinition`;
  - `HelpTests.EveryExSpellingTheHelpShows_IsReadAsTheCommandItDocuments`.

  `SetFileOptions` is raised by the engine's own `:set` instead. Both tests now exempt that one
  command, with a comment, and `VimSetFileOptionsTests` proves its spellings. This is a narrowed
  invariant, not a weakened assertion: every other command is still checked exactly as before.
- Fix cycles: 1 (the parser invariant above).
- Follow-up: `:e ++enc=…` (re-read in another encoding) is out of scope, as planned.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

- `README.md`: the `:set` bullet now names the file options, with example values.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2606 passed**, 0 failed, 0 skipped (2545 before,
  +61).

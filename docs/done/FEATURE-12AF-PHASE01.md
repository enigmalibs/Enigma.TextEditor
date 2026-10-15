# FEATURE-12AF-PHASE01 — Ex command abbreviations

**Item:** FEATURE-12AF — Help dialog & ex abbreviations
**Branch:** `feature/feature-12af-phase01-ex-abbreviations`
**Run:** vibe/2026-10-15-drop-icon-help

## Summary

`:vsp` now splits side by side. Every ex command the parser knew under a short and a long name now
reads its name as vim does: any start of the full name, from the shortest spelling on.

| Command | Accepted now (new in bold) |
|---|---|
| `:sp[lit]` | `sp`, **`spl`, `spli`**, `split` |
| `:vs[plit]` | `vs`, **`vsp`, `vspl`, `vspli`**, `vsplit` |
| `:e[dit] {file}` | `e`, **`ed`, `edi`**, `edit` |
| `:bn[ext]` | `bn`, **`bne`, `bnex`**, `bnext` |
| `:bp[revious]` | `bp`, **`bpr` … `bpreviou`**, `bprevious` |
| `:wa[ll]` | `wa`, **`wal`**, `wall` |
| `:qa[ll]` / `:qa[ll]!` | `qa`, **`qal`**, `qall`, each with or without `!` |
| `:vne[w]` | **`vne`**, `vnew` |
| `:on[ly]` | `on`, **`onl`**, `only` |
| `:clo[se]` | `clo`, **`clos`**, `close` |

The commands vim spells one way only accept exactly what they accepted before: `:w`, `:q`, `:q!`,
`:wq`, `:wq!`, `:x`, `:new`. The spelled-out `:write` and `:quit` are still refused, as are `:w!` and
`:wqa`. The bang, argument and range rules are unchanged.

The parser's switch became a table, `ExCommandParser.Definitions`. Each `VimExCommandDefinition` holds:

- the full name and the shortest spelling's length;
- the request it raises, and the request it raises with a `!`;
- what may follow the name (`VimExArgument`: none, optional, required).

The table also writes each command the way vim's documentation does (`:vs[plit]`, `:qa[ll]!`). The
help dialog of PHASE02 uses that spelling.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Core/Vim/VimExCommandDefinition.cs` — one command's spellings and rules;
  `Matches(name)` and `Spelling(bang)`.
- `src/Enigma.TextEditor.Core/Vim/VimExArgument.cs` — none / optional / required.

**Modified**

- `src/Enigma.TextEditor.Core/Vim/ExCommandParser.cs`:
  - `Definitions` (the table) and `Find(name)`;
  - `FileCommand` resolves through them;
  - the class remarks describe the rule (they also no longer call `:wa` refused, which had been stale
    since FEATURE-3EE8).
- `src/Enigma.TextEditor.Core/Vim/VimExCommand.cs` — each value's docs use vim's bracket spelling.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/ExCommandParserTests.cs`:
  - abbreviations read (17 cases);
  - near misses refused (12);
  - no spelling names two commands;
  - every request has a definition;
  - spellings (7).
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/VimWindowCommandTests.cs` — `:vsp` and `:spl b.txt`
  through the engine.
- `docs/roadmap.md`, `docs/plan/FEATURE-12AF.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where `Spelling` lives | A method on `VimExCommandDefinition` | It reads only the definition's own fields | `ExCommandParser.Spelling(definition, bang)` as the plan wrote it (same result, less cohesive) |
| `Find` visibility | Public | PHASE02's help and any later caller look a name up the same way | Internal (would need re-exposing next phase) |
| Ambiguity guard | A test walks every spelling of every definition and asserts exactly one match | Vim's rule makes a table ambiguous silently, e.g. a future `:wn` against `:wq`; the test makes that loud | Ordering the table and relying on the first match |

## Deviations & follow-ups

- The plan's `ExCommandParser.Spelling(definition, bang)` is `definition.Spelling(bang)`; see above.
- **Fix cycles used: 0 of 3.**
- Follow-up (optional): the engine's own ex names (`s`/`substitute`, `se`/`set`) are still matched
  literally in `VimEngine.RunEx`, so `:su` and `:sub` are not read. They are outside this table
  because they do not leave the engine.
- Line endings: the new and touched files are LF; no CRLF churn.

## Documentation sweep

Nothing became wrong. `src/Enigma.TextEditor.Core/Vim/README.md` describes the engine's provenance,
not its commands. `README.md` is a title only, `RELEASENOTES.md` is empty.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2411 passed**, 0 failed, 0 skipped (2371 before,
  plus 40 new cases). Every existing ex parser, command-line and window-command test passed unchanged,
  including the refusals `:write`, `:quit`, `:w!`, `:x!`, `:wqa`, `:e`, `:e!`, `:q notes.md`,
  `:bn 2`, `:W`.

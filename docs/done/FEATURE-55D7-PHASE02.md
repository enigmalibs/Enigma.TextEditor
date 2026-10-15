# FEATURE-55D7-PHASE02 — Vim engine port

**Item:** FEATURE-55D7 — Text editor foundation
**Branch:** `feature/feature-55d7-phase02-vim-engine-port`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

Enigma.MarkdownEditor's Vim engine (commit `ee8f6e9`) is now part of `Enigma.TextEditor.Core.Vim`,
together with its whole test suite:

- 49 source files, about 9.5k lines, ported verbatim apart from the namespace;
- 35 test files.

The engine's one markdown dependency is gone. `o`, `O` and their count and `.` replays no longer
carry a list marker (`ListContinuation`). They carry the current line's leading spaces and tabs
instead, which is what AvaloniaEdit's Enter does in insert mode.

The folder's `README.md` records where the code came from and how it differs from the source.

## Files / modules touched

**Created**

- `src/Enigma.TextEditor.Core/Vim/*.cs` (49 files, ported) and `src/Enigma.TextEditor.Core/Vim/README.md`.
- `tests/Enigma.TextEditor.Core.UnitTests/Vim/*.cs` (35 files, ported).

**Adapted after the port**

- `Vim/VimEngine.cs`:
  - the `Markdown` using is removed;
  - `OpenLine` and `RepeatTyped` use the new private `LeadingWhitespace(text)` helper;
  - the comments that described the markdown marker now describe indentation.
- `Vim/VimAction.cs`: the `OpenLineBelow` / `OpenLineAbove` remarks describe indentation.
- `Vim/VimOpenLineTests.cs`: rewritten for the indentation rule.
  - spaces, tabs and mixed indentation, for `o` and `O`;
  - `- ` / `1. ` / `#` lines open plain;
  - a whitespace-only line is carried whole;
  - count and `.` replays;
  - one undo step, one buffer write, CRLF kept.
- `Vim/VimInsertTests.cs`: one comment no longer points at the markdown case.

## Deviations & follow-ups

- **No `InternalsVisibleTo` needed.** The ported tests only use the public surface, as they did in
  MarkdownEditor.
- **Markdown wording left in doc comments.** Some comments still use markdown files as their example
  (`VimParagraphs`, `VimWords`, `VimSearch`, `VimBrackets`, `VimRegisters`). They describe behaviour
  that is unchanged, so the port stays verbatim. Rewording them is cosmetic.
- **Follow-up: one shared engine.** Two copies of the engine now exist. Extracting it into a shared
  `Enigma.Vim` package, consumed by both editors, would end the duplication. That is a separate work
  item in another repository.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What counts as indentation | Leading spaces and tabs only | Matches AvaloniaEdit's `DefaultIndentationStrategy` | Any Unicode whitespace (no editor does that) |
| How to handle markdown-flavoured comments | Left verbatim | A verbatim port makes the next sync with the source a clean diff | Rewriting them now (noise in a port diff) |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **1974 passed**, 0 failed, 0 skipped. That is the
  1928 ported or rewritten Vim test cases (theory rows expanded) plus PHASE01's 46.

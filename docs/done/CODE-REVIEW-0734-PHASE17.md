# CODE-REVIEW-0734-PHASE17 — [Medium] SOLID-03 The key tables leave `KeySequenceParser`

## Summary

`KeySequenceParser` (1,027 lines) held both the grammar (which key is waiting for which, the counts, the register)
and the tables of what each key spells. The tables now live in their own class:

- New internal static `VimKeyBindings` holds:
  - the leader's mappings;
  - the motions;
  - the operators, and the case operators and their doubling keys;
  - the find openers, the text-object openers and objects;
  - the control-key scrolls, the page keys and the `z` scrolls;
  - the visual pair;
  - the normal-mode and visual-mode actions.
  `ActionFor` and `VisualActionFor` were instance methods only because they read the parser's count; they are now
  static and take the count as a parameter.
- The parser keeps the state machine and looks each key up in `VimKeyBindings` once it knows what kind of key it is
  waiting for. `IsControlCommand` stays public on the parser and asks the table.
- `KeySequenceParser.cs` goes from 1,027 to 718 lines, and `VimKeyBindings.cs` is 350 lines. The behaviour and the
  public API are unchanged.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Vim/VimKeyBindings.cs`
- Modified: `src/Enigma.TextEditor.Core/Vim/KeySequenceParser.cs`

## Deviations & follow-ups

- No deviation from the plan.
- Fix cycles used: 0.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The two action tables | Moved too, with the count as a parameter | They are tables like the others, and reading the count was their only tie to the parser |
| Leader mappings | `IReadOnlyDictionary` on the table class | The parser only reads them |

## Build/test evidence

- Baseline: see `docs/done/CODE-REVIEW-0734-PHASE01.md`.
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3413 tests, 3411 passed, 2 skipped (PHASE06's), 0 failed.
- Guard: the Core Vim tests (2,231, including the parser's own suites) pass unchanged.
- Documentation sweep (§7): nothing user-facing changed.

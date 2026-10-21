# BUG-4A17 — Colours missing after a scroll

**Status:** TODO
**Type:** BUG
**Branch:** `bugfix/bug-4a17-scroll-colours`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

After scrolling, every line on screen gets its language colours. Today some lines sometimes stay in
the plain foreground until they are edited or scrolled out and back.

## Context & constraints

- Colouring is AvaloniaEdit.TextMate's (`EditorAppearance` installs it per pane). Read from the 12.0.0
  assembly (decompiled):
  - `TextMateColoringTransformer.TransformLine` colours a line from `TMModel.GetLineTokens`. A line
    whose tokens do not exist yet is drawn plain.
  - When tokens arrive, `ModelTokensChanged` (called from the background tokenizer thread, or from
    `ForceTokenization` on the UI thread) redraws the changed lines. It first **returns early when the
    changed lines lie outside the cached visible range** (`_firstVisibleLineIndex` …
    `_lastVisibleLineIndex`). That range is only updated on `TextView.VisualLinesChanged`.
  - `TextEditorModel.TextView_ScrollOffsetChanged` posts `ForceTokenization` of
    `TextView.VisualLines`, which can still be the lines from before the scroll.
- So the race is: the view scrolls, the new visual lines are built before their tokens exist (drawn
  plain), and the tokens arrive while the cached range still describes the old viewport. The redraw is
  dropped, and AvaloniaEdit keeps reusing the plain visual lines while they stay on screen.
- `Installation.EditorModel` (the `TextEditorModel`, an `AbstractLineList`) is public and has
  `ForceTokenization(start, end)`. The `TMModel` itself is private.

## Decisions

| Decision | Rationale |
|---|---|
| Fix in the app, in `EditorAppearance`: after the visual lines change, once the dispatcher is idle (Background priority), tokenize the visible range through `Installation.EditorModel.ForceTokenization` and redraw it — only when the visible range differs from the last one refreshed | Deterministic: the redraw no longer depends on the library's racy check. The range guard stops the redraw's own `VisualLinesChanged` from looping |
| No change to the library | It is a package; the fix belongs upstream too (follow-up) |
| Coalesced: one pending refresh at a time | A wheel scroll raises many changes; one refresh after them is enough |

## Steps

1. Reproduce headlessly: a long C# buffer, scroll far (offset set directly), drain the dispatcher,
   and check the colours of the visible lines (the foreground of their text runs). Record whether
   the race shows up deterministically.
2. `Views/EditorAppearance.cs`:
   - subscribe to `TextView.VisualLinesChanged`;
   - schedule a single Background-priority refresh;
   - the refresh: visible first/last line → `EditorModel.ForceTokenization(first, last)` →
     `TextView.Redraw(first line offset, length)`, skipped when the range equals the last refreshed
     one or there is no grammar, no document, or the appearance is disposed;
   - unsubscribe on `Dispose`;
   - remarks explaining the library race.
3. Tests (headless):
   - after a far scroll of a C# document, every visible line containing a keyword is coloured;
   - plain text (no grammar) never refreshes;
   - a disposed appearance does not refresh;
   - a redraw does not schedule another refresh for the same range (no loop).

## Acceptance criteria

- Scrolling a highlighted document never leaves visible lines uncoloured once the UI is idle.
- No redraw loop; Plain Text untouched.
- Zero warnings, whole suite green.

## Out of scope

- Reporting or fixing the race in AvaloniaEdit.TextMate itself.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where to fix | App-side refresh in `EditorAppearance` | The library's check is private; a package fork is heavy | Forking AvaloniaEdit.TextMate; replacing the transformer |
| What to refresh | The visible range, once per change of range | Cheap (one viewport) and loop-free | A redraw on every scroll event; a timer |

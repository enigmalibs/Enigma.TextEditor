# FEATURE-5731 — Vim search highlight

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5731-search-highlight`
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

The improvement asked over Enigma.MarkdownEditor's Vim: searching with `/` (and `?`) **highlights
every match**:

- while the pattern is being typed;
- after Enter, until `:nohlsearch` clears it.

This is Vim's `incsearch` + `hlsearch`.

## Context & constraints

- The ported engine compiles patterns in `VimPattern.TryCompile`, using the .NET dialect,
  `ignorecase`/`smartcase`, `\c`/`\C` and a 1 s timeout.
- It remembers the last search (shared across panes since FEATURE-04BC, in `VimSharedState`) and
  updates it from `/`, `?`, `*`, `#` and `:s`.
- The command line is the engine's own `StringBuilder`, re-sent through `IVimBuffer.ShowCommandLine`
  on every keystroke.
- `:set` knows `ignorecase`, `smartcase`, `wrapscan`, `expandtab` and `shiftwidth`.
- Rendering happens in AvaloniaEdit: an `IBackgroundRenderer` on the `TextView` draws rectangles
  behind text.

## Decisions

| Decision | Rationale |
|---|---|
| `VimOptions.HlSearch` and `IncSearch`, both default **on**; `:set hlsearch/hls`, `nohlsearch/nohls`, `incsearch/is`, `noincsearch/nois` | The draft asks for the behaviour by default; the options keep Vim's escape hatches |
| Shared highlight state in `VimSharedState`: `HighlightPattern` (Regex?), `HighlightSuppressed` (the `:noh` flag), `HighlightChanged` event | hlsearch is global in Vim: every pane shows the matches |
| Highlight pattern = the last search pattern. Set by `/`, `?`, `*`, `#`, `n`, `N` and `:s`; each of these lifts `:noh` | Vim's rules |
| `:noh[lsearch]` hides the highlight without forgetting the pattern | Vim's rule; `n` still works and re-shows it |
| incsearch: while `/` or `?` is open, every keystroke recompiles the typed pattern into an engine-local preview (all matches plus the *current* match: the next one from the caret in the search direction). The view scrolls to the current match; the caret does not move | Vim's incsearch; the caret only moves on Enter |
| A partial pattern that does not compile shows no preview and no error until Enter | Typing `foo(` must not flash errors |
| Esc restores the view and the previous highlight | Vim's rule |
| Desktop `SearchHighlightRenderer` (`IBackgroundRenderer`, layer `Selection`) matches only the visible document range, plus the length of one line on each side | Linear in what is on screen; works with the 32 MB cap |
| Colours: matches use the Enigma accent at about 35 % opacity; the incsearch current match is solid accent with a border; light and dark variants | Readable on both themes, distinct from the selection |
| Hidden when Vim is off | The highlight is a Vim feature |

## Steps

1. Core:
   - `VimOptions.HlSearch` / `IncSearch` and their `:set` entries;
   - `VimSharedState` highlight members;
   - the engine sets the highlight on every search-pattern write and on `n`/`N`;
   - `:noh`/`:nohlsearch` in `RunEx`;
   - incsearch: `IncSearchPreview` (`Regex?`, current match offset/length) as an engine property with
     a `IncSearchChanged` event, updated in `HandleCommandLineKey` for `/` and `?` lines and cleared
     on Enter and Esc;
   - `IVimBuffer.FirstVisibleLine`/`ScrollToLine` are used to show the current match, and the view is
     restored on Esc.
2. Desktop:
   - `SearchHighlightRenderer` registered on each pane's `TextView` by `VimEditorBehavior`;
   - redraw on `HighlightChanged`, `IncSearchChanged` and text changes (`InvalidateLayer`);
   - hidden while Vim is off or `hlsearch` is off (the incsearch preview still honours `incsearch`).
3. Tests, Core:
   - `/foo` while typing exposes a preview that matches `foo` after each key;
   - Enter sets the highlight to `foo`;
   - `:noh` suppresses it and `n` lifts the suppression;
   - Esc restores the previous highlight;
   - `*` highlights the word;
   - `:set nohls` / `:set nois`;
   - an invalid partial pattern gives no preview and no message;
   - the highlight is shared by two engines.
4. Tests, Desktop (headless): after `/foo<Enter>` the renderer reports the visible match rectangles
   for each occurrence on screen.

## Acceptance criteria

- Typing `/foo` highlights every visible `foo` as each character is typed. The next match is shown
  distinctly and scrolled into view.
- After Enter all matches stay highlighted, in every pane; `n`/`N` keep them.
- `:noh` clears the highlight; the next `/`, `?`, `*`, `#`, `n` or `N` brings it back.
- `:set nohlsearch` and `:set noincsearch` turn each behaviour off.
- Zero warnings, whole suite green.

## Out of scope

- Vim-dialect pattern translation (`\<`, `\v` …): the engine stays .NET-dialect, as ported.
- A match count (`[3/10]`) in the status bar.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Defaults | `hlsearch` and `incsearch` on | The draft asks for exactly this behaviour | Off by default (Vim's own default), contradicting the request |
| Clearing command | `:noh[lsearch]`, re-enabled by the next search or `n`/`N` | Vim's exact semantics, which the draft names ("nohighlight") | Clearing the pattern too (breaks `n`) |
| Where matches are computed | In the renderer, on the visible range only | Cost bounded by the screen, not the file | Precomputing every match in the engine (unbounded on large files) |
| Scope across panes | Global | Vim's hlsearch is global; the pattern is already shared | Per pane |

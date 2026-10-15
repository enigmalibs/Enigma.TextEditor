# FEATURE-5731 — Vim search highlight

**Item:** FEATURE-5731 — Vim search highlight
**Branch:** `feature/feature-5731-search-highlight`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

This is the improvement asked for over Enigma.MarkdownEditor's Vim: `/` and `?` highlight their
matches.

**While typing (`incsearch`)**

- Every match of the pattern typed so far is highlighted on screen.
- The match Enter would land on is drawn solid, with a border.
- If that match is off screen, the view scrolls to it; the caret does not move.
- A pattern that does not compile yet (`foo(`) shows nothing and reports nothing.
- Esc, or backspacing the line away, ends the preview and puts the view back where it was.

**After Enter (`hlsearch`)**

- Every match of the last search pattern stays highlighted, in every pane.
- The same applies to `*`, `#`, `n`, `N` and `:s`.

**Clearing and options**

- `:noh` / `:nohl` / … / `:nohlsearch` hides the highlight but keeps the pattern. The next `/`, `?`,
  `*`, `#`, `n` or `N` brings it back.
- `:set [no]hlsearch` (`hls`) and `:set [no]incsearch` (`is`) switch each behaviour. Both are on by
  default. They are global, as in Vim, so they live on `VimSharedState` and `:set nohls` in one pane
  clears every pane.
- With Vim off, nothing is highlighted.

**Rendering**

- `SearchHighlightRenderer` is an AvaloniaEdit `IBackgroundRenderer` on the selection layer, so it sits
  above the current-line fill and below the text.
- It runs the pattern over the visible document range only, on each draw.
- Colours are the Enigma accent: 35 % fill for every match, 75 % fill plus a solid border for the
  current one. They follow the theme variant.
- Each pane's `VimEditorBehavior` owns one renderer. It repaints on the shared `HighlightChanged` and
  on its engine's `IncSearchChanged`, and connects and disconnects with the pane's load and unload.

## Files / modules touched

**Created**

- Core: `Vim/VimIncSearch.cs`.
- Desktop: `Views/SearchHighlightRenderer.cs`.
- Tests:
  - `tests/.../Core.UnitTests/Vim/VimSearchHighlightTests.cs` (16);
  - `tests/.../Desktop.UnitTests/Views/SearchHighlightTests.cs` (5, rendered frames).

**Modified**

- Core:
  - `Vim/VimSharedState.cs`: `HlSearch`, `IncSearch`, `HighlightPattern`, `IsHighlightSuppressed`,
    `VisibleHighlight`, `HighlightChanged`, `Highlight()`, `SuppressHighlight()`.
  - `Vim/VimEngine.cs`: highlight on every search and `:s`; `:noh[lsearch]`; incsearch preview
    (`IncSearch`, `IncSearchChanged`); view restore on Esc.
  - `Vim/VimSearch.cs`: `FindMatch` returns the match length.
  - `Vim/VimSet.cs`: switches over options plus shared state; `hlsearch` and `incsearch`.
- Desktop:
  - `Views/VimEditorBehavior.cs`: renderer, `ConnectHighlight` / `DisconnectHighlight`, repaint on
    attach and detach.
  - `Views/EditorPaneView.axaml.cs`: connects on load, disconnects on unload.
- Tests: `Vim/VimSetTests.cs` (below).

## Deviations & follow-ups

- **Two ported `:set` tests changed with the behaviour.** The full `:set` listing now includes
  `hlsearch incsearch`. The "unknown option" example moved from `hlsearch`, which is known now, to
  `number`.
- **Global options live on `VimSharedState`.** The plan put `HlSearch`/`IncSearch` on `VimOptions`.
  `VimOptions` is per engine, and its `shiftwidth`/`expandtab` are re-seeded per pane, so sharing one
  instance was not possible. `VimSet`'s switches now read and write through a small target pairing
  the two.
- **The highlight pattern is compiled with the searching engine's case options.** `:set ic` in one
  pane changes how that pane's next search, and therefore the shared highlight, is compiled.
- **The incsearch preview is for the pane being typed in only.** The other panes keep showing the
  last search's highlight until Enter, as in Vim.
- **No match count** (`[3/10]`), as planned.
- **Fix cycles used: 2 of 3.**
  1. The ported `:set` tests above (intended behaviour change).
  2. A miscounted offset in a new test's expected data.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where `hlsearch`/`incsearch` live | `VimSharedState` (global) | Vim's options are global; `:set nohls` clears every pane | Per-engine `VimOptions` (the plan's wording; inconsistent across panes) |
| What incsearch highlights | All matches of the typed pattern plus the current one, in the typing pane | Vim's `incsearch` + `hlsearch` behaviour while typing | Current match only |
| When the view moves during incsearch | Only when the current match is off screen (centred), restored on cancel | Avoids jumping when the match is already visible | Always centring |
| Renderer layer | `KnownLayer.Selection` | Above the current-line fill, under the text and the caret | `Background` (hidden by the current-line fill) |
| A test seam for the renderer | `LastMatches` exposed | Asserting on pixels would test Skia, not the feature | Pixel comparison |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2290 passed**, 0 failed, 0 skipped.
- Core:
  - the preview after each key, forwards and backwards;
  - Enter sets the highlight and Esc keeps the previous one;
  - `:noh` variants hide it, `n` lifts the suppression, a range on `:noh` is refused;
  - `*` and `:s` highlight;
  - `:set nohls` / `:set nois`;
  - an uncompilable partial pattern is silent;
  - backspacing to nothing ends the preview;
  - the highlight is shared by two engines;
  - the off-screen match scrolls into view and Esc restores it.
- Headless (real frames):
  - matches drawn while typing, without the caret moving;
  - after Enter, then gone after `:noh`, back after `n`;
  - Esc restores the previous highlight;
  - both panes highlight;
  - nothing is drawn with Vim off.

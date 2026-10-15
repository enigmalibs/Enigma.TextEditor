# FEATURE-08A7 — Application icon

**Item:** FEATURE-08A7 — Application icon
**Branch:** `feature/feature-08a7-app-icon`
**Run:** vibe/2026-10-15-drop-icon-help

## Summary

The text editor now has an application icon, built the same way as Enigma.GitClient's and
Enigma.FileExplorer's.

**The drawing.** A page of text with a green block cursor at the end of its last line: what the editor
shows in Vim's normal mode. It sits on the family tile.

- `#1E1F22` tile, 240 units, corner 48, as Git Client and File Explorer.
- The page is `#3574F0`, the family blue. Its folded corner is `#2558C4`, File Explorer's darker blue.
- Four text lines in `#2D56A8`, File Explorer's row colour.
- The block cursor is `#3FB950`, the family green.

**Small frames hinted.** In the master, 12-unit lines twelve units apart blur into one shade at 16 px.
The small master changes four things:

- two lines instead of four;
- a bigger page and a wider cursor;
- every edge on a 16-unit grid, so each shape lands on whole pixels at both 16 and 32 px;
- darker lines (`#274174`).

At 16 px, the cursor is a crisp 2 × 3 pixel green block beside a 1-pixel line.

**Wiring.**

- `<ApplicationIcon>` embeds the icon in the Windows `.exe`. Verified: the built apphost contains the
  256 frame byte for byte.
- `Icon="/Assets/app.ico"` puts it on the running window on every platform.
- The About dialog shows `app-256.png` at 96 px above the product name. This closes the follow-up
  FEATURE-6BB4 left ("No app icon in the About dialog").

## Files / modules touched

**Created**

- `packaging/icon/enigma-text-editor.svg` — the master for 48, 64, 128 and 256, with the regeneration
  recipe in its header comment.
- `packaging/icon/enigma-text-editor-small.svg` — the hinted master for 16 and 32.
- `src/Enigma.TextEditor.Desktop/Assets/app.ico` — six 32-bit RGBA PNG frames, 16 → 256, smallest
  first, 9 053 bytes.
- `src/Enigma.TextEditor.Desktop/Assets/app-256.png` — the 256 frame, byte for byte.
- `tests/Enigma.TextEditor.Desktop.UnitTests/AppIconTests.cs` — 4 tests.

**Modified**

- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `ApplicationIcon`, and
  `<AvaloniaResource Include="Assets/**" />`.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — `Icon="/Assets/app.ico"`.
- `src/Enigma.TextEditor.Desktop/Views/AboutView.axaml` — the 96 px icon above the name.
- `docs/roadmap.md`, `docs/plan/FEATURE-08A7.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Line colour in the master | `#2D56A8`, solid | File Explorer's row colour: the tile at 35 % over the blue, written solid so every rasterizer agrees | An `opacity` attribute (blends differently across rasterizers) |
| Small-frame hinting | Two lines, every edge on a 16-unit grid, darker lines | The first small render had a cursor and lines straddling pixel edges at 16 px and blurred; the grid-aligned one is crisp at 16 and 32, compared upscaled side by side | Keeping the master's geometry scaled up |
| Rasterizer | Svg.Skia 3.7.0, from the local NuGet cache, run from a throwaway file-based C# script in the session scratchpad | `rsvg-convert`, which the recipe names, is not installed here. File Explorer did the same | Committing a renderer |
| ICO packing | The rendered PNGs unchanged, smallest first, 32-bit PNG entries | Git Client's and File Explorer's exact layout | Re-encoding the frames as BMP entries |
| Window-icon test | A new `MainWindow` has a non-null `Icon` | The headless icon loader is a stub; the asset tests pin the content | Comparing pixels |
| About icon | `Image` named `AppIcon`, 96 × 96, above the name, with an automation name | The siblings' About layout | No icon (FEATURE-6BB4's interim state) |

## Deviations & follow-ups

- **Recipe wording.** The SVG header names `rsvg-convert` as an example and says any SVG rasterizer
  will do. Another rasterizer's anti-aliasing can differ by a few edge pixels from the committed
  frames; the tests pin what matters (sizes, encoding, the 256 copy).
- **No Linux/macOS icon copies.** There is no packaging for those platforms in this repository.
- **Fix cycles used: 0 of 3.**
- **Not done:** no visual check of the running Debug build's taskbar. The frames were inspected
  rendered and upscaled, and the exe was checked to embed the icon.
- Line endings: the SVGs are LF, as `.gitattributes` asks; `*.ico` and `*.png` are marked binary.

## Documentation sweep

Nothing became wrong. `README.md` is a title only, `RELEASENOTES.md` is empty, and there is no
`CLAUDE.md` or `AGENTS.md`.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors** (no AVLN warnings).
- `dotnet test --solution Enigma.TextEditor.slnx`: **2371 passed**, 0 failed, 0 skipped (2367 before,
  plus 4 new). The new tests cover:
  - the icon has six square 32-bit PNG frames, 16 to 256, each PNG header agreeing with its directory
    entry;
  - `app-256.png` is the largest frame byte for byte and decodes as 256 × 256;
  - the main window carries an icon;
  - the About view shows the 256 image at 96 × 96 above the product name.

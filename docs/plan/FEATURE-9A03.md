# FEATURE-9A03 — New application icon

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-9a03-new-app-icon`
**Run:** feature/2026-10-28-new-icons-release

## Objective

Replace the application icon everywhere it ships with the new set the user dropped in
`new-icons-remove-when-done/`, then delete that folder. The new drawing is an editor window rather than a page: a
gutter of line marks, four text lines with a green block cursor, and a status bar with a green mode pill. It uses a
softer palette on the same `#1E1F22` family tile.

## Context & constraints

- **The new set** (untracked, at the repository root):
  - `enigma-text-editor.ico`: ten 32-bit RGBA PNG frames, smallest first: 16, 20, 24, 32, 40, 48, 64, 96, 128, 256.
  - `enigma-text-editor-<N>.png`: each one byte for byte the matching `.ico` frame.
  - `enigma-text-editor.svg` (the master for 40 px and up) and `enigma-text-editor-small.svg` (16 to 32 px). Their
    header comments point at the user's branding tooling for regeneration.
- **Where the icon ships today:**
  - `src/Enigma.TextEditor.Desktop/Assets/app.ico`: six frames (16, 32, 48, 64, 128, 256). The csproj's
    `ApplicationIcon` and `MainWindow`'s `Icon` use it.
  - `src/Enigma.TextEditor.Desktop/Assets/app-256.png`: the About dialog's image, the `.ico`'s largest frame byte
    for byte.
  - `packaging/icon/enigma-text-editor{,-small}.svg`: the sources.
  - `packaging/linux/icons/enigma-text-editor-{16,32,48,64,128,256}.png`: the frames `install.sh` copies into
    `hicolor`, and `uninstall.sh` removes. The two scripts each hold an `ICON_SIZES` array.
- **`AppIconTests`** checks four things:
  - the `.ico`'s frame sizes (exactly six today);
  - the 32-bit PNG encoding;
  - the 256 copy;
  - the window and About wiring.
- **The `hicolor` theme** (`/usr/share/icons/hicolor/index.theme`) has app folders for 16, 22, 24, 32, 36, 48, 64,
  72, 96, 128, 192, 256 and 512, and none for 20 or 40.

## Steps

1. Copy `enigma-text-editor.ico` over `Assets/app.ico`, and `enigma-text-editor-256.png` over `Assets/app-256.png`.
2. Copy both SVGs over `packaging/icon/`.
3. `packaging/linux/icons/`:
   - replace the six PNGs;
   - add the 24 and 96 frames;
   - set `ICON_SIZES=(16 24 32 48 64 96 128 256)` in `install.sh` and `uninstall.sh`;
   - fix their "six" wording.
4. `AppIconTests`: expect the ten sizes; the doc comment says ten frames.
5. Delete `new-icons-remove-when-done/` (untracked, so nothing to stage).
6. Documentation sweep: the README's *Install on Linux* table says "six sizes".

## Acceptance criteria

- `Assets/app.ico`, `Assets/app-256.png`, both SVGs and every shipped Linux PNG are byte-identical to the new set.
- `AppIconTests` passes against the ten-frame icon. The whole suite is green, and the build has zero warnings.
- `install.sh` and `uninstall.sh` agree on the eight sizes, and every size has a PNG in `packaging/linux/icons/`.
  `bash -n` passes on both.
- `new-icons-remove-when-done/` no longer exists. No `20` or `40` PNG under `packaging/linux/icons/`.

## Out of scope

- Redrawing or re-rendering any frame; the set is used as delivered.
- A `scalable/apps` SVG in the Linux install; Windows/macOS packaging; an MSI.
- The release itself (FEATURE-3C6D).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item type | FEATURE | A new look, not a defect | BUG |
| ICO frames | All ten, as delivered | The user's artwork. Windows picks 20, 24, 40 and 96 at 125–250 % scaling | Re-packing to the old six |
| Linux sizes | The eight `hicolor` sizes: 16, 24, 32, 48, 64, 96, 128, 256 | 20 and 40 have no `hicolor` folder, so no desktop would look them up | All ten; the old six only |
| SVG header comments | Kept as delivered | They name the user's own regeneration tool | Rewriting them to the old `rsvg-convert` recipe |
| Uninstalling an older install | The new array is a superset of the old six | An older install's icons are all still removed | A separate legacy list |
| Untracked temp folder at pre-flight | Proceed | It is the run's declared input, not someone else's uncommitted work | Stopping on a dirty tree |
| New tests | None beyond updating `AppIconTests` | It already pins what a replacement must keep | Pinning the Linux PNGs in a test |

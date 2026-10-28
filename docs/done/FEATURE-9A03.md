# FEATURE-9A03 — New application icon

**Item:** FEATURE-9A03 — New application icon
**Branch:** `feature/feature-9a03-new-app-icon`
**Run:** feature/2026-10-28-new-icons-release

## Summary

The application icon is replaced everywhere it ships by the user's new set, used exactly as delivered.

**The drawing.** An editor window instead of a page, on the same `#1E1F22` family tile, in a softer palette:

- a `#6A9BF4` window;
- a darker gutter of line marks down its left side;
- four text lines;
- a green block cursor in the second line;
- a status bar along the bottom carrying a green mode pill.

The small master (16 to 32 px) drops the gutter marks and uses heavier, grid-aligned shapes.

**Where it ships:**

- `Assets/app.ico`: ten 32-bit PNG frames, smallest first (16, 20, 24, 32, 40, 48, 64, 96, 128, 256). It was six frames
  and is now 15 106 bytes. `ApplicationIcon`, the window's `Icon` and the About dialog's wiring are unchanged.
- `Assets/app-256.png`: the new 256 frame, byte for byte.
- `packaging/icon/*.svg`: the new masters, with their own header comments.
- `packaging/linux/icons/`:
  - the six PNGs are replaced;
  - 24 and 96 are added, both standard `hicolor` sizes;
  - `install.sh` and `uninstall.sh` now list eight sizes.

The temporary `new-icons-remove-when-done/` folder is deleted. It was never tracked.

## Files / modules touched

- Created:
  - `packaging/linux/icons/enigma-text-editor-24.png`
  - `packaging/linux/icons/enigma-text-editor-96.png`
  - `docs/done/FEATURE-9A03.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/Assets/app.ico`
  - `src/Enigma.TextEditor.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-text-editor.svg`
  - `packaging/icon/enigma-text-editor-small.svg`
  - `packaging/linux/icons/enigma-text-editor-{16,32,48,64,128,256}.png`
  - `packaging/linux/install.sh`
  - `packaging/linux/uninstall.sh`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/AppIconTests.cs`
  - `README.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-9A03.md`
- Deleted (untracked, never in git): `new-icons-remove-when-done/`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `AppIconTests` remark "packed by hand" | "packed outside the build" | The new set comes from the user's export tool, not a manual pack |
| `AppIconTests` test name | `TheIcon_HoldsTenSquare32BitPngFramesFrom16To256` | The name states the frame count it pins |
| The 20 and 40 PNGs | Not kept in the repository | Only the `.ico` uses them, and it carries them as frames |

## Deviations & follow-ups

- **The SVG headers name `Branding/Apps/export.py`**, which lives outside this repository. They no longer carry the old
  `rsvg-convert` recipe.
- **Not done:** no visual check of a running build's taskbar or launcher. The frames were inspected at 256, and the
  Windows apphost embedding was not re-checked on this Linux machine. `ApplicationIcon` is unchanged.
- An older Linux install had six icon sizes, all among the new eight. Re-running `install.sh` overwrites them and
  adds 24 and 96, and `uninstall.sh` removes all eight.
- Fix cycles used: 0 of 3.

## Documentation sweep

- `README.md` *Install on Linux*: "The icon, in six sizes" → "in eight sizes" (cell re-padded).
- `install.sh` header ("six sizes") and `uninstall.sh` usage text ("the six icons") now say eight.
- `docs/RELEASE.md`: nothing became wrong.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2742 total, 2742 passed, 0 failed, 0 skipped.
  `AppIconTests` now pins the ten frames.
- `bash -n` on `install.sh` and `uninstall.sh`: no syntax errors.
- Byte comparison: each of the eight Linux PNGs and `app-256.png` equals the `.ico` frame of its size. The `.ico`, the
  PNGs and the SVGs were copied unchanged from the delivered set.

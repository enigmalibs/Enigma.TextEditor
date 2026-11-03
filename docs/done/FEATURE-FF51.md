# FEATURE-FF51 — Icon set filling the frame (final3)

**Item:** FEATURE-FF51 — Icon set filling the frame (final3)
**Branch:** `feature/feature-ff51-final3-icon-set`
**Run:** feature/2026-11-03-final3-icons-release

## Summary

Everywhere the application icon ships, it is replaced by the user's new set from `Branding/Apps/final3/text-editor`,
used exactly as delivered.

- **The drawing:** FEATURE-3266's editor window, still without the tile and on a transparent background, now
  **filling its frame**:
  - it is scaled by 1.2727, so its longest side spans 16 to 240 of 256;
  - the small master is redrawn on the same live area.
- `Assets/app.ico`: the same ten 32-bit PNG frames (16 to 256), now 12 835 bytes (was 7 662). It is used three ways,
  none of which changed:
  - the `ApplicationIcon` (the executable and the Windows taskbar);
  - `MainWindow`'s `Icon`;
  - the copy beside the executable, which the MSI uses for its shortcuts and *Installed apps*.
- `Assets/app-256.png`: the new 256 frame, byte for byte, which the About dialog shows (4 229 bytes).
- `packaging/icon/*.svg`: the new masters, with the header comments `export.py final3` wrote.
- `packaging/linux/icons/`: the eight sizes `install.sh` installs, replaced.

## Files/modules touched

- Created: `docs/done/FEATURE-FF51.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/Assets/app.ico`
  - `src/Enigma.TextEditor.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-text-editor.svg`, `packaging/icon/enigma-text-editor-small.svg`
  - `packaging/linux/icons/enigma-text-editor-{16,24,32,48,64,96,128,256}.png`
  - `docs/roadmap.md`, `docs/plan/FEATURE-FF51.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The two `FileDropViewTests` failures | No fix and no re-run | The run's baseline already lists both failures, and an icon swap cannot reach a file-drop view test |

## Deviations & follow-ups

- None from the plan. The delivered SVGs are LF this time, so every file is identical to the source, the SVGs
  included.
- **Manual check, for the user:**
  - in both the light and the dark theme, the larger icon in the About dialog and on the Windows taskbar;
  - after reinstalling, the Linux launcher.

  Explorer and some Linux desktops may show the old icon until their icon cache refreshes.
- **Pre-existing test failures** (the run's baseline):
  - `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` ("Pipe is broken");
  - `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`, a known flake
    (`docs/done/FEATURE-3266.md`);
  - `FileDropViewTests.DroppingFiles_NearAPanesRightEdge_SplitsIt_WithTheFilesInTheNewHalf_AndLeavesNoMark`, which
    failed in the baseline only.
- The source directory `Branding/Apps/final3/text-editor` is outside this repository and was left alone.
- Fix cycles used: 0 of 3.

## Documentation sweep

- Nothing stale.
  - The README, `docs/RELEASE.md`, `SECURITY.md`, the About view's comment and `AppIconTests`' comments never
    describe how much of its frame the icon fills.
  - The README's 1.14 callout ("without its dark tile") is still true. The release notes and callout come with
    FEATURE-1B68.

## Build/test evidence

- **Run baseline**, on run branch `feature/2026-11-03-final3-icons-release` at `1264181`, before this dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3112 total, 3109 passed, 3 failed. The failures were
    `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` and the two
    `FileDropViewTests` above.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3112 total, 3110 passed, 2 failed (`InstanceServerTests…`,
    `FileDropViewTests.ThePaneWithNoTab_…`), both in the baseline;
  - `AppIconTests` (`--filter-class`): 5 of 5 passed.
- `cmp`: `app.ico`, `app-256.png`, the two SVGs and the eight Linux PNGs are identical to the delivered files.
- A script read of `app.ico` found ten 32-bit RGBA PNG frames, 16 to 256. Each of the eight Linux PNGs equals the
  frame of its size, byte for byte.
- No tests were added, because `AppIconTests` already pins what the replacement had to keep.

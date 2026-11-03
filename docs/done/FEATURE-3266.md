# FEATURE-3266 — New icon set without the tile

**Item:** FEATURE-3266 — New icon set without the tile
**Branch:** `feature/feature-3266-new-icon-set`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The application icon is replaced everywhere it ships by the user's new set from
`Branding/Apps/final2/text-editor`, used exactly as delivered. The drawing is FEATURE-9A03's editor window (gutter
marks, four text lines, the green block cursor, the status bar with its mode pill), **without the dark `#1E1F22`
tile**: the background is now transparent.

- `Assets/app.ico`: the same ten 32-bit PNG frames (16 to 256), now 7 662 bytes (was 15 106). `ApplicationIcon`,
  `MainWindow`'s `Icon` and the copy beside the executable use it, unchanged.
- `Assets/app-256.png`: the new 256 frame, byte for byte. The About dialog shows it.
- `packaging/icon/*.svg`: the new masters, with the header comments `export.py final2` wrote.
- `packaging/linux/icons/`: the eight sizes `install.sh` installs, replaced.

## Files/modules touched

- Created: `docs/done/FEATURE-3266.md`
- Modified:
  - `src/Enigma.TextEditor.Desktop/Assets/app.ico`
  - `src/Enigma.TextEditor.Desktop/Assets/app-256.png`
  - `packaging/icon/enigma-text-editor.svg`, `packaging/icon/enigma-text-editor-small.svg`
  - `packaging/linux/icons/enigma-text-editor-{16,24,32,48,64,96,128,256}.png`
  - `docs/roadmap.md`, `docs/plan/FEATURE-3266.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The SVGs' CRLF line endings | Written to the working tree as LF | `.gitattributes` (`* text=auto eol=lf`) stores LF anyway; the working copy then matches the repository, and the text is otherwise identical |
| A re-run of the suite after the flaky `FileDropViewTests` failure | One re-run, no change | An icon swap cannot reach a file-drop view test; it is the flake earlier done docs name. A re-run without a fix is not a fix cycle |

## Deviations & follow-ups

- **Line endings:** the delivered SVGs have CRLF line endings, so the committed ones differ from the source only in
  line endings. The binaries are byte for byte. Recommendation only: have `export.py` write LF.
- **Manual check, for the user:** the About dialog's icon on its now-transparent background, in the light and the dark
  theme; and the taskbar icon on Windows. Explorer may show the old icon on a shortcut until its icon cache refreshes.
- **Pre-existing test failure** (the run's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  ("Pipe is broken") fails on this machine in every run.
- **Flaky test:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed in the first run and
  passed in the re-run. The same behaviour is in `docs/done/FEATURE-F0CE-PHASE04.md` and `docs/done/FEATURE-6D39.md`.
- The source directory `Branding/Apps/final2/text-editor` is outside this repository and was left alone.
- Fix cycles used: 0 of 3.

## Documentation sweep

- Nothing stale. The README, `docs/RELEASE.md`, `SECURITY.md` and the comments in `src/` and `packaging/` never
  describe the tile. The 1.8.0 notes ("on the same dark tile") are history and are not edited. The release notes come
  with FEATURE-5920.

## Build/test evidence

- **Run baseline** (run branch `feature/2026-11-03-icons-msi-release` at `38380a4`, before this dev):
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3109 total, 3108 passed, 1 failed (`InstanceServerTests…`).
- This dev:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`, first run: 3109 total, 3107 passed, 2 failed (the baseline's
    `InstanceServerTests…` and the flaky `FileDropViewTests…`);
  - the same, re-run: 3109 total, 3108 passed, 1 failed (`InstanceServerTests…`), as the baseline;
  - `AppIconTests` (`--filter-class`): 5 of 5 passed.
- `cmp`: `app.ico`, `app-256.png` and the eight Linux PNGs are identical to the delivered files; the SVGs are
  identical once CRs are stripped.
- A script read of `app.ico`: ten 32-bit RGBA PNG frames, 16 to 256. Each of the eight Linux PNGs equals the frame of
  its size, byte for byte.
- No tests added: `AppIconTests` already pins what the replacement had to keep.

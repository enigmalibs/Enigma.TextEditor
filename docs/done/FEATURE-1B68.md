# FEATURE-1B68 — Release 1.15.0

**Item:** FEATURE-1B68 — Release 1.15.0
**Branch:** `feature/feature-1b68-release-1-15-0`
**Run:** feature/2026-11-03-final3-icons-release

## Summary

1.15.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-5920 (1.14.0) as the template.

- **Version:** `<Version>1.15.0</Version>` in both csprojs (was 1.14.0). This follows the user's rule `x.y+1.z`;
  z is 0.
- **`buildmsi`:** `buildmsi.ps1` holds no version literal, so it is unchanged.
  - It reads the Desktop csproj's `<Version>` and requires Core's to match, so the bump above is what it builds.
  - The scratch-clone run shows it: it built `Enigma.TextEditor.1.15.0.msi`, `ProductVersion` 1.15.0.
- **`RELEASENOTES.md`:** a dated `## 1.15.0 — 2026-11-03` section above 1.14.0's. It holds a summary, then:
  - *Application icon* (FEATURE-FF51): the drawing fills its frame, the small sizes are redrawn, it shows everywhere
    it did, and the sizes are unchanged;
  - *Upgrading from 1.14*: settings, the MSI upgrade and its name, icon caches, re-running `install.sh`;
  - *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.15 first, then sums up 1.14 down to 1.0.
- **`SECURITY.md`:** `1.15.x` is supported, and `1.14.x` and earlier are not.
- **Dependencies:** the Avalonia set is held back at 12.1.4. 12.1.5 is out, but Enigma.Avalonia.Desktop has not moved
  past 1.3.0.

Nothing in this dev tagged, pushed, packed, ran `buildmsi` in the repository, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-1B68.md`
- Created: `docs/done/FEATURE-1B68.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How the notes describe the bigger drawing | "About a quarter larger"; the longest side "almost from edge to edge" | The SVG scales by 1.2727, and the drawing spans 16 to 240 of 256. The exact factor means nothing to a reader |
| The small sizes' grid in the notes | Left out | "Straight edges on the 16-unit grid" is whole pixels at 16 and 32 px but not at 24 px, so a "whole pixels" claim would be untrue |
| Where the notes mention `buildmsi` | *Upgrading from 1.14*: the MSI's name, and that the script needs no change | The user asked for the script's version to move. Saying it follows the projects answers that where an upgrader looks |
| README reflow | The callout's first three lines became four, all within 120 columns; the lines from 1.13 down are untouched | The plan: only the lines this release touches are reflowed |
| Release date | 2026-11-03 | The run's date, the same day as 1.14.0 |

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged: its steps are generic `X.Y.Z`, and nothing in it names 1.14.
- **Before tagging, for the user:**
  - merge, then build the 1.15.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted (it
    runs `git clean -dxf`);
  - install it over the installed version, then check that *Installed apps* lists one *Enigma Text Editor* at 1.15.0
    (`docs/RELEASE.md` §6);
  - look at the bigger icon on the taskbar and in the About dialog, in both themes (FEATURE-FF51).
- **Follow-up, from FEATURE-37C9:** the MSI is still about 187 MB, most of it the Skia and HarfBuzz `.pdb` files from
  the publish.
- **Pre-existing test failures** (the run's baseline):
  - `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`;
  - the flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.15.0.
- The merges into `develop` and `main`, the `1.15.0` tag and the push are the user's (`docs/RELEASE.md` §2–§4).
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md` and `SECURITY.md`, as above.
- Checked and left as they are:
  - `docs/RELEASE.md`;
  - the README's *Install on Windows* (the MSI name is `X.Y.Z`) and *Install on Linux* ("eight sizes", still true);
  - the help text of `buildmsi.ps1` and `buildmsi.bat` (`X.Y.Z`, from the csproj).
- The repository has no `CLAUDE.md`, `AGENTS.md` or `CONTRIBUTING.md`.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-FF51.md`.
- `git tag --list 1.15.0`: empty before the release. `1.14.0` is on `main` (`60a8fb9`).
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3112 total, 3111 passed, 1 failed
  (`InstanceServerTests…`).
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 3112 total, 3110 passed, 2 failed
  (`InstanceServerTests…`, `FileDropViewTests.ThePaneWithNoTab_…`), both in the baseline.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry:
  - file version `1.15.0.0`;
  - informational version `1.15.0+7654524…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: the six Avalonia packages have 12.1.5; nothing else.
- **`buildmsi`, end to end, in a scratch clone** of this branch with the dev's tree committed there:
  - **The run:**
    - `buildmsi.bat` exited **0** in 43 s;
    - git removed an untracked canary file;
    - `artifacts/` held `Enigma.TextEditor.1.15.0.msi` (186,697,703 bytes) and `Enigma.TextEditor.1.15.0.msipkg.json`
      (`"version": "1.15.0"`).
  - **The MSI's tables,** read through the Windows Installer API:
    - `ProductVersion` 1.15.0, `ProductName` Enigma Text Editor, `Manufacturer` Josué Clément, `ALLUSERS` 1;
    - `UpgradeCode` `{9F063D09-9F4E-4436-8CB3-39109EDA380D}` (unchanged);
    - `Upgrade`: `VersionMax 1.15.0` (attributes 1) and `VersionMin 1.15.0` (attributes 2);
    - two *Enigma Text Editor* shortcuts (`DesktopFolder`, `ProgramMenuFolder`) on
      `[INSTALLDIR]\Enigma.TextEditor.Desktop.exe`.
  - **The icon:** both `Icon` rows (`app_icon.ico`, `IconFile1_app.ico`) are 12,835 bytes, the size of the new
    `app.ico`. The publish folder's `app.ico` has the asset's SHA-256.
  - The scratch clone was deleted afterwards.
- No tests were added, because the dev changes no code.

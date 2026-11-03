# FEATURE-5920 — Release 1.14.0

**Item:** FEATURE-5920 — Release 1.14.0
**Branch:** `feature/feature-5920-release-1-14-0`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

1.14.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-6D39 (1.13.0) as the template:

- **Version:** `<Version>1.14.0</Version>` in both csprojs (was 1.13.0). This follows the user's rule: y+1, and z is
  already 0.
- **`RELEASENOTES.md`:** a dated `## 1.14.0 — 2026-11-03` section above 1.13.0's. It holds a summary, then:
  - *Application icon* (FEATURE-3266): the tile is gone, it shows everywhere it did, and the sizes are unchanged;
  - *Windows installer* (FEATURE-37C9): what `buildmsi` does, with the `git clean -dxf` warning; what the MSI
    installs; that it replaces an MSI-installed version; the prerequisites;
  - *Upgrading from 1.13*, *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.14 first, then sums up 1.13 down to 1.0.
- **`SECURITY.md`:** `1.14.x` is supported, and `1.13.x` and earlier are not. The scope gains the Windows installer.
- **Dependencies:** the Avalonia set is held back at 12.1.4 (12.1.5 is out; Enigma.Avalonia.Desktop has not moved).

Nothing in this dev tagged, pushed, packed, built an MSI in the repository, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-5920.md`
- Created: `docs/done/FEATURE-5920.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Order of the themed sections | The icon first, then the installer | The order the run built them; the icon is what every user sees |
| How the notes describe the 32-bit package | "`Program Files (x86)` on 64-bit Windows, since the package is 32-bit" | It is where the MSI really installs (`Intel;1033`), and where 1.9.0 is |
| The installer's build tools in *Dependencies* | One bullet: Enigma.Msi 1.7.0's worker and the WiX CLI, none of whose files ships | New third-party code in the repository, but not in the product |
| README callout wording for 1.13 | "1.13 brought a file explorer …; a new tab on a double-click …; and the mouse wheel scrolling a strip …" | The "1.x brought …" summary form of the older entries |
| README reflow | The four rewritten lines became seven, all within 120 columns; lines from 1.12 down are untouched | Plan decision: no reflow of the older overlong lines |
| `SECURITY.md` scope wording | What the script deletes, the profile it writes, and what the MSI installs and removes | The same three aspects the Linux scripts' entry lists |
| Release date | 2026-11-03 | The run's date |

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged in this dev. Its steps are generic `X.Y.Z`, and FEATURE-37C9-PHASE02 already wrote
  the Windows installer into it.
- **Before tagging, for the user:**
  - merge, then build the 1.14.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted (it
    runs `git clean -dxf`);
  - install it over the installed 1.9.0, then check that *Installed apps* lists one *Enigma Text Editor* at 1.14.0
    and that both shortcuts start it (`docs/RELEASE.md` §6);
  - look at the new icon on the taskbar and in the About dialog, in both themes (FEATURE-3266).
- **Follow-up from FEATURE-37C9:** the MSI is about 187 MB, 105 MB of which is `libSkiaSharp.pdb` and
  `libHarfBuzzSharp.pdb` from the publish.
- **Pre-existing test failure** (the run's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.14.0.
- The merges into `develop` and `main`, the `1.14.0` tag and the push are the user's (`docs/RELEASE.md` §2–§4).
- Fix cycles used: 0 of 3.

## Documentation sweep

- README callout, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.
- Checked and left as they are:
  - `docs/RELEASE.md`;
  - the README's *Install on Windows* (FEATURE-37C9) and *Install on Linux* ("eight sizes", still true).
- The repository has no `CLAUDE.md`, `AGENTS.md` or `CONTRIBUTING.md`.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-3266.md`.
- `git tag --list 1.14.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3112 total, 3111 passed, 1 failed (`InstanceServerTests…`).
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 3112 total, 3111 passed, 1 failed (`InstanceServerTests…`).
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.14.0.0`;
  - informational version `1.14.0+5eb5f40…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: the six Avalonia packages have 12.1.5; nothing else.
- No tests were added: the dev changes no code.

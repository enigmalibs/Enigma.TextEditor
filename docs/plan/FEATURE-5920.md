# FEATURE-5920 — Release 1.14.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-5920-release-1-14-0`
**Run:** feature/2026-11-03-icons-msi-release

## Objective

Cut **1.14.0** on `dotnet-release`'s routine-release path for an app, with FEATURE-6D39 (1.13.0) as the template:

- the version;
- the release notes for what 1.14.0 changes;
- the README callout;
- `SECURITY.md`;
- `docs/RELEASE.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.14.0` tag, any push and the MSI built for distribution are left to the
user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.13.0` today, and the two move together. Nothing in `src/` or `tests/`
  hard-codes it.
- **The version rule is the user's:** `x.y.z` → `x.(y+1).z`, with z reset to 0 when it is above 0. From 1.13.0 that
  gives **1.14.0**.
- **1.14.0 carries the items of this run** that are `DONE` on the run branch before this dev starts:
  - FEATURE-3266: the new icon set without the tile;
  - FEATURE-37C9: `buildmsi.ps1` / `buildmsi.bat` and the vendored Enigma.Msi worker. Its MSI upgrades the installed
    1.9.0, and installs per machine in `%ProgramFiles%\Enigma Text Editor` (`Program Files (x86)` on 64-bit Windows),
    with two shortcuts.
  An item that ended quarantined is left out of the notes. `git log 1.13.0..HEAD` holds nothing else.
- **1.13.0's notes are history**, and are not edited.
- **No settings key, no stored format** changes. 1.13 reads a 1.14 settings file unchanged.
- **BUG-4A17** is still quarantined from an earlier run: the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.13.x`. Its scope already lists the Linux install
  scripts. The Windows installer is the same kind of surface: what the MSI installs, where, its shortcuts, and that
  uninstalling leaves `%APPDATA%\Enigma.TextEditor` alone.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set, its companions, Enigma.Avalonia.Desktop and
  Enigma.Icons.Avalonia move together or not at all. At 1.13.0, Avalonia 12.1.5 was out, but Enigma.Avalonia.Desktop
  had not moved past 1.3.0, so the set was held back.
- **Tag convention:** bare `X.Y.Z`; `1.13.0` exists, `1.14.0` does not.
- **Known test noise** (earlier done docs): `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  fails on this machine ("Pipe is broken"), and `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`
  is flaky. The run's baseline decides what counts as pre-existing.

## Steps

1. Check that `git tag --list 1.14.0` is empty.
2. Both csprojs: `<Version>1.14.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.14.0 — <run date>` section above 1.13.0's, in the repository's style:
   - a summary;
   - *Application icon*: the tile is gone, the sizes are unchanged, and it shows everywhere it showed;
   - *Windows installer*: `buildmsi`, what the MSI installs, upgrading from an MSI-installed version;
   - *Upgrading from 1.13* (icon caches; the MSI);
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
   Every path the notes quote is checked against the files.
4. `README.md`: the callout names 1.14 first, then sums up 1.13 down to 1.0. Only the lines this release touches are
   reflowed.
5. `SECURITY.md`: `1.14.x` is supported, and `1.13.x` and earlier are not. The scope gains the Windows installer
   scripts and the MSI.
6. `docs/RELEASE.md`: fix anything 1.14.0 makes untrue. Its generic `X.Y.Z` steps and FEATURE-37C9's installer steps
   stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump what moved, stable only, with the coupled set together and only with Enigma.Avalonia.Desktop;
   - log the result in the notes.
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green, bar the baseline's failures;
   - the assemblies carry `1.14.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx` fails nothing the run's baseline did not.
- The built assemblies carry `1.14.0`: file version `1.14.0.0`, informational `1.14.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.14.0` section above 1.13.0's. It covers every `DONE` item of this run, logs the
  dependency check, and keeps the BUG-4A17 known issue. Its quoted paths match the files.
- The README callout names 1.14. `SECURITY.md` supports `1.14.x` only, and its scope names the Windows installer.
- No dev runs `git tag`, `git push`, `dotnet pack`, `dotnet nuget push` or the MSI, and no dev writes to `develop`
  or `main`.

## Out of scope

- NuGet packaging; a committed MSI profile (FEATURE-37C9 generates it per build).
- Building the 1.14.0 MSI for distribution: the user runs `buildmsi` after merging, since it cleans the repository.
- Any change to the application's behaviour; the flaky and failing tests named above.
- The merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.14.0 | The user's rule: y+1, and z is already 0 | 1.13.1; 2.0.0 |
| Order in the run | Last | The notes describe the icon and the installer, built first | The draft's order (release before the installer) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; the 1.6–1.13 precedent | Splitting notes and version |
| Notes shape | 1.13.0's | The repository's own style | The template's *New Features · Fixes* headings |
| `SECURITY.md` scope | Widened to the Windows installer | The Linux install scripts are in scope; the MSI writes into Program Files and creates shortcuts | Leaving the scope as it is |
| `SECURITY.md` versions | `1.14.x` only | Latest-minor-only policy | Keeping `1.13.x` supported |
| Dependencies | Stable non-coupled bumps applied; the Avalonia set only with Enigma.Avalonia.Desktop | House rule, 1.4–1.13 precedent | Bumping everything; prereleases |
| Building the MSI in this dev | No; FEATURE-37C9 verified the script in a scratch clone | `buildmsi` runs `git clean -dxf`, which the run may not run here | Running it in the repository |
| Release date | The run date | 1.13.0's precedent | A future date |
| Starting point | The run branch, cut from `develop` @ `0eba3af` | `/vibe` cuts from `HEAD` | `main` |

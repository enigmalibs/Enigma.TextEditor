# FEATURE-1B68 — Release 1.15.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-1b68-release-1-15-0`
**Run:** feature/2026-11-03-final3-icons-release

## Objective

Cut **1.15.0** on `dotnet-release`'s routine-release path for an app, with FEATURE-5920 (1.14.0) as the template:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.15.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight;
- an end-to-end `buildmsi` run in a scratch clone, showing the MSI comes out as 1.15.0.

The merges into `develop` and `main`, the `1.15.0` tag, any push and the MSI built for distribution are left to the
user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.14.0` today, and the two move together. Nothing in `src/` or `tests/`
  hard-codes it.
- **The version rule is the user's:** `x.y.z` → `x.(y+1).z`. From 1.14.0 that gives **1.15.0**. Tag `1.14.0` is on
  `main` (`60a8fb9`), and `1.15.0` does not exist.
- **`buildmsi.ps1` holds no version literal.**
  - It reads `<Version>` from `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` and stops when
    `Enigma.TextEditor.Core.csproj` says otherwise.
  - That `<Version>` names the MSI (`Enigma.TextEditor.X.Y.Z.msi`), its profile and the `ProductVersion`.
  - Every sibling repository's `buildmsi.ps1` (GitClient, Backup, FileExplorer, …) works the same way, from
    `Directory.Build.props`.
  - So "update the version in the buildmsi script" is the csproj bump, verified by a build of the MSI.
- **1.15.0 carries the items of this run** that are `DONE` on the run branch before this dev starts. Today that is
  FEATURE-FF51: the icon now fills its frame. An item that ended quarantined is left out of the notes.
  `git log 1.14.0..HEAD` holds nothing else.
- **1.14.0's notes are history**, and are not edited.
- **No settings key, no stored format** changes.
- **BUG-4A17** is still quarantined from an earlier run, so the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.14.x`. Its scope already covers the Windows
  installer.
- **The dependency rule** (`Directory.Packages.props`) is that these move together or not at all:
  - the Avalonia set and its companions;
  - Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia.

  At 1.14.0, Avalonia 12.1.5 was out, but Enigma.Avalonia.Desktop had not moved past 1.3.0.
- **Tag convention:** bare `X.Y.Z`.
- **Known test noise** (earlier done docs):
  - `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` fails on this machine;
  - `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` is flaky.

  The run's baseline decides what counts as pre-existing.

## Steps

1. Check that `git tag --list 1.15.0` is empty.
2. Both csprojs: `<Version>1.15.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.15.0 — <run date>` section above 1.14.0's, in the repository's style:
   - a summary;
   - *Application icon*: the drawing fills its frame, and nothing else changes;
   - *Upgrading from 1.14* (settings; the MSI upgrade; icon caches; re-running `install.sh`);
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.

   Every path the notes quote is checked against the files.
4. `README.md`: the callout names 1.15 first, then sums up 1.14 down to 1.0. Only the lines this release touches are
   reflowed.
5. `SECURITY.md`: `1.15.x` is supported, and `1.14.x` and earlier are not.
6. `docs/RELEASE.md`: fix anything 1.15.0 makes untrue. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump what moved, stable only, with the coupled set together and only with Enigma.Avalonia.Desktop;
   - log the result in the notes.
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green, bar the baseline's failures;
   - the assemblies carry `1.15.0`.
9. **`buildmsi`, end to end, in a scratch clone** of the dev branch with the dev's tree committed there. The run may
   not `git clean` the user's repository. The check:
   - `artifacts\Enigma.TextEditor.1.15.0.msi` and its `.msipkg.json` (`"version": "1.15.0"`) are built;
   - the MSI's `ProductVersion` is `1.15.0`, and its `UpgradeCode` is unchanged;
   - its icon is the new `app.ico`.

   Delete the clone afterwards.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx` fails nothing the run's baseline did not.
- The built assemblies carry `1.15.0`: file version `1.15.0.0`, and informational version `1.15.0+<commit>`.
- `buildmsi` builds `Enigma.TextEditor.1.15.0.msi`:
  - `ProductVersion` 1.15.0;
  - `UpgradeCode` `{9F063D09-9F4E-4436-8CB3-39109EDA380D}`.
- `RELEASENOTES.md` has one dated `1.15.0` section above 1.14.0's:
  - it covers every `DONE` item of this run;
  - it logs the dependency check;
  - it keeps the BUG-4A17 known issue;
  - its quoted paths match the files.
- The README callout names 1.15, and `SECURITY.md` supports `1.15.x` only.
- No dev in this run:
  - runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`;
  - runs `buildmsi` inside the user's repository;
  - writes to `develop` or `main`.

## Out of scope

- NuGet packaging; a committed MSI profile (buildmsi generates it per build).
- Building the 1.15.0 MSI for distribution. The user runs `buildmsi` after merging, since it cleans the repository.
- Any change to the application's behaviour; the flaky and failing tests named above.
- The merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.15.0 | The user's rule `x.y+1.z`; z is 0 | 1.14.1; 2.0.0 |
| "Update the version number in the buildmsi script" | Bump both csprojs, which are the script's only source of the version, and leave `buildmsi.ps1` unchanged. Prove it by building the MSI in a scratch clone | The script reads `<Version>` and checks that both csprojs agree. A literal in the script would be a second source that drifts, and no sibling repository's buildmsi carries one | Adding a `$Version = '1.15.0'` to the script; a `-Version` parameter |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; the 1.6–1.14 precedent | Splitting notes and version |
| Order in the run | Last | The notes describe the icon, built first | Release first |
| Notes shape | 1.14.0's | The repository's own style | The template's *New Features · Fixes* headings |
| `SECURITY.md` versions | `1.15.x` only | Latest-minor-only policy | Keeping `1.14.x` supported |
| Dependencies | Stable non-coupled bumps applied; the Avalonia set only with Enigma.Avalonia.Desktop | House rule, 1.4–1.14 precedent | Bumping everything; prereleases |
| Building the MSI | In a scratch clone only | `buildmsi` runs `git clean -dxf`, which the run may not run on the user's repository; FEATURE-37C9's precedent | Running it in the repository; not building it at all |
| Release date | The run date | 1.14.0's precedent | A future date |
| Starting point | The run branch, cut from `develop` @ `13b4fc2` | `/vibe` cuts from `HEAD` | `main` |

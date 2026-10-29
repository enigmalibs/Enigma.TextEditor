# FEATURE-8EDF — Release 1.9.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-8edf-release-1-9-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

Cut **1.9.0** on `dotnet-release`'s routine-release path, with FEATURE-3C6D (1.8.0) as the template. The work is:

- the version;
- the release notes for what 1.9.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.9.0` tag and any push are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.8.0` today; the two move together. The request's rule (minor + 1,
  patch to 0) gives **1.9.0**.
- **What 1.9.0 carries** is what this run merges before the release dev:
  - FEATURE-4092 (`app.ico` in the output folder);
  - FEATURE-452D (the unhandled-error safety net);
  - FEATURE-84C3 (the publish scripts).

  Whatever the run quarantined is left out and named. BUG-4A17 is still quarantined from an earlier run.
- **No settings key changes:** 1.8 reads a 1.9 settings file unchanged.
- `SECURITY.md` supports the latest minor line only, today `1.8.x`.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set moves together. At planning time every pin
  was at its latest stable version (`dotnet list package --outdated`, nuget.org).

## Steps

1. Check that `git tag --list 1.9.0` is empty.
2. Both csprojs: `<Version>1.9.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.9.0 — <run date>` section above 1.8.0's, in the repository's style:
   - a summary;
   - a themed section per merged item;
   - *Upgrading from 1.8*;
   - *Dependencies*;
   - *Compatibility*;
   - *Version*.
4. `README.md`: the callout names 1.9, then sums up 1.8 down to 1.0.
5. `SECURITY.md`: `1.9.x` is supported, `1.8.x` and earlier are not. Check that the scope still holds.
6. `docs/RELEASE.md`: fix anything 1.9.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump anything published since planning, stable only, the coupled set together;
   - log the result in the notes, including "nothing moved".
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.9.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` is green.
- The built assemblies carry `1.9.0`: file version `1.9.0.0`, informational `1.9.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.9.0` section above 1.8.0's. It covers every merged run item and names any
  quarantined one.
- The README callout names 1.9. `SECURITY.md` supports `1.9.x` only.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour.
- The merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.9.0 | The request's rule (x.y.z → x.(y+1).0) | 1.8.1 |
| Work-item shape | One single-phase FEATURE, last in the run | `dotnet-release`: a routine release is one phase; it describes what the run merged | Releasing first |
| "Update libraries" item | Folded into this dev's dependency check, no separate item | Every pin was already latest stable at planning; a separate dev would change nothing | A no-op dev |
| Notes shape | 1.8.0's | The repository's own style | The template's *New Features · Fixes* headings |
| `SECURITY.md` | `1.9.x` only | Latest-minor-only policy | Keeping `1.8.x` supported |
| Prerelease packages | Never (e.g. Microsoft.Extensions.Hosting 11.0.0-rc) | A release ships on stable dependencies | Taking the .NET 11 RC |
| MSI profile | None | Not asked for; the 1.0–1.8 precedent | Generating a first profile |
| Draft item "Remove splash screen" | No work item | The app has no splash screen (`grep -ri splash src tests`) | A no-op dev |
| Draft item "Uniform about dialog" | No work item | FEATURE-5D8B already ported GitClient's About view. View, VM and service differ from GitClient's only in element names, automation names and comments; layout, sizes, styles and brushes are identical | Re-porting it |
| Draft item "Linux packaging" | No work item | `install.sh` / `uninstall.sh` exist (FEATURE-7F58) and mirror GitClient's | Rewriting them |
| Draft item "Remove net8" | No work item | Every project already targets `net10.0` only | — |

# FEATURE-3C6D — Release 1.8.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-3c6d-release-1-8-0`
**Run:** feature/2026-10-28-new-icons-release

## Objective

Cut **1.8.0** on `dotnet-release`'s routine-release path, with FEATURE-AEBF (1.7.0) as the template. The work is:

- the version;
- the release notes for what 1.8.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.8.0` tag happen after the run, at the user's request. No dev runs them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.7.0` today; the two move together. The request's rule: minor + 1,
  patch to 0 → **1.8.0**.
- **What 1.8.0 carries** is what this run merges before the release dev: **FEATURE-9A03**, the new application
  icon. Whatever the run quarantined is left out and named. BUG-4A17 is still quarantined from an earlier run.
- `main` and the `1.7.0` tag are at 1.7.0, so 1.8.0 follows it directly.
- No settings key or behaviour changes: 1.7 reads a 1.8 settings file unchanged.
- `SECURITY.md` supports the latest minor line only, today `1.7.x`.
- **The dependency rule** (`Directory.Packages.props`):
  - the Avalonia set, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and
    Enigma.Icons.Avalonia move together or not at all;
  - AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0` … `1.7.0`), on `main`'s merge commit.

## Steps

1. Check that `git tag --list 1.8.0` is empty.
2. Both csprojs: `<Version>1.8.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.8.0 — 2026-10-28` section above 1.7.0's, in the repository's style:
   - a summary;
   - *Application icon*;
   - *Upgrading from 1.7*: re-run the Linux installer to pick up the new icons;
   - *Dependencies*;
   - *Compatibility*;
   - *Version*.
4. `README.md`: the callout names 1.8, then sums up 1.7 down to 1.0.
5. `SECURITY.md`: `1.8.x` is supported, `1.7.x` and earlier are not. Check that the scope still holds.
6. `docs/RELEASE.md`: fix anything 1.8.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - apply non-coupled patch and minor bumps;
   - hold back the coupled set unless Enigma.Avalonia.Desktop moved with it;
   - log every change.
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.8.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` is green.
- The built Core and Desktop assemblies carry `1.8.0`: file version `1.8.0.0`, informational `1.8.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.8.0` section above 1.7.0's. It covers FEATURE-9A03, or names it as quarantined.
- The README callout names 1.8. `SECURITY.md` supports `1.8.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour.
- The merges and the tag (post-run, user-requested), and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE, last in the run | `dotnet-release`: a routine release is one phase. It must describe what the run merged | Releasing first |
| Version number | 1.8.0 | The request's rule (x.y.z → x.(y+1).0) | 1.7.1 |
| Notes shape | 1.7.0's: summary, themed section, *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.8 first, then 1.7 down to 1.0 summed up | The 1.1.0–1.7.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.8.x` only | Latest-minor-only policy | Keeping `1.7.x` supported |
| Dependency refresh | Non-coupled bumps applied; coupled set held unless Enigma.Avalonia.Desktop moved; all logged | `dotnet-release` routine release; 1.7.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.7 precedent | Generating a first profile |

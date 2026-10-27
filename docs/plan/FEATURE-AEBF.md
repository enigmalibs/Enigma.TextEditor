# FEATURE-AEBF — Release 1.7.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-aebf-release-1-7-0`
**Run:** bugfix/2026-10-27-link-colours-release

## Objective

Cut **1.7.0** on `dotnet-release`'s routine-release path, with FEATURE-4362 (1.6.0) as the template:

- the version;
- the release notes for what 1.7.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.7.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so the version, the notes, the callout and the
  tag apply. Pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.6.0` today. The two move together.
- **What 1.7.0 carries** is what this run merges before the release dev: **BUG-78F9**.
  - PHASE01: lighter link colours.
  - PHASE02: settings hint lines and the hex view's selected row.
  - Whatever the run quarantined is left out and named as such.
  - **BUG-4A17** is still quarantined and unmerged, so it is not in 1.7.0.
- **1.6.0 is cut but not yet tagged or merged into `main`.** `develop` holds it (`6a79e6b`); `main` and the last tag
  are still 1.5.0. The `1.6.0` section of `RELEASENOTES.md` stays as it is, and 1.7.0's sits above it. Whether to
  tag 1.6.0 on its own before 1.7.0 is the user's call (end-of-run report).
- **No settings key is added**, and no behaviour changes beyond colours. 1.6 reads a 1.7 settings file unchanged.
- `SECURITY.md` supports the latest minor line only, today `1.6.x`.
- **The dependency rule** (`Directory.Packages.props`, `dotnet-release`):
  - the Avalonia set, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and
    Enigma.Icons.Avalonia move together or not at all;
  - AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0` … `1.5.0`).

## Steps

1. Check that `git tag --list 1.7.0` is empty.
2. Both csprojs: `<Version>1.7.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.7.0 — 2026-10-27` section above 1.6.0's, in the repository's style:
   - a summary;
   - *Colours*: links, settings hints, the hex row, with the values;
   - *Upgrading from 1.6*;
   - *Dependencies*.
4. `README.md`: the callout names 1.7, then sums up 1.6 down to 1.0 after it.
5. `SECURITY.md`: `1.7.x` is supported; `1.6.x` down to `1.0.x` are not. Check that the scope still holds.
6. `docs/RELEASE.md`: fix anything 1.7.0 makes untrue. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - apply non-coupled patch and minor bumps;
   - hold back the coupled Avalonia set unless Enigma.Avalonia.Desktop moved with it;
   - log every change.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.7.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.7.0`: file version `1.7.0.0`, informational version
  `1.7.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.7.0` section above 1.6.0's. It covers BUG-78F9, or names what was
  quarantined.
- The README callout names 1.7. `SECURITY.md` supports `1.7.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the `1.6.0` and `1.7.0` tags, and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE, last in the run | `dotnet-release`: a routine release is one phase; it must describe what the run merged | Several phases; releasing first |
| Version number | 1.7.0 | Given by the request. A colour-only fix would be 1.6.1 under SemVer, but the number is the user's | 1.6.1 |
| 1.6.0 not yet tagged | Leave its notes section as is; 1.7.0's goes above; tagging 1.6.0 is named in the report | The run cannot tag or write to `main`; the 1.6.0 notes are accurate for that cut | Folding 1.6.0 into 1.7.0's notes |
| Notes shape | 1.6.0's: summary, themed section, *Upgrading*, *Dependencies* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.7 first, then 1.6 down to 1.0 summed up | The 1.1.0–1.6.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.7.x` only | Latest-minor-only policy | Keeping `1.6.x` supported |
| Dependency refresh | Non-coupled bumps applied; coupled set held unless Enigma.Avalonia.Desktop moved; all logged | `dotnet-release` routine release; 1.6.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.6 precedent | Generating a first profile |
| Merges and tags | Printed in the end-of-run report | `vibe-workflow` §5 forbids `tag` and any write to `develop` or `main` | Running them |

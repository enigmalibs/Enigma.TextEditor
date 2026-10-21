# FEATURE-6622 — Release 1.4.0

**Status:** DONE — see `docs/done/FEATURE-6622.md`
**Type:** FEATURE
**Branch:** `feature/feature-6622-release-1-4-0`
**Run:** bugfix/2026-10-21-resize-jk-release

## Objective

Cut **1.4.0**, following `dotnet-release`'s routine-release path, with FEATURE-3F16 (1.3.0) as the template:

- the version;
- the release notes for what 1.4.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.4.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so `dotnet-release`'s *App vs library* rule applies:
  - the version, the notes, the callout and the tag apply;
  - pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.3.0` today. They move together. `AppInfoTests` asserts the shape,
  not a literal.
- **1.4.0 carries what was merged after the 1.3.0 cut (`23303ab`):** BUG-09C2 — `Space rj` now moves the bar between
  stacked panes down and `Space rk` up.
  - **BUG-4A17** is still quarantined and unmerged, so it is not in 1.4.0.
- **1.2.0 and 1.3.0 have not reached `main` and have no tag.** `main` is at 1.1.0's merge, and `git tag` lists `1.0.0`
  and `1.1.0` only. The end-of-run report says where the missing tags could go.
- **The version number is the request's**, 1.4.0. The release changes two documented keys and adds nothing.
- No settings key changes: a 1.3 `settings.json` loads unchanged, and 1.3 reads a 1.4 file.
- `SECURITY.md` supports the latest minor line only, today `1.3.x`.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`):
  - Avalonia, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia
    are one coupled set, bumped together or not at all;
  - AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0`, `1.1.0`).

## Steps

1. Check that `git tag --list 1.4.0` is empty.
2. Both csprojs: `<Version>1.4.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.4.0 — 2026-10-21` section above 1.3.0's, in the repository's style:
   - a summary;
   - *Panes*: `Space rj` and `Space rk` swapped;
   - *Upgrading from 1.3*: the two keys;
   - *Dependencies*, *Compatibility* and *Version*.
4. `README.md` — the callout names 1.4, then sums up 1.3, 1.2, 1.1 and 1.0 after it.
5. `SECURITY.md` — `1.4.x` is supported; `1.3.x`, `1.2.x`, `1.1.x` and `1.0.x` are not. The scope is unchanged.
6. `docs/RELEASE.md` — anything 1.4.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.4.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.4.0`: file version `1.4.0.0`, informational version
  `1.4.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.4.0` section above 1.3.0's, covering BUG-09C2.
- The README callout names 1.4. `SECURITY.md` supports `1.4.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the tags (`1.2.0`, `1.3.0`, `1.4.0`) and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; FEATURE-3F16's precedent | Several phases |
| Version number | 1.4.0, minor | Given by the request; it changes two documented keys | 1.3.1 as a SemVer patch for a fix (the request names 1.4.0) |
| What 1.4.0 contains | BUG-09C2; BUG-4A17 left out | Its fix is red and unmerged; the bug predates 1.0 | Holding the release for BUG-4A17 |
| Notes shape | 1.3.0's: summary, themed section, *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.4 first, then 1.3, 1.2, 1.1 and 1.0 summed up | The 1.1.0–1.3.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.4.x` only | Latest-minor-only policy | Keeping `1.3.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; 1.3.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.3 precedent | Generating a first profile |
| Merges and tags | Printed in the end-of-run report | `vibe-workflow` §5 forbids `tag` and any write to `develop` or `main` | Running them |

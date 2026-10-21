# FEATURE-3F16 — Release 1.3.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-3f16-release-1-3-0`
**Run:** feature/2026-10-21-leader-resize-release

## Objective

Cut **1.3.0**, following `dotnet-release`'s routine-release path, with FEATURE-2B84 (1.2.0) as the template:

- the version;
- the release notes for what 1.3.0 adds;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.3.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so `dotnet-release`'s *App vs library* rule applies:
  - the version, the notes, the callout and the tag apply;
  - pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.2.0` today. They move together. `AppInfoTests` asserts the shape,
  not a literal.
- **1.3.0 carries what was merged after the 1.2.0 cut (`cb28fd5`):** FEATURE-216A — the Space leader and
  `<Space>rh/rj/rk/rl`, which move the bar between panes.
  - **BUG-4A17** is still quarantined and unmerged, so it is not in 1.3.0.
- **The 1.2.0 cut has not reached `main` and has no tag.** `main` is at 1.1.0's merge, and `git tag` lists `1.0.0` and
  `1.1.0` only. Merging this run into `develop` and then `develop` into `main` brings 1.2.0 and 1.3.0 in together. The
  end-of-run report says where a `1.2.0` tag could go.
- **Semantic Versioning:** a minor release, new and backward compatible.
  - No settings key is added: a 1.2 `settings.json` loads unchanged, and 1.2 reads a 1.3 file.
  - One behaviour changes: Space in normal and visual mode is the leader, not "right, wrapping". `l` and Backspace
    remain, and `d<Space>` is unchanged.
- `SECURITY.md` supports the latest minor line only, today `1.2.x`.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`):
  - Avalonia, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia
    are one coupled set, bumped together or not at all;
  - AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0`, `1.1.0`).

## Steps

1. Check that `git tag --list 1.3.0` is empty.
2. Both csprojs: `<Version>1.3.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.3.0 — 2026-10-21` section above 1.2.0's, in the repository's style:
   - a summary;
   - *Panes*: the leader and the four keys;
   - *Upgrading from 1.2*: Space;
   - *Dependencies*, *Compatibility* and *Version*.
4. `README.md` — the callout names 1.3, then sums up 1.2, 1.1 and 1.0 after it.
5. `SECURITY.md` — `1.3.x` is supported; `1.2.x`, `1.1.x` and `1.0.x` are not. The scope is unchanged unless 1.3.0
   adds an attack surface.
6. `docs/RELEASE.md` — anything 1.3.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.3.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.3.0`: file version `1.3.0.0`, informational version
  `1.3.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.3.0` section above 1.2.0's, covering FEATURE-216A and Space's new meaning.
- The README callout names 1.3. `SECURITY.md` supports `1.3.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the tags (`1.2.0`, `1.3.0`) and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; FEATURE-2B84's precedent | Several phases |
| Version number | 1.3.0, minor | Given by the request; a new capability, nothing stored breaks | — |
| What 1.3.0 contains | FEATURE-216A; BUG-4A17 left out | Its fix is red and unmerged; the bug predates 1.0, so no regression | Holding the release for BUG-4A17 |
| Space's new meaning | Under *Upgrading from 1.2*, as a changed behaviour, in a minor release | An application with no API; every motion stays reachable (`l`, Backspace, `d<Space>`) | Cutting 2.0.0 |
| Notes shape | 1.2.0's: summary, themed section, *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.3 first, then 1.2, 1.1 and 1.0 summed up | The 1.1.0 and 1.2.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.3.x` only | Latest-minor-only policy | Keeping `1.2.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; 1.2.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.2 precedent | Generating a first profile |
| The missing 1.2.0 tag | Reported for the user, not created | `vibe-workflow` §5 forbids `tag` | Tagging it here |
| Merges and tag | Printed in the end-of-run report | `vibe-workflow` §5 forbids `tag` and any write to `main` | Running them |

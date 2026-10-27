# FEATURE-4362 — Release 1.6.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-4362-release-1-6-0`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Objective

Cut **1.6.0**, following `dotnet-release`'s routine-release path, with FEATURE-2A5C (1.5.0) as the template:

- the version;
- the release notes for what 1.6.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.6.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`: the version, the notes, the callout and the tag
  apply; pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.5.0` today. They move together. `AppInfoTests` asserts the
  shape, not a literal.
- **1.6.0 carries what this run merged after the 1.5.0 cut (`8dc3771`):**
  - **FEATURE-856D** — a narrow icon rail: Files (the split editor) and Settings (a page that replaces the
    Preferences dialog); *Settings → All Settings* (Ctrl+,), Esc back; the Files page comes back when a document
    is opened or created.
  - **FEATURE-0242** — new defaults: word wrap on, resize step 10 %, CRLF on Windows / LF elsewhere.
  - **FEATURE-0D4A** — a welcome dialog on the first start: theme, Vim mode, minimap, word wrap.
  - Whatever of these the run quarantined is left out and named as such.
  - **BUG-4A17** is still quarantined and unmerged, so it is not in 1.6.0.
- **No settings key is added.** The new defaults only reach keys a file lacks, so an upgraded install keeps every
  saved value; 1.5 reads a 1.6 file unchanged.
- **`main` holds 1.5.0's merge** (`8dc3771`, tagged `1.5.0`); the run branch is cut from it.
- `SECURITY.md` supports the latest minor line only, today `1.5.x`.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`): the Avalonia set, its companions,
  `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia move together or not at all;
  AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0` … `1.5.0`).

## Steps

1. Check that `git tag --list 1.6.0` is empty.
2. Both csprojs: `<Version>1.6.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.6.0 — 2026-10-27` section above 1.5.0's, in the repository's style:
   - a summary;
   - *Navigation rail and settings page*;
   - *First start*: the welcome dialog;
   - *Defaults*: the new values, and that saved values are kept;
   - *Upgrading from 1.5*: the dialog gone, the menu item renamed, defaults, going back to 1.5;
   - *Dependencies*, *Compatibility* and *Version*.
4. `README.md` — the callout names 1.6, then sums up 1.5 down to 1.0 after it.
5. `SECURITY.md` — `1.6.x` is supported; `1.5.x` down to `1.0.x` are not. The scope is checked against the
   first-run file probe.
6. `docs/RELEASE.md` — anything 1.6.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:** a clean build with zero warnings; the whole suite green; the assemblies
   carry `1.6.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.6.0`: file version `1.6.0.0`, informational version
  `1.6.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.6.0` section above 1.5.0's, covering FEATURE-856D, FEATURE-0242 and
  FEATURE-0D4A (or naming what was quarantined).
- The README callout names 1.6. `SECURITY.md` supports `1.6.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the `1.6.0` tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE, last in the run | `dotnet-release`: a routine release is one phase; it must describe what the run merged | Several phases; releasing first |
| Version number | 1.6.0, minor | Given by the request; it adds features | 1.5.1 |
| What 1.6.0 contains | FEATURE-856D, FEATURE-0242, FEATURE-0D4A as merged; BUG-4A17 left out | Its fix is red and unmerged | Holding the release for BUG-4A17 |
| Notes shape | 1.5.0's: summary, themed sections, *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.6 first, then 1.5 down to 1.0 summed up | The 1.1.0–1.5.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.6.x` only | Latest-minor-only policy | Keeping `1.5.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; 1.5.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.5 precedent | Generating a first profile |
| Merges and tags | Printed in the end-of-run report | `vibe-workflow` §5 forbids `tag` and any write to `develop` or `main` | Running them |

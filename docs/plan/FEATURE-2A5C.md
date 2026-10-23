# FEATURE-2A5C — Release 1.5.0

**Status:** DONE — see `docs/done/FEATURE-2A5C.md`
**Type:** FEATURE
**Branch:** `feature/feature-2a5c-release-1-5-0`
**Run:** feature/2026-10-23-release-1-5-0

## Objective

Cut **1.5.0**, following `dotnet-release`'s routine-release path, with FEATURE-6622 (1.4.0) as the template:

- the version;
- the release notes for what 1.5.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.5.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so `dotnet-release`'s *App vs library* rule applies:
  - the version, the notes, the callout and the tag apply;
  - pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.4.0` today. They move together. `AppInfoTests` asserts the shape,
  not a literal.
- **1.5.0 carries what was merged after the 1.4.0 cut (`917a04e`):**
  - **BUG-7624** — the editor's text is no longer drawn under the scrollbars, and stops short of the minimap and the
    vertical bar. With the minimap off, the bars are always drawn at full width instead of auto-hiding.
  - **FEATURE-003A PHASE01** — *Settings → Preferences…* (Ctrl+,): Appearance, Editing and New files, each with its
    full range, and *Reset to defaults*.
  - **FEATURE-003A PHASE02** — a *Text* group: the editor font (the machine's monospace faces) and the font size,
    which the zoom keys share. Both reach every pane's editor and the hex rows.
  - **BUG-4A17** is still quarantined and unmerged, so it is not in 1.5.0.
- **One settings key is added:** `EditorFontFamily` (empty = the built-in monospace stack). A 1.4 file lacks it and
  opens with the built-in stack. 1.4 ignores the key when it reads a 1.5 file
  (`SettingsLoaderTests.AnUnknownKey_IsIgnoredRatherThanTreatedAsBroken`); if 1.4 then saves, it leaves the key out.
- **`main` holds 1.4.0's merge** (`b15366f`, tagged `1.4.0`). The run branch is cut from
  `bugfix/2026-10-23-minimap-preferences-fonts`, which has not reached `develop` yet, so merging this run branch into
  `develop` brings both.
- `SECURITY.md` supports the latest minor line only, today `1.4.x`.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`):
  - Avalonia, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia
    are one coupled set, bumped together or not at all;
  - AvaloniaEdit is pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0` … `1.4.0`).

## Steps

1. Check that `git tag --list 1.5.0` is empty.
2. Both csprojs: `<Version>1.5.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.5.0 — 2026-10-23` section above 1.4.0's, in the repository's style:
   - a summary;
   - *Preferences*: the dialog, its groups, reset, and how it follows the menu and the zoom keys;
   - *Fonts*: the editor font and size;
   - *Editor*: the text clear of the minimap and the scrollbars, and the full-width bars;
   - *Upgrading from 1.4*: the new key, going back to 1.4, the bars, Linux;
   - *Dependencies*, *Compatibility* and *Version*.
4. `README.md` — the callout names 1.5, then sums up 1.4, 1.3, 1.2, 1.1 and 1.0 after it.
5. `SECURITY.md` — `1.5.x` is supported; `1.4.x` down to `1.0.x` are not. The scope is checked against the new
   settings key and font probe.
6. `docs/RELEASE.md` — anything 1.5.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.5.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.5.0`: file version `1.5.0.0`, informational version
  `1.5.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.5.0` section above 1.4.0's, covering BUG-7624 and FEATURE-003A.
- The README callout names 1.5. `SECURITY.md` supports `1.5.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the `1.5.0` tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; the 1.2.0–1.4.0 precedent | Several phases |
| Version number | 1.5.0, minor | Given by the request; it adds features (the Preferences dialog, the editor font) | 1.4.1 (it is not a fix-only release) |
| What 1.5.0 contains | BUG-7624 and FEATURE-003A; BUG-4A17 left out | Its fix is red and unmerged; the bug predates 1.0 | Holding the release for BUG-4A17 |
| Notes shape | 1.4.0's: summary, themed sections, *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style | The template's *New Features · Fixes* headings |
| Callout | 1.5 first, then 1.4, 1.3, 1.2, 1.1 and 1.0 summed up | The 1.1.0–1.4.0 precedent | Dropping the earlier summaries |
| `SECURITY.md` | `1.5.x` only | Latest-minor-only policy | Keeping `1.4.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; 1.4.0's precedent | Bumping Avalonia alone |
| MSI profile | None | Not asked for; the 1.0–1.4 precedent | Generating a first profile |
| Merges and tags | Printed in the end-of-run report | `vibe-workflow` §5 forbids `tag` and any write to `develop` or `main` | Running them |

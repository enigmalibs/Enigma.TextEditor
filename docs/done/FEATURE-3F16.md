# FEATURE-3F16 — Release 1.3.0

**Item:** FEATURE-3F16 — Release 1.3.0
**Branch:** `feature/feature-3f16-release-1-3-0`
**Run:** feature/2026-10-21-leader-resize-release

## Summary

Version **1.3.0** is cut in the repository. It is a minor release, and it carries FEATURE-216A: Space as a leader key,
and `Space rh`, `rj`, `rk`, `rl`, which move the bar between two panes from either of them. The application's code is
unchanged by this dev.

- **Both csprojs:** `<Version>1.3.0</Version>` (was `1.2.0`). The built assemblies carry file version `1.3.0.0` and
  informational version `1.3.0+<commit>`, which *Help → About* shows.
- **`RELEASENOTES.md`:** a dated `## 1.3.0 — 2026-10-21` section above 1.2.0's, with:
  - a summary;
  - *Panes*;
  - *Upgrading from 1.2*: Space, the settings, Linux;
  - *Dependencies*, *Compatibility* and *Version*.
- **`README.md`:** the callout names 1.3, then sums up 1.2, 1.1 and 1.0.
- **`SECURITY.md`:** `1.3.x` is supported; `1.2.x`, `1.1.x` and `1.0.x` are not.

## Files / modules touched

**Created**

- `docs/done/FEATURE-3F16.md`

**Modified**

- `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` — `<Version>1.3.0</Version>`
- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `<Version>1.3.0</Version>`
- `RELEASENOTES.md` — the 1.3.0 section
- `README.md` — the callout
- `SECURITY.md` — supported versions
- `docs/roadmap.md`, `docs/plan/FEATURE-3F16.md` — statuses

## Dependency check

`dotnet list Enigma.TextEditor.slnx package --outdated` against nuget.org:

| Package                                                                            | Current | Latest | Action        |
|------------------------------------------------------------------------------------|---------|--------|---------------|
| Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent (Desktop) | 12.1.2  | 12.1.4 | **held back** |
| Avalonia.Headless, Avalonia.Skia (Desktop.UnitTests)                               | 12.1.2  | 12.1.4 | **held back** |
| Every other package, including Enigma.Avalonia.Desktop 1.2.0                       | —       | —      | up to date    |

The set moves as a whole, with what Enigma.Avalonia.Desktop is built against, and Enigma.Avalonia.Desktop has no
newer version. Nothing changed, as at 1.2.0, and the notes' *Dependencies* section says so.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Notes structure | Summary, *Panes*, then *Upgrading from 1.2*, *Dependencies*, *Compatibility*, *Version* | 1.2.0's shape, with one themed section for one item |
| How Space's change is told | Under *Upgrading from 1.2*: what it does now, what moves right instead, and `d<Space>` unchanged | It is the one thing a 1.2 user's fingers will notice |
| `j`/`k` direction in the notes | Stated as built: `rk` down, `rj` up | The notes describe the product; the direction question is FEATURE-216A's follow-up |
| `SECURITY.md` scope | Unchanged | The leader keys fall under "the Vim engine's handling of what is typed into it" |
| `docs/RELEASE.md` | Unchanged | Its steps are generic in `X.Y.Z`. Its *e.g. `1.1.0`* is an example and still true |

## Deviations & follow-ups

- **None from the plan.**
- **BUG-4A17 is not in 1.3.0.** It is still quarantined, with its branch unmerged.
- **1.2.0 is already tagged.** The plan was written where `main` was still at 1.1.0's merge (`dc70c99`) and `1.2.0`
  had no tag. In this repository `main` holds 1.2.0's merge (`ad4c2cd`, tagged `1.2.0`), so merging `develop` into
  `main` brings 1.3.0 alone. Nothing here ran `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or wrote to
  `develop` or `main`.
- **Follow-ups carried from 1.2.0, unchanged:**
  - the editor does not end on SIGTERM;
  - a third-party notices file before distributing a build;
  - private vulnerability reporting enabled in the GitHub repository.
- **Line endings:** every touched file is LF, per `.gitattributes`.
- **Fix budget:** none used. The first build and test runs were green.
- **Documentation sweep:** the callout, the notes and `SECURITY.md`, as above. FEATURE-216A's own sweep already updated
  the README's body. Nothing else names the version.

## Build/test evidence

- Release:
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2676 passed, 0 failed, 0 skipped.
- Debug:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2676 passed, 0 failed, 0 skipped.
- **The built version**, read with the runbook's Windows recipe from
  `src/Enigma.TextEditor.Desktop/bin/Release/net10.0`:
  - **Core:** `FileVersion 1.3.0.0`, `ProductVersion 1.3.0+3e8dffd081fe3fa6665e53050b3cdc5e224681a0`;
  - **Desktop:** the same.
  - The commit is the run branch's FEATURE-216A merge, since a build stamps `HEAD`.
- `git tag --list 1.3.0` was empty before the dev.
- No test was added. The dev changes no behaviour, and `AppInfoTests` already asserts the version's shape.

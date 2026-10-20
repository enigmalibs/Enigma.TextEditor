# FEATURE-5301 — Release 1.1.0

**Item:** FEATURE-5301 — Release 1.1.0
**Branch:** `feature/feature-5301-release-1-1-0`
**Run:** feature/2026-10-20-linux-install-release

## Summary

Version **1.1.0** is cut in the repository. It is a minor release whose one change is FEATURE-7F58, the Linux
installer. The application's code is unchanged.

- **Both csprojs:** `<Version>1.1.0</Version>` (was `1.0.0`). The built assemblies carry file version `1.1.0.0` and
  informational version `1.1.0+<commit>`, which *Help → About* shows.
- **`RELEASENOTES.md`:** a dated `## 1.1.0 — 2026-10-20` section above 1.0.0's, with:
  - a summary;
  - *Install on Linux*: what goes where, the bundled runtime and the options, *Open With*, upgrade in place, the
    uninstaller;
  - *Upgrading from 1.0*, *Dependencies*, *Compatibility* and *Version*.
- **`README.md`:** the callout names 1.1, then sums up 1.0.
- **`SECURITY.md`:**
  - `1.1.x` is supported and `1.0.x` no longer is;
  - the scope adds the Linux install and uninstall scripts.

## Files / modules touched

**Created**

- `docs/done/FEATURE-5301.md`

**Modified**

- `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` — `<Version>1.1.0</Version>`
- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `<Version>1.1.0</Version>`
- `RELEASENOTES.md` — the 1.1.0 section
- `README.md` — the callout
- `SECURITY.md` — supported versions, scope
- `docs/roadmap.md`, `docs/plan/FEATURE-5301.md` — statuses

## Dependency check

`dotnet list Enigma.TextEditor.slnx package --outdated` against nuget.org:

| Package                                                                                               | Current | Latest | Action        |
|-------------------------------------------------------------------------------------------------------|---------|--------|---------------|
| Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent (Desktop)                    | 12.1.2  | 12.1.4 | **held back** |
| Avalonia.Headless, Avalonia.Skia (Desktop.UnitTests)                                                  | 12.1.2  | 12.1.4 | **held back** |
| Every other package, including Enigma.Avalonia.Desktop 1.2.0, AvaloniaEdit 12.0.0 and Hosting 10.0.13 | —       | —      | up to date    |

**Why the set is held back:** Avalonia moves as one set, together with what Enigma.Avalonia.Desktop is built against
(`Directory.Packages.props` and `dotnet-release`). Enigma.Avalonia.Desktop has no newer version, so nothing changed.
The notes' *Dependencies* section says so.

## Deviations & follow-ups

- **`docs/RELEASE.md` is unchanged.**
  - FEATURE-7F58 already rewrote the two passages the installer made untrue.
  - Its runbook is generic in `X.Y.Z`.
  - The 1.0.0 licence audit it points at still lists what a build ships, since no dependency moved.
- **The merges and the tag happen after the run,** as the request asks and the plan records:
  - `develop ← run branch`, then `main ← develop`, both `--no-ff`;
  - then a lightweight `1.1.0` on `main`.

  Nothing in this dev ran `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`.
- **Follow-up carried from FEATURE-7F58:** the editor does not end on SIGTERM. Candidate for a BUG item.
- **Follow-ups carried from 1.0.0, unchanged:**
  - a third-party notices file before distributing a build;
  - private vulnerability reporting enabled in the GitHub repository.
- **Line endings:** every touched file is LF, per `.gitattributes`.
- **Fix budget:** none used. The first build and test runs were green.
- **Documentation sweep:** the callout, the notes and `SECURITY.md`, as above. Nothing else names the version.

## Build/test evidence

- Release:
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2503 passed, 0 failed, 0 skipped.
- Debug:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2503 passed, 0 failed, 0 skipped.
- **The built version**, read with the runbook's Linux recipe from `src/Enigma.TextEditor.Desktop/bin/Release/net10.0`:
  - **Core:** `1.1.0+55f01a11b523cf6b688acbe4878f2fdba92051c3`, and `FileVersion 1.1.0.0` in its Win32 resource.
  - **Desktop:** the same.
  - The commit is the run branch's tip at build time, since a build stamps `HEAD`.
- `git tag --list 1.1.0` was empty before the dev.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Notes structure | Summary, *Install on Linux*, *Upgrading from 1.0*, *Dependencies*, *Compatibility*, *Version* | 1.0.0's heading style, with GitClient's minor-release closing sections |
| *Dependencies* with nothing moved | One line naming the held-back set and why | `dotnet-release`: a held-back set is recorded; GitClient 6.4.0's wording |
| Callout | 1.1 first, then 1.0's summary | GitClient's minor-release shape keeps the previous release's line |
| `SECURITY.md` table | `1.1.x` supported, `1.0.x` listed as not supported | The house table keeps older lines, marked `:x:` (GitClient) |
| RELEASE.md's `e.g. 1.1.0` | Left as it is | An example, still true |

# FEATURE-2B84 — Release 1.2.0

**Item:** FEATURE-2B84 — Release 1.2.0
**Branch:** `feature/feature-2b84-release-1-2-0`
**Run:** feature/2026-10-21-release-1-2-0

## Summary

Version **1.2.0** is cut in the repository. It is a minor release. It carries the eight items that
`feature/2026-10-21-minimap-keys-dialogs` merged since `1.1.0`: FEATURE-4217, 3546, 2F9C, 684E, 2860, 1FBE, 7D6B and
5D8B. The application's code is unchanged by this dev.

- **Both csprojs:** `<Version>1.2.0</Version>` (was `1.1.0`). The built assemblies carry file version `1.2.0.0` and
  informational version `1.2.0+<commit>`, which *Help → About* shows.
- **`RELEASENOTES.md`:** a dated `## 1.2.0 — 2026-10-21` section above 1.1.0's, with:
  - a summary;
  - *Minimap*, *Panes and tabs*, *Vim*, *Keyboard focus* and *About*, covering the eight items;
  - *Upgrading from 1.1*: the two new settings keys, going back to 1.1, and `:q` on the last tab;
  - *Dependencies*, *Compatibility* and *Version*.
- **`README.md`:** the callout names 1.2, then sums up 1.1 and 1.0.
- **`SECURITY.md`:** `1.2.x` is supported; `1.1.x` and `1.0.x` are not.

## Files / modules touched

**Created**

- `docs/done/FEATURE-2B84.md`

**Modified**

- `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` — `<Version>1.2.0</Version>`
- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `<Version>1.2.0</Version>`
- `RELEASENOTES.md` — the 1.2.0 section
- `README.md` — the callout
- `SECURITY.md` — supported versions
- `docs/roadmap.md`, `docs/plan/FEATURE-2B84.md` — statuses

## Dependency check

`dotnet list Enigma.TextEditor.slnx package --outdated` against nuget.org:

| Package                                                                                      | Current | Latest | Action        |
|----------------------------------------------------------------------------------------------|---------|--------|---------------|
| Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent (Desktop)           | 12.1.2  | 12.1.4 | **held back** |
| Avalonia.Headless, Avalonia.Skia (Desktop.UnitTests)                                         | 12.1.2  | 12.1.4 | **held back** |
| Every other package, including Enigma.Avalonia.Desktop 1.2.0 and Enigma.Configuration 1.0.0  | —       | —      | up to date    |

**Why the set is held back:** Avalonia moves as one set, together with what Enigma.Avalonia.Desktop is built against
(`Directory.Packages.props` and `dotnet-release`). Enigma.Avalonia.Desktop has no newer version, so nothing changed.
The notes' *Dependencies* section says so.

## Deviations & follow-ups

- **BUG-4A17 is not in 1.2.0.** It is quarantined by `feature/2026-10-21-minimap-keys-dialogs`, and its fix
  (`bugfix/bug-4a17-scroll-colours`) is red and unmerged. The bug predates 1.0, so 1.2.0 is no worse than 1.1.0. The
  notes do not mention it, as 1.0 and 1.1 did not. Finish it with `/build BUG-4A17`, for a 1.2.1.
- **`docs/RELEASE.md` is unchanged.** Its runbook is generic in `X.Y.Z`. Its *e.g. `1.1.0`* is an example and still
  true. The About dialog still shows the short commit, as its verification step says (`ProductVersion` keeps 7
  characters). The 1.0.0 licence audit it points at still lists what a build ships, since no dependency moved.
- **`ProductVersionTests`' `1.1.0` strings stay.** They are sample input to the parser, not the product version.
- **The merges and the tag are the user's.** `vibe-workflow` §5 forbids `tag` and any write to `main`, and the
  request does not ask for them. The end-of-run report prints them:
  - `develop ← feature/2026-10-21-release-1-2-0`, then `main ← develop`, both `--no-ff`;
  - then a lightweight `1.2.0` on `main`.

  Nothing in this dev ran `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`.
- **The run branch includes `feature/2026-10-21-minimap-keys-dialogs`,** which is not merged into `develop` yet. Merging
  the release run branch brings both runs in.
- **Follow-ups carried from 1.1.0, unchanged:**
  - the editor does not end on SIGTERM (from FEATURE-7F58);
  - a third-party notices file before distributing a build;
  - private vulnerability reporting enabled in the GitHub repository.
- **Line endings:** every touched file is LF, per `.gitattributes`.
- **Fix budget:** none used. The first build and test runs were green.
- **Documentation sweep:** the callout, the notes and `SECURITY.md`, as above. The README's body already describes
  the eight items: each dev's own sweep updated it. Nothing else names the version.

## Build/test evidence

- Release:
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2650 passed, 0 failed, 0 skipped.
- Debug:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2650 passed, 0 failed, 0 skipped.
- **The built version**, read with the runbook's Windows recipe from
  `src/Enigma.TextEditor.Desktop/bin/Release/net10.0`:
  - **Core:** `FileVersion 1.2.0.0`, `ProductVersion 1.2.0+b6db818cb91212246be3d6d277c3b85d7458d0a9`;
  - **Desktop:** the same.
  - The commit is the run branch's planning commit, since a build stamps `HEAD`.
- `git tag --list 1.2.0` was empty before the dev.
- No test was added. The dev changes no behaviour, and `AppInfoTests` already asserts the version's shape.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Notes structure | Summary, *Minimap*, *Panes and tabs*, *Vim*, *Keyboard focus*, *About*, then *Upgrading from 1.1*, *Dependencies*, *Compatibility*, *Version* | 1.0.0's themed sections and 1.1.0's closing sections |
| Where the drop item goes | *Panes and tabs* | It changes what a pane's new tab does, not how a file is read |
| Where `g…` and `gt`/`gT` go | *Panes and tabs*, not *Vim* | 1.0.0 lists the `Ctrl+W` pane keys under *Tabs and panes*; these are their twins |
| `fileencoding` names in the notes | The canonical names only | The aliases (`utf8`, `latin9`, `windows-125x`…) are in the help; the notes stay short |
| Going back to 1.1 | Stated: 1.1 ignores the two keys, and a save from 1.1 drops them | Enigma.Configuration binds non-strictly, and `WriteAsync` replaces the file from the settings object (its README) |
| Linux upgrade line | One line in *Upgrading from 1.1* | The installer is unchanged; re-running it is how to upgrade |
| `SECURITY.md` scope | Unchanged | The new `:set` options and keys fall under "what is typed into the Vim engine"; the drop change opens files as before |

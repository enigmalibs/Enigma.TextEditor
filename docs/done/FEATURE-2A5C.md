# FEATURE-2A5C — Release 1.5.0

**Item:** FEATURE-2A5C — Release 1.5.0
**Branch:** `feature/feature-2a5c-release-1-5-0`
**Run:** feature/2026-10-23-release-1-5-0

## Summary

Version **1.5.0** is cut in the repository. It is a minor release, and it carries:

- **FEATURE-003A:** *Settings → Preferences…* (Ctrl+,) with the Text, Appearance, Editing and New files groups, and
  the editor's font;
- **BUG-7624:** the text kept clear of the scrollbars and the minimap.

The application's code is unchanged by this dev.

- **Both csprojs:** `<Version>1.5.0</Version>` (was `1.4.0`). The built assemblies carry file version `1.5.0.0` and
  informational version `1.5.0+<commit>`, which *Help → About* shows.
- **`RELEASENOTES.md`:** a dated `## 1.5.0 — 2026-10-23` section above 1.4.0's, with:
  - a summary;
  - *Preferences*, *Fonts* and *Editor*;
  - *Upgrading from 1.4*: the `EditorFontFamily` key, going back to 1.4, the scrollbars, Linux;
  - *Dependencies*, *Compatibility* and *Version*.
- **`README.md`:** the callout names 1.5, then sums up 1.4, 1.3, 1.2, 1.1 and 1.0.
- **`SECURITY.md`:** `1.5.x` is supported; `1.4.x` down to `1.0.x` are not.

## Files / modules touched

**Created**

- `docs/done/FEATURE-2A5C.md`

**Modified**

- `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` — `<Version>1.5.0</Version>`
- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `<Version>1.5.0</Version>`
- `RELEASENOTES.md` — the 1.5.0 section
- `README.md` — the callout
- `SECURITY.md` — supported versions
- `docs/roadmap.md`, `docs/plan/FEATURE-2A5C.md` — statuses

## Dependency check

`dotnet list Enigma.TextEditor.slnx package --outdated` against nuget.org:

| Package                                                                            | Current | Latest | Action        |
|------------------------------------------------------------------------------------|---------|--------|---------------|
| Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent (Desktop) | 12.1.2  | 12.1.4 | **held back** |
| Avalonia.Headless, Avalonia.Skia (Desktop.UnitTests)                               | 12.1.2  | 12.1.4 | **held back** |
| Every other package, including Enigma.Avalonia.Desktop 1.2.0                       | —       | —      | up to date    |

The set moves as a whole, with what Enigma.Avalonia.Desktop is built against, and Enigma.Avalonia.Desktop has no
newer version. Nothing changed, as at 1.4.0, and the notes' *Dependencies* section says so.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Notes structure | Summary, *Preferences*, *Fonts*, *Editor*, then *Upgrading from 1.4*, *Dependencies*, *Compatibility*, *Version* | 1.2.0's shape for a release with several themes; one section per thing a user will see |
| The new settings key | Told as 1.2.0 told `ShowMinimap` and `PaneResizeStep`: what a 1.4 file starts with, and what going back to 1.4 does | The same mechanism; `SettingsLoaderTests.AnUnknownKey_IsIgnoredRatherThanTreatedAsBroken` shows 1.4 ignores the key |
| What the default font is | Named from `EditorPreferences.MonospaceStack`: Cascadia Mono, Consolas, JetBrains Mono, DejaVu Sans Mono, Menlo, Courier New | The notes say what *Default* means without pointing at code |
| BUG-7624's visible change | Under *Editor*, and again under *Upgrading from 1.4*, as the one behaviour change | The always-visible bar with the minimap off is what a 1.4 user will notice |
| *Version* wording | "Adds features, keeps every stored format, and removes nothing. One behaviour changes" | 1.2.0's and 1.3.0's wording for a feature release; the new key is an addition, not a format change |
| Callout | 1.5 first, then 1.4's swap summed up, then 1.3, 1.2, 1.1, 1.0 unchanged | The 1.1.0–1.4.0 precedent |
| `SECURITY.md` scope | Unchanged | The new key falls under "the way it reads and writes its settings file". The font probe reads the machine's installed faces, not a file the editor is given |
| `docs/RELEASE.md` | Unchanged | Its steps are generic in `X.Y.Z`. Its *e.g. `1.1.0`* is an example and still true |

## Deviations & follow-ups

- **None from the plan.**
- **BUG-4A17 is not in 1.5.0.** It is still quarantined. Its branch, `bugfix/bug-4a17-scroll-colours`, is no longer
  in the local repository, so `/build BUG-4A17` would start it again from its plan.
- **Merging.** The run branch is cut from `bugfix/2026-10-23-minimap-preferences-fonts`, which has not reached
  `develop`. Merging this run branch into `develop` brings BUG-7624, FEATURE-003A and 1.5.0 together; `develop` into
  `main` then carries 1.5.0 alone after 1.4.0's merge (`b15366f`). Nothing here ran `git tag`, `git push`,
  `dotnet pack` or `dotnet nuget push`, or wrote to `develop` or `main`.
- **README, cosmetic:** line 89 (*Settings*) runs past the 120-column wrap the rest of the file keeps. FEATURE-003A
  PHASE02's sweep left it so. It is not wrong, so this release does not touch it.
- **Follow-ups carried from 1.4.0, unchanged:**
  - the editor does not end on SIGTERM;
  - a third-party notices file before distributing a build;
  - private vulnerability reporting enabled in the GitHub repository.
- **Line endings:** every touched file is LF, per `.gitattributes`.
- **Fix budget:** none used. The first build and test runs were green.
- **Documentation sweep:** the callout, the notes and `SECURITY.md`, as above. BUG-7624 and FEATURE-003A's own sweeps
  already brought the README's *Settings* paragraph up to date. Nothing else names the version.

## Build/test evidence

- Release:
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2706 passed, 0 failed, 0 skipped.
- Debug:
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
  - `dotnet test --solution Enigma.TextEditor.slnx`: 2706 passed, 0 failed, 0 skipped.
- **The built version**, read with the runbook's Windows recipe from
  `src/Enigma.TextEditor.Desktop/bin/Release/net10.0`:
  - **Core:** `FileVersion 1.5.0.0`, `ProductVersion 1.5.0+ce1cea5b49d018e148f44e9d8211fd85a51c63be`;
  - **Desktop:** the same.
  - The commit is the run branch's planning commit, since a build stamps `HEAD`.
- `git tag --list 1.5.0` was empty before the dev.
- No test was added. The dev changes no behaviour, and `AppInfoTests` already asserts the version's shape.

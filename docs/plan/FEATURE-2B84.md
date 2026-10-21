# FEATURE-2B84 — Release 1.2.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-2b84-release-1-2-0`
**Run:** feature/2026-10-21-release-1-2-0

## Objective

Cut **1.2.0**, with FEATURE-5301 (1.1.0) as the template and following `dotnet-release`'s routine-release path:

- the version;
- the release notes for what 1.2.0 adds;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main` and the `1.2.0` tag are the user's: the run prints them and runs none of them.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so `dotnet-release`'s *App vs library* rule applies:
  - the version, the notes, the callout and the tag apply;
  - pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.1.0` today. Their comments say the two move together.
  - `AppInfoTests` asserts the shape, not a literal. `ProductVersionTests`' `1.1.0` strings are sample input, not
    the product version, and stay.
  - *Help → About* shows the version.
- **1.2.0 carries what `feature/2026-10-21-minimap-keys-dialogs` merged since `1.1.0`**, all `DONE`:

  | Item         | What a user sees                                                                  |
  |--------------|-----------------------------------------------------------------------------------|
  | FEATURE-4217 | A file dropped on a new, empty tab takes its place                                |
  | FEATURE-3546 | A minimap beside the text, and *Settings → Minimap* to swap it for the scrollbar  |
  | FEATURE-2F9C | `:set` for the status bar items: `et`/`sw`/`ts`, `ff`, `fenc`, `bomb`, `ft`       |
  | FEATURE-684E | `:q` on the last tab closes the tab and keeps the application open                |
  | FEATURE-2860 | `g=`, `g+`, `g-`, `g>`, `g<`, `gt`, `gT`; *Settings → Pane Resize Step*           |
  | FEATURE-1FBE | Dialogs take the keyboard when they open (Tab, Enter, Esc) and give it back       |
  | FEATURE-7D6B | The status bar buttons leave the keyboard in the pane                             |
  | FEATURE-5D8B | The About dialog has Enigma.GitClient's look                                      |

  **BUG-4A17** (colours missing after a scroll) is quarantined and its fix is unmerged, so it is not in 1.2.0.
- **Semantic Versioning:** a minor release, new and backward compatible.
  - Two settings keys are new, `ShowMinimap` and `PaneResizeStep`. A 1.1 `settings.json` lacks them and loads with
    their defaults.
  - Enigma.Configuration binds non-strictly, so 1.1 reads a 1.2 file and ignores the two keys. Its `WriteAsync`
    replaces the file from the settings object, so a save from 1.1 drops them.
  - `:q` on the last tab no longer quits. `:qa`, File → Exit and the window's close button do.
- `SECURITY.md` supports the latest minor line only, today `1.1.x`.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`):
  - Avalonia, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia
    are one coupled set, bumped together or not at all;
  - AvaloniaEdit and AvaloniaEdit.TextMate are pinned to what that set ships with.
  - At planning time, the only updates on nuget.org are Avalonia 12.1.2 → 12.1.4 (the six set members).
    Enigma.Avalonia.Desktop has not moved.
- **Tags** are bare and lightweight (`1.0.0`, `1.1.0`), on `main`'s `Merge branch 'develop'` commit.
- `docs/RELEASE.md` is the runbook. It names `X.Y.Z` generically.

## Steps

1. Check that `git tag --list 1.2.0` is empty.
2. Both csprojs: `<Version>1.2.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.2.0 — 2026-10-21` section above 1.1.0's, in the repository's style:
   - a one-paragraph summary;
   - themed sections for the eight items above;
   - *Upgrading from 1.1*: the settings keys, going back to 1.1, and `:q` on the last tab;
   - *Dependencies*: every `old → new`, or the coupled set held back;
   - *Compatibility*;
   - *Version*.
4. `README.md` — the callout names 1.2, then sums up 1.1 and 1.0 after it. Anything in the body 1.2.0 makes untrue
   is fixed.
5. `SECURITY.md` — supported versions: `1.2.x` yes; `1.1.x` and `1.0.x` no. The scope changes only if 1.2.0 adds
   an attack surface.
6. `docs/RELEASE.md` — anything 1.2.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.2.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.2.0`: file version `1.2.0.0`, informational version
  `1.2.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.2.0` section above 1.1.0's, covering the eight items, and not BUG-4A17.
- The README callout names 1.2. `SECURITY.md` supports `1.2.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, or writes to `develop` or
  `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour, BUG-4A17 included.
- The merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; FEATURE-5301's precedent | Several phases |
| Version number | 1.2.0, minor | Given by the request; new backward-compatible capability, nothing stored breaks | — |
| What 1.2.0 contains | The eight `DONE` items merged since `1.1.0`; BUG-4A17 left out | Its fix is red and unmerged. The bug predates 1.0, so leaving it out is no regression | Holding the release for BUG-4A17; merging its red branch |
| BUG-4A17 in the notes | Not mentioned | 1.0 and 1.1 did not list it either; it is tracked on the roadmap | A *Known issues* section |
| `:q` on the last tab | Under *Upgrading from 1.1*, as a changed behaviour, in a minor release | An application with no API; it follows Vim, and every way to quit remains | Calling it a breaking change and cutting 2.0.0 |
| Notes shape | 1.1.0's `## X.Y.Z — date`, summary, themed sections, then *Upgrading*, *Dependencies*, *Compatibility*, *Version* | The repository's own style wins over the template's | The template's *New Features · Fixes* headings |
| Callout | 1.2 first, then 1.1 and 1.0 summed up | GitClient's minor-release shape keeps the previous lines, as 1.1.0 did | Dropping the earlier summaries |
| `SECURITY.md` | `1.2.x` only; `1.1.x` and `1.0.x` listed as not supported; scope unchanged | Latest-minor-only policy; the new `:set` options and keys fall under "what is typed into the Vim engine" | Keeping `1.1.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; 1.1.0's precedent | Bumping Avalonia alone |
| MSI profile | None | No Windows installer is asked for; the 1.0.0 and 1.1.0 precedent | Generating a first profile |
| Merges and tag | Printed in the end-of-run report for the user to run | `vibe-workflow` §5 forbids `tag` and any write to `main`, and the request does not ask for them | Running them as 1.1.0's run did |

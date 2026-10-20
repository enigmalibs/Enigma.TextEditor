# FEATURE-5301 — Release 1.1.0

**Status:** DONE — see `docs/done/FEATURE-5301.md`
**Type:** FEATURE
**Branch:** `feature/feature-5301-release-1-1-0`
**Run:** feature/2026-10-20-linux-install-release

## Objective

Cut **1.1.0**, with FEATURE-8F24 (1.0.0) as the template and following `dotnet-release`'s routine-release path:

- the version;
- the release notes for what 1.1.0 adds;
- the README callout;
- `SECURITY.md`;
- `docs/RELEASE.md`;
- a dependency check;
- a Release pre-flight.

Once this dev is done, the run branch is merged into `develop`, `develop` into `main`, and `main` is tagged
`1.1.0`, as the request asks. Nothing is pushed.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so `dotnet-release`'s *App vs library* rule applies:
  - the version, the notes, the callout and the tag apply;
  - pack, push and `PackageReleaseNotes` do not.
- **The version** is `<Version>` in both csprojs, `1.0.0` today. Their comments say the two move together.
  - `AppInfoTests` asserts the shape, not a literal.
  - *Help → About* shows the version.
- **1.1.0 carries one item:** FEATURE-7F58, the Linux installer. It must be `DONE` by the time this dev starts.
  - If it is quarantined, this dev builds against what is merged, and records it.
  - Nothing else has changed since `1.0.0`.
- **Semantic Versioning:** a minor release, new and backward compatible.
  - The application's code does not change.
  - Settings and logs stay where they are, in the same format.
- `SECURITY.md` supports the latest minor line only, today `1.0.x`. Its scope lists what is sensitive in the
  application.
- **The dependency rule** (`Directory.Packages.props` and `dotnet-release`):
  - Avalonia, its companions, `AvaloniaUI.DiagnosticsSupport`, Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia
    are one coupled set, bumped together or not at all;
  - AvaloniaEdit and AvaloniaEdit.TextMate are pinned to what that set ships with.
- **Tags** are bare and lightweight (`1.0.0`), on `main`'s `Merge branch 'develop'` commit.
- `docs/RELEASE.md` is the runbook. It names `X.Y.Z` generically and says why there is no MSI.

## Steps

1. Check that `git tag --list 1.1.0` is empty.
2. Both csprojs: `<Version>1.1.0</Version>`.
3. `RELEASENOTES.md` — a dated `## 1.1.0 — 2026-10-20` section above 1.0.0's:
   - a one-paragraph summary;
   - *Install on Linux*: what the installer does, its options, *Open With*, and the uninstaller;
   - *Dependencies*, when anything moved: every `old → new`, and the coupled set held back;
   - *Compatibility*;
   - *Version*.
4. `README.md` — the callout names 1.1 and sums up 1.0 after it.
5. `SECURITY.md`:
   - supported versions: `1.1.x` yes, `1.0.x` no;
   - the scope adds the Linux install and uninstall scripts.
6. `docs/RELEASE.md` — anything 1.1.0 makes untrue is fixed. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - non-coupled patch and minor bumps are applied;
   - the coupled Avalonia set is held back unless Enigma.Avalonia.Desktop moved with it;
   - every change is logged.
8. **Pre-flight in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.1.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.1.0`: file version `1.1.0.0`, informational version
  `1.1.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.1.0` section above 1.0.0's, covering FEATURE-7F58.
- The README callout names 1.1. `SECURITY.md` supports `1.1.x` only.
- Every dependency change is logged in the notes, or the notes say nothing moved.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`.

## Out of scope

- NuGet packaging; an MSI profile.
- Moving the Avalonia set on its own.
- Any change to the application's behaviour.
- Pushing anything to `origin`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One single-phase FEATURE after FEATURE-7F58 | `dotnet-release`: a routine release is one phase; the installer is its own feature | One multi-phase item covering both |
| Version number | 1.1.0, minor | Given by the request; new backward-compatible capability, nothing stored changes | — |
| Notes shape | 1.0.0's `## X.Y.Z — date` with themed sub-sections, as a short minor release | The repository's own style | The template's `<Name> vX.Y.Z Release Notes` heading |
| Callout | Names 1.1, then sums up 1.0 | GitClient's minor-release shape keeps the previous summary | Dropping 1.0's summary |
| `SECURITY.md` | `1.1.x` only, scope plus the install scripts | Latest-minor-only policy; the scripts write to and remove from `$HOME` | Keeping `1.0.x` supported |
| Dependency refresh | Non-coupled bumps applied, coupled set held unless Enigma.Avalonia.Desktop moved, all logged | `dotnet-release` routine release; GitClient 6.4.0 | Skipping it, as the first release did; bumping Avalonia alone |
| MSI profile | None | No Windows installer is asked for; the 1.0.0 precedent | Generating a first profile |
| Merges and tag | After the run, when every run item is `DONE`: `develop ← run branch` (`--no-ff`), `main ← develop` (`--no-ff`), lightweight `1.1.0` on `main`. Not done if anything is quarantined | The request asks for them explicitly, as for 1.0.0. `--no-ff` and the default messages match the history; the bare lightweight tag matches `1.0.0` | Leaving them to the user (the run's default, overridden by the request); an annotated or `v`-prefixed tag; pushing |

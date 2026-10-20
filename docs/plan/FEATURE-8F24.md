# FEATURE-8F24 — Release 1.0.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-8f24-<phase>-<slug>` (one per phase)
**Run:** feature/2026-10-20-release-1-0-0

## Objective

Cut version **1.0.0** of Enigma Text Editor:

- the version stamped in the build;
- a README that says what the application is and does;
- dated, complete release notes;
- a release runbook in `docs/RELEASE.md`;
- a `SECURITY.md`;
- the first-release runtime licence audit.

Once both phases are done, the run branch is merged into `develop`, `develop` into `main`, and `main`
is tagged `1.0.0`, as the request asks. Nothing is pushed.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`. `dotnet-release`'s *App vs library* rule
  applies:
  - the version bump, the release notes, the README callout and the tag apply;
  - `dotnet pack`, `dotnet nuget push` and the 12 packable-library properties do not.
- Both csprojs carry `<Version>0.1.0</Version>`. Their comments say the two move together: Core ships
  only inside the Desktop app.
  - `AppInfo.Version` reads the version from Core's assembly.
  - The About dialog (*Help → About Enigma Text Editor*) shows it with the build's short commit.
  - `AppInfoTests` asserts the version's shape, not a literal, so a bump needs no test edit.
- `README.md` is one title line. `RELEASENOTES.md` exists and is empty. There is no `SECURITY.md` and no
  `docs/RELEASE.md`.
- **House precedent: Enigma.Launchpad's 1.0.0 (FEATURE-10CE).**
  - The README describes the app in prose: intro, what's-new callout, features, *Your data*,
    *Requirements*, *Build, run, test*, *Solution layout*, *License*.
  - The notes are `# Release notes`, then `## X.Y.Z — <date>` with themed sub-sections, *Compatibility*
    and *Version*.
  - The runbook covers: pre-flight in Release → read the version off the build → merge into `develop`,
    then `main` → tag → push → publish.
  - Tags are bare and lightweight, on `main`'s `Merge branch 'develop'` commit.
- **Repository state:**
  - There are no tags. `origin` is `github.com/enigmalibs/Enigma.TextEditor`, and `origin/HEAD` is `main`.
  - Work is integrated on `develop` by `--no-ff` merges of run branches.
  - `main` (`8021b2c`, `Merge branch 'develop'`) is not an ancestor of `develop`, but `develop` merges
    into it cleanly.
- The app has no installer (`packaging/` holds only the icon sources). It is built and run from source,
  or published with `dotnet publish`.
- Data lives under the per-user application-data folder `Enigma.TextEditor`:
  - `settings.json`, with an unreadable one moved aside to `settings.invalid.json`;
  - `logs/app.log`.

## Decisions

| Decision | Rationale |
|---|---|
| Two phases: the README and `SECURITY.md` first, then the version, the notes and the runbook | One reviewable commit each. The README is written from nothing, and the callout pointing at the notes lands with the notes |
| The version stays in both csprojs, both set to `1.0.0` | The existing, commented structure; a release is not the place to move it |
| Launchpad's app shapes for the README, the notes and the runbook | The house precedent for an application |
| No badges in the README | Not a NuGet package; Launchpad, the app precedent, has none |
| No MSI profile, no Linux installer | Packaging infrastructure nobody asked for. The runbook says how to publish and why there is no installer |
| No dependency refresh | First release. The coupled Avalonia set is pinned to what AvaloniaEdit 12.0.0 and Enigma.Avalonia.Desktop 1.2.0 ship with, and unrelated churn does not belong in a release commit |
| `SECURITY.md` from the `dotnet-release` template | The repository is public, and the editor parses any file it is given, plus its own settings file |
| Tag bare and lightweight `1.0.0`, on `main`'s merge commit | No tag to match in this repository; it matches every sibling app's tags |

## PHASE01 — README & SECURITY.md

**Status:** TODO
**Branch:** `feature/feature-8f24-phase01-readme-security`

### Steps

1. `README.md` — the app README in Launchpad's shape, written from the code and the completion records:
   - the intro: a cross-platform Avalonia 12 text editor with Vim bindings;
   - the features in prose:
     - tabs and split panes;
     - the Vim engine (modes, operators, text objects, registers, macros, search highlight, ex
       commands, window commands);
     - files: encodings and byte-order marks, line endings, indentation, the 32 MB limit, unsaved
       changes, the command line, drag and drop;
     - syntax highlighting and language;
     - the read-only hex viewer;
     - the menus, the context menu and the Vim hints;
     - Settings and Help;
   - *Your data*: the settings file and the logs, per platform, and what happens to an unreadable
     settings file;
   - *Requirements*, *Build, run, test*, *Solution layout*, *License*.
   - No what's-new callout yet: it lands with the release notes in PHASE02.
2. `SECURITY.md` — from the `dotnet-release` template. The scope names what is sensitive here:
   - reading any file it is given, including binary sniffing, decoding and the hex viewer;
   - writing files back in their encoding;
   - the settings file and the logs;
   - the syntax grammars it runs over a file's text.

### Acceptance criteria

- The README describes only what the application does today. Every key, menu item and ex command it
  names exists in the code (`HelpCatalog`, `MainWindow.axaml`, `ExCommandParser`).
- The README links only to repository files that exist (`LICENSE.md`, `RELEASENOTES.md`), with no
  absolute GitHub URLs into this repository.
- `SECURITY.md` exists, names the latest version line as supported, and its scope fits this
  application.
- `dotnet build Enigma.TextEditor.slnx` is clean (zero warnings) and the whole suite is green.

## PHASE02 — Version 1.0.0, notes & runbook

**Status:** TODO
**Branch:** `feature/feature-8f24-phase02-version-notes`

### Steps

1. Both csprojs: `<Version>1.0.0</Version>`.
2. `RELEASENOTES.md` — `# Release notes`, then `## 1.0.0 — 2026-10-20`:
   - a positioning paragraph;
   - themed sections covering what the application does today;
   - *Compatibility*: Windows, Linux, macOS; .NET 10; settings from the 0.1.0 development builds load
     as they are;
   - *Version*.
3. `README.md` — the *What's new in 1.0* callout under the intro.
4. `docs/RELEASE.md` — the `dotnet-release` runbook adapted to an app, after Launchpad's:
   - pre-flight build and test in Release;
   - reading the version off the build, and in *Help → About*;
   - merging into `develop`, then `main`; tagging `X.Y.Z`; pushing;
   - publishing with `dotnet publish -r <rid>`;
   - post-release checks;
   - why there is no NuGet step and no installer.
5. The runtime licence audit, recorded in the completion doc. It covers every package the published
   application redistributes, each licence read from the package itself.

### Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` is green. The same holds in Debug.
- The built Core and Desktop assemblies carry `1.0.0`: assembly version `1.0.0.0`, informational
  version `1.0.0+<commit>`.
- `RELEASENOTES.md` carries one dated `1.0.0` section, and the README has the callout.
- `docs/RELEASE.md` exists, and every command in it names this repository's real paths.
- The licence audit finds every redistributed component's licence compatible with redistribution, or
  names the one that is not.
- Nothing in the dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`.

## Out of scope

- NuGet packaging and publishing.
- An MSI profile or any installer.
- A dependency refresh.
- Any change to the application's behaviour.
- Pushing anything to `origin`.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Work-item shape | One FEATURE with two phases | A release is a FEATURE (`dotnet-release`). The README from nothing plus the notes, runbook and audit would be one oversized review | One single-phase dev; the four-phase library shape, which is about package metadata, guides and pack-verify an app does not have |
| Where the version lives | Both csprojs, as today | Their comments document that the two move together | Moving it to `Directory.Build.props`, as Launchpad does |
| README and notes shape | Launchpad's app shape | The house precedent for an application | The library template, with badges, a quick start and guides |
| Per-category guides | None | They describe a library's API surface. The app's guide is its own *Help → Key Bindings* dialog, and the README describes the features | A `docs/guides/` set |
| MSI profile | None | No Windows installer is asked for or exists. `docs/RELEASE.md` says so | Generating a first profile |
| Linux installer | None | New infrastructure; the request is a release, not packaging | Porting Launchpad's `packaging/linux` |
| Dependency refresh | None | First release; coupled set pinned; no churn in a release commit | Bumping the non-coupled packages |
| `SECURITY.md` | Added | Public repository; the app reads arbitrary files | Skipping it |
| Merges and tag | After the run, when both phases are `DONE`: `develop ← run branch` (`--no-ff`), `main ← develop` (`--no-ff`), lightweight `1.0.0` on `main`. Not done if a phase is quarantined | The request asks for them explicitly. `--no-ff` and the default messages match the repository's history. A lightweight bare tag matches every sibling app | Leaving them to the user (the run's default, overridden by the request); an annotated or `v`-prefixed tag; pushing |

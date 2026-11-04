# FEATURE-97A6 — Release 1.16.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-97a6-release-1-16-0`
**Run:** bugfix/2026-11-04-bug-375b

## Objective

Cut **1.16.0** on `dotnet-release`'s routine-release path for an app, with FEATURE-1B68 (1.15.0) as the template:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.16.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check.

The merges into `develop` and `main` and the `1.16.0` tag are asked for by the user in this session. Any push and the
MSI built for distribution are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.15.0` today, and the two move together. Nothing in `src/` or `tests/`
  hard-codes it.
- **The version is the user's:** 1.16.0. Tag `1.15.0` is on `main` (`cc6517b`), and `1.16.0` does not exist.
- **`buildmsi.ps1` holds no version literal:** it reads `<Version>` from the Desktop csproj and stops when Core's
  says otherwise, so the csproj bump is what it builds.
- **1.16.0 carries BUG-375B**, the only item since 1.15.0 (`git log 1.15.0..HEAD`): with vim on, `Ctrl+Z` in insert
  mode, the `Esc` after it, redo, the Edit and context menus' Undo/Redo, and *Reopen with encoding* in insert mode no
  longer crash.
- **1.15.0's notes are history**, and are not edited.
- **No settings key, no stored format** changes.
- **BUG-4A17** is still quarantined from an earlier run, so the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.15.x`.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia.
- **Tag convention:** bare `X.Y.Z`, lightweight, on `main`'s merge commit.
- **This session runs no tests** (the user's instruction), and runs on Linux, where `buildmsi` (PowerShell, WixSharp,
  net472 worker) cannot run.

## Steps

1. Check that `git tag --list 1.16.0` is empty.
2. Both csprojs: `<Version>1.16.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.16.0 — 2026-11-04` section above 1.15.0's, in the repository's style: a summary,
   *Undo and redo with vim on* (BUG-375B), *Upgrading from 1.15*, *Dependencies*, *Compatibility*, *Known issue*
   (BUG-4A17, by its symptom), *Version*.
4. `README.md`: the callout names 1.16 first, then 1.15 down to 1.0. Only the lines this release touches are
   reflowed.
5. `SECURITY.md`: `1.16.x` is supported, and `1.15.x` and earlier are not.
6. `docs/RELEASE.md`: fix anything 1.16.0 makes untrue. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`; log the result in the notes.
8. A Release build with zero warnings whose assemblies carry `1.16.0`.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and the built assemblies carry
  `1.16.0`.
- `RELEASENOTES.md` has one dated `1.16.0` section above 1.15.0's that covers BUG-375B, logs the dependency check and
  keeps the BUG-4A17 known issue.
- The README callout names 1.16, and `SECURITY.md` supports `1.16.x` only.

## Out of scope

- NuGet packaging; a committed MSI profile.
- Building the 1.16.0 MSI for distribution, and any push.
- Any change to the application's behaviour.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.16.0 | The user's choice in this session, and the house rule `x.y+1.z` | 1.15.1 |
| Work-item shape | One single-phase FEATURE, after BUG-375B | `dotnet-release`: a routine release is one phase; the notes describe the fix | Folding the bump into BUG-375B |
| Notes shape | 1.15.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Pre-flight | A Release build only; no tests, no `buildmsi` | The user asked for no tests this session; `buildmsi` needs Windows. BUG-375B's done doc carries the suite's evidence | Running the suite; skipping the build |
| Release date | 2026-11-04 | The run date | A future date |

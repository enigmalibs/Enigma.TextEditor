# FEATURE-8AEF — Release 1.17.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-8aef-release-1-17-0`
**Run:** feature/2026-11-06-release-1-17-0

## Objective

Cut **1.17.0**, the `x.y+1.z` bump of 1.16.0. Use `dotnet-release`'s routine-release path for an app, with
FEATURE-97A6 (1.16.0) as the template. The release covers:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.17.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check.

The merges into `develop` and `main`, the `1.17.0` tag, any push and the MSI built for distribution are printed for
the user to run, not run here.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.16.0` today, and the two move together. Nothing in `src/` or `tests/`
  hard-codes it.
- **Tags:** `1.16.0` is on `main` (`f830e8e`), and `1.17.0` does not exist. Tags are bare `X.Y.Z` and lightweight, on
  `main`'s merge commit.
- **`buildmsi.ps1` holds no version literal.** It reads `<Version>` from the Desktop csproj and stops when Core's says
  otherwise, so the csproj bump is what it builds.
- **1.17.0 carries two items since 1.16.0** (`git log 1.16.0..HEAD`), both with vim on:
  - **BUG-B026, find and replace:**
    - `Ctrl+F` / `Ctrl+H` open the panel in every mode (`Ctrl+F` no longer pages down; `PgDn` does);
    - the panel's fields take typing, `Backspace`, `Enter` and `Esc` in every mode;
    - opening the panel drops a half-typed command, cancels a `:` / `/` line and leaves a visual mode;
    - `Esc` from the text closes the panel in idle normal mode;
    - in replace mode, `Tab` goes from the find field to the replace field and `Shift+Tab` back, with vim on or off;
    - Help → Key Bindings lists the keys.
  - **BUG-47C2, mouse in normal mode:**
    - a click past a line's end puts the block on its last character, and a click ends a visual mode;
    - a double-click selects the word in visual mode and a triple-click the line in V-LINE;
    - insert mode and vim off are unchanged.
- **1.16.0's notes are history**, and are not edited.
- **No settings key, no stored format** changes.
- **BUG-4A17** is still quarantined from an earlier run, so the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.16.x`.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia.
- **A run may not run `buildmsi.ps1`.** It starts with `git clean -dxf`, which `vibe-workflow` §5 forbids.

## Steps

1. Check that `git tag --list 1.17.0` is empty.
2. Both csprojs: `<Version>1.17.0</Version>`.
3. `RELEASENOTES.md`: add a dated `## 1.17.0 — 2026-11-06` section above 1.16.0's, in the repository's style:
   - a summary;
   - *Find and replace with vim on* (BUG-B026);
   - *The mouse with vim on* (BUG-47C2);
   - *Upgrading from 1.16*, *Dependencies*, *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
4. `README.md`: the callout names 1.17 first, then 1.16, then 1.15 down to 1.0. Reflow only the lines this release
   touches.
5. `SECURITY.md`: `1.17.x` is supported, and `1.16.x` and earlier are not.
6. `docs/RELEASE.md`: fix anything 1.17.0 makes untrue. Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`. Apply any bump outside the coupled set. Move the
   Avalonia set only as a whole, and only if Enigma.Avalonia.Desktop moved. Log the result in the notes.
8. Pre-flight: a Release build with zero warnings whose assemblies carry `1.17.0`, and the Release suite measured
   against the run's baseline.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and the built assemblies carry
  `1.17.0`.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release` has no failure the run's baseline did not have.
- `RELEASENOTES.md` has one dated `1.17.0` section above 1.16.0's. It covers BUG-B026 and BUG-47C2, logs the
  dependency check and keeps the BUG-4A17 known issue.
- The README callout names 1.17, and `SECURITY.md` supports `1.17.x` only.

## Out of scope

- NuGet packaging; a committed MSI profile.
- Merging into `develop` / `main`, tagging, pushing, and building the 1.17.0 MSI for distribution: these are printed
  for the user.
- Any change to the application's behaviour.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.17.0 | The draft's `x.y+1.z` applied to 1.16.0 | 1.16.1 (a patch); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Several phases (notes, version, docs): too small to split |
| Notes shape | 1.16.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Release date | 2026-11-06 | The run date | A future date |
| Merges, tag, push | Printed in the end-of-run report, not run | `vibe-workflow` §5 forbids writing to `develop`/`main`, `tag` and `push` | Running them, as 1.16.0's session did at the user's request |
| `buildmsi.ps1` | Not run; left to the user before distributing | It runs `git clean -dxf`, which §5 forbids, and it would delete untracked files | Running it |
| Pre-flight | A Release build and the Release suite, measured against a Release baseline taken on the run branch | `docs/RELEASE.md` §1 asks for both in Release. The baseline's configuration must match the dev's | Debug only (not what ships); build only (1.16.0's no-tests choice was that session's instruction) |
| Dependency policy | Apply bumps outside the coupled set. Move the Avalonia set only as a whole and with Enigma.Avalonia.Desktop | `dotnet-release` and the rule in `Directory.Packages.props` | Bumping Avalonia alone |
| `SECURITY.md` scope | Unchanged | The panel is AvaloniaEdit's own, and vim's input handling is already in scope. Neither item adds a surface | A new scope bullet for the find panel |

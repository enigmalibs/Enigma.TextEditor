# FEATURE-EF88 — Release 1.18.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-ef88-release-1-18-0`
**Run:** feature/2026-11-09-release-1-18-0

## Objective

Cut **1.18.0**, the minor bump of 1.17.0. Use `dotnet-release`'s routine-release path for an app, with FEATURE-8AEF
(1.17.0) as the template. The release covers:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.18.0 changes;
- the README callout;
- `SECURITY.md`, its supported versions and its scope;
- a dependency check.

The merges into `develop` and `main`, the `1.18.0` tag, any push and the MSI built for distribution are printed for
the user to run, not run here.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.17.0` today, and the two move together. Nothing in `src/` or `tests/`
  hard-codes it.
- **Tags:** `1.17.0` is on `main` (`0fbad94`), and `1.18.0` does not exist. Tags are bare `X.Y.Z` and lightweight, on
  `main`'s merge commit.
- **`buildmsi.ps1` holds no version literal.** It reads `<Version>` from the Desktop csproj and stops when Core's says
  otherwise, so the csproj bump is what it builds.
- **1.18.0 carries two items since 1.17.0** (`git log 1.17.0..HEAD`), both from run
  `bugfix/2026-11-09-ctrl-y-session-restore`, which is not yet merged into `develop`:
  - **BUG-C6A9, `Ctrl+Y`:**
    - with vim on, `Ctrl+Y` is the redo in every mode, as the Edit menu and the context menu say. It redoes in
      normal mode, leaves a visual selection and then redoes, and redoes inside insert mode;
    - a recording writes it as the redo;
    - vim's scroll of one line up is given up, and `Ctrl+E` still scrolls down;
    - with vim off, nothing changes: AvaloniaEdit already redid on `Ctrl+Y`;
    - Help → Key Bindings: *Scrolling* lists `Ctrl+E` alone, and *Editing* has `Ctrl+Z, Ctrl+Y` "Undo, redo, as the
      Edit menu does".
  - **FEATURE-34A1, the session:**
    - quitting keeps the tabs, the splits, each tab's caret and scroll position, and every unsaved change, untitled
      documents included. The next start brings them back, and files named on the command line open after them in
      the active pane;
    - a document with no unsaved change is read again from its file, so edits made outside show. A document with
      unsaved changes comes back with the kept text, marked unsaved, in its encoding, line endings and indentation,
      even if its file is gone;
    - a file that cannot be read back is skipped and named once on the status line, and a pane left empty closes;
    - quitting no longer asks about unsaved changes. Closing a tab still asks;
    - `:qa!` keeps the tabs but throws the changes away;
    - the first window owns the session through `session.lock`. A second window starts empty and asks as before when
      it closes. If the session cannot be written, quitting asks as before;
    - the session is written as the window closes, so a crash brings back the last close's session;
    - a session file that cannot be used is moved to `session.invalid.json` and logged;
    - Help: the `:qa` and `:qa!` rows are reworded.
  - The README's feature text and *Your data* section already describe the session (FEATURE-34A1-PHASE04).
- **1.17.0's notes are history**, and are not edited, nor are 1.16.0's ("`Ctrl+Y` still scrolls the view up a line").
- **No settings key changes.** Two files are new in the app-data folder: `session.json` and `session.lock`. 1.17 does
  not read them.
- **BUG-4A17** is still quarantined from an earlier run, so the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.17.x`. Its scope lists the settings file and the log
  among the files the editor reads and writes, but not the session file.
- **`docs/RELEASE.md` §5** says uninstalling leaves `%APPDATA%\Enigma.TextEditor` "(the settings, the log and
  `instance.json`)" alone. That list now lacks the session files.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia. On 2026-11-09, `dotnet list package --outdated` shows the
  Avalonia set (six packages) at 12.1.6 and `xunit.v3` at 4.0.3. Enigma.Avalonia.Desktop has no update.
- **A run may not run `buildmsi.ps1`.** It starts with `git clean -dxf`, which `vibe-workflow` §5 forbids.

## Steps

1. Check that `git tag --list 1.18.0` is empty.
2. Both csprojs: `<Version>1.18.0</Version>`.
3. `RELEASENOTES.md`: add a dated `## 1.18.0 — 2026-11-09` section above 1.17.0's, in the repository's style:
   - a summary;
   - *The session comes back* (FEATURE-34A1);
   - *`Ctrl+Y` redoes with vim on* (BUG-C6A9);
   - *Upgrading from 1.17*: quitting no longer asks, `Ctrl+Y` no longer scrolls, the new files, what going back to 1.17
     does, the MSI name and the Linux upgrade;
   - *Dependencies*, *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
4. `README.md`: the callout names 1.18 first, then 1.17, then 1.16 down to 1.0. Reflow only the lines this release
   touches.
5. `SECURITY.md`: `1.18.x` is supported, and `1.17.x` and earlier are not. The scope's settings-and-log bullet also
   names the session file and its lock.
6. `docs/RELEASE.md`: add the session files to what an uninstall leaves, and fix anything else 1.18.0 makes untrue.
   Its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`. Apply any bump outside the coupled set (`xunit.v3`
   4.0.2 → 4.0.3). Move the Avalonia set only as a whole, and only if Enigma.Avalonia.Desktop moved. Log the result
   in the notes.
8. Pre-flight: a Release build with zero warnings whose assemblies carry `1.18.0`, and the Release suite measured
   against the run's baseline.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean with zero warnings, and the built assemblies carry
  `1.18.0`.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release` has no failure the run's baseline did not have.
- `RELEASENOTES.md` has one dated `1.18.0` section above 1.17.0's. It covers FEATURE-34A1 and BUG-C6A9, logs the
  dependency check and keeps the BUG-4A17 known issue.
- The README callout names 1.18. `SECURITY.md` supports `1.18.x` only, and its scope names the session file.
- `docs/RELEASE.md` names the session files among those an uninstall leaves.

## Out of scope

- NuGet packaging; a committed MSI profile.
- Merging into `develop` / `main`, tagging, pushing, and building the 1.18.0 MSI for distribution: these are printed
  for the user.
- Any change to the application's behaviour, including the FEATURE-34A1 follow-ups: writing the session while the
  editor runs, and a setting to turn the restore off.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.18.0 | The draft, and a minor bump fits: the release adds a feature and breaks no stored format | 2.0.0 (nothing stored breaks); 1.17.1 (a feature is not a patch) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Several phases (notes, version, docs): too small to split |
| Notes shape | 1.17.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Release date | 2026-11-09 | The run date | A future date |
| Cut from | The current `HEAD`, the unmerged run `bugfix/2026-11-09-ctrl-y-session-restore` | The command cuts from `HEAD`, and the items the release documents are there | Cutting from `develop`, which lacks both items |
| Behaviour changes users rely on | Stated twice, in the item's section and under *Upgrading from 1.17*: quitting no longer asks, and `Ctrl+Y` no longer scrolls | They change what a key or a quit does | Only in the item's section |
| Session limits in the notes | Stated plainly: written at close, so a crash brings back the last close; kept text wins over the file on disk | Users keep unsaved text there and must know when it is safe | Leaving them out |
| Going back to 1.17 | Noted: 1.17 ignores `session.json`, so save the changes it keeps before going back | Downgrading silently loses kept unsaved text | Saying nothing |
| Avalonia 12.1.6 | Held back at 12.1.4 | The set moves as a whole with Enigma.Avalonia.Desktop, which has not moved | Bumping Avalonia alone |
| `xunit.v3` 4.0.3 | Applied | A patch outside the coupled set, and test-only. `dotnet-release` applies non-coupled bumps | Holding it back |
| `SECURITY.md` scope | The settings-and-log bullet also names `session.json` and `session.lock` | The editor now reads a file at start that holds the user's text: a new surface | Unchanged (1.17's choice, when nothing was added) |
| `docs/RELEASE.md` | The session files added to what an uninstall leaves | The parenthesis lists the folder's contents and is now incomplete | Leaving it |
| README's uninstall sentences | Unchanged | They name the settings and the log, which stays true | Rewording them (beyond this release's scope) |
| Merges, tag, push | Printed in the end-of-run report, not run | `vibe-workflow` §5 forbids writing to `develop`/`main`, `tag` and `push` | Running them |
| `buildmsi.ps1` | Not run; left to the user before distributing | It runs `git clean -dxf`, which §5 forbids, and it would delete untracked files | Running it |
| Pre-flight | A Release build and the Release suite, measured against a Release baseline taken on the run branch | `docs/RELEASE.md` §1 asks for both in Release. The baseline's configuration must match the dev's | Debug only (not what ships) |

# FEATURE-96BA — Release 1.20.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-96ba-release-1-20-0`
**Run:** feature/2026-11-17-release-1-20-0

## Objective

Cut **1.20.0**, the minor bump of 1.19.0 (`x.y+1.z`, with `z` = 0). Use `dotnet-release`'s routine-release path for an
app, with FEATURE-F4F8 (1.19.0) as the template. The release covers:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.20.0 changes;
- the README callout;
- `SECURITY.md`, its supported versions and its scope;
- a dependency check.

The merges into `develop` and `main`, the `1.20.0` tag, any push and the MSI built for distribution are printed for
the user to run, not run here.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.19.0` today, and the two move together.
- **Tags:** `1.19.0` is on `main` (`06a49a0`), and `main` has merged `develop` (`bcf0c95`), so nothing from 1.19 is
  left to merge or tag. `1.20.0` does not exist. Tags are bare `X.Y.Z` and lightweight, on `main`'s merge commit.
- **`buildmsi.ps1` holds no version literal.** It reads `<Version>` from the Desktop csproj and stops when Core's
  differs, so bumping the csprojs is all `buildmsi` needs.
- **1.20.0 carries two items since 1.19.0** (`git log 1.19.0..HEAD`), from the run
  `feature/2026-11-16-session-explorer-autosave`, which is not yet merged into `develop`:
  - **FEATURE-768C, the explorer comes back with the session:**
    - quitting keeps the explorer's folder and whether it was shown. The next start brings it back shown or hidden
      as it was left, with its folder listed;
    - it comes back after the tabs and the files named on the command line, so listing a large folder does not keep
      the documents waiting;
    - an explorer shown by the start does not take the keyboard: it stays in the active pane;
    - a folder that no longer exists, or a path that is not a full one, leaves the explorer with no folder. Nothing
      is shown to the user, and the log gets an Information line. The tabs still come back;
    - a folder opened before the restore finished is the one that stays;
    - not kept: the explorer's width, its expanded folders and its selection.
  - **FEATURE-4158, the session is kept while the editor runs:**
    - once the start has completed, the session (tabs, splits, explorer, unsaved text) is checked every 5 seconds and
      written when it changed. A crash, a kill or a power cut loses at most the last few seconds, and the next start
      restores the file as it restores a closed session;
    - closing works as before and has the last word: nothing written in the background replaces it;
    - after `:qa!` no unsaved text is kept, as when closing;
    - a second window, which does not own the session, writes nothing, as before;
    - the write runs off the UI thread, one at a time. A failed write is retried at the next check. The log records
      the first failure as an error, and the next success;
    - the interval is fixed. It is not a setting.
  - The README already describes both: the quitting paragraph, and the `session.json` line.
- **1.19.0's notes are history and are not edited.**
- **No settings key and no new file.** `session.json` stays format version 1. It gains two optional members, the
  explorer's folder and whether it was shown. A 1.19 file reads as no folder with the explorer hidden. 1.19 reads a
  1.20 file and ignores the two members: `SessionFile` uses `JsonSerializerDefaults.Web`, which skips unknown members.
  `session.json` is now also written while the editor runs, not only when it closes.
- **BUG-4A17** is still quarantined, so the *Known issue* line stays.
- **`SECURITY.md`** supports only the latest minor line, `1.19.x` today. Its scope names `session.json` and what the
  editor reopens from it. 1.20 adds a folder path that is reopened, and a write from a background thread.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  together with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia. Hosting tracks the .NET runtime band (10.0.x). On
  2026-11-17, `dotnet list package --outdated` shows:
  - Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia:
    12.1.6. Enigma.Avalonia.Desktop has no update;
  - Microsoft.Extensions.Hosting: 11.0.0 as the latest, and 10.0.15 as the highest patch (`--highest-patch`);
  - nothing else.
- **A run may not run `buildmsi.ps1`.** It starts with `git clean -dxf`, which `vibe-workflow` §5 forbids.

## Steps

1. Check that `git tag --list 1.20.0` is empty.
2. Set `<Version>1.20.0</Version>` in both csprojs.
3. `Directory.Packages.props`: Microsoft.Extensions.Hosting `10.0.14` → `10.0.15`. Nothing else moves.
4. `RELEASENOTES.md`: add a dated `## 1.20.0 — 2026-11-17` section above 1.19.0's, in the repository's style:
   - a summary;
   - *The explorer comes back with the session* (FEATURE-768C);
   - *The session is kept while the editor runs* (FEATURE-4158);
   - *Upgrading from 1.19*:
     - no new key or file; `session.json` gains the explorer, and going back to 1.19;
     - `session.json` is now written while the editor runs;
     - the MSI name;
     - the Linux upgrade;
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.

   Take every fact from the done docs and the source, not from memory.
5. `README.md`: the callout names 1.20 first, then 1.19, then 1.18 down to 1.0. Reflow only the lines this release
   touches.
6. `SECURITY.md`:
   - `1.20.x` is supported; `1.19.x` and earlier are not;
   - the scope's session clause also names the explorer's folder it reopens, and the write every few seconds while
     the editor runs.
7. `docs/RELEASE.md`: fix anything 1.20.0 makes untrue, and leave the generic `X.Y.Z` steps as they are.
8. Pre-flight: a Release build with zero warnings whose assemblies carry `1.20.0`. Then the Release suite, measured
   against the run's baseline.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean, with zero warnings, and the built assemblies carry
  `1.20.0`.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release` has no failure that the run's baseline did not have.
- `RELEASENOTES.md` has one dated `1.20.0` section above 1.19.0's. It:
  - covers FEATURE-768C and FEATURE-4158;
  - states the session file's change and going back to 1.19 under *Upgrading from 1.19*;
  - logs the dependency check, Hosting 10.0.14 → 10.0.15 included;
  - keeps the BUG-4A17 known issue.
- The README callout names 1.20.
- `SECURITY.md` supports `1.20.x` only, and its scope names the explorer's folder and the periodic write.
- `docs/RELEASE.md` says nothing that 1.20.0 makes untrue.

## Out of scope

- NuGet packaging, and a committed MSI profile.
- Printed for the user, not run here:
  - merging into `develop` and `main`;
  - tagging `1.20.0`;
  - pushing;
  - building the 1.20.0 MSI.
- Any change to the application's behaviour, including these follow-ups:
  - FEATURE-768C: keeping the explorer's width, expanded folders and selection;
  - FEATURE-4158: a setting for the interval or to turn the autosave off; a status-line notice when the session
    cannot be written.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.20.0 | The draft asks for `x.y+1.z` from 1.19.0, and `z` is 0. A minor bump fits: the release adds features and breaks no stored format | 2.0.0 (nothing breaks); 1.19.1 (features are not a patch) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Several phases (notes, version, docs): too small to split |
| Notes shape | 1.19.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Release date | 2026-11-17 | The run date | A future date |
| Cut from | The current `HEAD`, the unmerged run `feature/2026-11-16-session-explorer-autosave` | The command cuts from `HEAD`, and the two items the release documents are there | Cutting from `develop`, which lacks both |
| Microsoft.Extensions.Hosting | 10.0.14 → 10.0.15 | A patch within the runtime band it tracks. 1.17.0 took 10.0.13 → 10.0.14 the same way | Holding it (no reason to); 11.0.0 (below) |
| Microsoft.Extensions.Hosting 11.0.0 | Not taken | It belongs to the .NET 11 band. The app targets .NET 10, and the props file says Hosting tracks the runtime band | Taking it: a major dependency move in a minor release |
| Avalonia 12.1.6 | Held back at 12.1.4 | The set moves as a whole with Enigma.Avalonia.Desktop, which has not moved | Bumping Avalonia alone |
| The session file's change | Under *Upgrading from 1.19*: two optional members, format 1 unchanged, both directions read | A user going back to 1.19 needs to know nothing is lost but the explorer | Only in the item's section |
| The periodic write | Stated in *Upgrading from 1.19*: `session.json` now changes while the editor runs | It changes when the file is written, which matters to anyone who backs up or syncs the folder | Not mentioned |
| A real crash not verified | The notes describe the behaviour as the README does. The done doc and the end-of-run report ask the user to check it on a real desktop | The paths are tested through the harness. Hedging in the notes would read as a defect | A *Known issue* line (nothing is known to fail) |
| `SECURITY.md` scope | The session clause names the explorer's folder that is reopened and the write every few seconds | A path read from the file is now reopened, and the file is written from a background thread | Unchanged |
| Merges, tag, push | Printed in the end-of-run report, not run | `vibe-workflow` §5 forbids writing to `develop` or `main`, `tag` and `push` | Running them |
| `buildmsi.ps1` | Not run. Left to the user before distributing | It runs `git clean -dxf`, which §5 forbids, and would delete untracked files | Running it |
| Pre-flight | A Release build and the Release suite, measured against a Release baseline taken on the run branch | `docs/RELEASE.md` §1 asks for both in Release, and the baseline's configuration must match the dev's | Debug only (not what ships) |

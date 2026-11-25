# FEATURE-6C0E — Release 1.21.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-6c0e-release-1-21-0`
**Run:** feature/2026-11-25-release-1-21-0

## Objective

Cut **1.21.0**, the minor bump of 1.20.0 (`x.y+1.z`, with `z` = 0). Use `dotnet-release`'s routine-release path for an
app, with FEATURE-96BA (1.20.0) as the template. The release covers:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.21.0 changes;
- the README callout;
- `SECURITY.md`, its supported versions and its scope;
- `docs/RELEASE.md`, where 1.21.0 makes it untrue;
- a dependency check.

The merges into `main`, the `1.21.0` tag, any push and the MSI built for distribution are printed for the user to
run, not run here.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.20.0` today, and the two move together.
- **Tags:** `1.20.0` is on `main` (`f158edd`), which has merged `develop` up to the 1.20.0 release. `1.21.0` does not
  exist. Tags are bare `X.Y.Z` and lightweight, on `main`'s merge commit.
- **`buildmsi.ps1` holds no version literal.** It reads `<Version>` from the Desktop csproj and stops when Core's
  differs, so bumping the csprojs is all `buildmsi` needs.
- **1.21.0 carries one item since 1.20.0** (`git log 1.20.0..HEAD`): CODE-REVIEW-0734, which fixes every High and
  Medium finding of the solution review `docs/review/2026-11-19-solution.md`. It is merged into `develop`
  (`e70fddd`). What the user can see, from its done docs:
  - **PHASE06, saves are atomic:**
    - a save writes the new bytes to `.<name>.<guid>.tmp` beside the file, flushes them to the disk, then moves the
      file over the original;
    - a full disk, a dropped network share or a crash part-way through leaves the original whole, where it used to
      be left empty or cut short;
    - a symbolic link stays a link, and the file's metadata is carried over (on Windows the creation time and the
      attributes, on Linux and macOS the file mode);
    - a file that cannot be written is refused exactly as before;
    - when the folder refuses a new file, or the move is refused while the file can be written, the save writes in
      place, as before.
  - **PHASE07 and PHASE20, file work leaves the UI thread:**
    - reading the session at the start;
    - a save's encoding and its whole write;
    - opening a hex view, at the start or by hand;
    - the checks on dropped files and command-line paths, and on files another launch hands over;
    - the hex view's copy, which reads and formats the bytes.

    On a large file, or a slow or disconnected network path, the window keeps responding.
  - **PHASE18 and PHASE19, the session writes only what changed:**
    - each unsaved text is now a file of its own, in an `unsaved` folder beside `session.json`;
    - the 5-second tick rewrites a text only when it changed. After a caret move only the small `session.json` is
      written;
    - `session.json` is now format version 2. 1.21 still reads version 1;
    - a missing text file costs only that text: its tab comes back without it, and the log has a warning;
    - a session that cannot be used is moved aside with its texts, to `session.invalid.json` and `unsaved.invalid`;
    - **1.20 reads only version 1.** It moves a 1.21 session aside to `session.invalid.json` and starts without it.
      It does not touch `unsaved`. The next 1.21 start writes its own session and deletes the texts it does not name.
  - **PHASE04, the explorer's failures are logged:** a create, rename, delete or move to the trash that fails is
    written to `app.log`, as well as shown in a dialog.
  - **PHASE05, `logs/early-errors.log`:** a failure before the log is ready (while the settings are read or the host
    is built) is written there in full. The file is started again past 1 MiB.
  - **PHASE01, the instance pipe:** a program that sends a window's pipe more than it reads is refused at once,
    instead of holding the pipe until a deadline, and the next launch is served.
  - **PHASE02, 03 and 08–17, 21:** internal refactors and steadier tests. Behaviour is unchanged.
  - The README already describes the `unsaved` folder and `early-errors.log` (PHASE05 and PHASE19).
- **1.20.0's notes are history and are not edited.**
- **No settings key.** The session file's format and the `unsaved` folder are the only stored changes.
- **BUG-4A17** is still quarantined, so the *Known issue* line stays.
- **`SECURITY.md`** supports only the latest minor line, `1.20.x` today. CODE-REVIEW-0734 already rewrote its session
  clause for the `unsaved` folder. Its write-back clause does not name the temporary file a save now creates, and its
  log clause does not name `early-errors.log`.
- **`docs/RELEASE.md`** §5 says uninstalling leaves the session in `session.json` and `session.lock`. The `unsaved`
  folder is now part of it.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  together with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia. Hosting tracks the .NET runtime band (10.0.x). On
  2026-11-25, `dotnet list package --outdated` shows:
  - Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia:
    12.2.0 as the latest, and 12.1.6 as the highest patch. Enigma.Avalonia.Desktop has no update;
  - Microsoft.Extensions.Hosting: 11.0.0 as the latest, and no 10.0.x patch past 10.0.15;
  - nothing else.
- **A run may not run `buildmsi.ps1`.** It starts with `git clean -dxf`, which `vibe-workflow` §5 forbids.

## Steps

1. Check that `git tag --list 1.21.0` is empty.
2. Set `<Version>1.21.0</Version>` in both csprojs.
3. `Directory.Packages.props`: nothing moves (see *Context*). Run the outdated check again at build time and record
   it.
4. `RELEASENOTES.md`: add a dated `## 1.21.0 — 2026-11-25` section above 1.20.0's, in the repository's style:
   - a summary;
   - *Saving cannot leave a file cut short* (PHASE06);
   - *The window keeps responding during file work* (PHASE07, PHASE20);
   - *The session writes only what changed* (PHASE18, PHASE19);
   - *Fixes and diagnostics* (PHASE01, PHASE04, PHASE05);
   - *Under the hood* (the refactors and the test waits, in one short list);
   - *Upgrading from 1.20*:
     - no new key; the session's format 2 and the `unsaved` folder;
     - going back to 1.20;
     - `early-errors.log`;
     - the MSI name;
     - the Linux upgrade;
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.

   Take every fact from the done docs and the source, not from memory.
5. `README.md`: the callout names 1.21 first, then 1.20, then 1.19 down to 1.0. Reflow only the lines this release
   touches.
6. `SECURITY.md`:
   - `1.21.x` is supported; `1.20.x` and earlier are not;
   - the write-back clause names the temporary file beside the file and the move over it;
   - the log clause names `early-errors.log`.
7. `docs/RELEASE.md`: the uninstall line names the `unsaved` folder. Fix anything else 1.21.0 makes untrue, and leave
   the generic `X.Y.Z` steps as they are.
8. Pre-flight: a Release build with zero warnings whose assemblies carry `1.21.0`. Then the Release suite, measured
   against the run's baseline.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean, with zero warnings, and the built assemblies carry
  `1.21.0`.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release` has no failure that the run's baseline did not have.
- `RELEASENOTES.md` has one dated `1.21.0` section above 1.20.0's. It:
  - covers every user-visible change of CODE-REVIEW-0734 listed in *Context*;
  - states the session's format 2, the `unsaved` folder and going back to 1.20 under *Upgrading from 1.20*;
  - logs the dependency check, with the Avalonia set held back and Hosting 11.0.0 not taken;
  - keeps the BUG-4A17 known issue.
- The README callout names 1.21.
- `SECURITY.md`:
  - supports `1.21.x` only;
  - its scope names a save's temporary file and `early-errors.log`.
- `docs/RELEASE.md` says nothing that 1.21.0 makes untrue.

## Out of scope

- NuGet packaging, and a committed MSI profile.
- Printed for the user, not run here:
  - merging into `develop` and `main`;
  - tagging `1.21.0`;
  - pushing;
  - building the 1.21.0 MSI.
- Any change to the application's behaviour, including the review's Low findings (SOLID-04, EXC-02, EXC-03,
  ASYNC-01, ASYNC-02, MVVM-01, STYLE-01, PERF-03).
- Moving the Avalonia set to 12.1.6 or 12.2.0.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.21.0 | The draft asks for `x.y+1.z` from 1.20.0, and `z` is 0. The settings are untouched, and the session file is the editor's own state, read in both formats by 1.21 | 2.0.0 (no public surface breaks); 1.20.1 (the draft asks for a minor bump) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Several phases (notes, version, docs): too small to split |
| Notes shape | 1.20.0's, with *Fixes and diagnostics* and *Under the hood* added | The repository's own style. A review release has fixes and refactors that the feature releases did not | The template's *New Features · Fixes* headings; leaving the refactors out (a reader of the diff would miss them) |
| Release date | 2026-11-25 | The run date | A future date |
| Cut from | The current `HEAD`, `develop` @ `e70fddd` | The command cuts from `HEAD`, and CODE-REVIEW-0734 is merged there | Cutting from `main`, which lacks it |
| Session format 2 | Under *Upgrading from 1.20*: 1.21 reads both; 1.20 moves a 1.21 session aside and starts without it | A user going back to 1.20 loses the restored session and must know it before going back. The texts stay in `unsaved` as plain text | Only in the item's section (it would be missed); a *Known issue* (it is by design) |
| How the refactors are listed | One *Under the hood* list, by area, no class names past what a reader needs | They change no behaviour; the notes are for users, and the done docs carry the detail | One bullet per phase; leaving them out |
| Avalonia 12.1.6 and 12.2.0 | Held back at 12.1.4 | The set moves as a whole with Enigma.Avalonia.Desktop, which has not moved | Bumping Avalonia alone |
| Microsoft.Extensions.Hosting 11.0.0 | Not taken; 10.0.15 stays, no later 10.0.x exists | It belongs to the .NET 11 band. The app targets .NET 10 | Taking it: a major dependency move in a minor release |
| `SECURITY.md` scope | The write-back clause names the temporary file and the move; the log clause names `early-errors.log` | A save now creates a file beside the user's, and a new file is written before the log is ready | Unchanged (the scope would not describe what the editor writes) |
| `docs/RELEASE.md` uninstall line | Names the `unsaved` folder with `session.json` and `session.lock` | Uninstalling leaves it, and it holds the user's unsaved text | Unchanged |
| Merges, tag, push | Printed in the end-of-run report, not run | `vibe-workflow` §5 forbids writing to `develop` or `main`, `tag` and `push` | Running them |
| `buildmsi.ps1` | Not run. Left to the user before distributing | It runs `git clean -dxf`, which §5 forbids, and would delete untracked files | Running it |
| Pre-flight | A Release build and the Release suite, measured against a Release baseline taken on the run branch | `docs/RELEASE.md` §1 asks for both in Release, and the baseline's configuration must match the dev's | Debug only (not what ships) |

# FEATURE-2448 — Release 1.10.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-2448-release-1-10-0`
**Run:** feature/2026-10-30-release-1-10-0

## Objective

Cut **1.10.0** on `dotnet-release`'s routine-release path, with FEATURE-8EDF (1.9.0) as the template. The work is:

- the version;
- the release notes for what 1.10.0 changes;
- the README callout, and the README's *Your data*, which FEATURE-7B55 left stale;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.10.0` tag and any push are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.9.0` today; the two move together. The request names
  **1.10.0**: a minor release, since it adds features and changes no stored format or default. Nothing in `src/` or
  `tests/` hard-codes the version (`AppInfoTests` compares against the stamped assembly version).
- **What 1.10.0 carries** is everything merged after the `1.9.0` tag (`git log 1.9.0..HEAD`):
  - FEATURE-9F56 (move tabs between panes): `Space mh`, `mj`, `mk`, `ml`, and dragging a tab onto another pane;
  - FEATURE-7B55 (open files in the running instance): a file opened from the system while the editor runs opens
    as a tab in the window used last, which comes to the front. Without a file, a launch still opens a new window.
    Windows and Linux; macOS (Apple events) is out of 7B55's scope;
  - BUG-5A45 (the tab drag): the drag runs on pointer events instead of a platform drag session, so the pointer is
    a move cursor rather than the refusal cursor. The pane a tab would land on is highlighted, and Escape cancels.

  The drag of FEATURE-9F56 and its fix BUG-5A45 both landed after 1.9.0. No release shipped the refusal cursor, so
  the notes describe the drag as it now behaves, not as a fix. BUG-4A17 is still quarantined from an earlier run and
  is not in 1.10.0.
- **No settings key changes:** 1.9 reads a 1.10 settings file unchanged. FEATURE-7B55 adds `instance.json` beside
  `settings.json` (`AppPaths.ActiveInstanceFile`). It names the window used last, is removed on exit when it still
  names that window, and a 1.9 build never reads it.
- **Instance surface** (FEATURE-7B55): each process serves a pipe `Enigma.TextEditor.<pid>`, opened with
  `PipeOptions.CurrentUserOnly`. A request is length-prefixed JSON of at most 1 MiB (`InstanceProtocol`), and
  anything else is refused.
- **README *Your data*** still says the editor keeps "two things of its own", `settings.json` and the log. That has
  been untrue since FEATURE-7B55.
- `SECURITY.md` supports the latest minor line only, today `1.9.x`.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set moves together. At planning time every pin
  was at its latest stable version (`dotnet list package --outdated`: no updates in any of the four projects).
- **Tag convention:** bare `X.Y.Z` (`1.0.0` … `1.9.0`); `1.10.0` does not exist yet.
- **Open manual checks** the merged items left (from their done docs): the tab drag in a real window, including the
  KDE Wayland pointer and the drop highlight in both themes; and a forward between two real processes on Windows and
  Linux. They are the user's to run before tagging.

## Steps

1. Check that `git tag --list 1.10.0` is empty.
2. Both csprojs: `<Version>1.10.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.10.0 — <run date>` section above 1.9.0's, in the repository's style:
   - a summary;
   - a themed section per merged feature: tabs between panes (9F56 with 5A45's drag), and files opened while the
     editor runs (7B55);
   - *Upgrading from 1.9*, including what a 1.9 window still open does with a forwarded file, and the new
     `instance.json`;
   - *Dependencies*;
   - *Compatibility*, including the platforms forwarding covers;
   - *Version*.
4. `README.md`:
   - the callout names 1.10, then sums up 1.9 down to 1.0;
   - *Your data* names `instance.json`, what it is for, and that it is removed on exit.
5. `SECURITY.md`:
   - `1.10.x` is supported, `1.9.x` and earlier are not;
   - the scope adds the instance pipe (what a request may hold, who may connect) and `instance.json`.
6. `docs/RELEASE.md`: fix anything 1.10.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump anything published since planning, stable only, the coupled set together;
   - log the result in the notes, including "nothing moved".
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.10.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` is green.
- The built assemblies carry `1.10.0`: file version `1.10.0.0`, informational `1.10.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.10.0` section above 1.9.0's. It covers every item merged since `1.9.0` and names
  BUG-4A17 as not included.
- The README callout names 1.10, and *Your data* names `instance.json`. `SECURITY.md` supports `1.10.x` only, and its
  scope names the instance pipe and `instance.json`.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour.
- The manual checks in a real window; the merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.10.0 | The request names it; new features, nothing breaking: a minor bump | 1.9.1 (it adds features); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Splitting notes and version into two devs |
| Contents | FEATURE-9F56, FEATURE-7B55, BUG-5A45 | Exactly `git log 1.9.0..HEAD` | Waiting for BUG-4A17, which is quarantined |
| BUG-5A45 in the notes | Folded into the drag's description; no *Fixes* section | The refusal cursor never shipped in a release; users meet the drag as it now is | A *Fixes* entry for a defect no release had |
| Notes shape | 1.9.0's | The repository's own style | The template's *New Features · Fixes* headings |
| README *Your data* | Corrected in this dev | It is factually wrong since FEATURE-7B55, and the release owns the README | A separate docs item; leaving it |
| `SECURITY.md` | `1.10.x` only; scope adds the pipe and `instance.json` | Latest-minor-only policy; a local IPC endpoint is new surface a report could concern | Keeping `1.9.x` supported; leaving the scope as is |
| Prerelease packages | Never | A release ships on stable dependencies | Taking a .NET 11 preview or RC |
| MSI profile | None | Not asked for; the 1.0–1.9 precedent | Generating a first profile |
| Release date | The run date | 1.9.0's precedent | A future date |
| Open manual checks | Listed for the user in the done doc and the run report | A run opens no window on the user's desktop; they are not release edits | Adding them to `docs/RELEASE.md`'s generic checklist |

# FEATURE-1CC6 — Release 1.11.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-1cc6-release-1-11-0`
**Run:** feature/2026-10-30-release-1-11-0

## Objective

Cut **1.11.0** on `dotnet-release`'s routine-release path, with FEATURE-2448 (1.10.0) as the template. The work is:

- the version;
- the release notes for what 1.11.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.11.0` tag and any push are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.10.0` today; the two move together. The request names
  **1.11.0**: a minor release, since it adds a feature and changes no stored format or default. Nothing in `src/` or
  `tests/` hard-codes the version (`AppInfoTests` compares against the stamped assembly version).
- **What 1.11.0 carries** is everything after the `1.10.0` tag (`git log 1.10.0..HEAD`): FEATURE-9E4A only (drop a
  tab on a pane's edge to split it):
  - released over a pane's tab strip or the middle of its content, a tab moves there, as in 1.10, with the whole pane
    highlighted;
  - released within the outer quarter of a pane's content (top, right, bottom or left), that pane splits on that side,
    evenly, and the tab goes into the new half, which takes the keyboard. While the tab is there, only that half is
    highlighted. In a corner, the nearer side wins;
  - this works on the tab's own pane too, as long as that pane keeps another tab. A pane's only tab dropped on its own
    edge shows no hint and does nothing;
  - the tab strip never splits;
  - a source pane left empty closes; a file the target also shows still goes to the new pane;
  - **Help → Key Bindings** has a new *Panes* row, "Drag a tab to a pane's edge". The README's *Tabs and panes* already
    says so (9E4A PHASE02);
  - there is no key for it: the `Space m` keys still move a tab only to a pane that exists. `Ctrl+W v` is no
    stand-in: its new pane opens with an untitled tab and takes the keyboard (`WorkspaceViewModel.Split`).
- **1.10.0's notes say** "Releasing the tab over its own pane … moves nothing". That stays true of its middle and its
  strip; its edges now split. The 1.11.0 notes say so; 1.10.0's section is history and is not edited.
- **No settings key, no new file:** 1.10 reads a 1.11 settings file unchanged.
- **The run starts from `feature/2026-10-30-drag-drop-split`** (FEATURE-9E4A's run), which is not merged into
  `develop` yet. This run branch carries it, so merging this run into `develop` brings 9E4A too.
- **BUG-4A17** is still quarantined from an earlier run: the *Known issue* line stays.
- `SECURITY.md` supports the latest minor line only, today `1.10.x`. 9E4A works inside the window and opens no new
  surface, so the scope is unchanged.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set moves together. At planning time every pin
  was at its latest stable version (`dotnet list package --outdated`: no updates in any of the four projects).
- **Tag convention:** bare `X.Y.Z` (`1.0.0` … `1.10.0`); `1.11.0` does not exist yet.
- **Open manual checks** 9E4A left (its PHASE02 done doc): the half-pane hint by eye in both themes, and whether a
  quarter is the right edge band on very wide panes. They are the user's to run before tagging.

## Steps

1. Check that `git tag --list 1.11.0` is empty.
2. Both csprojs: `<Version>1.11.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.11.0 — <run date>` section above 1.10.0's, in the repository's style:
   - a summary;
   - a themed section for the edge drop (9E4A): where to drop, what is highlighted, own pane, the strip, what the
     new pane gets, the help row, and that the `Space m` keys are unchanged;
   - *Upgrading from 1.10*;
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
4. `README.md`: the callout names 1.11, then sums up 1.10 down to 1.0.
5. `SECURITY.md`: `1.11.x` is supported, `1.10.x` and earlier are not.
6. `docs/RELEASE.md`: fix anything 1.11.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump anything published since planning, stable only, the coupled set together;
   - log the result in the notes, including "nothing moved".
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.11.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` is green.
- The built assemblies carry `1.11.0`: file version `1.11.0.0`, informational `1.11.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.11.0` section above 1.10.0's. It covers FEATURE-9E4A and keeps the BUG-4A17
  known issue.
- The README callout names 1.11. `SECURITY.md` supports `1.11.x` only.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour.
- The manual checks in a real window; the merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.11.0 | The request names it; a new feature, nothing breaking: a minor bump | 1.10.1 (it adds a feature); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Splitting notes and version into two devs |
| Contents | FEATURE-9E4A | Exactly `git log 1.10.0..HEAD` | Waiting for BUG-4A17, which is quarantined |
| Notes shape | 1.10.0's | The repository's own style | The template's *New Features · Fixes* headings |
| 1.10.0's "own pane moves nothing" line | Left as history; 1.11.0's section states the new behaviour | Release sections describe their own release | Editing a shipped release's notes |
| Keyboard route in the notes | None named; the notes say the split is a drag and the `Space m` keys still move to an existing pane | 9E4A's plan suggested `Ctrl+W v` then `Space ml`, but that split leaves an untitled tab in the new pane and moves the keyboard there (`WorkspaceViewModel.Split`): not the same layout | Naming that route (inexact); promising a key (not in 9E4A) |
| README | Callout only | 9E4A already updated *Tabs and panes*; nothing else is stale | Rewriting other sections |
| `SECURITY.md` | `1.11.x` only; scope unchanged | Latest-minor-only policy; the drag is in-window, no new surface | Keeping `1.10.x` supported; widening the scope |
| Prerelease packages | Never | A release ships on stable dependencies | Taking a .NET 11 preview or RC |
| MSI profile | None | Not asked for; the 1.0–1.10 precedent | Generating a first profile |
| Release date | The run date | 1.10.0's precedent | A future date |
| Open manual checks | Listed for the user in the done doc and the run report | A run opens no window on the user's desktop; they are not release edits | Adding them to `docs/RELEASE.md`'s generic checklist |
| Starting point | The current `HEAD`, FEATURE-9E4A's unmerged run branch | `/vibe` cuts from `HEAD`; 9E4A is what the release ships | Starting from `develop`, which lacks 9E4A |

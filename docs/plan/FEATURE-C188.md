# FEATURE-C188 — Release 1.12.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-c188-release-1-12-0`
**Run:** feature/2026-10-31-release-1-12-0

## Objective

Cut **1.12.0** on `dotnet-release`'s routine-release path, with FEATURE-1CC6 (1.11.0) as the template. The work is:

- the version;
- the release notes for what 1.12.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.12.0` tag and any push are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.11.0` today; the two move together. The request names
  **1.12.0**: a minor release, since it adds features and changes no stored format or default. Nothing in `src/` or
  `tests/` hard-codes the version (`AppInfoTests` compares against the stamped assembly version).
- **What 1.12.0 carries** is everything after the `1.11.0` tag (`git log 1.11.0..HEAD`): two items.
  - **FEATURE-A198 — a font size button on the status bar**, last on the right, reading `Font Size: 14`:
    - pressed, it lists 8–16, 18, 20, 24, 28, 32 and 36. The current size is checked, and the default reads
      `14 (default)`. A size that is not in the list (17 after a zoom) is added in its place, checked;
    - choosing one resizes every editor's text and the hex view at once, and is remembered, as the zoom keys and the
      settings page do. The button follows those, and a reset, too;
    - it shows for any open tab, text or hex, and hides with the other items when no tab is open. It takes no
      focus, and the keyboard goes back to the pane when its list closes;
    - the README's *The status bar* already says so (A198).
  - **FEATURE-B751 — files dropped near a pane's edge split it**, as a dragged tab does since 1.11:
    - over a pane's tab strip or the middle of its text, the whole pane is highlighted and the files open in it, as
      before;
    - within the outer quarter of a pane's text (top, right, bottom, left), only the half the new pane will take is
      highlighted. Dropped there, the pane splits on that side, every file opens in the new pane in drop order, the
      last one shown, and the new pane takes the keyboard;
    - a pane with no tab (the window's only pane, "No file is open") has no edges: the files open in it;
    - a fresh tab in the split pane stays there. A file the split pane also shows opens in the new pane too;
    - no existing file among the paths (folders, gone): nothing splits, and the status line names them. Every file
      refused (a binary file declined): the new pane closes again and the split pane takes the keyboard back;
    - text dragged over a pane gets no highlight;
    - **Help → Key Bindings** has a new *Files* row, "Drop files on a pane's edge". The README's *Files* paragraph
      already says so (B751 PHASE02).
- **1.11.0's notes are history.** They describe the tab drag only, which is still true; they are not edited.
- **No settings key, no new file:** A198 writes the existing `EditorFontSize` through `SetFontSizeAsync`. Core is
  unchanged since 1.11.0 (`git diff 1.11.0..HEAD -- src/Enigma.TextEditor.Core` is empty). 1.11 reads a 1.12 settings
  file unchanged.
- **The running window's hand-over is unchanged** (the instance pipe and `instance.json` are untouched), so a 1.11
  window still open takes the files of a 1.12 launch.
- **The run starts from `feature/2026-10-31-font-size-drop-split`** (A198 and B751's run), which is not merged into
  `develop` yet. This run branch carries it, so merging this run into `develop` brings both features too.
- **BUG-4A17** is still quarantined from an earlier run: the *Known issue* line stays.
- `SECURITY.md` supports the latest minor line only, today `1.11.x`. The file drop existed already and the font size
  is an existing setting, so the scope is unchanged.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set moves together. At planning time every pin
  was at its latest stable version (`dotnet list package --outdated`: no updates in any of the four projects).
- **Tag convention:** bare `X.Y.Z` (`1.0.0` … `1.11.0`); `1.12.0` does not exist yet.
- **The README callout** carries some overlong lines from earlier releases. Only the lines this release touches are
  reflowed.
- **Open manual checks** the two features left (their done docs), for the user before tagging:
  - A198: the font size list, by eye, in both themes;
  - B751: a real file manager's drag across two panes, out of the window, and cancelled, on each platform.

## Steps

1. Check that `git tag --list 1.12.0` is empty.
2. Both csprojs: `<Version>1.12.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.12.0 — <run date>` section above 1.11.0's, in the repository's style:
   - a summary;
   - a themed section for the font size button (A198): where it is, the list, what choosing does, what it follows,
     when it shows, the keyboard;
   - a themed section for the file edge drop (B751): the zones, the highlight, what the new pane gets, the pane with no
     tab, nothing to open, every file refused, text drags, the help row;
   - *Upgrading from 1.11*;
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
4. `README.md`: the callout names 1.12, then sums up 1.11 down to 1.0.
5. `SECURITY.md`: `1.12.x` is supported, `1.11.x` and earlier are not.
6. `docs/RELEASE.md`: fix anything 1.12.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump anything published since planning, stable only, the coupled set together;
   - log the result in the notes, including "nothing moved".
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green;
   - the assemblies carry `1.12.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` is green.
- The built assemblies carry `1.12.0`: file version `1.12.0.0`, informational `1.12.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.12.0` section above 1.11.0's. It covers FEATURE-A198 and FEATURE-B751 and keeps
  the BUG-4A17 known issue.
- The README callout names 1.12. `SECURITY.md` supports `1.12.x` only.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour.
- The manual checks in a real window; the merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.12.0 | The request names it; two new features, nothing breaking: a minor bump | 1.11.1 (it adds features); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; the 1.6–1.11 precedent | Splitting notes and version into two devs |
| Contents | FEATURE-A198 and FEATURE-B751 | Exactly `git log 1.11.0..HEAD` | Waiting for BUG-4A17, which is quarantined |
| Notes shape | 1.11.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Order of the two themed sections | The file edge drop first, then the font size | The edge drop extends 1.11's headline feature, so the summary reads on from 1.11; the font size stands alone | Roadmap order (A198 first) |
| README | Callout only; only touched lines reflowed | A198 and B751 already updated *The status bar* and *Files*; reflowing the older overlong lines would bury the change in the diff | Rewriting other sections; reflowing the whole callout |
| `SECURITY.md` | `1.12.x` only; scope unchanged | Latest-minor-only policy. The file drop and the font size setting both existed; no new surface | Keeping `1.11.x` supported; widening the scope |
| Prerelease packages | Never | A release ships on stable dependencies | Taking a .NET 11 preview or RC |
| MSI profile | None | Not asked for; the 1.0–1.11 precedent | Generating a first profile |
| Release date | The run date | 1.11.0's precedent | A future date |
| Open manual checks | Listed for the user in the done doc and the run report | A run opens no window on the user's desktop; they are not release edits | Adding them to `docs/RELEASE.md`'s generic checklist |
| Starting point | The current `HEAD`, A198 and B751's unmerged run branch | `/vibe` cuts from `HEAD`; those two items are what the release ships | Starting from `develop`, which lacks them |

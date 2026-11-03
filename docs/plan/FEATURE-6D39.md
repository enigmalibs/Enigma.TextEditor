# FEATURE-6D39 — Release 1.13.0

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-6d39-release-1-13-0`
**Run:** feature/2026-11-02-release-1-13-0

## Objective

Cut **1.13.0** on `dotnet-release`'s routine-release path, with FEATURE-C188 (1.12.0) as the template. The work is:

- the version;
- the release notes for what 1.13.0 changes;
- the README callout;
- `SECURITY.md`;
- a dependency check;
- a Release and Debug pre-flight.

The merges into `develop` and `main`, the `1.13.0` tag and any push are left to the user.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
- **The version** is `<Version>` in both csprojs, `1.12.0` today; the two move together. The request names
  **1.13.0**: a minor release, since it adds features and a fix and changes no stored format or default. Nothing in
  `src/` or `tests/` hard-codes the version (`AppInfoTests` compares against the stamped assembly version).
- **What 1.13.0 carries** is everything after the `1.12.0` tag (`git log 1.12.0..HEAD`): three items.
  - **FEATURE-F0CE — a file explorer on the files page:**
    - a panel left of the panes, hidden at every start, shown and hidden by the status bar's first button (files
      page only), **View → Explorer** or Ctrl+Shift+E. Its edge drags (260 px by default, at least 160; the panes
      keep 240), and hiding then showing keeps the width for the session. Showing it from the settings page brings
      the files page back. Ctrl+Shift+E gives the keyboard to the tree, or to *Open Folder…* with no folder;
    - **Open Folder…** (panel header, or **File → Open Folder…**) asks with a folder picker, starting in the open
      folder, else the active file's folder; one folder at a time. **Close Folder** (header, File menu) empties it.
      **Refresh** reads the folder again: there is no watcher. Neither the panel, its width nor the folder is
      remembered across runs;
    - the tree: the folder is the header (name, path as tooltip), its entries the top rows; folders first, then
      files, natural order ignoring case (`file2` before `file10`); hidden and dot entries shown; a folder is read
      when first expanded; a folder that cannot be read shows a row saying why (*Not found*, *No access*, *Cannot be
      read*);
    - a double-click or Enter opens a file in the active pane, which takes the keyboard (through File → Open's door:
      binary prompt, size limit, already open → shown); on a folder it expands or collapses. Esc goes back to the
      text;
    - the right-click menu: **New File…** (empty, then opened) and **New Folder…**, in the selected folder, beside a
      selected file, or in the root; the name is checked as it is typed, and nothing is ever overwritten.
      **Rename…** (F2): starts with the current name, its stem selected; a change of case alone works; the tabs of
      the renamed file, or of files under a renamed folder, follow the new path with their unsaved changes.
      **Delete** (Delete): after asking (Cancel by default), to the Recycle Bin on Windows or the trash on Linux
      (`gio trash`, else the home trash); where there is none, or it refuses the entry, a second question for a
      permanent delete. Clean tabs of deleted files close; tabs with unsaved changes stay. Failures show an error
      and the folder is read again;
    - **Help → Key Bindings**: *Files & tabs* gains three explorer rows; *Application* gains Ctrl+Shift+E and Esc;
    - the README's *Files* paragraph and *The status bar* already say so (F0CE PHASE02 and PHASE05).
  - **FEATURE-4107 — a double-click on the empty part of a pane's tab strip** (right of the last tab, or below the
    tabs) opens a new untitled tab at the end of that pane, which takes the keyboard. On a tab, its close button or
    the scrollbar it opens nothing. *Files & tabs* gains a help row; the README's *Tabs and panes* already says so.
  - **BUG-74B6 — the mouse wheel scrolls a tab strip** too narrow for its tabs: down towards the later tabs, up
    towards the first, 50 px a notch (as Shift+wheel), stopping at both ends. Before, only Shift+wheel, a sideways
    wheel or the scrollbar moved it.
- **1.12.0's notes are history**, and are not edited.
- **No settings key, no new file:** the explorer remembers nothing. Core gains `Files/` (listing, names, operations,
  trash) and nothing in `Settings/`. 1.12 reads a 1.13 settings file unchanged.
- **The running window's hand-over is unchanged** (the instance pipe and `instance.json` are untouched), so a 1.12
  window still open takes the files of a 1.13 launch.
- **The run starts from `feature/2026-11-02-tab-strip-file-explorer`** (the three items' run), which is not merged
  into `develop` yet. This run branch carries it, so merging this run into `develop` brings all three too.
- **BUG-4A17** is still quarantined from an earlier run: the *Known issue* line stays.
- **`SECURITY.md`** supports the latest minor line only, today `1.12.x`. Its scope lists what reads and writes files;
  the explorer is new surface: it lists folders, takes names for new and renamed entries, creates, renames and deletes
  files and folders, and moves them to the Recycle Bin or the trash, on Linux by running `gio trash` or writing into
  `$XDG_DATA_HOME/Trash`.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set, its companions, Enigma.Avalonia.Desktop and
  Enigma.Icons.Avalonia move together or not at all. At planning time (`dotnet list package --outdated`):
  - Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter, Avalonia.Headless and Avalonia.Skia
    have **12.1.5** (pinned 12.1.4);
  - Enigma.Avalonia.Desktop has no update past 1.3.0, so the set is held back, as in 1.4–1.7;
  - nothing else has an update.
- **Tag convention:** bare `X.Y.Z` (`1.0.0` … `1.12.0`); `1.13.0` does not exist yet.
- **The README callout** carries some overlong lines from earlier releases. Only the lines this release touches are
  reflowed.
- **Open manual checks** the three items left (their done docs), for the user before tagging:
  - F0CE PHASE02: the explorer toggle's checked look and the splitter's hairline, in both themes;
  - F0CE PHASE05: one real delete to the Recycle Bin / trash on each platform, and the dialogs' wording.
- **Known test noise** (earlier done docs): `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  fails on this machine ("Pipe is broken"), and `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`
  is flaky. The run's baseline decides what counts as pre-existing.

## Steps

1. Check that `git tag --list 1.13.0` is empty.
2. Both csprojs: `<Version>1.13.0</Version>`.
3. `RELEASENOTES.md`: a dated `## 1.13.0 — <run date>` section above 1.12.0's, in the repository's style:
   - a summary;
   - a themed section for the explorer (F0CE): showing it, opening and closing a folder, the tree, opening a file,
     creating, renaming, deleting, failures, the help rows;
   - a themed section for the tab strip (4107, 74B6): the double-click, the wheel;
   - *Upgrading from 1.12*;
   - *Dependencies* (the Avalonia set held back at 12.1.4, 12.1.5 being out);
   - *Compatibility* (plus where deleting goes on each platform);
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.
4. `README.md`: the callout names 1.13, then sums up 1.12 down to 1.0.
5. `SECURITY.md`: `1.13.x` is supported, `1.12.x` and earlier are not. The scope gains the explorer.
6. `docs/RELEASE.md`: fix anything 1.13.0 makes untrue; its generic `X.Y.Z` steps stay.
7. **Dependency check** with `dotnet list package --outdated`:
   - bump anything published since planning, stable only, the coupled set together and only with
     Enigma.Avalonia.Desktop;
   - log the result in the notes.
8. **Pre-flight, in Release and in Debug:**
   - a clean build with zero warnings;
   - the whole suite green, bar the baseline's failures;
   - the assemblies carry `1.13.0`.

## Acceptance criteria

- In both Release and Debug, `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings and
  `dotnet test --solution Enigma.TextEditor.slnx` fails nothing the run's baseline did not.
- The built assemblies carry `1.13.0`: file version `1.13.0.0`, informational `1.13.0+<commit>`.
- `RELEASENOTES.md` has one dated `1.13.0` section above 1.12.0's. It covers FEATURE-F0CE, FEATURE-4107 and
  BUG-74B6, logs the held-back Avalonia set, and keeps the BUG-4A17 known issue.
- The README callout names 1.13. `SECURITY.md` supports `1.13.x` only, and its scope names the explorer.
- No dev runs `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`, and no dev writes to `develop` or `main`.

## Out of scope

- NuGet packaging; an MSI profile.
- Any change to the application's behaviour; the flaky and failing tests named above.
- The manual checks in a real window; the merges, the tag and any push.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.13.0 | The request names it; two features and a fix, nothing breaking: a minor bump | 1.12.1 (it adds features); 2.0.0 (nothing breaks) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase; the 1.6–1.12 precedent | Splitting notes and version into two devs |
| Contents | FEATURE-F0CE, FEATURE-4107 and BUG-74B6 | Exactly `git log 1.12.0..HEAD` | Waiting for BUG-4A17, which is quarantined |
| Notes shape | 1.12.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Where the wheel fix goes | In *The tab strip*, with the double-click | The notes group by theme and fold fixes into it (1.4's *Panes*, 1.7's *Colours*) | A separate *Fixes* section |
| Order of the themed sections | The explorer first, then the tab strip | The explorer is the release's headline; the strip changes are small | Roadmap order (4107 first) |
| Avalonia 12.1.5 | Held back at 12.1.4, logged | The set moves only with Enigma.Avalonia.Desktop (1.4–1.8 precedent), which has not moved | Bumping the Avalonia packages alone |
| README | Callout only; only touched lines reflowed | F0CE and 4107 already updated *Files*, *The status bar* and *Tabs and panes* | Rewriting other sections; reflowing the whole callout |
| `SECURITY.md` scope | Widened to the explorer | It writes, renames and deletes files, takes names, and on Linux runs `gio` and writes the trash: new surface, as the pipe was in 1.10 | Leaving the scope as it is |
| `SECURITY.md` versions | `1.13.x` only | Latest-minor-only policy | Keeping `1.12.x` supported |
| Prerelease packages | Never | A release ships on stable dependencies | Taking a .NET 11 preview or RC |
| MSI profile | None | Not asked for; the 1.0–1.12 precedent | Generating a first profile |
| Release date | The run date | 1.12.0's precedent | A future date |
| Open manual checks | Listed for the user in the done doc and the run report | A run opens no window on the user's desktop; they are not release edits | Adding them to `docs/RELEASE.md`'s generic checklist |
| Starting point | The current `HEAD`, the three items' unmerged run branch | `/vibe` cuts from `HEAD`; those items are what the release ships | Starting from `develop`, which lacks them |

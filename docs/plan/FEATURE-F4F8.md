# FEATURE-F4F8 — Release 1.19.0

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-f4f8-release-1-19-0`
**Run:** feature/2026-11-10-release-1-19-0

## Objective

Cut **1.19.0**, the minor bump of 1.18.0 (`x.y+1.z`, with `z` = 0). Use `dotnet-release`'s routine-release path for an
app, with FEATURE-EF88 (1.18.0) as the template. The release covers:

- the version, which is also the version `buildmsi` builds;
- the release notes for what 1.19.0 changes;
- the README callout;
- `SECURITY.md`, its supported versions and its scope;
- a dependency check.

The merges into `develop` and `main`, the `1.19.0` tag, any push and the MSI built for distribution are printed for
the user to run, not run here.

## Context & constraints

- **An app, not a package.** Neither project has a `PackageId`, so pack, push and `PackageReleaseNotes` do not apply.
  The version is `<Version>` in both csprojs, `1.18.0` today, and the two move together.
- **Tags:** `1.17.0` is on `main` (`0fbad94`). Neither `1.18.0` nor `1.19.0` exists. 1.18.0 (`dd5ae34`) is merged
  into `develop` (`89ac412`) but not into `main`, so `main` and the tags still stop at 1.17.0. Tags are bare `X.Y.Z`
  and lightweight, on `main`'s merge commit.
- **`buildmsi.ps1` holds no version literal.** It reads `<Version>` from the Desktop csproj and stops when Core's
  differs, so bumping the csprojs is all `buildmsi` needs.
- **1.19.0 carries three items since 1.18.0** (`git log 89ac412..HEAD`), from two runs that are not yet merged into
  `develop`. `feature/2026-11-10-set-lang-zoom-gestures` sits on `feature/2026-11-10-hex-selection-copy`:
  - **FEATURE-BE30, selecting and copying bytes in the hex view:**
    - press and drag in the hex column or in the text column to select bytes in that column. The same bytes are
      shown in the other column at half strength. Shift with a press extends the selection from its anchor;
    - dragging above or below the rows scrolls a row per move and extends the selection;
    - a press past the last byte of a short last row selects that row's last byte;
    - the right-click menu has *Copy Hex String* (`48 65 6C`), *Copy Text* (as the text column shows it) and
      *Copy Address Range* (`0x00000001 - 0x00000004`). A right-click inside the selection keeps it; elsewhere it
      selects the byte under the pointer. The items are disabled when nothing is selected;
    - **Ctrl+C** copies in the format of the selection's column, with vim on or off. A pending `:` line or `Ctrl+W`
      still takes the key first;
    - hex and text copies stop at 1 MB, with a status-line message. The range is always copied, and a read failure
      is reported on the status line;
    - the current row is now marked in the editor's current-line colour instead of the selection colour, and the
      pointer-over band is gone;
    - the arrow keys still move the current row and leave the selection alone. The selection is not kept in the
      session.
  - **FEATURE-2BFB, `:set lang`:**
    - `:set lang` (or `:set language`) opens the language list, the same list as the status bar's language button
      and **Tools → Set Language…**, for the buffer it was typed in;
    - `:set lang?` shows `language=<id>`, and `:set lang=<name>` sets the language;
    - in the hex view, the command is refused with the existing message;
    - Help: *Options* gains *Pick the language from the list* — `:set language, lang`.
  - **FEATURE-A47C, zooming with Ctrl+wheel and the touchpad pinch:**
    - Ctrl+wheel over the editor area (text, hex rows, tab strips, minimaps) changes the font size by one size per
      notch, between 6 and 72. Up zooms in, down zooms out. A Ctrl+wheel no longer scrolls;
    - precision touchpads and high-resolution wheels add their fractions up;
    - the touchpad's zoom gesture (fingers apart to zoom in, together to zoom out) zooms one size per 0.1 of
      magnification. On Windows it arrives as Ctrl+wheel;
    - neither gesture has been tested on real hardware. The headless tests cover their code paths;
    - Help: *Application* gains `Ctrl+wheel` and *Pinch on the touchpad*.
  - The README already describes all three: *Hex viewer*, the `:set` paragraph, and the zoom sentence.
- **1.18.0's notes are history and are not edited.** Neither is the README's 1.7 line, which says the hex view's
  current row uses the selection colour.
- **No settings key and no file change.** The zoom writes the existing font-size setting, and the session format is
  unchanged. Going back to 1.18 reads the same files.
- **BUG-4A17** is still quarantined, so the *Known issue* line stays.
- **`SECURITY.md`** supports only the latest minor line, `1.18.x` today. Its scope names "the hex viewer" among the
  ways a file is read. The hex viewer can now copy file bytes to the clipboard.
- **The dependency rule** (`Directory.Packages.props`): the Avalonia set and its companions move together, and only
  together with Enigma.Avalonia.Desktop and Enigma.Icons.Avalonia. On 2026-11-10, `dotnet list package --outdated`
  shows six packages with an update to 12.1.6: Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter,
  Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia. Nothing else has an update, and
  Enigma.Avalonia.Desktop has none.
- **A run may not run `buildmsi.ps1`.** It starts with `git clean -dxf`, which `vibe-workflow` §5 forbids.

## Steps

1. Check that `git tag --list 1.19.0` is empty.
2. Set `<Version>1.19.0</Version>` in both csprojs.
3. `RELEASENOTES.md`: add a dated `## 1.19.0 — 2026-11-10` section above 1.18.0's, in the repository's style:
   - a summary;
   - *Selecting and copying bytes in the hex view* (FEATURE-BE30);
   - *`:set lang` opens the language list* (FEATURE-2BFB);
   - *Zooming with Ctrl+wheel and the touchpad* (FEATURE-A47C);
   - *Upgrading from 1.18*:
     - Ctrl+wheel zooms instead of scrolling;
     - a hex press selects a byte;
     - the current hex row has a new colour;
     - no new key or file, and going back to 1.18;
     - the MSI name;
     - the Linux upgrade;
   - *Dependencies*;
   - *Compatibility*;
   - *Known issue* (BUG-4A17, by its symptom);
   - *Version*.

   Take every user-visible string (menu items, Help rows, messages) from the source, not from memory.
4. `README.md`: the callout names 1.19 first, then 1.18, then 1.17 down to 1.0. Reflow only the lines this release
   touches.
5. `SECURITY.md`:
   - `1.19.x` is supported; `1.18.x` and earlier are not;
   - the scope's hex-viewer clause also names what the viewer copies to the clipboard from a selection.
6. `docs/RELEASE.md`: fix anything 1.19.0 makes untrue, and leave the generic `X.Y.Z` steps as they are.
7. **Dependency check** with `dotnet list package --outdated`:
   - apply any bump outside the coupled set;
   - move the Avalonia set only as a whole, and only if Enigma.Avalonia.Desktop has moved;
   - log the result in the notes.
8. Pre-flight: a Release build with zero warnings whose assemblies carry `1.19.0`. Then the Release suite, measured
   against the run's baseline.

## Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx -c Release` is clean, with zero warnings, and the built assemblies carry
  `1.19.0`.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release` has no failure that the run's baseline did not have.
- `RELEASENOTES.md` has one dated `1.19.0` section above 1.18.0's. It:
  - covers FEATURE-BE30, FEATURE-2BFB and FEATURE-A47C;
  - states the behaviour changes under *Upgrading from 1.18*;
  - logs the dependency check;
  - keeps the BUG-4A17 known issue.
- The README callout names 1.19.
- `SECURITY.md` supports `1.19.x` only, and its scope names the hex viewer's clipboard copy.
- `docs/RELEASE.md` says nothing that 1.19.0 makes untrue.

## Out of scope

- NuGet packaging, and a committed MSI profile.
- Printed for the user, not run here:
  - merging into `develop` and `main`;
  - tagging `1.18.0` and `1.19.0`;
  - pushing;
  - building the 1.19.0 MSI.
- Any change to the application's behaviour, including these follow-ups:
  - FEATURE-BE30: keyboard selection, Select All, *Edit → Copy* on a hex tab, the selection length in the status
    bar;
  - FEATURE-A47C: tuning the zoom gestures on real hardware.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Version number | 1.19.0 | The draft asks for `x.y+1.z` from 1.18.0, and `z` is 0. A minor bump fits: the release adds features and breaks no stored format | 2.0.0 (nothing breaks); 1.18.1 (features are not a patch) |
| Work-item shape | One single-phase FEATURE | `dotnet-release`: a routine release is one phase | Several phases (notes, version, docs): too small to split |
| Notes shape | 1.18.0's | The repository's own style | The template's *New Features · Fixes* headings |
| Release date | 2026-11-10 | The run date | A future date |
| Cut from | The current `HEAD`, the unmerged run `feature/2026-11-10-set-lang-zoom-gestures` | The command cuts from `HEAD`, and the three items the release documents are there | Cutting from `develop`, which lacks all three |
| Behaviour changes users rely on | Stated twice, in each item's section and under *Upgrading from 1.18*: Ctrl+wheel zooms instead of scrolling, a hex press selects a byte, the current hex row has a new colour | Each changes what a gesture does, or how the view looks | Only in the item's section |
| Zoom not tested on real hardware | The notes describe the gestures as the README does. The done doc and the end-of-run report ask the user to check them before distributing | The behaviour is documented platform behaviour and its paths are tested. Hedging in the notes would read as a defect | A *Known issue* line (nothing is known to fail) |
| Avalonia 12.1.6 | Held back at 12.1.4 | The set moves as a whole with Enigma.Avalonia.Desktop, which has not moved | Bumping Avalonia alone |
| `SECURITY.md` scope | The hex-viewer clause also names the clipboard copy and its 1 MB limit | The viewer now reads file bytes onto the clipboard, a new path that is bounded by a limit. The error-dialog bullet already scopes a clipboard copy | Unchanged |
| 1.18.0 is not on `main` and has no tag | Reported. The end-of-run report prints merging `develop` into `main` and tagging `1.18.0` before 1.19.0's merge | Tags go on `main`'s merge commits; merging 1.19 first would leave 1.18.0 with nothing to tag | Tagging it here (§5 forbids `tag` and writes to `main`) |
| Merges, tag, push | Printed in the end-of-run report, not run | `vibe-workflow` §5 forbids writing to `develop` or `main`, `tag` and `push` | Running them |
| `buildmsi.ps1` | Not run. Left to the user before distributing | It runs `git clean -dxf`, which §5 forbids, and would delete untracked files | Running it |
| Pre-flight | A Release build and the Release suite, measured against a Release baseline taken on the run branch | `docs/RELEASE.md` §1 asks for both in Release, and the baseline's configuration must match the dev's | Debug only (not what ships) |

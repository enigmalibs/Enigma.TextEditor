# FEATURE-C188 — Release 1.12.0

**Item:** FEATURE-C188 — Release 1.12.0
**Branch:** `feature/feature-c188-release-1-12-0`
**Run:** feature/2026-10-31-release-1-12-0

## Summary

1.12.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-1CC6 (1.11.0) as the template:

- **Version:** `<Version>1.12.0</Version>` in both csprojs (was 1.11.0).
- **`RELEASENOTES.md`:** a dated `## 1.12.0 — 2026-10-31` section above 1.11.0's. It holds a summary, then:
  - *Splitting a pane with dropped files* (FEATURE-B751): the edge zones, the highlight, one split for all the files,
    the pane with no tab, what the split pane keeps, nothing to open, every file refused, text drags, the help row;
  - *The font size on the status bar* (FEATURE-A198): the list, the default, an off-list size, what choosing does,
    what the button follows, when it shows, the keyboard;
  - *Upgrading from 1.11*, *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.12 first, then sums up 1.11 down to 1.0.
- **`SECURITY.md`:** `1.12.x` is supported, `1.11.x` and earlier are not. The scope is unchanged.
- **Dependencies:** `dotnet list package --outdated` was run again; nothing has moved.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-C188.md`
- Created: `docs/done/FEATURE-C188.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the help row is listed | *Files & tabs* | That is the section's real name in `HelpCatalog`; the plan said *Files* |
| How the 1.11 file drop is contrasted | "In 1.11, files carried over a pane were not highlighted at all" | B751's plan: files opened where dropped "with no hint". It is the visible change for a middle drop |
| The reset the button follows | The settings page's *Reset to defaults*, named | `SettingsPageView`'s button; "a reset" alone could read as Reset Zoom, which the zoom keys already cover |
| README callout wording for 1.11 | "splitting a pane by dropping a tab near its edge, its own pane included, with the half it will take highlighted" | Fits 1.11's lead into the "1.11 brought …" summary form the earlier entries use |
| README reflow | The two rewritten lines became four, all within 120 columns; lines from 1.10 down are untouched | Plan decision: no reflow of the older overlong lines |
| Release date | 2026-10-31 | The run's date |

## Deviations & follow-ups

- **Help section name.** The plan's step 3 called the help group *Files*; it is *Files & tabs*. The notes use the real
  name.
- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong.
- **Manual checks still open**, for the user before tagging (this run opened no window):
  - FEATURE-A198: the font size list, by eye, in both themes;
  - FEATURE-B751: a real file manager's drag across two panes, out of the window, and cancelled (Escape in the file
    manager), on each platform. How X11/Wayland and Windows OLE report leave and cancel was only simulated headless.
- **Starting point.** The run was cut from `feature/2026-10-31-font-size-drop-split` (A198 and B751's run), which is
  not merged into `develop`. Merging this run into `develop` brings both features with it.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.12.0. Its plan's Status line reads `TODO` while
  its roadmap row reads `IN PROGRESS`; that is the earlier run's state and is left alone.
- The README callout still has four overlong lines in the 1.10 to 1.5 entries (126 to 186 columns). Reflowing them is
  a separate cosmetic change, not a release edit.
- The merges into `develop` and `main`, the `1.12.0` tag and the push are the user's (see `docs/RELEASE.md` §2–§4).

## Documentation sweep

- README callout, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.
- Checked and left as they are: `docs/RELEASE.md`; the README's *Files* paragraph and *The status bar* (B751 and A198
  already describe them); `SECURITY.md`'s scope (the file drop and the font size setting both existed: no new
  surface).

## Build/test evidence

- **Run baseline** (run branch at `3f1e3d6`, before this dev): `dotnet build Enigma.TextEditor.slnx --no-incremental`:
  0 warnings, 0 errors; `dotnet test --solution Enigma.TextEditor.slnx`: 2945 passed, 0 failed, 0 skipped. No
  pre-existing warning or failure.
- `git tag --list 1.12.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2945 total, 2945 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2945 total, 2945 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.12.0.0`;
  - informational version `1.12.0+3f1e3d6…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: no updates for any of the four projects. With `--include-prerelease`, only
  Microsoft.Extensions.Hosting `11.0.0-rc.3.26522.148`, not taken.
- No tests were added: the dev changes no code. Fix cycles: 0 of 3.

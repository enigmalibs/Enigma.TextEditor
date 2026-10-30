# FEATURE-1CC6 — Release 1.11.0

**Item:** FEATURE-1CC6 — Release 1.11.0
**Branch:** `feature/feature-1cc6-release-1-11-0`
**Run:** feature/2026-10-30-release-1-11-0

## Summary

1.11.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-2448 (1.10.0) as the template:

- **Version:** `<Version>1.11.0</Version>` in both csprojs (was 1.10.0).
- **`RELEASENOTES.md`:** a dated `## 1.11.0 — 2026-10-30` section above 1.10.0's. It holds a summary, then:
  - *Splitting a pane with a dragged tab* (FEATURE-9E4A): the edge zones, the half-pane highlight, the tab's own
    pane, what the new pane gets, and that the `Space m` keys are unchanged;
  - *Upgrading from 1.10*, *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.11 first, then sums up 1.10 down to 1.0.
- **`SECURITY.md`:** `1.11.x` is supported, `1.10.x` and earlier are not. The scope is unchanged.
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
  - `docs/plan/FEATURE-1CC6.md`
- Created: `docs/done/FEATURE-1CC6.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the edge drop works | "a pane's text or hex view"; the empty placeholder not named | The side is found from the pane's geometry (`EditorPaneView.SideAt`), so it holds over either view. A pane with no tab is only ever the last pane, so no tab can be dragged onto it |
| Upgrading note on the running window | A 1.10 window still open takes a 1.11 launch's files; close every window to start 1.11 | `git diff 1.10.0..HEAD` leaves the instance pipe and `instance.json` untouched, so the hand-over works across the two versions, and an old window would keep receiving files |
| README callout wording for 1.10 | "for a file opened from the system while the editor is running, a tab in the window you used last" | Fits 1.10's sentence into the "1.10 brought …" summary form the earlier entries use |
| Release date | 2026-10-30 | The run's date |

## Deviations & follow-ups

- **Keyboard route.** The planning summary printed in the console proposed naming `Ctrl+W v`, then `Space ml`, as a
  keyboard route. Checking `WorkspaceViewModel.Split` before the plan was committed showed that this split opens an
  untitled tab in the new pane and moves the keyboard there, so the plan dropped it. The notes say instead that the
  `Space m` keys are unchanged.
- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong.
- **Manual checks still open from FEATURE-9E4A**, for the user before tagging (no window was opened by this run):
  - the half-pane hint, by eye, in both themes;
  - whether a quarter of the width or height is the right edge band on very wide panes.
- **Starting point.** The run was cut from `feature/2026-10-30-drag-drop-split` (FEATURE-9E4A's run), which is not
  merged into `develop`. Merging this run into `develop` brings 9E4A with it.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.11.0. Its plan's Status line reads `TODO` while
  its roadmap row reads `IN PROGRESS`; that is the earlier run's state and is left alone.
- The merges into `develop` and `main`, the `1.11.0` tag and the push are the user's (see `docs/RELEASE.md` §2–§4).

## Documentation sweep

- README callout, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.
- Checked and left as they are: `docs/RELEASE.md`; the README's *Tabs and panes* (FEATURE-9E4A already describes the
  edge drop there); `SECURITY.md`'s scope (the drag is in-window and opens no new surface).

## Build/test evidence

- **Run baseline** (run branch at `6ead34c`, before this dev): `dotnet build Enigma.TextEditor.slnx --no-incremental`:
  0 warnings, 0 errors; `dotnet test --solution Enigma.TextEditor.slnx`: 2911 passed, 0 failed, 0 skipped. No
  pre-existing warning or failure.
- `git tag --list 1.11.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2911 total, 2911 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2911 total, 2911 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.11.0.0`;
  - informational version `1.11.0+6ead34c…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: no updates for any of the four projects. With `--include-prerelease`, only
  Microsoft.Extensions.Hosting `11.0.0-rc.3.26522.148`, not taken.
- No tests were added: the dev changes no code. Fix cycles: 0 of 3.

# FEATURE-2448 — Release 1.10.0

**Item:** FEATURE-2448 — Release 1.10.0
**Branch:** `feature/feature-2448-release-1-10-0`
**Run:** feature/2026-10-30-release-1-10-0

## Summary

1.10.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-8EDF (1.9.0) as the template:

- **Version:** `<Version>1.10.0</Version>` in both csprojs (was 1.9.0).
- **`RELEASENOTES.md`:** a dated `## 1.10.0 — 2026-10-30` section above 1.9.0's. It holds a summary, then:
  - *Moving tabs between panes* (FEATURE-9F56, with BUG-5A45's drag folded in);
  - *Files opened while the editor is running* (FEATURE-7B55), with its limits;
  - *Upgrading from 1.9*, *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.10 first, then sums up 1.9 down to 1.0. *Your data* now names `instance.json`.
- **`SECURITY.md`:**
  - `1.10.x` is supported, `1.9.x` and earlier are not;
  - the scope adds the instance pipe and `instance.json`.
- **Dependencies:** `dotnet list package --outdated` was run again; nothing has moved. The notes say so, and name the
  Microsoft.Extensions.Hosting 11.0.0 release candidate that was not taken.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-2448.md`
- Created: `docs/done/FEATURE-2448.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How BUG-4A17 is named in the notes | A *Known issue* line by its symptom, no work-item ID | The plan asks the notes to name it; the notes are written for users, who meet the symptom, not the ID |
| Forwarding timeout in the notes | "within a few seconds" | The client waits 1 s to connect, then 2 s for the reply (`InstanceClient`), so "two seconds" would be wrong |
| What the notes say about folders | "a folder named alongside files is left out" | The launch side forwards files only (`InstanceForwarding`). The receiver's folder report never sees a forwarded folder |
| macOS in *Compatibility* | Built for Windows and Linux; not tried on macOS | FEATURE-7B55 put Apple events out of scope, and its tests ran on Linux only |
| Release date | 2026-10-30 | The run's date |

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged. None of its generic `X.Y.Z` steps became wrong; its Linux `strings | grep` version
  check was run against this build and finds `1.10.0+<sha>`.
- **Manual checks still open from the merged items**, for the user before tagging (no window was opened by this run):
  - FEATURE-9F56 / BUG-5A45: drag a tab in a real window. The pointer is the move cursor, notably on KDE Wayland, and
    the drop highlight reads in both themes;
  - FEATURE-7B55: with one or two windows open, double-click an associated file on Windows, and use the launcher on
    Linux. The file opens in the window used last, which comes to the front.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.10.0. Its plan's Status line reads `TODO` while
  its roadmap row reads `IN PROGRESS`; that is the earlier run's state and is left alone.
- The merges into `develop` and `main`, the `1.10.0` tag and the push are the user's (see `docs/RELEASE.md` §2–§4).

## Documentation sweep

- README callout and *Your data*, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.
- Checked and left as they are: `docs/RELEASE.md`; the README's *Install on Linux* and `packaging/linux/uninstall.sh`,
  which say the settings and the log are left alone. They still are, and so is `instance.json`.

## Build/test evidence

- **Run baseline** (run branch at `5583f0d`, before this dev): `dotnet build Enigma.TextEditor.slnx --no-incremental`:
  0 warnings, 0 errors; `dotnet test --solution Enigma.TextEditor.slnx`: 2875 passed, 0 failed, 0 skipped. No
  pre-existing warning or failure.
- `git tag --list 1.10.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2875 total, 2875 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2875 total, 2875 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.10.0.0`;
  - informational version `1.10.0+5583f0d…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: no updates for any of the four projects. With `--include-prerelease`, only
  Microsoft.Extensions.Hosting `11.0.0-rc.3.26522.148`, not taken.
- No tests were added: the dev changes no code. Fix cycles: 0 of 3.

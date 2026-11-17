# FEATURE-96BA — Release 1.20.0

**Item:** FEATURE-96BA — Release 1.20.0
**Branch:** `feature/feature-96ba-release-1-20-0`
**Run:** feature/2026-11-17-release-1-20-0

## Summary

1.20.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-F4F8 (1.19.0) as the template.

- **Version:** `<Version>1.20.0</Version>` in both csprojs (was 1.19.0).
- **`buildmsi`:** unchanged. It reads the version from the csprojs, so it now builds
  `Enigma.TextEditor.1.20.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.20.0 — 2026-11-17` section above 1.19.0's. It has a summary, then:
  - *The explorer comes back with the session* (FEATURE-768C);
  - *The session is kept while the editor runs* (FEATURE-4158);
  - *Upgrading from 1.19*: no new key or file, `session.json`'s two new members both ways, the file now written while
    the editor runs, the MSI, Linux;
  - *Dependencies*, *Compatibility*, *Known issue* (BUG-4A17, by its symptom), *Version*.
- **README:** the callout names 1.20 first, then 1.19, then 1.18 down to 1.0.
- **`SECURITY.md`:**
  - `1.20.x` is supported; `1.19.x` and earlier are not;
  - the session clause in the scope names the explorer's folder, which is reopened at the start, and the write every
    few seconds in the background.
- **Dependencies:** Microsoft.Extensions.Hosting 10.0.14 → 10.0.15. Its 11.0.0 is not taken (.NET 11 band). The
  Avalonia set is held back at 12.1.4: 12.1.6 is out, but Enigma.Avalonia.Desktop has not moved past 1.3.0.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `Directory.Packages.props`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-96BA.md`
- Created: `docs/done/FEATURE-96BA.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the notes' facts come from | The done docs of FEATURE-768C and FEATURE-4158 PHASE01–02. Checked in the source: the order of `MainWindowViewModel.StartAsync`, `SessionAutosave.Interval` (5 s), the log lines in `FileExplorerViewModel.RestoreAsync` and `SessionStore`, and `SessionFile`'s `JsonSerializerDefaults.Web` | The notes must match what shipped |
| Going back to 1.19 | "1.19 ignores the explorer's folder" | `JsonSerializerDefaults.Web` skips unknown members, so 1.19 reads a 1.20 file whole |
| How the background write is described | "so writing the file does not hold up typing" | The capture runs on the UI thread and only the write is off it, so "never held up" would claim too much |
| `docs/RELEASE.md` | Not edited | Its steps are generic `X.Y.Z`. Its uninstall line already names `session.json` and `session.lock` |
| README reflow | The callout's first four lines became six, all within 120 columns. The lines from 1.18's "back:" on are untouched | The plan: reflow only the lines this release touches |

## Deviations & follow-ups

- **Before distributing, for the user:**
  - build the 1.20.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted. It runs
    `git clean -dxf`, which this run may not;
  - install it over the installed version, and check that *Installed apps* lists one *Enigma Text Editor* at 1.20.0
    (`docs/RELEASE.md` §6);
  - **check a real crash once:** with a change unsaved, wait more than five seconds, kill the process, and start
    again. The change has to come back. FEATURE-4158 tested this only through the harness.
- **Merging into `develop` and `main`, the `1.20.0` tag and the push are the user's** (`docs/RELEASE.md` §2–4). This
  release sits on the unmerged run `feature/2026-11-16-session-explorer-autosave`. Merging this run's branch into
  `develop` brings both.
- **Pre-existing test failure** (the run's baseline, on this Linux machine):
  `HexMouseSelectionTests.APressPastTheLastByteOfAShortRow_SelectsItsLastByte`. It also fails on `develop` at
  `bcf0c95`, before FEATURE-768C and FEATURE-4158. The done docs written on Windows recorded
  `InstanceServerTests.AClientSpeakingAnotherLanguage_…` instead, which passes here. Left alone: neither is in this
  release's scope.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.20.0.
- The follow-ups of FEATURE-768C and FEATURE-4158 are not in 1.20.0: the explorer's width, expanded folders and
  selection; a setting for the interval; a status-line notice when the session cannot be written.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md` and `SECURITY.md`, as above.
- Checked and left as they are:
  - `docs/RELEASE.md`, which is generic;
  - the README's quitting paragraph and `session.json` line, which the two items already updated;
  - `src/Enigma.TextEditor.Core/Vim/README.md`, which the version does not affect.

## Build/test evidence

- `git tag --list 1.20.0`: empty before the release. The last tag is `1.19.0`, on `main` (`06a49a0`).
- **Run baseline** (run branch `feature/2026-11-17-release-1-20-0` @ `ff352dd`, before the dev branch, SDK 10.0.103):
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3371 tests, 3370 passed, 1 failed
    (`HexMouseSelectionTests.APressPastTheLastByteOfAShortRow_SelectsItsLastByte`).
- `dotnet list Enigma.TextEditor.slnx package --outdated` (2026-11-17):
  - Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia
    have 12.1.6;
  - Microsoft.Extensions.Hosting has 11.0.0, and 10.0.15 with `--highest-patch`;
  - nothing else has an update.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3371 tests, 3370 passed, 1 failed (the baseline's);
  - `project.assets.json` resolves Microsoft.Extensions.Hosting 10.0.15.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry the informational version
  `1.20.0+ff352dd…`, the run-branch tip the build was cut from.
- No tests were added, because the dev changes no code.

# FEATURE-EF88 — Release 1.18.0

**Item:** FEATURE-EF88 — Release 1.18.0
**Branch:** `feature/feature-ef88-release-1-18-0`
**Run:** feature/2026-11-09-release-1-18-0

## Summary

1.18.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-8AEF (1.17.0) as the template.

- **Version:** `<Version>1.18.0</Version>` in both csprojs (was 1.17.0).
- **`buildmsi`:** unchanged. It reads the Desktop csproj's `<Version>` and requires Core's to match, so it now builds
  `Enigma.TextEditor.1.18.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.18.0 — 2026-11-09` section above 1.17.0's. It holds a summary, then:
  - *The session comes back* (FEATURE-34A1), which covers:
    - what quitting keeps and the unsaved changes;
    - `:qa!`;
    - files read again from disk, and the skipped ones;
    - command-line files;
    - the one window that keeps the session;
    - what a crash brings back;
    - the session files and the Help rows.
  - *`Ctrl+Y` redoes with vim on* (BUG-C6A9);
  - *Upgrading from 1.17*:
    - quitting no longer asks, and `Ctrl+Y` no longer scrolls;
    - the new files;
    - going back to 1.17;
    - the MSI;
    - Linux.
  - *Dependencies*, *Compatibility*, *Known issue* (BUG-4A17, by its symptom), *Version*.
- **README:** the callout names 1.18 first, then 1.17, then 1.16 down to 1.0.
- **`SECURITY.md`:** `1.18.x` is supported, and `1.17.x` and earlier are not. The scope's settings-and-log bullet now
  names the session (`session.json`, what is reopened from it, `session.lock`).
- **`docs/RELEASE.md` §5:** what an uninstall leaves now names `session.json` and `session.lock`.
- **Dependencies:** `xunit.v3` 4.0.2 → 4.0.3. The Avalonia set is held back at 12.1.4 (12.1.6 is out, but
  Enigma.Avalonia.Desktop has not moved past 1.3.0).

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `Directory.Packages.props`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`, `docs/RELEASE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-EF88.md`
- Created: `docs/done/FEATURE-EF88.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Release-notes facts | Taken from the done docs of BUG-C6A9 and FEATURE-34A1 PHASE01–04. The Help wording comes from `HelpCatalog.cs`, the status-line text from `WorkspaceViewModel.cs`, and the `:qa!` capture rule from `WorkspaceViewModel.CaptureSession` | The notes must match what shipped |
| What `:qa!` brings back | "the files' tabs come back as the files are on disk, and an untitled document with text does not come back" | `CaptureSession(keepUnsaved: false)` stores an untitled buffer only when it is empty |
| **File → Exit** among the ways to close | Named | `ExitCommand` takes the same `RequestExit` path as `:qa` |
| README reflow | The callout's first three lines became six, all within 120 columns. The lines from 1.16 on are untouched | The plan: only the lines this release touches are reflowed |

## Deviations & follow-ups

- **Before distributing, for the user:**
  - build the 1.18.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted (it runs
    `git clean -dxf`, which this run may not);
  - install it over the installed version, and check that *Installed apps* lists one *Enigma Text Editor* at 1.18.0
    (`docs/RELEASE.md` §6).
- **Merging into `develop` and `main`, the `1.18.0` tag and the push** are the user's (`docs/RELEASE.md` §2–4). The
  release sits on top of the unmerged run `bugfix/2026-11-09-ctrl-y-session-restore`, so merging this run's branch
  into `develop` brings both.
- **Pre-existing test failure** (the run's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_…`. The
  flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` passed in both runs.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.18.0.
- FEATURE-34A1's follow-ups are not in this release, and the notes state their effect: writing the session while the
  editor runs, and a setting to turn the restore off.
- The README's uninstall sentences ("leaves your settings…", "Your settings and the log…") are still true and are
  unchanged, though they do not mention the session.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md`, `SECURITY.md` and `docs/RELEASE.md`, as above.
- Checked and left as they are:
  - `docs/RELEASE.md`'s other steps are generic `X.Y.Z`, and the versions it names are still true;
  - the README's feature text and *Your data* already describe the session (FEATURE-34A1-PHASE04);
  - the `xunit.v3` comment in `Directory.Packages.props` (Avalonia.Headless.XUnit pins 3.x) is unaffected.

## Build/test evidence

- `git tag --list 1.18.0`: empty before the release. `1.17.0` is on `main` (`0fbad94`).
- **Run baseline** (run branch `feature/2026-11-09-release-1-18-0` @ `59c8cfc`, before the dev branch):
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3257 tests, 3256 passed, 1 failed
    (`InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`).
- `dotnet list package --outdated` (2026-11-09):
  - the six Avalonia packages have 12.1.6;
  - `xunit.v3` has 4.0.3;
  - nothing else has an update.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3257 tests, 3256 passed, 1 failed (the baseline's).
  - Both test projects resolve `xunit.v3` 4.0.3.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry file version `1.18.0.0` and
  informational version `1.18.0+59c8cfc…`, the run-branch tip the build was cut from.
- No tests were added, because the dev changes no code.

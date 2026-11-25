# FEATURE-6C0E — Release 1.21.0

**Item:** FEATURE-6C0E — Release 1.21.0
**Branch:** `feature/feature-6c0e-release-1-21-0`
**Run:** feature/2026-11-25-release-1-21-0

## Summary

1.21.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-96BA (1.20.0) as the template. It ships
CODE-REVIEW-0734, the fixes for the High and Medium findings of the 2026-11-19 review.

- **Version:** `<Version>1.21.0</Version>` in both csprojs (was 1.20.0).
- **`buildmsi`:** unchanged. It reads the version from the csprojs, so it now builds
  `Enigma.TextEditor.1.21.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.21.0 — 2026-11-25` section above 1.20.0's. It has a summary, then:
  - *Saving cannot leave a file cut short* (PHASE06);
  - *The window keeps responding during file work* (PHASE07, PHASE20);
  - *The session writes only what changed* (PHASE18, PHASE19);
  - *Fixes and diagnostics* (PHASE01, PHASE04, PHASE05);
  - *Under the hood* (PHASE08–17, and the test waits of PHASE02, 03 and 21);
  - *Upgrading from 1.20*: no new key, the session's format 2 and the `unsaved` folder, going back to 1.20,
    `early-errors.log`, the MSI, Linux;
  - *Dependencies*, *Compatibility*, *Known issue* (BUG-4A17, by its symptom), *Version*.
- **README:** the callout names 1.21 first, then 1.20, then 1.19 down to 1.0.
- **`SECURITY.md`:**
  - `1.21.x` is supported; `1.20.x` and earlier are not;
  - the write-back clause names the temporary file beside the file, the metadata carried over and the move;
  - the log clause names `app.log` and `early-errors.log`.
- **`docs/RELEASE.md`:** the uninstall line names the `unsaved` folder, and "the logs" for "the log".
- **Dependencies:** nothing moves. The Avalonia set is held back at 12.1.4 (12.1.6 and 12.2.0 are out, but
  Enigma.Avalonia.Desktop has not moved past 1.3.0). Microsoft.Extensions.Hosting stays at 10.0.15: no later 10.0.x
  exists, and 11.0.0 is the .NET 11 band.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`, `docs/RELEASE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-6C0E.md`
- Created: `docs/done/FEATURE-6C0E.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the notes' facts come from | The 21 done docs of CODE-REVIEW-0734. Checked in the source: `DocumentFile`'s temporary name, in-place fallbacks and metadata copy; `SessionState.CurrentVersion` 2 and `OldestVersion` 1; `AppPaths`' `unsaved`, `unsaved.invalid` and `early-errors.log`; `UnhandledExceptionHooks.EarlyFileLimit` (1 MiB); `SessionStore`'s missing-text warning; `FileExplorerViewModel`'s two `LogError` lines; `InstanceServer`'s 5-second time-out and one-at-a-time serving | The notes must match what ships |
| Going back to 1.20 | "1.20 moves `session.json` aside and starts without it; the texts stay in `unsaved` until 1.21 next writes" | 1.20.0's `SessionFile.Validate` throws on any version but 1, then quarantines the file. 1.20 knows nothing of `unsaved`. 1.21's write deletes the texts its session does not name |
| Temporary name in the notes | `.<name>.<random>.tmp` | The source uses a GUID; "random" says what a user needs, which is that the name differs every time |
| The refactors | One *Under the hood* list, by area | Users see no change; the list tells a reader of the diff where the code went |
| README callout | No "See RELEASENOTES.md" sentence | The existing callout has none, and the repository's style wins over the template |
| `SECURITY.md` session clause | Reflowed as a whole after the log clause grew | One bullet, so the raw text keeps its width; the words are unchanged past the log clause |

## Deviations & follow-ups

- No deviation from the plan.
- **Before distributing, for the user:**
  - build the 1.21.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted. It runs
    `git clean -dxf`, which this run may not;
  - install it over the installed version, and check that *Installed apps* lists one *Enigma Text Editor* at 1.21.0
    (`docs/RELEASE.md` §6);
  - **check the session upgrade once:** with an unsaved change in 1.20, quit, start 1.21, and check the change comes
    back and an `unsaved` folder appears beside `session.json`.
- **Merging into `main`, the `1.21.0` tag and the push are the user's** (`docs/RELEASE.md` §2–4). CODE-REVIEW-0734 is
  already on `develop`; merging this run's branch into `develop`, then `develop` into `main`, brings the release.
- **Pre-existing test failure and skip** (the run's baseline, on this Linux machine):
  - failed: `HexMouseSelectionTests.APressPastTheLastByteOfAShortRow_SelectsItsLastByte`. It also fails on `develop`
    before CODE-REVIEW-0734 (FEATURE-96BA's done doc records it at 1.20.0). Left alone: not in this release's scope.
  - skipped: `DocumentFileAtomicWriteTests.AHiddenFile_StaysHidden_AndKeepsItsCreationTime`, a Windows-only test.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.21.0.
- The review's Low findings are not in 1.21.0.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md`, `SECURITY.md` and `docs/RELEASE.md`, as above.
- Checked and left as they are:
  - the README's data section, which CODE-REVIEW-0734 already updated for `unsaved`, `unsaved.invalid` and
    `early-errors.log`;
  - `src/Enigma.TextEditor.Core/Vim/README.md` and `packaging/windows/Enigma.Msi.Worker/README.md`, which the
    version does not affect.

## Build/test evidence

- `git tag --list 1.21.0`: empty before the release. The last tag is `1.20.0`, on `main` (`f158edd`).
- **Run baseline** (run branch `feature/2026-11-25-release-1-21-0` @ `2d6f98c`, before the dev branch, SDK 10.0.103):
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3436 tests, 3434 passed, 1 failed
    (`HexMouseSelectionTests.APressPastTheLastByteOfAShortRow_SelectsItsLastByte`), 1 skipped
    (`DocumentFileAtomicWriteTests.AHiddenFile_StaysHidden_AndKeepsItsCreationTime`).
- `dotnet list Enigma.TextEditor.slnx package --outdated` (2026-11-25):
  - Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia
    have 12.2.0, and 12.1.6 with `--highest-patch`;
  - Microsoft.Extensions.Hosting has 11.0.0, and nothing with `--highest-patch`;
  - nothing else has an update.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3436 tests, 3434 passed, 1 failed, 1 skipped (the
    baseline's).
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry the informational version
  `1.21.0+2d6f98c…`, the run-branch tip the build was cut from.
- No tests were added, because the dev changes no code.

# FEATURE-8AEF — Release 1.17.0

**Item:** FEATURE-8AEF — Release 1.17.0
**Branch:** `feature/feature-8aef-release-1-17-0`
**Run:** feature/2026-11-06-release-1-17-0

## Summary

1.17.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-97A6 (1.16.0) as the template.

- **Version:** `<Version>1.17.0</Version>` in both csprojs (was 1.16.0), the draft's `x.y+1.z`.
- **`buildmsi`:** unchanged. It reads the Desktop csproj's `<Version>` and requires Core's to match, so it now builds
  `Enigma.TextEditor.1.17.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.17.0 — 2026-11-06` section above 1.16.0's. It holds a summary, then:
  - *Find and replace with vim on* (BUG-B026): `Ctrl+F` / `Ctrl+H` in every mode, typing in the fields, `Esc`, the
    `Tab` order, the Help entries;
  - *The mouse with vim on* (BUG-47C2): a click past a line's end, the double-click and triple-click, the Help entry;
  - *Upgrading from 1.16* (with `Ctrl+F` no longer paging down), *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.17 first, then 1.16, then sums up 1.15 down to 1.0.
- **`SECURITY.md`:** `1.17.x` is supported, and `1.16.x` and earlier are not.
- **Dependencies:** the Avalonia set is held back at 12.1.4. 12.1.5 is out, but Enigma.Avalonia.Desktop has not moved
  past 1.3.0.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-8AEF.md`
- Created: `docs/done/FEATURE-8AEF.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| README reflow | The callout's first three lines became six, all within 120 columns. The lines from 1.14's installer on are untouched | The plan: only the lines this release touches are reflowed |
| `Ctrl+F` no longer paging down | Stated in the BUG-B026 section and again under *Upgrading from 1.16* | It is the one change to a key a user may already rely on |
| How the notes name the old behaviour | The symptom a user saw in 1.16 ("`x` deleted the character to its left", "typing did nothing") | 1.16.0's notes did the same |
| Release-notes facts | Taken from the done docs of BUG-B026 PHASE01–03 and BUG-47C2 PHASE01–02, with the Help wording checked in `HelpCatalog.cs` | The notes must match what shipped |

## Deviations & follow-ups

- **Before distributing, for the user:**
  - build the 1.17.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted (it runs
    `git clean -dxf`, which this run may not);
  - install it over the installed version, and check that *Installed apps* lists one *Enigma Text Editor* at 1.17.0
    (`docs/RELEASE.md` §6).
- **Merging into `develop` and `main`, the `1.17.0` tag and the push** are the user's (`docs/RELEASE.md` §2–4). This
  run may not write to either branch, tag or push.
- **Pre-existing test failure** (the run's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_…`. The
  flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` from earlier runs passed in both runs.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.17.0.
- The README's long lines outside the callout (lines 25 onward) predate this release and are untouched.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md` and `SECURITY.md`, as above.
- Checked and left as they are:
  - `docs/RELEASE.md`: its steps are generic `X.Y.Z`, and the versions it names (1.1.0 as an example, 1.9.0's MSI)
    are still true;
  - the README's *Install on Windows* (the MSI name is `X.Y.Z`);
  - `SECURITY.md`'s *Scope*: the find panel is AvaloniaEdit's own, and vim's input handling is already listed.

## Build/test evidence

- `git tag --list 1.17.0`: empty before the release. `1.16.0` is on `main` (`f830e8e`).
- **Run baseline** (run branch `feature/2026-11-06-release-1-17-0` @ `2628713`, before the dev branch):
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3199 tests, 3198 passed, 1 failed
    (`InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`).
- `dotnet list package --outdated`: the six Avalonia packages have 12.1.5; nothing else.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3199 tests, 3198 passed, 1 failed (the baseline's).
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry file version `1.17.0.0` and
  informational version `1.17.0+2628713…`, the run-branch tip the build was cut from.
- No tests were added, because the dev changes no code.

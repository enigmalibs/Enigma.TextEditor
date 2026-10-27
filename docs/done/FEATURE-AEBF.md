# FEATURE-AEBF — Release 1.7.0

**Item:** FEATURE-AEBF — Release 1.7.0
**Branch:** `feature/feature-aebf-release-1-7-0`
**Run:** bugfix/2026-10-27-link-colours-release

## Summary

1.7.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-4362 (1.6.0) as the template:

- **Version**: `<Version>1.7.0</Version>` in both csprojs.
- **`RELEASENOTES.md`**: a dated `## 1.7.0 — 2026-10-27` section above 1.6.0's, in the repository's style. It holds a
  summary, then:
  - *Colours* (BUG-78F9 PHASE01 and PHASE02), with the old and new values and their contrast;
  - *Upgrading from 1.6*;
  - *Dependencies*;
  - *Compatibility*;
  - *Version*.
- **README**: the callout names 1.7 first, then sums up 1.6 down to 1.0.
- **`SECURITY.md`**: `1.7.x` is supported; `1.6.x` down to `1.0.x` are not. The scope still holds, because 1.7.0
  changes colours only.
- **Dependencies**: nothing moved. The only updates are the coupled Avalonia set (12.1.2 → 12.1.4 available: Avalonia,
  .Desktop, .Fonts.Inter, .Themes.Fluent, .Headless, .Skia). It is held back because Enigma.Avalonia.Desktop has not
  moved past 1.2.0.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files / modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-AEBF.md`

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong.
- Every run item was merged, so nothing is named as quarantined in the notes. BUG-4A17 is still quarantined from an
  earlier run and is not in 1.7.0.
- **1.6.0 is cut but neither merged into `main` nor tagged** (`main` is at 1.5.0's `8dc3771`). To ship 1.6.0 on its own,
  merge `develop` (`6a79e6b`) into `main` and tag it `1.6.0` before merging this run. Otherwise 1.7.0 is the first
  release to carry 1.6.0's changes, and the notes still describe them under 1.6.0.
- No MSI profile (as for 1.0–1.6).
- The merges and the tags are the user's (see the run report).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.4 | Held at 12.1.2 | The coupled set moves only with Enigma.Avalonia.Desktop, still 1.2.0 |
| Mention Ctrl+click on links in the notes | No | 1.7.0 does not change how links are opened; the notes describe only the colours |
| Release date | 2026-10-27 | The run's date |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2742 total, 2742 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2742 total, 2742 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release): file version `1.7.0.0`, product version
  `1.7.0+7d7e172…`, the run-branch tip the build was cut from.
- `git tag --list 1.7.0`: empty before the release.
- `dotnet list package --outdated`: only the coupled Avalonia set.
- Fix cycles: 0.

# FEATURE-3C6D — Release 1.8.0

**Item:** FEATURE-3C6D — Release 1.8.0
**Branch:** `feature/feature-3c6d-release-1-8-0`
**Run:** feature/2026-10-28-new-icons-release

## Summary

1.8.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-AEBF (1.7.0) as the template:

- **Version**: `<Version>1.8.0</Version>` in both csprojs (was 1.7.0; minor + 1, patch 0).
- **`RELEASENOTES.md`**: a dated `## 1.8.0 — 2026-10-28` section above 1.7.0's. It holds a summary, then:
  - *Application icon* (FEATURE-9A03);
  - *Upgrading from 1.7*;
  - *Dependencies*;
  - *Compatibility*;
  - *Version*.
- **README**: the callout names 1.8 first, then sums up 1.7 down to 1.0.
- **`SECURITY.md`**: `1.8.x` is supported, `1.7.x` and earlier are not. The scope still holds, since 1.8.0 changes the
  icon and patch-level dependencies only.
- **Dependencies**: the coupled Avalonia set moves together, because Enigma.Avalonia.Desktop moved with it:

  | Package                                                              | Old    | New    |
  |----------------------------------------------------------------------|--------|--------|
  | Avalonia, .Desktop, .Themes.Fluent, .Fonts.Inter                     | 12.1.2 | 12.1.4 |
  | Avalonia.Headless, Avalonia.Skia (tests)                             | 12.1.2 | 12.1.4 |
  | Enigma.Avalonia.Desktop                                              | 1.2.0  | 1.3.0  |
  | AvaloniaEdit, AvaloniaEdit.TextMate, Enigma.Icons.Avalonia, the rest | —      | —      |

  Enigma.Avalonia.Desktop 1.3.0's nuspec sets an Avalonia floor of 12.1.4. Its notes say it is backward compatible with
  1.2.0, with no API change and no theme key renamed, removed or re-valued.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files / modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` (`<Version>`, and the set's comment now says 12.1.4)
  - `Directory.Packages.props` (the coupled set, and its comment)
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-3C6D.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Avalonia 12.1.4 | Moved, as a set, with Enigma.Avalonia.Desktop 1.3.0 | The plan's rule: the set moves when Enigma.Avalonia.Desktop moves with it; 1.3.0 needs 12.1.4 |
| AvaloniaEdit | Held at 12.0.0 | No newer release; it builds and the whole suite is green on 12.1.4 |
| `Directory.Packages.props` comment | Now says 12.1.4 is 1.3.0's floor | The old one (12.1.2, the 1.2.0 floor) became untrue |
| Release date | 2026-10-28 | The run's date |

## Deviations & follow-ups

- **The plan expected the set to stay held**, as in 1.5–1.7. Enigma.Avalonia.Desktop 1.3.0 is now out, so the plan's
  own rule moved it. Logged in the notes.
- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong.
- Every run item was merged, so nothing is named as quarantined in the notes. BUG-4A17 is still quarantined from an
  earlier run and is not in 1.8.0.
- No MSI profile (as for 1.0–1.7).
- The merges into `develop` and `main` and the `1.8.0` tag were requested by the user and are done after the run ends.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2742 total, 2742 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2742 total, 2742 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.8.0.0`;
  - informational version `1.8.0+21c5067…`, the run-branch tip the build was cut from.
- `project.assets.json` resolves Avalonia 12.1.4 and Enigma.Avalonia.Desktop 1.3.0.
- `git tag --list 1.8.0`: empty before the release.
- Fix cycles: 0.

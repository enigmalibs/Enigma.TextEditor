# FEATURE-4362 — Release 1.6.0

**Item:** FEATURE-4362 — Release 1.6.0
**Branch:** `feature/feature-4362-release-1-6-0`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Summary

1.6.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-2A5C (1.5.0) as the template:

- **Version** — `<Version>1.6.0</Version>` in both csprojs.
- **`RELEASENOTES.md`** — a dated `## 1.6.0 — 2026-10-27` section above 1.5.0's, in the repository's style:
  - a summary;
  - *Navigation rail and settings page* (FEATURE-856D);
  - *First start* (FEATURE-0D4A);
  - *Defaults* (FEATURE-0242);
  - *Upgrading from 1.5*: no new key, the dialog now a page, the menu item renamed, the rail's 44 px, the Linux
    re-install;
  - *Dependencies*, *Compatibility* and *Version*.
- **README** — the callout names 1.6 first, then sums up 1.5 down to 1.0.
- **`SECURITY.md`** — `1.6.x` is supported; `1.5.x` down to `1.0.x` are not. The scope still holds: the first-run
  probe only checks that the settings file exists, which is covered by "the way it reads and writes its settings
  file".
- **Dependencies** — Microsoft.Extensions.Hosting 10.0.13 → 10.0.14, the only non-coupled update. The Avalonia set
  (12.1.4 available) is held at 12.1.2: Enigma.Avalonia.Desktop has not moved past 1.2.0.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files / modules touched

- Modified: `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`,
  `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`, `Directory.Packages.props`, `RELEASENOTES.md`,
  `README.md`, `SECURITY.md`.

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong.
- Every run item was merged, so nothing is named as quarantined in the notes. BUG-4A17 is still quarantined from an
  earlier run and is not in 1.6.0.
- No MSI profile (as for 1.0–1.5).
- The merges and the `1.6.0` tag are the user's (see the run report).

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Microsoft.Extensions.Hosting 10.0.14 | Applied | A patch inside the 10.0.x band that `Directory.Packages.props` tracks; not part of the coupled set |
| Avalonia 12.1.4 | Held at 12.1.2 | The coupled set moves only with Enigma.Avalonia.Desktop, still 1.2.0 |
| Release date | 2026-10-27 | The run's date |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2737 total, 2737 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2737 total, 2737 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release): file version `1.6.0.0`, product version
  `1.6.0+06ae654…` (the run-branch tip the build was cut from).
- `git tag --list 1.6.0`: empty before the release.
- Fix cycles: 0.

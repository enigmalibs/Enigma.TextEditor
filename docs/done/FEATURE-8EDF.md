# FEATURE-8EDF — Release 1.9.0

**Item:** FEATURE-8EDF — Release 1.9.0
**Branch:** `feature/feature-8edf-release-1-9-0`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

1.9.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-3C6D (1.8.0) as the template:

- **Version:** `<Version>1.9.0</Version>` in both csprojs (was 1.8.0; minor + 1, patch 0).
- **`RELEASENOTES.md`:** a dated `## 1.9.0 — 2026-10-29` section above 1.8.0's. It holds a summary, then:
  - *Errors that nothing else handles* (FEATURE-452D);
  - *Packaging* (FEATURE-4092, FEATURE-84C3);
  - *Upgrading from 1.8*, *Dependencies*, *Compatibility* and *Version*.
- **README:** the callout names 1.9 first, then sums up 1.8 down to 1.0.
- **`SECURITY.md`:**
  - `1.9.x` is supported, `1.8.x` and earlier are not;
  - the scope adds the error dialog: what it shows, and what *Copy details* puts on the clipboard.
- **Dependencies:** `dotnet list package --outdated` was run again; nothing has moved. The notes say so, and name the
  .NET 11 release candidate that was not taken.

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`
  - `README.md`
  - `SECURITY.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-8EDF.md`
- Created: `docs/done/FEATURE-8EDF.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `SECURITY.md` scope | Add the error dialog | It shows exception text and writes to the clipboard: new surface a report could concern |
| The publish scripts in scope | Not added | They only run `dotnet publish` in the repository; they write nothing outside its build output |
| Release date | 2026-10-29 | The run's date |
| MSI profile | None | The plan; 1.0–1.8 precedent; an unattended run does not open a Windows-installer decision |

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged here. FEATURE-84C3 already updated §5, and none of its generic `X.Y.Z` steps became
  wrong.
- **Every run item was merged**, so the notes name nothing as quarantined. BUG-4A17 is still quarantined from an
  earlier run and is not in 1.9.0.
- The merges into `develop` and `main`, the `1.9.0` tag and the push are the user's (see `docs/RELEASE.md` §2–§4).

## Documentation sweep

- README callout, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.

## Build/test evidence

- `git tag --list 1.9.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2780 total, 2780 passed.
- `dotnet build Enigma.TextEditor.slnx -c Debug`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 2780 total, 2780 passed.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.9.0.0`;
  - informational version `1.9.0+6a7e21a…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated`: no updates for any of the four projects.
- Fix cycles: 0 of 3.

# FEATURE-97A6 — Release 1.16.0

**Item:** FEATURE-97A6 — Release 1.16.0
**Branch:** `feature/feature-97a6-release-1-16-0`
**Run:** bugfix/2026-11-04-bug-375b

## Summary

1.16.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-1B68 (1.15.0) as the template.

- **Version:** `<Version>1.16.0</Version>` in both csprojs (was 1.15.0), the number the user chose.
- **`buildmsi`:** unchanged. It reads the Desktop csproj's `<Version>` and requires Core's to match, so it now builds
  `Enigma.TextEditor.1.16.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.16.0 — 2026-11-04` section above 1.15.0's. It holds a summary, then:
  - *Undo and redo with vim on* (BUG-375B): `Ctrl+Z` / `Ctrl+Y` / `Ctrl+Shift+Z` in insert mode, the keys in normal
    and visual modes, the menus, *Reopen with encoding*, the Help entry, vim off unchanged;
  - *Upgrading from 1.15*, *Dependencies*, *Compatibility*;
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.16 first, then sums up 1.15 down to 1.0.
- **`SECURITY.md`:** `1.16.x` is supported, and `1.15.x` and earlier are not.
- **Dependencies:** the Avalonia set is held back at 12.1.4. 12.1.5 is out, but Enigma.Avalonia.Desktop has not moved
  past 1.3.0.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-97A6.md`
- Created: `docs/done/FEATURE-97A6.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| README reflow | The callout's first three lines became five, all within 120 columns; the lines from 1.14's installer on are untouched | The plan: only the lines this release touches are reflowed |
| How the notes name the old behaviour | "In 1.15 it showed an error, and the next `Esc` showed a second one" | The symptom a user saw, not the exception text |
| Normal-mode `Ctrl+Z` in the notes | Listed as a change ("`Ctrl+Z` is `u`") | It used to run the editor's undo, whose caret can differ from `u`'s |

## Deviations & follow-ups

- **No tests and no `buildmsi` run in this dev:** the user asked for no tests in this session, and `buildmsi` needs
  Windows. BUG-375B's done doc carries the suite's evidence (3144 total, the baseline's failure and a known flake).
- `docs/RELEASE.md` is unchanged: its steps are generic `X.Y.Z`, and nothing in it names 1.15.
- **Before distributing, for the user:** build the 1.16.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with
  nothing uncommitted (it runs `git clean -dxf`), install it over the installed version, and check that
  *Installed apps* lists one *Enigma Text Editor* at 1.16.0 (`docs/RELEASE.md` §6).
- **Pre-existing test failures** (BUG-375B's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_…` and
  the flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.16.0.
- The merges into `develop` and `main` and the `1.16.0` tag were asked for by the user in this session; the push is
  the user's.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md` and `SECURITY.md`, as above.
- Checked and left as they are: `docs/RELEASE.md`; the README's *Install on Windows* (the MSI name is `X.Y.Z`).

## Build/test evidence

- `git tag --list 1.16.0`: empty before the release. `1.15.0` is on `main` (`cc6517b`).
- `dotnet list package --outdated`: the six Avalonia packages have 12.1.5; nothing else.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry file version `1.16.0.0` and
  informational version `1.16.0+00888ad…`, the run-branch tip the build was cut from.
- Tests: not run (the user's instruction). No tests were added, because the dev changes no code.

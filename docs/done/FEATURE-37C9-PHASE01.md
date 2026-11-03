# FEATURE-37C9-PHASE01 — Vendor the Enigma.Msi worker CLI

**Item:** FEATURE-37C9 — Windows MSI build script
**Branch:** `feature/feature-37c9-phase01-vendor-msi-worker`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

The repository now carries Enigma.Msi's headless worker CLI in `packaging/windows/Enigma.Msi.Worker/`. PHASE02's
`buildmsi.ps1` runs it as `Enigma.Msi.Worker.exe build <profile>`.

- **What it is:** Enigma.Msi `1.7.0` (`a7a1bfb`) built against WixSharp_wix4 2.14.2. That is the 20 files of its
  `bin/Release/net472/` output, without the PDBs, the API XML and `.wixsharp/` (3.4 MB).
- **Where it comes from:** Enigma.GitClient's committed copy of the same folder, so the two applications vendor the
  same bytes.
- **A `README.md`** in the folder, adapted from GitClient's (the MSI's name is `Enigma.TextEditor.X.Y.Z.msi`). It
  gives:
  - the source;
  - the CLI's exit codes;
  - the requirements (.NET Framework 4.7.2, and the WiX CLI with WiX 7's EULA);
  - the licences (Enigma.Msi, WixSharp and .NET are MIT, the WiX 4.0.1 assemblies MS-RL);
  - how to refresh it.
- **`.gitignore`** ignores `packaging/windows/Enigma.Msi.Worker/.wixsharp/`, with GitClient's comment. WixSharp
  writes that folder beside the worker on its first build. `git clean -dxf` removes it, and the next build writes it
  again.

## Files/modules touched

- Created:
  - `packaging/windows/Enigma.Msi.Worker/`: 20 binaries.
    - `Enigma.Msi.Worker.exe`, `Enigma.Msi.Worker.exe.config`, `Enigma.Msi.dll`;
    - `WixSharp.dll`, `WixSharp.Msi.dll`, `WixSharp.UI.dll`, `WixSharp.UI.WPF.dll`, `WixSharp.MsiEventHost.exe`;
    - `WixToolset.Dtf.WindowsInstaller.dll`, `WixToolset.Mba.Core.dll`, `mbanative.dll`;
    - `Microsoft.Bcl.AsyncInterfaces.dll` and eight `System.*.dll`.
  - `packaging/windows/Enigma.Msi.Worker/README.md`
  - `docs/done/FEATURE-37C9-PHASE01.md`
- Modified:
  - `.gitignore`
  - `docs/roadmap.md`, `docs/plan/FEATURE-37C9.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which tree to copy from | GitClient's tracked files (`git ls-files`), compared with Enigma.Msi's build output | The plan's choice. Both are the same bytes, and copying from GitClient keeps the `.exe.config` already in its stored LF form |
| The README's link to Enigma.Msi | Plain text, no relative link | As GitClient: a link out of the repository works only with the sibling checkout |
| A README sentence on GitClient | "Enigma.GitClient vendors the same 20 files" | Tells whoever refreshes one that the other exists |

## Deviations & follow-ups

- `Enigma.Msi.Worker.exe.config` differs from Enigma.Msi's build output in line endings only. The build writes CRLF;
  `.gitattributes` (`* text=auto eol=lf`) stores LF. .NET Framework reads either. Every `.exe`/`.dll` is byte for byte
  (`.gitattributes` marks them binary).
- **Flaky test:** `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed in the first run and
  passed in the re-run (see `docs/done/FEATURE-3266.md`). This dev changes no code.
- Fix cycles used: 0 of 3.

## Documentation sweep

- Nothing stale yet. The README and `docs/RELEASE.md` change with the script itself (PHASE02).

## Build/test evidence

- Baseline: see `docs/done/FEATURE-3266.md` (0 warnings; 3109 tests, 1 known failure, `InstanceServerTests…`).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`:
  - first run: 3109 total, 3107 passed, 2 failed (`InstanceServerTests…` and the flaky `FileDropViewTests…`);
  - re-run: 3109 total, 3108 passed, 1 failed (`InstanceServerTests…`), as the baseline.
- `cmp` of each copied file:
  - against GitClient's copy: all 20 identical;
  - against `Enigma.Msi/src/Enigma.Msi.Worker/bin/Release/net472/`: 19 identical, and the `.exe.config` identical
    once CRs are stripped.
- `git status --porcelain --ignored -uall packaging/windows/`: the 21 files (20 binaries and the README) are
  untracked, none ignored. `git check-ignore -v --no-index` matches `.wixsharp/WixSharp.dll` to `.gitignore:369`.
- `Enigma.Msi.Worker.exe` with no arguments printed "No arguments were given." and the two usage forms, exit
  code **2**.

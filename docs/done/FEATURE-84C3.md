# FEATURE-84C3 — Windows and Linux publish scripts

**Item:** FEATURE-84C3 — Windows and Linux publish scripts
**Branch:** `feature/feature-84c3-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

There are now one-command publish scripts for both platforms. Each resolves the Desktop project from its own location,
so it runs from anywhere, and exits with `dotnet`'s exit code:

| Script | Command | Notes |
|---|---|---|
| `packaging/windows/publish.ps1` | `dotnet publish … -c Release -r win-x64 -p:PublishReadyToRun=true` | Enigma.GitClient's script, with this app's names; passes `$LASTEXITCODE` on |
| `packaging/windows/publish.bat` | the same command | Needs no PowerShell or execution policy; `exit /b %ERRORLEVEL%`; CRLF |
| `packaging/linux/publish.sh` | `dotnet publish … -c Release -r linux-x64 -p:PublishReadyToRun=true` | `set -euo pipefail` and `exec`, like `install.sh`; mode `100755` in the index |

Documentation:

- `docs/RELEASE.md` §5 now opens with the scripts: a table of the script and output folder for each platform, the
  `app.ico` beside the executable, and the execution-policy note. The self-contained command stays as the variant
  for a machine without .NET.
- The README's *Build, run, test* names the scripts.
- The README's *Solution layout* has a `packaging/windows` row, and the `packaging/linux` row names the publish script.

## Files/modules touched

- Created:
  - `packaging/windows/publish.ps1`
  - `packaging/windows/publish.bat`
  - `packaging/linux/publish.sh`
  - `docs/done/FEATURE-84C3.md`
- Modified:
  - `docs/RELEASE.md`
  - `README.md`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-84C3.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Executable bit | `git add --chmod=+x` | It sets mode `100755` on Windows, and `add` is among the run's allowed git operations |
| `.bat` header | No "double-click it" advice | The console window would close on exit and take the output with it |
| README layout row | A shorter single row for `packaging/linux` | A two-line cell would render as an empty row; widening the column would re-pad the whole table |

## Deviations & follow-ups

- **Not tested:** the failure path (a failing `dotnet publish` propagating its exit code) was not exercised. It rests on
  `exit $LASTEXITCODE`, `exit /b %ERRORLEVEL%` and `exec`.
- **`.ps1` line endings:** `.gitattributes` has no `*.ps1` rule, so the script falls under `* text=auto eol=lf`. Windows
  PowerShell 5.1 runs LF scripts. It is ASCII only, so it needs no BOM.

## Documentation sweep

- `docs/RELEASE.md` §5 and the README, as above. These are part of the plan, not stale documentation.

## Build/test evidence

- `publish.ps1` (`powershell -ExecutionPolicy Bypass -File …`) and `publish.bat` (`cmd /c …`), both started from
  another directory: exit 0, output in `win-x64/publish/`.
- `bash -n publish.sh`: OK. `bash publish.sh` from another directory: exit 0, output in `linux-x64/publish/`, holding
  `Enigma.TextEditor.Desktop`.
- `app.ico` in both publish folders is byte-identical to `Assets/app.ico` (FEATURE-4092).
- `file publish.bat`: CRLF line terminators.
- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2780 total, 2780 passed.
- Fix cycles: 0 of 3.

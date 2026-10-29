# FEATURE-84C3 — Windows and Linux publish scripts

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-84c3-publish-scripts`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

One-command publish scripts on both platforms, as part of the baseline every Enigma app shares:

- Windows: `publish.ps1` and `publish.bat`, both running
  `dotnet publish <Desktop project> -c Release -r win-x64 -p:PublishReadyToRun=true`.
- Linux: `publish.sh` next to `install.sh` / `uninstall.sh`, running
  `dotnet publish <Desktop project> -c Release -r linux-x64 -p:PublishReadyToRun=true`.

## Context & constraints

- **What exists today:**
  - `packaging/linux/install.sh` and `uninstall.sh`: `set -euo pipefail`, `script_dir`/`repo_root`, mode `100755`
    in the index;
  - no `packaging/windows/` folder.
- **The models:**
  - Enigma.GitClient's `packaging/windows/publish.ps1` is the model for the PowerShell script: a comment-based help
    header, resolved from `$PSScriptRoot`, passing `$LASTEXITCODE` on;
  - GitClient's FEATURE-0D4E plans the same `publish.bat` and `publish.sh`.
- **Line endings** (`.gitattributes`): `*.bat` is CRLF and `*.sh` is LF. `.ps1` falls under `* text=auto eol=lf`.
- **Documentation:**
  - `docs/RELEASE.md` §5 documents a self-contained `dotnet publish … -o ./artifacts`;
  - the README's *Build, run, test* points at `docs/RELEASE.md`.

## Steps

1. `packaging/windows/publish.ps1`: GitClient's script with this app's names and paths, and its output folder
   `src\Enigma.TextEditor.Desktop\bin\Release\net10.0\win-x64\publish\`.
2. `packaging/windows/publish.bat`:
   - `@echo off` and `setlocal`;
   - the project resolved from `%~dp0`;
   - the command, then `exit /b %ERRORLEVEL%`;
   - a header comment in the `.ps1`'s spirit;
   - CRLF in the working tree.
3. `packaging/linux/publish.sh`:
   - `#!/usr/bin/env bash` and `set -euo pipefail`;
   - the project resolved from the script's directory;
   - the command, through `exec`;
   - a header comment like `install.sh`'s;
   - staged with mode `100755`.
4. Documentation:
   - `docs/RELEASE.md` §5 names the three scripts and where their output lands, and keeps the self-contained
     variant;
   - the README's *Build, run, test* names them.
5. Verify:
   - run `publish.bat` and `publish.ps1` (win-x64), and `publish.sh` through Git Bash (linux-x64);
   - each exits 0 and leaves its `publish/` folder, and `app.ico` is in it (FEATURE-4092);
   - `bash -n` on `publish.sh`.

## Acceptance criteria

- The three scripts run from any directory and exit with `dotnet`'s exit code.
- `publish.sh` is executable in the index (`100755`); `publish.bat` is CRLF.
- The build has zero warnings and the whole suite is green. Nothing in the solution changes.

## Out of scope

- Self-contained publishing, other runtime identifiers, and an archive or installer for Windows.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Where | `packaging/windows/publish.{ps1,bat}`, `packaging/linux/publish.sh` | Next to each platform's existing scripts; GitClient's layout | A root `publish/` folder |
| `publish.bat` | Calls `dotnet` itself | No dependency on PowerShell's execution policy | Wrapping `publish.ps1` |
| Arguments | None accepted | The spec fixes the command; one obvious behaviour; GitClient's `publish.ps1` takes none | Passing `%*` / `"$@"` through |
| Linux RID | `linux-x64`, fixed | The spec's command; `install.sh --rid` covers arm64 | Guessing from `uname -m` |
| Self-contained | Not set (the framework-dependent default for a RID publish in .NET 10) | The spec's command, verbatim | Adding `--self-contained` |
| Tests | Run all three scripts; no unit test | No test in this repo reads packaging scripts; running them is the real proof | A test that greps the scripts |

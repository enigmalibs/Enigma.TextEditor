# FEATURE-37C9 — Windows MSI build script

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** one per phase, see below
**Run:** feature/2026-11-03-icons-msi-release

## Objective

One command builds the Windows installer. `packaging/windows/buildmsi.ps1`, and a `buildmsi.bat` twin:

1. cleans the repository;
2. publishes the release;
3. builds `artifacts/Enigma.TextEditor.X.Y.Z.msi` with Enigma.Msi.

This is exactly as Enigma.GitClient does it (its FEATURE-EAB7), adapted to Enigma Text Editor:

- its name;
- its own upgrade code;
- its version.

The repository carries a copy of Enigma.Msi's worker CLI.

## Context & constraints

- **The installed product.** This machine has *Enigma Text Editor 1.9.0* installed from an MSI built with
  Enigma.Msi. Read from its cached package (`C:\Windows\Installer\1056c3.msi`, read-only) while planning:
  - `UpgradeCode` `{9F063D09-9F4E-4436-8CB3-39109EDA380D}` (GitClient's is different);
  - `ProductName` "Enigma Text Editor", `Manufacturer` "Josué Clément", `ALLUSERS` 1 (per machine);
  - `INSTALLDIR` = `ProgramFilesFolder\Enigma Text Editor`;
  - shortcuts "Enigma Text Editor" in `DesktopFolder` and `ProgramMenuFolder`, target
    `[INSTALLDIR]\Enigma.TextEditor.Desktop.exe`, icon `IconFile1_app.ico`; `ARPPRODUCTICON` `app_icon.ico`;
  - `Upgrade`: `VersionMax 1.9.0` (attributes 1) and `VersionMin 1.9.0` (attributes 2).
  The new script must keep that upgrade code. A new one would install 1.14.0 beside 1.9.0 instead of upgrading it.
- **Enigma.GitClient's implementation** is the model (`C:\Dev\EnigmaLibs\Enigma.GitClient`, `develop`/`main` at
  `a58d8fe`):
  - `packaging/windows/buildmsi.ps1`, `buildmsi.bat`;
  - `packaging/windows/Enigma.Msi.Worker/`: 20 binaries and a README;
  - the `.gitignore` entry for `.wixsharp/`;
  - `WindowsInstallerIdentityTests`;
  - the README *Windows installer* section and the `docs/RELEASE.md` changes.
  Its vendored files are byte for byte Enigma.Msi `1.7.0`'s (`a7a1bfb`) `src/Enigma.Msi.Worker/bin/Release/net472/`
  output, except `Enigma.Msi.Worker.exe.config`, which differs in line endings only (checked while planning).
- **The worker CLI:** `Enigma.Msi.Worker.exe build <file.msipkg.json>`.
  - It is a net472 program.
  - Exit codes: `0` built, `1` invalid profile or failed build, `2` bad invocation.
  - It resolves relative paths against the working directory.
  - It needs the WiX CLI (`dotnet tool install --global wix`). That CLI is at `C:\Users\Jo\.dotnet\tools\wix.exe`
    here, and GitClient built its MSI with it today.
  - WixSharp writes a `.wixsharp/` folder beside the worker on its first build.
- **Differences from GitClient that the adaptation must handle:**
  - **The version** is `<Version>` in **both** csprojs (`src/Enigma.TextEditor.Core/…csproj` and
    `src/Enigma.TextEditor.Desktop/…csproj`, `1.13.0` today), not `Directory.Build.props`. The two move together
    (`docs/RELEASE.md`). `AppInfo.Version`, which the About dialog shows, reads Core's assembly.
  - **The assembly** is `Enigma.TextEditor.Desktop`. The publish folder is
    `src\Enigma.TextEditor.Desktop\bin\Release\net10.0\win-x64\publish\`, with `app.ico` beside the exe (FEATURE-4092).
  - **The publish** is FEATURE-84C3's `packaging/windows/publish.ps1`, which exits with dotnet's code.
  - **No consistency tests** read repository files yet. The repository root is found from the test output by its
    `Enigma.TextEditor.slnx`, as GitClient's test does.
  - **The whole suite runs on this machine**, Desktop tests included (FEATURE-6D39). GitClient's "Desktop tests
    compiled only" rule does not apply.
- **Repository files:**
  - `.gitignore` already ignores `artifacts/` (line 66). None of the vendored paths is ignored (`git check-ignore`
    while planning). `.wixsharp/` is not ignored yet.
  - `.gitattributes`: `* text=auto eol=lf`, `*.dll` / `*.exe` binary, `*.bat` CRLF.
  - `.editorconfig` says UTF-8 without BOM. Windows PowerShell 5.1 reads a BOM-less script as ANSI, so the `.ps1`
    stays ASCII.
- **The run may not run `git clean`** in this repository. The script is verified end to end in a scratch clone
  instead, as GitClient's was.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Upgrade code | `9f063d09-9f4e-4436-8cb3-39109eda380d`, the installed 1.9.0's | "Its own UpgradeCode": Text Editor's, not GitClient's. It keeps the upgrade from 1.9.0 working | A new GUID (1.14.0 would install beside 1.9.0); GitClient's code |
| How to use Enigma.Msi | The 1.7.0 worker CLI, committed in `packaging/windows/Enigma.Msi.Worker/` | "Exactly like GitClient" | A net472 builder project in the solution |
| Where the copy comes from | GitClient's committed copy, checked byte for byte against Enigma.Msi 1.7.0's build output | The same files GitClient reviewed; the source table stays Enigma.Msi 1.7.0 | Rebuilding Enigma.Msi (a write into another repository) |
| Which files are vendored | GitClient's 20 binaries; no `*.pdb`, `Enigma.Msi.xml` or `.wixsharp/` | As GitClient | Everything in `bin/` |
| `.wixsharp/` | Git-ignored | As GitClient: WixSharp rewrites it; `git clean -dxf` resets it | Committing it |
| Version source | The Desktop csproj's `<Version>`. The script stops when Core's differs | The MSI installs the Desktop app. About shows Core's version, so a mismatch would ship two numbers | Core's csproj only; a new `Directory.Build.props` version (a build change the request did not ask for) |
| Profile | Generated per build into `artifacts/Enigma.TextEditor.X.Y.Z.msipkg.json`, with absolute paths | As GitClient: nothing to edit per release; the file stays as the build's record | `dotnet-release`'s committed `msiProfiles/` per version |
| `productId` | A new GUID per build | As GitClient and Enigma.Msi's model | Derived from the version |
| Names | Product and shortcuts "Enigma Text Editor" (`AppInfo.DisplayName`); MSI `Enigma.TextEditor.X.Y.Z.msi`; manufacturer "Josué Clément" | The user's spec; the installed 1.9.0's names; GitClient's `Enigma.<App>.X.Y.Z` file name | — |
| Install path, scope | `%ProgramFiles%\Enigma Text Editor`, `PerMachine` | As the installed 1.9.0 and GitClient | `%ProgramFiles64%` |
| Compression | `None` | GitClient's choice, for build speed | `High` (`dotnet-release`'s default) |
| Control Panel and shortcut icon | The publish folder's `app.ico` | As GitClient; 1.9.0 had `ARPPRODUCTICON` | — |
| The publish step | `buildmsi.ps1` calls `publish.ps1` | One definition of the publish | Repeating `dotnet publish` |
| `.bat` twin | Runs `buildmsi.ps1` with `-ExecutionPolicy Bypass`, passing its exit code on | As GitClient and `publish.bat` | — |
| Prerequisites | `git`, `dotnet`, `wix` and the worker checked before anything is cleaned | Fails fast | Letting the worker fail after a full publish |
| Tests | One `WindowsInstallerIdentityTests` in the Desktop tests. It pins `$UpgradeCode`, that `$ProductName` is `AppInfo.DisplayName`, and that `$Assembly` is the app's assembly name | The identity facts a typo would silently break. The Desktop test project is the only one that sees both Core's `AppInfo` and the Desktop assembly | GitClient's split over three test files (two of them do not exist here) |
| Verification | The whole script in a scratch clone of the dev's tree, then the MSI's tables read through the Windows Installer API; nothing installed | The run may not `git clean` the user's repository; installing changes the user's machine | Running it in the repository |
| Phases | Two: the vendored worker, then the script | As GitClient: where a binary comes from and whether a script is right are separate reviews | One phase |

## PHASE01 — Vendor the Enigma.Msi worker CLI

**Branch:** `feature/feature-37c9-phase01-vendor-msi-worker`
**Status:** DONE

### Steps

1. Copy GitClient's tracked `packaging/windows/Enigma.Msi.Worker/` binaries (20 files, `git ls-files`) into
   `packaging/windows/Enigma.Msi.Worker/`. Check each one with `cmp` against Enigma.Msi's
   `src/Enigma.Msi.Worker/bin/Release/net472/`.
2. `packaging/windows/Enigma.Msi.Worker/README.md`: GitClient's README adapted to this repository (the MSI name
   `Enigma.TextEditor.X.Y.Z.msi`, "a solution that is also built on Linux" still true). The source table is Enigma.Msi
   `1.7.0` (`a7a1bfb`), WixSharp_wix4 2.14.2, with the licences and the refresh steps.
3. `.gitignore`: `packaging/windows/Enigma.Msi.Worker/.wixsharp/`, with GitClient's comment.
4. Check that `git status --porcelain --ignored -uall` lists every vendored file as untracked, none ignored, and
   that the worker runs: no arguments → usage, exit code 2.

### Acceptance criteria

- Every vendored file is identical to GitClient's copy and to Enigma.Msi's output (`cmp`; the `.exe.config` may
  differ from the latter in line endings only).
- No vendored file is git-ignored; `.wixsharp/` is.
- `Enigma.Msi.Worker.exe` with no arguments prints its usage and exits 2.
- The build has zero warnings; the whole suite fails nothing the run's baseline did not.

## PHASE02 — The buildmsi script

**Branch:** `feature/feature-37c9-phase02-buildmsi-script`
**Status:** TODO

### Steps

1. `packaging/windows/buildmsi.ps1`: GitClient's script, adapted. It is ASCII, uses `Set-StrictMode` and
   `$ErrorActionPreference = 'Stop'`, carries comment-based help, and resolves everything from its own location:
   1. constants: `$UpgradeCode = '9f063d09-9f4e-4436-8cb3-39109eda380d'`,
      `$ProductName = 'Enigma Text Editor'`, the manufacturer, and `$Assembly = 'Enigma.TextEditor.Desktop'`;
   2. check `git`, `dotnet` and `wix` are on the `PATH`, and the vendored worker exists;
   3. read `<Version>` from both csprojs with `XmlDocument.Load`:
      - check the Desktop one is `^\d+\.\d+\.\d+$`;
      - stop when Core's differs;
   4. `git -C <root> clean -dxf`; stop on a non-zero exit code;
   5. run `publish.ps1`; stop on a non-zero exit code; check that the exe and `app.ico` are in the publish folder;
   6. write `artifacts/Enigma.TextEditor.X.Y.Z.msipkg.json` (UTF-8 without BOM, absolute paths, a new `productId`):
      - `PerMachine`, `%ProgramFiles%\Enigma Text Editor`, `compression` `None`;
      - `controlPanel.productIcon` and both shortcuts' icon = the publish folder's `app.ico`;
      - shortcuts `%Desktop%` and `%ProgramMenu%`, named "Enigma Text Editor", targeting
        `[INSTALLDIR]\Enigma.TextEditor.Desktop.exe`;
   7. run `Enigma.Msi.Worker.exe build <profile>`, exit with its code, and on success print the MSI's path and the
      elapsed time.
2. `packaging/windows/buildmsi.bat`: runs `buildmsi.ps1` through
   `powershell -NoProfile -ExecutionPolicy Bypass -File`, and passes its exit code on. CRLF, as `.gitattributes` asks.
3. `tests/Enigma.TextEditor.Desktop.UnitTests/WindowsInstallerIdentityTests.cs`. It finds the root by
   `Enigma.TextEditor.slnx` and checks three whole lines of `buildmsi.ps1`:
   - the upgrade code;
   - `$ProductName = '<AppInfo.DisplayName>'`;
   - `$Assembly = '<the app assembly's name>'`.
4. Docs:
   - `README.md`: an *Install on Windows* section before *Install on Linux*, with what `buildmsi` does (the
     `git clean -dxf` warning), what the MSI installs and the prerequisites;
   - `README.md`: the *Solution layout* row for `packaging/windows`;
   - `docs/RELEASE.md`:
     - §5: Windows has an installer now;
     - §6: a Windows check after installing the MSI;
     - *Why there is no NuGet step or MSI* is split: the NuGet reason stays, and *The Windows installer's identity*
       says the upgrade code is fixed in the script.
5. Verify in a scratch clone: commit the dev's tree there, run `buildmsi.bat`, then read the MSI's `Property`,
   `Directory`, `Shortcut`, `Upgrade`, `Icon` and `Media` tables. Also run once with `wix` hidden from the `PATH`, with
   a canary untracked file present.

### Acceptance criteria

- In the scratch clone, `buildmsi.bat` exits 0 and leaves `artifacts/Enigma.TextEditor.<version>.msi` and its
  `.msipkg.json`. After it, `git status --porcelain` is empty.
- The MSI's tables show:
  - `ProductName` "Enigma Text Editor", `Manufacturer` "Josué Clément", and `ProductVersion` = the csprojs'
    version;
  - `UpgradeCode` `{9F063D09-9F4E-4436-8CB3-39109EDA380D}`, `ALLUSERS` 1, and `ARPPRODUCTICON` set;
  - `INSTALLDIR` "Enigma Text Editor" under `ProgramFilesFolder`;
  - two "Enigma Text Editor" shortcuts (`DesktopFolder`, `ProgramMenuFolder`) on
    `[INSTALLDIR]\Enigma.TextEditor.Desktop.exe`, each with an icon.
- The profile says `"compression": "None"`.
- A missing `wix` stops the script before `git clean`, with the install command in the message, and the canary
  file survives.
- `WindowsInstallerIdentityTests` passes. The build has zero warnings, and the whole suite fails nothing the run's
  baseline did not.

## Out of scope

- Installing, launching or uninstalling the MSI (outward-facing; the user's).
- Signing the MSI; a 64-bit package (Enigma.Msi has no platform setting).
- Changing Enigma.Msi or Enigma.GitClient.
- A `-NoClean` switch or other options the user did not ask for; trimming the publish folder's native PDBs.

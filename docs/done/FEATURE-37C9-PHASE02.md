# FEATURE-37C9-PHASE02 — The buildmsi script

**Item:** FEATURE-37C9 — Windows MSI build script
**Branch:** `feature/feature-37c9-phase02-buildmsi-script`
**Run:** feature/2026-11-03-icons-msi-release

## Summary

`packaging/windows/buildmsi.ps1` builds the Windows installer in one command, from any directory. It is
Enigma.GitClient's script, adapted:

1. It checks that `git`, `dotnet`, `wix` and the vendored worker are there, **before** anything is cleaned.
2. It reads `<Version>` from the Desktop csproj and checks it is `X.Y.Z`. It stops when the Core csproj's differs
   (the About dialog shows Core's).
3. `git -C <root> clean -dxf`.
4. `publish.ps1`, the existing `-c Release -r win-x64 -p:PublishReadyToRun=true` publish. Then it checks that
   `Enigma.TextEditor.Desktop.exe` and `app.ico` are in the publish folder.
5. It writes `artifacts/Enigma.TextEditor.X.Y.Z.msipkg.json`, an Enigma.Msi profile:
   - absolute paths, and a new `productId` each build;
   - upgrade code `9f063d09-9f4e-4436-8cb3-39109eda380d`;
   - "Enigma Text Editor", "Josué Clément", `PerMachine`, `%ProgramFiles%\Enigma Text Editor`;
   - `compression` `None`;
   - `app.ico` as the Control Panel icon and both shortcuts' icon;
   - shortcuts in `%Desktop%` and `%ProgramMenu%`.
6. It runs `Enigma.Msi.Worker.exe build` on that profile into `artifacts/Enigma.TextEditor.X.Y.Z.msi`, and prints the
   elapsed time.

Every failure prints `buildmsi: <reason>` and exits non-zero, with the failing tool's exit code when there is one.
`buildmsi.bat` runs it from `cmd` with `-ExecutionPolicy Bypass` and passes the exit code on.

The upgrade code is the one the installed 1.9.0 MSI carries, so the next MSI upgrades it instead of installing beside
it. `WindowsInstallerIdentityTests` pins it, the product name (`AppInfo.DisplayName`) and the executable (the app
assembly's name).

## Files/modules touched

- Created:
  - `packaging/windows/buildmsi.ps1`
  - `packaging/windows/buildmsi.bat`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/WindowsInstallerIdentityTests.cs`
  - `docs/done/FEATURE-37C9-PHASE02.md`
- Modified:
  - `README.md`, `docs/RELEASE.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-37C9.md`: statuses.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Reading the two versions | One `Get-ProjectVersion` helper: `XmlDocument.Load` + `/Project/PropertyGroup/Version` | Both csprojs hold UTF-8 (`—` in comments), which `Get-Content` would read as ANSI in Windows PowerShell 5.1 |
| Failure message on a version mismatch | Names both csprojs and both values | The fix is an edit to one of them; the message says which |
| Where the test reads the script | Once per test (`ReadScript()`), not a static field | A missing file then fails each test with its own `FileNotFoundException`, not a `TypeInitializationException` |
| Where the README section goes | *Install on Windows*, before *Install on Linux* | Parallel to the existing *Install on Linux*; GitClient's *Windows installer* sits after its *Publish* section, which this README does not have |
| `docs/RELEASE.md`'s last section | Split into *Why there is no NuGet step* and *The Windows installer's identity* | The NuGet reason still holds; "no Windows installer has been asked for" no longer does |

## Deviations & follow-ups

- **Follow-up: the MSI is 187 MB because of native PDBs.** The publish folder carries `libSkiaSharp.pdb` (84 MB) and
  `libHarfBuzzSharp.pdb` (21 MB) from the SkiaSharp/HarfBuzz packages, as GitClient's does. Leaving them out would make
  the MSI about 100 MB smaller and its build faster. It changes the publish (FEATURE-84C3), so it is not done here.
- **A 32-bit package.** Enigma.Msi has no platform setting, so the MSI's template is `Intel;1033`, and
  `%ProgramFiles%` is `Program Files (x86)` on 64-bit Windows. That is where 1.9.0 is installed too. `docs/RELEASE.md`
  says so.
- **WixSharp's "source base directory"** is the caller's current directory: it printed `C:\Dev\EnigmaLibs\Enigma.TextEditor`
  for the scratch-clone build. It resolves relative paths only, and the profile's paths are absolute. The repository
  had no new file, ignored or not, after the build.
- **Not run, by design:** installing, launching or uninstalling the MSI.
- Line endings: `buildmsi.bat` is CRLF, as `.gitattributes` asks; the `.ps1` is LF and ASCII.
- **Pre-existing test failure:** the baseline's `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
- Fix cycles used: 0 of 3.

## Documentation sweep

- `README.md`:
  - a new *Install on Windows* section: what `buildmsi` does (with the `git clean -dxf` warning), what the MSI
    installs, and the prerequisites;
  - *Solution layout*: `packaging/windows` holds "the Windows publish and installer scripts, and Enigma.Msi's
    worker".
- `docs/RELEASE.md`:
  - §5: "On Windows and macOS there is no installer" became the Windows MSI steps and a table of what it installs. The
    macOS sentence stays;
  - §6: a Windows check after installing the MSI;
  - *Why there is no NuGet step or MSI* became *Why there is no NuGet step* and *The Windows installer's identity*:
    the upgrade code is fixed in the script, and the product code is new per build.
- `SECURITY.md`'s scope and the release notes come with FEATURE-5920.

## Build/test evidence

- Baseline: see `docs/done/FEATURE-3266.md`.
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **3112** total (the baseline's 3109 plus the 3 new tests), 3111
  passed, 1 failed (the baseline's `InstanceServerTests…`; nothing new). `WindowsInstallerIdentityTests` on its own:
  3/3 passed.
- **End to end, in a scratch clone** of this branch with the dev's tree committed there (the run may not `git clean`
  the user's repository):
  - **No `wix` on the `PATH`:**
    - `buildmsi: wix is not on the PATH: dotnet tool install --global wix.`, exit code 1;
    - an untracked canary file was still there, so nothing was cleaned.
  - **`buildmsi.bat`:**
    - git removed the canary file, the publish ran, and the worker built the MSI (WiX 7.0.0);
    - exit code **0**, in **21 s** wall time (restore, R2R publish and MSI);
    - `artifacts/` holds `Enigma.TextEditor.1.13.0.msi` (186,656,743 bytes for a payload of 183,754,444 bytes in 89
      files, so uncompressed) and its `.msipkg.json` (`"compression": "None"`);
    - `git status --porcelain` was empty afterwards. Only `artifacts/` and
      `packaging/windows/Enigma.Msi.Worker/.wixsharp/` appear, as ignored.
  - **The MSI's tables**, read through the Windows Installer API:
    - `ProductName` Enigma Text Editor, `Manufacturer` Josué Clément, `ProductVersion` 1.13.0 (the csprojs' version
      on this branch);
    - `UpgradeCode` `{9F063D09-9F4E-4436-8CB3-39109EDA380D}`, `ALLUSERS` 1, `ARPPRODUCTICON` `app_icon.ico`;
    - `INSTALLDIR` "Enigma Text Editor" under `ProgramFilesFolder`;
    - two "Enigma Text Editor.lnk" shortcuts (`DesktopFolder`, `ProgramMenuFolder`) on
      `[INSTALLDIR]\Enigma.TextEditor.Desktop.exe`, icon `IconFile1_app.ico`;
    - `Upgrade`: `VersionMax 1.13.0` (attributes 1) and `VersionMin 1.13.0` (attributes 2);
    - template `Intel;1033`.
    These are the same rows as the installed 1.9.0's package, except the version and the product code.
  - The scratch clone was deleted afterwards.

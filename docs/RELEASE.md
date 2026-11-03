# Release runbook

Reusable checklist for cutting a new **Enigma.TextEditor** version.

Replace `X.Y.Z` with the version being released (e.g. `1.1.0`) throughout. The version is the `<Version>` property of
**both** projects, `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` and
`src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`: Core ships only inside the Desktop application, so
the two always carry the same number.

**Nothing here is published to NuGet.** Neither project has a `PackageId`, so there is no `pack` step, no
`nuget push`, and no package metadata to keep in order. A release produces two things: a tag on `main`, and a
published build for whoever wants one. Step 5 replaces what would be the pack-and-push steps for a library.

## 1. Pre-release checks

Run from the repository root, on the branch that will be merged:

- [ ] `<Version>X.Y.Z</Version>` set in both csprojs.
- [ ] `RELEASENOTES.md` has a top `X.Y.Z` section, dated, describing the release (newest first).
- [ ] The README's *what's new* callout reflects `X.Y.Z`.
- [ ] `SECURITY.md`'s supported-versions table names the `X.Y.x` line.
- [ ] Clean, warning-free build:
      ```bash
      dotnet build Enigma.TextEditor.slnx -c Release
      ```
- [ ] Full test suite green:
      ```bash
      dotnet test --solution Enigma.TextEditor.slnx -c Release
      ```
- [ ] The build carries the version. The informational version is the version and the commit the build was cut
      from, `X.Y.Z+<sha>`.
      - On Linux (`strings` is in binutils):
        ```bash
        strings src/Enigma.TextEditor.Desktop/bin/Release/net10.0/Enigma.TextEditor.Core.dll | grep -m1 -o 'X\.Y\.Z+[0-9a-f]*'
        ```
      - On Windows:
        ```powershell
        (Get-Item src/Enigma.TextEditor.Desktop/bin/Release/net10.0/Enigma.TextEditor.Core.dll).VersionInfo.ProductVersion
        ```
      - In the application: **Help → About Enigma Text Editor** shows `X.Y.Z` and the short commit.

## 2. Merge into `develop`, then into `main`

Work is integrated on `develop`; `main` carries the releases:

```bash
git switch develop
git merge --no-ff <release-branch>
git switch main
git merge --no-ff develop
```

## 3. Tag the release

The first release set the convention: a **bare `X.Y.Z`** lightweight tag, no `v` prefix, on `main`'s
`Merge branch 'develop'` commit. Run `git tag` before tagging a later release and match what you find.

```bash
git tag X.Y.Z
```

## 4. Push

```bash
git push origin develop main
git push origin X.Y.Z
```

## 5. Publish the application

The publish scripts build a Release for one platform, compiled ReadyToRun so it starts faster, which runs on an
installed .NET 10 runtime. They run from wherever they are started, and fail when `dotnet publish` fails:

| Platform | Script                                                             | Output, under `src/Enigma.TextEditor.Desktop/` |
|----------|--------------------------------------------------------------------|------------------------------------------------|
| Windows  | `packaging/windows/publish.ps1` or `packaging/windows/publish.bat` | `bin/Release/net10.0/win-x64/publish/`         |
| Linux    | `packaging/linux/publish.sh`                                       | `bin/Release/net10.0/linux-x64/publish/`       |

Each runs `dotnet publish src/Enigma.TextEditor.Desktop -c Release -r <rid> -p:PublishReadyToRun=true`, and the
output folder holds `app.ico` beside the executable, for a shortcut. Windows PowerShell's default policy refuses local
scripts: run `powershell -ExecutionPolicy Bypass -File packaging\windows\publish.ps1`, or the `.bat`.

For a build that runs on a machine with no .NET installed, bundle the runtime instead:

```bash
dotnet publish src/Enigma.TextEditor.Desktop -c Release -r linux-x64 --self-contained true -o ./artifacts
```

Substitute the runtime identifier for the target platform: `linux-arm64`, `win-x64`, `osx-arm64`.

- The entry point is `Enigma.TextEditor.Desktop` (`Enigma.TextEditor.Desktop.exe` on Windows). It takes the files
  to open as arguments.
- **Always pass `-r`.** Without a runtime identifier, the Skia, HarfBuzz and Oniguruma natives for *every* platform
  are carried in a `runtimes/` tree. With one, the natives sit beside the assemblies.
- On `linux-x64`, the result is about 123 MB self-contained, or 44 MB against an installed .NET 10 runtime
  (`--self-contained false`).
- `./artifacts` is a build output: it is not committed.

On Linux, `packaging/linux/install.sh --from ./artifacts` installs that folder for the current user, with a launcher
entry and the icon (the README's *Install on Linux* has the rest).

On Windows, build the MSI and install it:

```powershell
packaging\windows\buildmsi.ps1          # or buildmsi.bat from a cmd prompt
```

It runs `git clean -dxf` first, which deletes every file git does not track. Commit or move anything new before
running it. Then it publishes (`publish.ps1`, above) and packages the result with Enigma.Msi into
`artifacts\Enigma.TextEditor.X.Y.Z.msi`, beside the `.msipkg.json` profile it was built from.

| What                                | Where                                                                                           |
|-------------------------------------|-------------------------------------------------------------------------------------------------|
| The application, for all users      | `%ProgramFiles%\Enigma Text Editor` (a 32-bit package: `Program Files (x86)` on 64-bit Windows) |
| The shortcuts, "Enigma Text Editor" | The desktop and the program menu                                                                |
| The entry in *Installed apps*       | "Enigma Text Editor", publisher "Josué Clément"                                                 |

Installing it replaces an earlier version installed from an MSI, and uninstalling it from *Installed apps* leaves
`%APPDATA%\Enigma.TextEditor` (the settings, the log and `instance.json`) alone.

On macOS there is no installer: run the published folder as it is.

## 6. Post-release verification

- [ ] The published build starts, and **Help → About** shows `X.Y.Z` with the short commit of the tagged commit.
- [ ] It opens a text file and a binary one (the hex-viewer question), and a save keeps the file's encoding and line
      endings.
- [ ] Settings made in the previous version are still there.
- [ ] On Windows, after installing the MSI, *Installed apps* lists one **Enigma Text Editor**, at `X.Y.Z`, and the
      desktop and program-menu shortcuts start it.
- [ ] `git tag` lists `X.Y.Z` on the merge commit on `main`, and the tag is on the remote.

## Before distributing a build

A published build redistributes third-party components. Every licence permits that, but those under BSD (ANGLE on
Windows, NLog, Oniguruma) and Apache 2.0 (some of TextMateSharp's grammars) ask for their copyright notices to go with
binaries. Before handing a build to anyone, ship a third-party notices file beside it. The 1.0.0 licence audit, in
`docs/done/FEATURE-8F24-PHASE02.md`, lists the components.

## Why there is no NuGet step

`dotnet-release` drives a library's release to NuGet. The editor is an application, not a package, so that does not
apply.

Neither installer is a release step:

- Linux's, `packaging/linux/install.sh`, builds or copies an application into the current user's home, so a release
  has nothing to produce for it.
- Windows', `packaging/windows/buildmsi.ps1`, builds the MSI from the version the csprojs carry. Run it after the
  merge, when an MSI is wanted.

## The Windows installer's identity

There is no committed MSI profile per version (`dotnet-release`'s `msiProfiles/`). `buildmsi.ps1` writes the profile
on every build from the csprojs' `<Version>` and its own constants, so a release changes nothing for the installer.

- **`upgradeCode`** is `9f063d09-9f4e-4436-8cb3-39109eda380d`, written once in `buildmsi.ps1`. It is the code the MSI
  that installed 1.9.0 carries, read from its package. It must never change: an MSI with another code installs beside
  the old version instead of replacing it. `WindowsInstallerIdentityTests` pins it, with the product name and the
  executable.
- **`productId`** is new for every build.

The MSI that installed 1.9.0 was built outside this repository with Enigma.Msi, as Enigma.GitClient's were.

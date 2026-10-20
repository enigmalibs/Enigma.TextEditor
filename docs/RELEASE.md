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

A Release build for one platform, with the .NET runtime bundled so the result runs on a machine with no .NET
installed:

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

There is no installer: run the published folder as it is.

## 6. Post-release verification

- [ ] The published build starts, and **Help → About** shows `X.Y.Z` with the short commit of the tagged commit.
- [ ] It opens a text file and a binary one (the hex-viewer question), and a save keeps the file's encoding and line
      endings.
- [ ] Settings made in the previous version are still there.
- [ ] `git tag` lists `X.Y.Z` on the merge commit on `main`, and the tag is on the remote.

## Before distributing a build

A published build redistributes third-party components. Every licence permits that, but those under BSD (ANGLE on
Windows, NLog, Oniguruma) and Apache 2.0 (some of TextMateSharp's grammars) ask for their copyright notices to go with
binaries. Before handing a build to anyone, ship a third-party notices file beside it. The 1.0.0 licence audit, in
`docs/done/FEATURE-8F24-PHASE02.md`, lists the components.

## Why there is no NuGet step or installer

`dotnet-release` drives a library's release to NuGet, and offers a WixSharp MSI profile for an application. Neither
applies to 1.0.0:

- the editor is an application, not a package;
- no Windows installer was asked for.

When one is wanted, generate the **first** MSI profile then. Its `upgradeCode` is created once and reused verbatim in
every later version; its `productId` is new each time.

# FEATURE-8F24-PHASE02 — Version 1.0.0, notes & runbook

**Item:** FEATURE-8F24 — Release 1.0.0
**Branch:** `feature/feature-8f24-phase02-version-notes`
**Run:** feature/2026-10-20-release-1-0-0

## Summary

Version **1.0.0** is cut in the repository.

- **Both csprojs:** `<Version>1.0.0</Version>` (was `0.1.0`).
  - The built assemblies carry file version `1.0.0.0` and informational version `1.0.0+<commit>`.
  - The About dialog reads that value.
- **`RELEASENOTES.md`** (it was empty): `# Release notes`, then `## 1.0.0 — 2026-10-20`, in Launchpad's app shape:
  - a positioning paragraph;
  - themed sections: *Tabs and panes*, *Vim*, *Files*, *Syntax highlighting*, *Hex viewer*,
    *Status bar, settings and help*, *Your data*;
  - *Compatibility* and *Version*.
- **`README.md`:**
  - the *What's new in 1.0* callout under the intro;
  - a line pointing at the runbook for a build that runs without the SDK;
  - the layout table's `docs/` row naming the runbook.
- **`docs/RELEASE.md`:** the `dotnet-release` runbook, adapted to an app after Launchpad's:
  - pre-flight in Release;
  - the version read off the build (Linux `strings`, Windows `VersionInfo`, or *Help → About*);
  - merge into `develop` then `main`, a bare lightweight tag `X.Y.Z`, push;
  - `dotnet publish -r <rid>`, then post-release checks;
  - the third-party notices a distributed build needs;
  - why there is no NuGet step and no installer.

## Files / modules touched

**Created**

- `docs/RELEASE.md`
- `docs/done/FEATURE-8F24-PHASE02.md`

**Modified**

- `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj` — `<Version>1.0.0</Version>`
- `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj` — `<Version>1.0.0</Version>`
- `RELEASENOTES.md` — the 1.0.0 section
- `README.md` — the callout, the publish pointer, the `docs/` row
- `docs/roadmap.md`, `docs/plan/FEATURE-8F24.md` — statuses

## Runtime licence audit

**What it covers:** what a published build redistributes. That is:

- every package in the Desktop project's `project.assets.json` for `net10.0` that has runtime or native assets,
  each licence read from the package's own nuspec, licence file or notices;
- the .NET runtime itself, for a self-contained build.

**Cross-checks:**

- The `linux-x64` Release publishes were listed against this.
- `AvaloniaUI.DiagnosticsSupport` and `Microsoft.IO.RecyclableMemoryStream` are absent from them.

| Component                                          | Licence                        | Ships                 |
|----------------------------------------------------|--------------------------------|-----------------------|
| .NET runtime                                       | MIT                            | self-contained builds |
| Avalonia 12.1.2 and its platform packages¹         | MIT                            | all builds            |
| `Avalonia.Angle.Windows.Natives` (ANGLE)           | BSD-3-Clause (licence file)    | Windows builds        |
| Inter, the font in `Avalonia.Fonts.Inter`          | SIL OFL 1.1 (in the font)      | all builds            |
| AvaloniaEdit and AvaloniaEdit.TextMate 12.0.0      | MIT                            | all builds            |
| TextMateSharp, TextMateSharp.Grammars 2.0.3        | MIT                            | all builds            |
| The grammars inside TextMateSharp.Grammars²        | MIT and permissive             | all builds            |
| Onigwrap 1.0.10, with Oniguruma natives            | MIT; Oniguruma BSD-2-Clause    | all builds            |
| SkiaSharp 3.119.4 and its natives                  | MIT                            | all builds            |
| HarfBuzzSharp 8.3.1.3 and its natives              | MIT                            | all builds            |
| CommunityToolkit.Mvvm 8.4.2                        | MIT                            | all builds            |
| Microsoft.Extensions.* 10.0.13³                    | MIT                            | all builds            |
| NLog 6.1.4, NLog.Database 6.0.3                    | BSD-3-Clause                   | all builds            |
| NLog.Extensions.Logging 6.1.4                      | BSD-2-Clause                   | all builds            |
| The Enigma packages⁴                               | MIT (`LICENSE.md`)             | all builds            |
| Phosphor Icons artwork, in Enigma.Icons.Phosphor   | MIT (`THIRD-PARTY-NOTICES.md`) | all builds            |
| BouncyCastle.Cryptography 2.6.2, via Enigma.Core   | MIT                            | all builds            |
| MicroCom.Runtime 0.11.6, Tmds.DBus.Protocol 0.94.1 | MIT                            | all builds            |
| AvaloniaUI.DiagnosticsSupport 2.2.3                | —                              | **no**: Debug only    |
| Avalonia.BuildServices 11.3.2                      | MIT                            | no: build time only   |
| Microsoft.IO.RecyclableMemoryStream 3.0.1          | MIT                            | no: no runtime asset  |

1. Avalonia.Desktop, X11, Win32, Native, FreeDesktop, FreeDesktop.AtSpi, Skia, HarfBuzz, Themes.Fluent,
   Remote.Protocol, Fonts.Inter (the package itself is MIT).
2. 107 components, per the `cgmanifest.json` each grammar embeds:
   - MIT: 88;
   - TextMate Bundle License: 14;
   - Apache-2.0: 3;
   - GitHub License (MIT-style): 1;
   - BSD: 1.
3. Hosting, Configuration (and its providers), DependencyInjection, Diagnostics, FileProviders, FileSystemGlobbing,
   Logging (and its providers), Options, Primitives; and System.Diagnostics.EventLog 10.0.13.
4. Enigma.Avalonia.Desktop 1.2.0; Enigma.Configuration, Enigma.Core, Enigma.Icons, Enigma.Icons.Avalonia,
   Enigma.Icons.Phosphor, Enigma.Logging.Abstractions and Enigma.Logging.NLog 1.0.0.

**Verdict:** every licence permits redistribution in binary form. The application's own licence is MIT
(`LICENSE.md`).

**Attribution:** BSD (ANGLE, NLog, Oniguruma) and Apache-2.0 (three grammars) require their notices to travel with
distributed binaries. Nothing is distributed by this release: the tag is a source release, and the runbook's
publish is for the user's own use. `docs/RELEASE.md` says so under *Before distributing a build*, and the
follow-up below covers it.

## Deviations & follow-ups

- **Follow-ups, none blocking the release:**
  - Before handing a published build to anyone, add a third-party notices file to the publish output, with the
    BSD and Apache notices listed above.
  - Enable *Private vulnerability reporting* in the GitHub repository (from PHASE01).
  - An installer (an MSI profile, or a Linux installer like Launchpad's), when distribution is wanted.
- **One deviation:** `SECURITY.md` was added to the runbook's pre-release checklist. The template has no such line,
  but its supported-versions table must follow each release.
- **Not done, as planned:** no dependency refresh.
- **The publish sizes in the runbook were measured**, not copied: 123 MB self-contained and 44 MB
  framework-dependent, for `linux-x64` at this commit. Both were published to a scratch directory, outside the
  repository.
- **Fix budget:** none used. The first build and test runs were green.
- **Documentation sweep:** the README callout, its publish pointer and its `docs/` row, as above. Nothing else
  names the version. `SECURITY.md` already lists `1.0.x`.
- **Line endings:** every touched file is LF, per `.gitattributes`.

## Build/test evidence

- Release: `dotnet build Enigma.TextEditor.slnx -c Release` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx -c Release` gave 2503 passed, 0 failed, 0 skipped.
- Debug: `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` gave 2503 passed, 0 failed, 0 skipped.
- **The built version, read with the runbook's Linux recipe:**
  - `strings …/Enigma.TextEditor.Core.dll | grep -m1 -o '1\.0\.0+[0-9a-f]*'` prints
    `1.0.0+f73682cf49f83cdfb94b189872e3627c12e74019`. That is the run branch's tip at build time, since a build
    stamps `HEAD`.
  - The Desktop assembly prints the same.
  - The Win32 resource holds `FileVersion 1.0.0.0` and `ProductVersion 1.0.0+f73682c…` (read with `strings -el`).
    That is what the Windows `VersionInfo` recipe reads; PowerShell is not installed here, so that recipe itself
    was not run.
- **Publish:** `dotnet publish src/Enigma.TextEditor.Desktop -c Release -r linux-x64` succeeded with
  `--self-contained true` and with `--self-contained false`.
- Nothing in this dev ran `git tag`, `git push`, `dotnet pack` or `dotnet nuget push`.

## Decisions taken at build time

| Question                  | Chosen                                                  | Why                                                                   |
|---------------------------|---------------------------------------------------------|-----------------------------------------------------------------------|
| Release-notes heading     | `## 1.0.0 — 2026-10-20` under `# Release notes`         | The house `version — date` style                                      |
| Release-notes content     | What the application does today, by theme               | A first release describes the whole product                           |
| Reading the version       | Off the build, two recipes, plus *Help → About*         | The About dialog already shows it, unlike Launchpad's app             |
| Runbook branch flow       | release branch → `develop` → `main`, then tag `main`    | How this repository integrates; `main` carries the releases           |
| Tag kind                  | Lightweight, bare                                       | Every sibling app's tags are lightweight and bare                     |
| Runtime identifiers named | `linux-x64`, plus `linux-arm64`, `win-x64`, `osx-arm64` | The README names Windows, Linux and macOS                             |
| Notices for binaries      | A runbook section and a follow-up, no notices file now  | Nothing is distributed by this release; a file nobody ships would rot |

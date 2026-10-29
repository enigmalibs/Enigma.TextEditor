# FEATURE-4092 — App icon in the output directory

**Item:** FEATURE-4092 — App icon in the output directory
**Branch:** `feature/feature-4092-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Summary

`Assets/app.ico` is now written beside the executable as `app.ico`, in both the build output and the publish output.
Shortcuts, launchers and installers can point at a file instead of extracting the icon from the `.exe`. This is the
same name and place as Enigma.GitClient's plan (FEATURE-5096).

- **The copy:** the Desktop csproj has a `None` item with `CopyToOutputDirectory="PreserveNewest"` and
  `TargetPath="app.ico"`. It flows to publish on its own, and also to the Desktop test project's output.
- **Unchanged:**
  - the icon stays an `AvaloniaResource`, so `avares://…/Assets/app.ico`, the window's `Icon` and the About image
    are untouched;
  - `ApplicationIcon` is unchanged.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `tests/Enigma.TextEditor.Desktop.UnitTests/AppIconTests.cs`
  - `docs/roadmap.md`
  - `docs/plan/FEATURE-4092.md`
- Created: `docs/done/FEATURE-4092.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Test location | `AppIconTests.TheIcon_SitsBesideTheExecutable_ByteForByteTheAsset`, reading `AppContext.BaseDirectory` | The test project's output receives the referenced app's copy-to-output items, so the test sees the real copy |

## Deviations & follow-ups

- None. `bin/` and `obj/` are ignored, so nothing from the build is staged.

## Documentation sweep

- `README.md` and `docs/RELEASE.md`: nothing became wrong, so no edit was made.

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx -c Release`: 0 warnings, 0 errors. Debug: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 2743 total, 2743 passed (one new test).
- `cmp Assets/app.ico bin/Release/net10.0/app.ico`: identical.
- `dotnet publish src/Enigma.TextEditor.Desktop -c Release -r win-x64 -p:PublishReadyToRun=true`, then `cmp` against
  `win-x64/publish/app.ico`: identical.
- Fix cycles: 0 of 3.

# FEATURE-4092 — App icon in the output directory

**Status:** DONE
**Type:** FEATURE
**Branch:** `feature/feature-4092-icon-in-output-dir`
**Run:** feature/2026-10-29-uniform-enigma-apps

## Objective

The application icon is written to the build output directory, and so to the publish directory, as `app.ico` next to
`Enigma.TextEditor.Desktop(.exe)`. This is part of the baseline every Enigma app shares: shortcuts, launchers and
installers can point at a file instead of extracting the icon from the executable.

## Context & constraints

- `src/Enigma.TextEditor.Desktop/Assets/app.ico` holds ten PNG frames (16–256). It is:
  - the `ApplicationIcon`;
  - the `MainWindow`'s `Icon`.
- `Assets/**` is an `AvaloniaResource` (embedded), so nothing lands in `bin/` today.
- Items with `CopyToOutputDirectory` are also published. They also flow to the output of a referencing project (the
  Desktop test project).
- `AppIconTests` (Desktop tests) pins the icon's frames and the 256 copy.
- Enigma.GitClient plans the same change (its FEATURE-5096): `app.ico` at the output root. One name across the apps.

## Steps

1. `Enigma.TextEditor.Desktop.csproj`: copy `Assets/app.ico` to the output root as `app.ico` (`PreserveNewest`,
   `TargetPath="app.ico"`), with a one-line comment saying why. It stays an `AvaloniaResource` too.
2. `AppIconTests`: a test that `app.ico` sits in `AppContext.BaseDirectory` and is byte for byte the embedded asset.
3. Verify:
   - a Release build puts `app.ico` in `bin/Release/net10.0/`;
   - a `win-x64` publish puts it in `publish/`;
   - both are byte-identical to `Assets/app.ico`.

## Acceptance criteria

- `bin/<Configuration>/net10.0/app.ico` and the publish folder's `app.ico` exist, byte for byte `Assets/app.ico`.
- The embedded resource still resolves (`avares://…/Assets/app.ico` unchanged; `AppIconTests` green).
- The whole build has zero warnings and the whole suite is green.

## Out of scope

- The PNG copy (`app-256.png`), and the Linux `hicolor` icons (`install.sh` installs them).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which file | `app.ico` only | "The app icon" is the `ApplicationIcon`, the narrowest reading. Linux launchers already get PNGs from `install.sh` | Also copying `app-256.png` |
| Name and place | `app.ico` at the output root | Every Enigma app's asset is `Assets/app.ico`, and GitClient's FEATURE-5096 uses the same name: one predictable name | `Assets/app.ico` sub-folder; `Enigma.TextEditor.Desktop.ico` |
| Mechanism | A `None` item with `CopyToOutputDirectory` + `TargetPath` | Standard MSBuild; flows to publish with no custom target | A custom `Copy` target after build and publish |
| Test | One Desktop test, plus a manual build/publish check | Pins the copy where the other icon copies are pinned | No test; a Core test (wrong project for an app asset) |

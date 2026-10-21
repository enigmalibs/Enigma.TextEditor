# FEATURE-5D8B — About dialog like GitClient

**Item:** FEATURE-5D8B — About dialog like GitClient
**Branch:** `feature/feature-5d8b-about-dialog`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

*Help → About Enigma Text Editor* is now Enigma.GitClient's About dialog, with no "About" title at its
top left.

- **Service:** `Title = null` and `DefaultButton = Close`, as in GitClient. Close is the only button;
  with FEATURE-1FBE it opens with the keyboard, and Esc closes it.
- **View:** GitClient's markup and styles, copied as they are:
  - width 380, spacing 14;
  - the 96 px icon from `app-256.png`;
  - an inner stack with the name (18, SemiBold), *Version* and the version as two secondary texts,
    and *Build* with a selectable SHA (11 px, tertiary; the line is absent without a SHA);
  - the copyright (11 px, secondary, wrapping, absent when empty).
- **View model:** gains `HasCopyright`. It keeps this app's plain class, since there is no
  `ViewModelBase` here.

## Files / modules touched

**Created**

- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/AboutDialogTests.cs` — through the real dialog host:
  - no title, the About view as content, one Close button as default;
  - the dialog opens focused on Close, and Esc closes it.

**Modified**

- `src/Enigma.TextEditor.Desktop/Views/AboutView.axaml` — GitClient's layout. Element names kept
  where tests use them (`AppIcon`, `ProductName`, `VersionLine`, `BuildLine`, `CopyrightLine`);
  `Version` and `BuildSha` added.
- `src/Enigma.TextEditor.Desktop/ViewModels/AboutViewModel.cs` — `HasCopyright`.
- `src/Enigma.TextEditor.Desktop/Services/AboutDialogService.cs` — no title, Close as default.
- `tests/…/ViewModels/SettingsMenuTests.cs` — the About view test reads the new structure: the
  version line's two texts, the build line's visibility and SHA, the copyright's visibility, width 380.
- `tests/…/AppIconTests.cs` — the "icon above the name" check now compares positions in the view's
  coordinates (see *Deviations*).
- `docs/roadmap.md`, `docs/plan/FEATURE-5D8B.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Element names | Kept this app's (`VersionLine`, `BuildLine`, `CopyrightLine`) on GitClient's structure | The tests keep their anchors; the look is GitClient's | GitClient's `Copyright` name |

## Deviations & follow-ups

- Fix cycles: 1. `AppIconTests` compared `icon.Bounds.Bottom` with `name.Bounds.Top`, which only
  works for siblings. In GitClient's layout the name sits in a nested stack. The test now translates
  both into the view's coordinates. The assertion (icon above the name) is unchanged.
- Visual check: the dialog was rendered headlessly in the real window to a PNG (temporary scratch test,
  deleted). No title; icon, name, version, build, copyright; one focused Close button.
- Follow-up suggestion: a shared About view in Enigma.Avalonia.Desktop, so the apps cannot drift apart.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong: the README says Help → About shows the version and the commit, which it still
does.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2650 passed**, 0 failed, 0 skipped (2649 before,
  +1).

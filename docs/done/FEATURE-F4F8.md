# FEATURE-F4F8 — Release 1.19.0

**Item:** FEATURE-F4F8 — Release 1.19.0
**Branch:** `feature/feature-f4f8-release-1-19-0`
**Run:** feature/2026-11-10-release-1-19-0

## Summary

1.19.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-EF88 (1.18.0) as the template.

- **Version:** `<Version>1.19.0</Version>` in both csprojs (was 1.18.0).
- **`buildmsi`:** unchanged. It reads the version from the csprojs, so it now builds
  `Enigma.TextEditor.1.19.0.msi`.
- **`RELEASENOTES.md`:** a dated `## 1.19.0 — 2026-11-10` section above 1.18.0's. It has a summary, then:
  - *Selecting and copying bytes in the hex view* (FEATURE-BE30): selecting, the menu, Ctrl+C, the 1 MB limit, the
    current row's new colour, and the selection being per pane and not kept in the session;
  - *`:set lang` opens the language list* (FEATURE-2BFB);
  - *Zooming with Ctrl+wheel and the touchpad* (FEATURE-A47C);
  - *Upgrading from 1.18*: Ctrl+wheel zooms instead of scrolling, a hex press selects a byte, no new key or file, the
    MSI, Linux;
  - *Dependencies*, *Compatibility*, *Known issue* (BUG-4A17, by its symptom), *Version*.
- **README:** the callout names 1.19 first, then 1.18, then 1.17 down to 1.0.
- **`SECURITY.md`:**
  - `1.19.x` is supported; `1.18.x` and earlier are not;
  - the hex-viewer clause in the scope also names the clipboard copy and its 1 MB limit.
- **Dependencies:** no change. The Avalonia set is held back at 12.1.4. 12.1.6 is out, but Enigma.Avalonia.Desktop has
  not moved past 1.3.0.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-F4F8.md`
- Created: `docs/done/FEATURE-F4F8.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Where the notes' facts come from | The done docs of FEATURE-BE30 PHASE01–04, FEATURE-2BFB and FEATURE-A47C PHASE01–02. Strings are checked in the source: the menu items in `HexView.axaml`, the status messages in `HexView.axaml.cs`, the hex refusal in `FileOptions.cs`, the Help rows in `HelpCatalog.cs` | The notes must match what shipped |
| Whether the zoom resizes the hex view | Said so ("over the text, the hex view…") | The hex panel inherits `Preferences.FontSize` (`EditorPaneView.axaml`), and `ZoomGestureTests` zooms over the hex view |
| The `:set lang` refusal text | *Not available in the hex view: lang* | `FileOptions.Apply` names the word as typed, and `FileOptionsTests` asserts that string |
| How the hardware-untested gestures are described | As the README describes them, with no hedging in the notes | Plan decision. The check is left to the user, below |
| `docs/RELEASE.md` | Not edited | Its steps are generic `X.Y.Z`, and nothing in 1.19.0 makes any of it untrue |
| README reflow | The callout's first three lines became six, all within 120 columns. The lines from 1.17 on are untouched | The plan: reflow only the lines this release touches |

## Deviations & follow-ups

- **Before distributing, for the user:**
  - build the 1.19.0 MSI with `packaging\windows\buildmsi.ps1` from a tree with nothing uncommitted. It runs
    `git clean -dxf`, which this run may not;
  - install it over the installed version, and check that *Installed apps* lists one *Enigma Text Editor* at 1.19.0
    (`docs/RELEASE.md` §6);
  - **try the zoom gestures on real hardware.** A Windows precision touchpad's pinch has to arrive as Ctrl+wheel, and
    on macOS the magnify `Delta.X` has to have a usable size. Neither was tested on real hardware
    (FEATURE-A47C-PHASE01/02). If the pinch is too slow or too fast, the constant to tune is the 0.1 threshold in
    `MainWindow`.
- **1.18.0 is on `develop` but not on `main`, and is not tagged.** Merge `develop` into `main` and tag `1.18.0`
  before merging this run. Otherwise 1.18.0 has no merge commit of its own to carry its tag.
- **Merging into `develop` and `main`, the `1.19.0` tag and the push are the user's** (`docs/RELEASE.md` §2–4). This
  release sits on top of two unmerged runs:
  - `feature/2026-11-10-set-lang-zoom-gestures`;
  - `feature/2026-11-10-hex-selection-copy`, which the first one sits on.

  Merging this run's branch into `develop` brings all three.
- **Pre-existing test failure** (the run's baseline): `InstanceServerTests.AClientSpeakingAnotherLanguage_…`. The
  flaky `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` passed in both runs.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.19.0.
- FEATURE-BE30's follow-ups are not in 1.19.0: keyboard selection, Select All, *Edit → Copy* on a hex tab, the
  selection length in the status bar, and auto-scrolling while the pointer is held still below the rows.
- The README's 1.7 history line ("the hex view's current row in the editor's selection colour") describes what 1.7
  shipped, and is left as history.
- Fix cycles used: 0 of 3.

## Documentation sweep

- This dev's own work: the README callout, `RELEASENOTES.md` and `SECURITY.md`, as above.
- Checked and left as they are:
  - `docs/RELEASE.md`, which is generic;
  - the README's *Hex viewer*, `:set` and zoom text, which the three items already updated;
  - `src/Enigma.TextEditor.Core/Vim/README.md`, which the version does not affect.

## Build/test evidence

- `git tag --list 1.19.0`: empty before the release. `1.18.0` is not tagged either. The last tag is `1.17.0`, on
  `main` (`0fbad94`).
- **Run baseline** (run branch `feature/2026-11-10-release-1-19-0` @ `4cd38db`, before the dev branch):
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3332 tests, 3331 passed, 1 failed
    (`InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`).
- `dotnet list Enigma.TextEditor.slnx package --outdated` (2026-11-10):
  - Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent, Avalonia.Headless and Avalonia.Skia
    have 12.1.6;
  - nothing else has an update.
- **This dev:**
  - `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3332 tests, 3331 passed, 1 failed (the baseline's).
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release) carry:
  - file version `1.19.0.0`;
  - informational version `1.19.0+4cd38db…`, the run-branch tip the build was cut from.
- No tests were added, because the dev changes no code.

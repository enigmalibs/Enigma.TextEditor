# FEATURE-6D39 — Release 1.13.0

**Item:** FEATURE-6D39 — Release 1.13.0
**Branch:** `feature/feature-6d39-release-1-13-0`
**Run:** feature/2026-11-02-release-1-13-0

## Summary

1.13.0 is cut on `dotnet-release`'s routine path for an app, with FEATURE-C188 (1.12.0) as the template:

- **Version:** `<Version>1.13.0</Version>` in both csprojs (was 1.12.0).
- **`RELEASENOTES.md`:** a dated `## 1.13.0 — 2026-11-02` section above 1.12.0's. It holds a summary, then:
  - *The file explorer* (FEATURE-F0CE): showing and hiding it, its width, the keyboard, Open and Close Folder, the
    tree's order and unreadable folders, opening a file, Refresh, New File/Folder, Rename, Delete and the tabs of
    deleted files, failures, nothing remembered, the help rows;
  - *The tab strip*: the double-click for a new tab (FEATURE-4107) and the wheel (BUG-74B6);
  - *Upgrading from 1.12*, *Dependencies*, *Compatibility* (with where a deleted entry goes on each platform);
  - *Known issue* (BUG-4A17, by its symptom);
  - *Version*.
- **README:** the callout names 1.13 first, then sums up 1.12 down to 1.0.
- **`SECURITY.md`:** `1.13.x` is supported, `1.12.x` and earlier are not. The scope gains the file explorer.
- **Dependencies:** the Avalonia set is held back at 12.1.4 (12.1.5 is out; Enigma.Avalonia.Desktop has not moved).

Nothing in this dev tagged, pushed, packed, or wrote to `develop` or `main`.

## Files/modules touched

- Modified:
  - `src/Enigma.TextEditor.Core/Enigma.TextEditor.Core.csproj`
  - `src/Enigma.TextEditor.Desktop/Enigma.TextEditor.Desktop.csproj`
  - `RELEASENOTES.md`, `README.md`, `SECURITY.md`
  - `docs/roadmap.md`, `docs/plan/FEATURE-6D39.md`
- Created: `docs/done/FEATURE-6D39.md`

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| The delete questions with no trash | "Where there is no trash, that is the only question"; macOS "asks once, for a permanent delete" | `FileExplorerViewModel` skips the trash question when `IsAvailable` is false. A first draft said "a second question" for both cases, which was wrong |
| How much of the name rules to list | The rules by kind, not the full Windows list | The reserved names and trailing dots are what "a name the system does not allow" covers. The notes stay readable |
| Where the explorer's keys are said to be listed | *Files & tabs* and *Application* | `HelpCatalog`'s real sections |
| README callout wording for 1.12 | "splitting a pane by dropping files from outside the editor near its edge, with the half they will take highlighted, and a font size button" | The "1.x brought …" summary form of the older entries |
| README reflow | The four rewritten lines became seven, all within 120 columns; lines from 1.10 down are untouched | Plan decision: no reflow of the older overlong lines |
| Release date | 2026-11-02 | The run's date |

## Deviations & follow-ups

- `docs/RELEASE.md` is unchanged: none of its generic `X.Y.Z` steps became wrong. There is no new third-party
  component (the explorer's code is ported from Enigma.MarkdownEditor, the same author's MIT code).
- **Manual checks still open**, for the user before tagging (this run opened no window):
  - FEATURE-F0CE PHASE02: the explorer toggle's checked look and the splitter's hairline, in both themes;
  - FEATURE-F0CE PHASE05: one real delete to the Recycle Bin / trash on each platform, and the dialogs' wording.
- **Worth checking on Windows (not verified):** `WindowsTrashService` calls `SHFileOperationW` with `FOF_ALLOWUNDO`
  and `FOF_NOCONFIRMATION`. On a drive with no Recycle Bin (a network share), or for an entry too big for it, the shell
  documents a permanent delete, and `FOF_NOCONFIRMATION` answers its warning. The explorer's second question would then
  not appear. The notes say "the Recycle Bin" for Windows without promising the second question there.
- **README *Files* paragraph** (written by FEATURE-F0CE, not this dev): after the explorer sentences, "Dropped near an
  edge of a pane's text, they split…" now reads as if "they" were the deleted files' tabs, and the paragraph's lines
  run past 120 columns. A wording fix is a separate doc change.
- **Pre-existing test failures** (the run's baseline, left alone):
  - `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn` ("Pipe is broken") fails on
    this machine in every run;
  - `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` is flaky (`docs/done/FEATURE-F0CE-PHASE04.md`):
    it failed in the baseline and passed in both pre-flight runs.
- **Test side effect:** `TrashServiceTests.OnAPlatformWithATrash_AFileReallyGoesIntoIt` put one
  `enigma-texteditor-trash-test-<guid>.md` file in the Recycle Bin per suite run, three for this run.
- BUG-4A17 is still quarantined from an earlier run and is not in 1.13.0.
- **Starting point.** The run was cut from `feature/2026-11-02-tab-strip-file-explorer` (the three items' run), which
  is not merged into `develop`. Merging this run into `develop` brings all three with it.
- The merges into `develop` and `main`, the `1.13.0` tag and the push are the user's (see `docs/RELEASE.md` §2–§4).

## Documentation sweep

- README callout, `RELEASENOTES.md` and `SECURITY.md`, as above. Those are this dev's own work.
- Checked and left as they are: `docs/RELEASE.md`; the README's *Files*, *Tabs and panes* and *The status bar*
  (F0CE and 4107 already describe them). The repository has no `CLAUDE.md`, `AGENTS.md` or `CONTRIBUTING.md`.

## Build/test evidence

- **Run baseline** (run branch at `3704034`, before this dev):
  - `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors;
  - `dotnet test --solution Enigma.TextEditor.slnx`: 3109 total, 3107 passed, 2 failed (`InstanceServerTests…`
    and the flaky `FileDropViewTests…`, both above).
- `git tag --list 1.13.0`: empty before the release.
- `dotnet build Enigma.TextEditor.slnx -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Release`: 3109 total, 3108 passed, 1 failed (`InstanceServerTests…`).
- `dotnet build Enigma.TextEditor.slnx -c Debug --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx -c Debug`: 3109 total, 3108 passed, 1 failed (`InstanceServerTests…`).
- `Enigma.TextEditor.Core.dll` and `Enigma.TextEditor.Desktop.dll` (Release):
  - file version `1.13.0.0`;
  - informational version `1.13.0+3704034…`, the run-branch tip the build was cut from.
- `dotnet list package --outdated` (at planning and again at build time): the six Avalonia packages have 12.1.5;
  nothing else. With `--include-prerelease`, also Microsoft.Extensions.Hosting `11.0.0-rc.3.26522.148`, not taken.
- No tests were added: the dev changes no code. Fix cycles: 0 of 3.

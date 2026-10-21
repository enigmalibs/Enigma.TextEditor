# FEATURE-4217 — Dropped file replaces a new tab

**Item:** FEATURE-4217 — Dropped file replaces a new tab
**Branch:** `feature/feature-4217-drop-replaces-new-tab`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

A file dropped on a pane whose active tab is a new tab now takes that tab's place, instead of opening
beside it and leaving an empty `Untitled-1` behind.

- **A new tab** is one whose buffer is untitled, text, empty, not dirty, and shown by no other tab.
  That is what a new pane, a new window or *File → New* gives you before you type.
- **Which file:** the first dropped file that opens in a new tab takes the new tab's place. The
  others open after it, as before, and the last one is shown.
- **Checked after opening:** the tab is checked again once the file has opened, because a question (a
  binary file) can come in between. A file that does not open (cancelled binary prompt) leaves the new
  tab as it was.
- **Already open:** a dropped file already on the pane is shown where it is. The new tab is left for
  the next dropped file.
- **Everything else is unchanged:** a tab with text, a file tab, an untitled buffer another pane also
  shows. File → Open, `:e` and the command line still open beside the current tab.

## Files / modules touched

**Modified**

- `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`:
  - `OpenDroppedAsync` notes the pane's active tab when it is fresh;
  - new `IsFresh` and `OpenInPlaceOfAsync` (replace through `EditorPaneViewModel.Replace`, with
    `Retain`/`Release` in the registry);
  - remarks.
- `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileDropTests.cs` — seven new tests:
  - a new pane's tab is replaced;
  - several files: the first replaces the new tab, the rest follow;
  - an untitled tab with text is kept;
  - a file tab is kept;
  - an untitled buffer shown in another pane is kept;
  - a file that does not open keeps the new tab;
  - an already-open file is shown, and the next file takes the new tab.
- `docs/roadmap.md`, `docs/plan/FEATURE-4217.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| A dropped file that is already on the pane, before a new one | Show it; the next file that opens takes the new tab | The new tab is still unused, and the drop asked for the other file | Stop replacing as soon as any file is handled |
| Where the check runs | In the workspace, at drop time and again after the open | The open can ask a question; the tab must still be fresh when replaced | Checking once before the loop |

## Deviations & follow-ups

- The plan said "only by the first file that opens". The implementation refines it to "the first file
  that opens *in a new tab*". The case where an already-open file comes first is covered by a test.
- Follow-up suggestion: apply the same rule to File → Open and `:e` if it feels right in use. Notepad++
  does this.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong. The README says files open "by dropping them on a pane", which is still true.
RELEASENOTES only describes released versions.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2510 passed**, 0 failed, 0 skipped (2503 before,
  +7). Fix cycles: 0.

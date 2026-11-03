# FEATURE-F0CE-PHASE01 — Folder tree model

**Item:** FEATURE-F0CE — File explorer on the files page
**Branch:** `feature/feature-f0ce-phase01-folder-tree-model`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Summary

This phase adds the explorer's model, with no window yet. It lists a folder lazily, refreshes it, and opens files from
it.

- **Core `Files/`** (ported from Enigma.MarkdownEditor, namespace `Enigma.TextEditor.Core.Files`):
  - `FileSystemEntry`, `NaturalNameComparer`, `FileSystemEntryComparer` (folders first, then natural order, ignoring
    case);
  - `DirectoryListing` / `DirectoryListingStatus`, and `DirectoryLister.ListAsync(folder, token)`. It lists one level
    off the calling thread, hidden and dot entries included (MarkdownEditor's filter is dropped). An entry whose
    attributes cannot be read (a broken link) is skipped.
- **Desktop `ViewModels/`:**
  - `FileTreeNodeKind` (folder, file, reading, unreadable) and `FileTreeNodeViewModel`:
    - a folder holds a *Reading…* row until its first expansion lists it, through a callback. The listing task is
      kept as `Loading`;
    - icon kinds `Folder` / `FolderOpen` / `File`, tooltip = full path. A failed listing becomes one row: *Not found*,
      *No access* or *Cannot be read*;
  - `FileExplorerViewModel`:
    - state: `IsVisible`, `FolderPath`, `HasFolder`, `Header` / `HeaderToolTip`, `Roots` (the folder's entries),
      `SelectedNode`;
    - methods: `OpenFolderAsync` (replaces the folder, shows the explorer), `CloseFolder`, `RefreshAsync`,
      `OpenAsync(node)` (a file → `WorkspaceViewModel.OpenAsync`; a folder toggles);
    - commands: `ToggleCommand`, `RefreshCommand`, `CloseFolderCommand`, `OpenCommand`;
    - every listing is merged into the rows already there. Rows whose entry survives are kept, and with them their
      expansion, selection and children; listed folders below are listed again;
    - a listing still running when the folder changes or closes is cancelled and dropped.
  - Registered as a singleton; `MainWindowViewModel.Explorer`.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Core/Files/FileSystemEntry.cs`, `NaturalNameComparer.cs`,
  `FileSystemEntryComparer.cs`, `DirectoryListing.cs`, `DirectoryListingStatus.cs`, `DirectoryLister.cs`
- Created: `src/Enigma.TextEditor.Desktop/ViewModels/FileTreeNodeKind.cs`, `FileTreeNodeViewModel.cs`,
  `FileExplorerViewModel.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/Extensions/ServiceCollectionExtensions.cs`
- Created: `tests/Enigma.TextEditor.Core.UnitTests/Files/TempTreeFolder.cs`, `DirectoryListerTests.cs`,
  `FileTreeOrderTests.cs`; `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FileExplorerTests.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Infrastructure/EditorHarness.cs`, `TempFolder.cs`,
  `CompositionRootTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/FEATURE-F0CE.md`; created this file

## Deviations & follow-ups

- **Unreadable rows** say why (*Not found*, *No access*, *Cannot be read*) rather than always *Cannot be read*. The
  status was already at hand.
- **`TempFolder.Write`** (Desktop tests) now creates the folders a name holds. Existing callers pass bare names, so
  they are unaffected.
- **Pre-existing failure:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`, as
  recorded in `docs/done/FEATURE-4107.md` (the run's baseline).
- **Documentation sweep:** no user-facing change yet (no view). The README and help follow in PHASE02.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Re-expanding a listed folder | Not listed again | Refresh does that. Listing on every expansion would flicker the rows |
| How a refresh changes rows | Move/insert/remove in place, reusing surviving nodes | The tree keeps the containers of the rows that stay, so expansion and selection survive without bookkeeping |
| A failed first expansion | Marked loaded, showing the reason row | Refresh lists it again, so a folder that becomes readable recovers |
| Faults in a lazy listing | `Forget()` (the safety net) | The expansion is fire-and-forget from a property setter. A fault there is a bug, and must be seen, not swallowed |
| Path comparison when merging | Ordinal | The paths come from the same enumeration, so a case change on disk is a new entry |

## Build/test evidence

- **Baseline:** the run's, recorded in `docs/done/FEATURE-4107.md` (0 warnings; 2944 passed, 1 failed).
- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2985 total, 2984 passed, 1 failed (the baseline's).
- **Tests added (+31):**
  - Core `DirectoryListerTests` (11): order, dot and hidden entries listed, empty, missing, full paths, one level,
    cancellation, nameless directory, failed listing needs a reason;
  - Core `FileTreeOrderTests` (9, ported): natural digits, leading zeros, long runs, case, total order, folders
    first;
  - Desktop `FileExplorerTests` (11): starts hidden with no folder; toggle; open lists the top level and shows the
    explorer; expanding lists once; a vanished folder shows *Not found*; close empties; a listing in flight is
    dropped on close; open replaces; refresh merges external changes, keeping rows, expansion and selection; a file
    row opens in the active pane; a folder row expands, then collapses.

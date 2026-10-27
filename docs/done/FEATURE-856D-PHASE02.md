# FEATURE-856D-PHASE02 — Files page follows the editor

**Item:** FEATURE-856D — Icon rail & settings page (PHASE02 of 2)
**Branch:** `feature/feature-856d-phase02-files-page-follows-editor`
**Run:** feature/2026-10-27-rail-defaults-setup-release

## Summary

While the settings page is shown, anything that opens, creates or rearranges a document brings the Files
page back, with the keyboard in the active pane. That covers:

- File → New / Open / Open as Hex, Ctrl+N, `:e` and the startup files;
- reopening the document already shown;
- splits, closing a pane, *Only This Pane*, *Equalize Panes*;
- the tab keys, and closing a tab.

How:

- `WorkspaceViewModel.DocumentShown`, raised by `Show(document)` every time — a new tab or one brought
  forward again. The active document does not change when the file opened is the one already shown, so the
  window cannot rely on `ActiveDocument` alone.
- `MainWindowViewModel` calls `Shell.ShowFiles()` on `DocumentShown`, `LayoutChanged` and an
  `ActiveDocument` change.
- `MainWindow.ActiveEditor` is `null` while Settings is shown, so the Edit menu (Undo, Cut, Paste,
  Select All…) edits no hidden document.
- The focus posted after a page switch waits for an open dialog to close. A file opened behind a question
  brings the files back, and the dialog then hands the keyboard to the page shown.

Changing a setting, resetting, saving or zooming leaves the settings page where it is.

## Files / modules touched

- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/FilesPageTests.cs`.
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/WorkspaceViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/ViewModels/MainWindowViewModel.cs`,
  `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`,
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SettingsPageTests.cs`.

## Deviations & follow-ups

- Added `WorkspaceViewModel.DocumentShown`, which the plan did not name. `ActiveDocument` and
  `LayoutChanged` miss "open the file that is already active".
- Doc sweep: nothing became wrong. The README describes the rail, the page and Esc; it never said the
  page stays put while a file is opened.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| Which workspace signals bring Files back | `DocumentShown`, `LayoutChanged`, `ActiveDocument` changes | Together they cover every open, new, split, close and tab switch; saving and settings changes raise none |
| Where the rule lives | `MainWindowViewModel` | It already follows the workspace for the title; testable without a window |
| Edit menu on the Settings page | `ActiveEditor` returns `null` | One guard covers all six Edit items, which go through it |
| Focus after a switch while a dialog is open | Deferred to the dialog's own hand-back | Taking the keyboard from an open question would answer it with the next key |

## Build/test evidence

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2720 total, 2720 passed, 0 failed, 0 skipped.
- Added: `FilesPageTests` (5 cases): New, Open, opening the file already shown, a tab switch and a split each
  bring Files back; settings changes, reset and save keep the settings page. `SettingsPageTests` (+2): Ctrl+N
  on the page shows the new tab with the keyboard; Edit → Select All on the page edits nothing, and the
  active editor comes back with the Files page.
- Fix cycles: 1. One test scenario was wrong, not the code: after `Split(…, newDocument: false)` the active
  pane is empty, so the tab switch was moved before the split.

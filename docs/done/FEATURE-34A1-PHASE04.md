# FEATURE-34A1-PHASE04 — Keep unsaved changes on exit

## Summary

Closing the editor no longer asks about unsaved changes. The session keeps them: modified files and untitled
documents with text come back at the next start, marked unsaved, with the same text, encoding, line endings,
indentation and language. Closing a tab still asks.

- `CaptureSession(keepUnsaved)` and `DocumentViewModel.ToSession(keepUnsaved)` store, for each buffer with changes
  its file does not have and each untitled buffer with text, the text and the format it will be saved in (a pending
  encoding or line-ending change included).
- `IDocumentService.RestoreAsync` rebuilds such a buffer from the stored text without reading its file, which may be
  gone; a save writes it again. `DocumentViewModel.MarkRestoredUnsaved()` keeps the dot until a save or a reload,
  because undo cannot reach back past the restart.
- `MainWindow.OnClosing`: the owner instance writes the session with the unsaved changes and closes without a
  question. After `:qa!` the tabs are kept but the changes are not. If the session cannot be kept (another instance
  owns it, or the write failed) and changes would be lost, today's questions are asked, and the session is written
  after them without unsaved text.
- `MainWindowViewModel.KeepSessionOnClose()` is new; `SaveSession()` stays for after the questions.
- Help: the `:qa` / `:qa!` rows describe the new behaviour.

## Files/modules touched

- Modified: `src/Enigma.TextEditor.Desktop/Services/IDocumentService.cs`, `DocumentService.cs`
- Modified: `src/Enigma.TextEditor.Desktop/ViewModels/DocumentViewModel.cs`, `WorkspaceViewModel.cs`,
  `MainWindowViewModel.cs`, `HelpCatalog.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/SessionStartupTests.cs`, `UnsavedChangesTests.cs`
- Created: `tests/Enigma.TextEditor.Desktop.UnitTests/ViewModels/HotExitTests.cs`
- Modified: `README.md`, `docs/roadmap.md`, `docs/plan/FEATURE-34A1.md`

## Deviations & follow-ups

- Two tests encoded "quitting asks" and now cover the fallback instead, with no assertion weakened:
  - `UnsavedChangesTests.TheRealWindow_…` runs as an instance that does not own the session, which still asks.
  - PHASE03's `ClosingTheWindow_KeepsTheSession_AfterTheQuestionsAreAnswered` became
    `ClosingTheWindowWithEverythingSaved_KeepsTheTabsForTheNextStart`. The "questions, then keep" path is covered
    by `HotExitTests.WhenTheSessionCannotBeKept_ClosingAsksAsBefore`.
- The README's *Your data* section ("three things", "Your documents are only ever the files you open and save")
  became wrong in PHASE03, when `session.json` first appeared; the PHASE03 sweep missed it. It is fixed here.
- Flaky, unrelated: `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge` failed in 1 of 4 isolated
  runs (and in one full run) with no change to the drop path. It waits on real file I/O through a fixed number of
  dispatcher yields (`DrainAsync`). Left alone; worth a follow-up.
- Pre-existing failure, unchanged: `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`.
- Release notes, for the next release item: quitting no longer asks; `:qa` keeps unsaved changes; a second window
  asks as before.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| How a restored buffer stays unsaved | A flag, `_restoredChanges`, cleared by a save or reload | The undo stack has no step to say so; undo must not clear it |
| A pending format change on a kept buffer | Stored and restored as the format to save in | The user chose it; dropping it would save in the old encoding |
| Testing a failed write | Session path made a folder | Fails `File.Move` on every platform, unlike a read-only file on Linux |
| Help wording | `:qa` "Quit; tabs and unsaved changes come back at the next start"; `:qa!` "Quit, throwing every change away; the tabs come back" | The old `:qa` row said it asks, which is no longer true |

## Build/test evidence

- Baseline: see `docs/done/BUG-C6A9.md` (0 warnings; 3199 tests, 1 pre-existing failure).
- `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 3257 tests, 3256 passed, 1 failed (the pre-existing failure).
- `%APPDATA%\Enigma.TextEditor` holds no `session.json` or `session.lock` after the full run.
- Tests added (`HotExitTests`, 6):
  - unsaved file (UTF-16 LE, CRLF, tabs) and untitled draft close without a question and come back dirty and exact;
    the save writes the expected bytes;
  - a file deleted since the close comes back with its text, and a save recreates it;
  - `:qa!` keeps the tabs but not the changes;
  - an unwritable session falls back to the questions (Cancel keeps the window, Discard closes it);
  - a restored buffer stays unsaved through undo, and closing its tab asks;
  - a reload clears the restored mark.
- Docs sweep (applied): README, the paragraph on closing and quitting and the *Your data* section (`session.json`,
  `session.lock`, `session.invalid.json`).

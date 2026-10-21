# FEATURE-4217 — Dropped file replaces a new tab

**Status:** DONE — see `docs/done/FEATURE-4217.md`
**Type:** FEATURE
**Branch:** `feature/feature-4217-drop-replaces-new-tab`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

A file dropped on a pane whose tab is a new, untouched untitled buffer takes that tab's place. Today
it opens in a new tab beside it, which leaves an empty `Untitled-1` behind.

## Context & constraints

- `WorkspaceViewModel.OpenDroppedAsync(pane, paths)` activates the pane, then calls `OpenAsync(path)`
  for every existing file. `OpenAsync` goes through `IDocumentService.OpenAsync` (binary prompt, size
  limit, error dialog) and `Show(document)`, which adds a tab.
- `EditorPaneViewModel.Replace(tab, document, out created)` already puts another buffer in a tab's
  place. `ReplaceWithNewDocument` and `ReopenActiveAsync` use it, retaining the new buffer and
  releasing the old one in the `DocumentRegistry`.
- A new pane always starts with an untitled buffer (`Split` → `NewDocument()`), and so does the app
  without startup files.
- `DocumentViewModel`: `IsUntitled` (no file), `IsDirty`, `Document.TextLength`, `IsHex`.
  `CreateForMissingFile` buffers have a file path, so they are not untitled.

## Decisions

| Decision | Rationale |
|---|---|
| A tab is "new" when its buffer is untitled, text (not hex), empty, not dirty, and shown in no other tab | The draft's "just created, no content, not modified". The view-count check keeps a buffer that another pane shows |
| Only the pane's **active** tab is replaced, and only by the **first** file that opens; the other dropped files open as new tabs after it | The tab the user dropped on is the one they see |
| The replacement happens only once the file has opened; a cancelled binary prompt or a failed open leaves the new tab as it was | Never lose the tab for nothing |
| A dropped file already open in that pane is shown, and the new tab stays | Replacing would close a tab to show another one that already exists |
| Drop only; File → Open, `:e` and startup files are unchanged | The draft names the drop; narrowest reading |

## Steps

1. `WorkspaceViewModel`:
   - a private `IsPristine(tab)` check as decided;
   - `OpenDroppedAsync`: before the loop, note the pane's active tab when it is pristine; for the first
     file that opens (`_documents.OpenAsync` returns a buffer that is not already a tab in that pane),
     replace the noted tab (`pane.Replace`, `Retain`/`Release`) instead of `Show`; later files go
     through `OpenAsync` as today;
   - remarks updated.
2. Tests (`FileDropTests`, view-model level):
   - a drop on a pane with one new untitled tab leaves one tab, showing the file;
   - several files: the first replaces the new tab, the others open after it;
   - a dirty or typed-in untitled tab is kept, and the file opens beside it;
   - a file tab is never replaced;
   - an untitled buffer shown in another pane is kept;
   - a failed open (missing file) keeps the new tab;
   - dropping a file already open in the pane shows it and keeps the new tab.

## Acceptance criteria

- Dropping a file on a pane showing a new untouched untitled tab replaces that tab.
- Any other tab is kept, and files open in new tabs as before.
- Zero warnings, whole suite green.

## Out of scope

- The same rule for File → Open, `:e` or the command line.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| What counts as a new tab | Untitled, text, empty, not dirty, one view | "Just created, no content, not modified" | Untitled alone (would lose typed text); only the pane's single tab |
| Which tab is replaced | The active one, by the first opened file | That is the tab the drop landed on | Every pristine tab in the pane |
| Other open paths | Unchanged | Scope: the draft names the drop | Apply to File → Open and `:e` too |

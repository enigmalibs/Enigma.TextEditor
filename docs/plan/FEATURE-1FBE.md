# FEATURE-1FBE — Dialog keyboard focus

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-1fbe-dialog-focus`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

A dialog that opens takes the keyboard. Its button has the focus, Tab moves to the next button and
wraps, and Esc cancels. When it closes, the keyboard goes back where it was.

## Context & constraints

- Every dialog is the window's one `ContentDialog` host (`HostDialog` in `MainWindow.axaml`), from
  Enigma.Avalonia.Desktop 1.2.0, driven by `IContentDialogService.ShowAsync`.
- The template: `PART_PrimaryButton`, `PART_SecondaryButton` and `PART_CloseButton`, left to right,
  hidden when their text is empty. `DefaultButton` only styles a button.
- `ContentDialog.OnKeyDown` closes with `DialogResult.None` on Esc, but only when the key reaches it.
  Nothing moves the focus into the dialog, so today the focus stays in the editor behind it: Esc
  reaches the Vim engine and Tab moves through the window.
- Every prompt maps `DialogResult.None` to its cancel answer (`UnsavedChangesPrompt` → Cancel, etc.).
- Some dialogs focus their own content: the language picker's filter box, the encoding list, the help
  (which also returns the focus itself).
- `ConfirmationPrompt` (e.g. "Discard and reopen") declares `DefaultButton.Close`, i.e. Cancel.

## Decisions

| Decision | Rationale |
|---|---|
| On open, unless the content took the focus itself, focus the dialog's default button, or else the first visible one | The draft's "Save changes?" gets Save (its first and default button); a destructive confirmation gets Cancel, so Enter or Space cannot discard by accident |
| `KeyboardNavigation.TabNavigation="Cycle"` on the host, so Tab and Shift+Tab stay inside the dialog | "Tab focuses the next button" |
| Esc: the library already cancels once the focus is inside | No new key handling |
| On close, the element that had the focus gets it back when it is still on screen, else the active pane's editor | The user goes on typing where they were |
| App-side helper (`Views/DialogKeyboard.cs`) attached to `HostDialog` in `MainWindow`; no library change | The library is a package; upstreaming is a follow-up |

## Steps

1. `Views/DialogKeyboard.cs`:
   - `Attach(ContentDialog host, Func<IInputElement?> fallback)` watches `IsOpenProperty`;
   - on open: remember the focused element; post at Background priority: if the focus is not inside
     the host, focus the button chosen as decided;
   - on close: restore.
2. `MainWindow.axaml`: `KeyboardNavigation.TabNavigation="Cycle"` on `HostDialog`.
   `MainWindow.axaml.cs`: attach the helper, with the active pane's editor as fallback.
3. Tests (headless, real window):
   - "Save changes?" opens with Save focused; Tab goes to Don't save, then Cancel, then back to Save;
   - Esc answers Cancel and the tab stays open;
   - a confirmation with Cancel as default focuses Cancel;
   - the language picker keeps its filter box focused;
   - after the dialog closes, the editor has the focus again.

## Acceptance criteria

- Opening any button dialog focuses its default button (else its first); Tab cycles its buttons; Esc
  cancels.
- Dialogs whose content focuses itself are unchanged.
- The focus returns to the editor after the dialog.
- Zero warnings, whole suite green.

## Out of scope

- Changing Enigma.Avalonia.Desktop itself.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which button gets the focus | The default button, else the first | Meets the draft on "Save changes?" and keeps destructive confirmations safe | Always the first button (Enter would discard on "Discard and reopen?") |
| Where the fix lives | App-side helper | No library release needed | Patching Enigma.Avalonia.Desktop now |
| Focus after closing | Restore the previous element, else the active editor | Typing resumes without a click | Leave it where the dialog left it |

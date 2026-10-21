# FEATURE-7D6B — Status bar gives focus back

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-7d6b-status-bar-focus`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Objective

After using a status bar button (indentation, encoding, language, line endings), the active pane has
the keyboard again, so the user can type at once without clicking the text.

## Context & constraints

- `MainWindow.axaml`, status bar:
  - `IndentationStatus`, `EncodingStatus` and `LineEndingStatus` are buttons with a `MenuFlyout`;
  - `LanguageStatus` runs `Tools.SetLanguageCommand`, which opens the language picker dialog;
  - the encoding flyout's items open the encoding picker dialog.
- Clicking a button takes the focus from the editor, and nothing gives it back.
- FEATURE-1FBE (earlier in this run) makes a dialog restore the focus it found when it opened.
- `SplitHost.ActivePaneView?.FocusEditor()` focuses the active pane's editor (or its hex rows).

## Decisions

| Decision | Rationale |
|---|---|
| The four status buttons are `Focusable="False"`: a click leaves the focus in the editor | Nothing to give back for the language button, and a dialog it opens restores the editor |
| When a status flyout closes (chosen or dismissed), the active pane's editor gets the focus, posted so it lands after the flyout's own focus handling | A flyout takes the focus into its menu |
| The same `FocusActivePane()` in `MainWindow` serves both | One door |

## Steps

1. `MainWindow.axaml`: `Focusable="False"` on the four status buttons; `Closed` handlers on the three
   flyouts.
2. `MainWindow.axaml.cs`: `FocusActivePane()` (through `Splits`), posted from the flyouts' `Closed`.
   `SplitHost`: expose the active pane focus if needed.
3. Tests (headless, real window):
   - after choosing *Spaces: 2* in the indentation flyout, the editor of the active pane has the
     focus;
   - after dismissing the line-ending flyout, the same;
   - clicking the language button and picking a language leaves the focus in the editor;
   - in a split, the active pane (not the first) gets it.

## Acceptance criteria

- After any status bar button is used, typing goes to the active pane without a click.
- Zero warnings, whole suite green.

## Out of scope

- The menu bar's menus.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Mechanism | Non-focusable buttons + refocus on flyout close | Covers both the flyouts and the dialog path with the least code | Refocusing after every command in the view models (they must not know about focus) |

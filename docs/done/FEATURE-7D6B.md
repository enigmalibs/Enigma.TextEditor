# FEATURE-7D6B — Status bar gives focus back

**Item:** FEATURE-7D6B — Status bar gives focus back
**Branch:** `feature/feature-7d6b-status-bar-focus`
**Run:** feature/2026-10-21-minimap-keys-dialogs

## Summary

After using a status bar button — indentation, encoding, language, line endings — the active pane has
the keyboard again, so typing goes on without a click in the text.

- **The four status buttons are `Focusable="False"`.** Clicking one leaves the keyboard where it was, in
  the active pane's editor. When its flyout closes, or the language picker's dialog closes
  (FEATURE-1FBE restores the focus a dialog found), the keyboard is still or again in the pane.
- **Safeguard:** when a status flyout closes, `MainWindow` also gives the keyboard to the active pane,
  posted after the flyout's own focus handling. It skips this while a dialog is open, because an item
  that opened one (Change File Encoding…) leaves the keyboard to it.

## Files / modules touched

**Created**

- `tests/Enigma.TextEditor.Desktop.UnitTests/Views/StatusBarFocusTests.cs` — real window, mouse clicks:
  - the four buttons take no focus;
  - each of the three flyouts, opened and closed, leaves the editor focused;
  - choosing *Spaces: 2* in a split leaves the active right-hand pane focused;
  - the language button leaves the editor focused.

**Modified**

- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml` — `Focusable="False"` on the four buttons;
  `Closed="OnStatusFlyoutClosed"` on the three flyouts.
- `src/Enigma.TextEditor.Desktop/Views/MainWindow.axaml.cs` — `OnStatusFlyoutClosed`.
- `docs/roadmap.md`, `docs/plan/FEATURE-7D6B.md` — statuses.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Keep the flyout `Closed` safeguard although the headless tests pass without it | Keep | On a real desktop a flyout is a popup window whose focus hand-back may differ from the headless platform's; ten lines guard the requirement there | Rely on `Focusable="False"` alone |

## Deviations & follow-ups

- Fix cycles: 1 (a test used a member the language-picker fake does not have).
- **Mutation check (temporary, reverted):**
  - with both changes reverted (focusable buttons, no handler), all 9 new tests fail, so they catch
    the reported problem;
  - with `Focusable="False"` alone, all 9 pass. In headless, the non-focusable buttons are the fix,
    and the `Closed` safeguard is not separately exercised. It is unverified on a real desktop;
    please check it once by hand.
- Line endings: the touched files are LF, as `.gitattributes` asks; no churn.

## Documentation sweep

Nothing became wrong: the README says the status items are buttons that change them, which is
unchanged.

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2649 passed**, 0 failed, 0 skipped (2640 before,
  +9).

# BUG-B026-PHASE03 — Tab from find to replace field

**Item:** BUG-B026 — Find and replace panel with vim on
**Branch:** `bugfix/bug-b026-phase03-tab-to-replace`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Summary

In the find and replace panel (`Ctrl+H`), `Tab` in the find field now goes straight to the replace field, and
`Shift+Tab` in the replace field comes back. This works with vim on (any mode) and off.

- **Cause:** the panel's template puts the expander toggle, three option toggles and three buttons between the two
  fields. The platform's focus order took four presses of `Tab` to reach the replace field. A headless probe showed
  the order: find field → match case → whole words → regex → buttons → replace field.
- **Fix:** a new static `SearchPanelTabOrder`, attached by `EditorPaneView` to its editor. It adds a tunnelling
  `KeyDown` handler. In replace mode it moves `Tab` from `PART_searchTextBox` to `PART_replaceTextBox` and
  `Shift+Tab` back, and handles the key so the window's own navigation does not run. `NextField` holds the rule.
- The find-only panel (`Ctrl+F`) keeps the platform's order. Every other key and modifier is untouched.

## Files/modules touched

- Created: `src/Enigma.TextEditor.Desktop/Views/SearchPanelTabOrder.cs`
- Modified: `src/Enigma.TextEditor.Desktop/Views/EditorPaneView.axaml.cs`
- Modified: `tests/Enigma.TextEditor.Desktop.UnitTests/Views/SearchPanelTests.cs`
- Modified: `docs/roadmap.md`, `docs/plan/BUG-B026.md`. Created this file.

## Deviations & follow-ups

- **Template part names confirmed** (plan step 1) with a throwaway headless probe, removed before the commit:
  `PART_searchTextBox` and `PART_replaceTextBox`.
- **No change to `VimEditorBehavior`** (plan step 3): PHASE02 already makes it skip every key raised inside the panel.
- **Pre-existing failure and flake:** `InstanceServerTests.AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn`
  and `FileDropViewTests.ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge`, both in the run baseline
  (`BUG-B026-PHASE01.md`).
- **Documentation sweep:** no prose doc describes the panel's keyboard order, so nothing was stale.
- **Release notes** are left to the next release item.

## Build/test evidence

- Baseline: see `docs/done/BUG-B026-PHASE01.md`.
- **This dev:** `dotnet build Enigma.TextEditor.slnx` gave 0 warnings and 0 errors.
  `dotnet test --solution Enigma.TextEditor.slnx` ran 3175 tests: 3173 passed and 2 failed, the baseline test and
  the baseline flake.
- **Tests added (4 cases in `SearchPanelTests`):**
  - `TabInTheFindField_GoesToTheReplaceField_AndShiftTabComesBack`: vim on in normal mode, in insert mode, and vim
    off. The focus starts in the find field, moves to the replace field and comes back. The text is unchanged.
  - `TabInTheFindOnlyPanel_KeepsThePlatformsOrder`: focus moves to another control of the panel, never to either
    field.
- **Sanity check:** with `Attach` commented out, the three Tab/Shift+Tab cases fail.

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Shape of the class | Static `Attach(editor)`, the handler reading the editor from `sender` | No state to keep, and nothing to dispose: the handler lives as long as the editor | An instance held in a field of the pane |
| Routing | Tunnel on the editor | Runs before the window's focus navigation and before any handler in the panel | Bubble on the text box (depends on the template's own handlers leaving Tab alone) |
| Toggles and buttons | Left out of the Tab cycle between the fields, still reachable by mouse and their keys | The draft asks for find → replace; the toggles sat between them | Re-ordering the whole template's tab indices |

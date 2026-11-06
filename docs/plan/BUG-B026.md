# BUG-B026 — Find and replace panel with vim on

**Status:** TODO
**Type:** BUG
**Branch (PHASE01):** `bugfix/bug-b026-phase01-open-panel`
**Branch (PHASE02):** `bugfix/bug-b026-phase02-panel-keys`
**Branch (PHASE03):** `bugfix/bug-b026-phase03-tab-to-replace`
**Run:** bugfix/2026-11-06-vim-search-mouse

## Objective

With vim on, AvaloniaEdit's find (`Ctrl+F`) and find-and-replace (`Ctrl+H`) panel only works in insert mode. Make it
work whatever the vim mode:

- `Ctrl+F` and `Ctrl+H` open the panel in every mode;
- typing in its fields works in every mode;
- `Esc` closes it, and does nothing else, in every mode;
- in replace mode, `Tab` in the find field moves to the replace field.

## Context & constraints

- The panel is AvaloniaEdit's own `AvaloniaEdit.Search.SearchPanel`, installed by `TextEditor.OnApplyTemplate`
  (`TextEditor.SearchPanel`). It is shown inside the text area (`TextArea.AddChild`), so it is a visual descendant of
  the `TextEditor`. Its gestures (checked by reflection on AvaloniaEdit 12.0.0) are `ApplicationCommands.Find` =
  `Ctrl+F`, `ApplicationCommands.Replace` = `Ctrl+H`, `SearchCommands.CloseSearchPanel` = `Escape`, `FindNext` = `F3`,
  `FindPrevious` = `Shift+F3`, `ReplaceNext` = `Alt+R` and `ReplaceAll` = `Alt+A`. They are bound through
  `SearchInputHandler`, a nested input handler of the `TextArea`. Its fields are two `TextBox`es (`_searchTextBox`,
  `_replaceTextBox`), and `IsReplaceMode` shows the second one.
- `VimEditorBehavior` (`src/Enigma.TextEditor.Desktop/Views/VimEditorBehavior.cs`) adds **tunnelling** `KeyDown` and
  `TextInput` handlers on the whole `TextEditor` (`Refresh`, lines 205-209). Every key typed in the panel's text boxes
  passes through them first:
  - `OnTextInput` swallows all text outside insert mode → **typing in the fields does nothing in normal mode**;
  - `OnKeyDown` offers `Esc`, `Enter`, `Tab`, `Backspace`, arrows, `Ctrl+…` to the engine, which takes them
    (`Esc` always, insert mode included) → **`Esc` never reaches the panel**, and `Ctrl+Z` in a field undoes the document.
- `Ctrl+F` is one of vim's six scroll commands (`KeySequenceParser.ControlActionFor`), taken by the engine in the
  command modes → **`Ctrl+F` never reaches AvaloniaEdit in normal mode**. `Ctrl+H` is turned down by the engine
  (`VimEngine.Dispatch`, the `HasCommandModifier` branch), so it already reaches the editor. It still opens a panel you
  cannot type in.
- FEATURE-3EE8 left "Find without Vim" out of scope because `Ctrl+F` is vim's page-down. This item overrides that: the
  draft asks for `Ctrl+F` explicitly, and `PgDn` still pages down.
- Desktop tests drive a real `MainWindow` headlessly (`HeadlessAvaloniaFixture`, `EditorHarness`, `KeyPressQwerty`,
  `KeyTextInput`). Vim is on by default in the harness. The existing vim view tests are in
  `tests/Enigma.TextEditor.Desktop.UnitTests/Views/VimEditingTests.cs`.

## Decisions

- **`Ctrl+F` / `Ctrl+H` belong to the editor's panel in every mode.** The desktop view never offers them to the
  engine. The Core engine keeps `<C-f>` as page-down (its contract and tests are unchanged). Only the desktop decides
  which keys reach it.
- **Opening the panel outside insert mode leaves vim clean.** The panel opens first and takes a one-line selection as
  its pattern. Then the engine is interrupted: a half-typed command is dropped, an open `:` / `/` line is cancelled,
  and a visual mode is left. Insert mode is kept as it is.
- **Keys whose source is inside the panel are the panel's.** Neither vim handler acts on an event raised from inside
  `TextEditor.SearchPanel`. This makes typing, `Enter` (find next), `Esc` (close), `Backspace`, the arrows, `Ctrl+Z`
  and IME input in the fields behave as with vim off.
- **`Esc` with the text focused and the panel open:** in idle normal mode (nothing pending, no visual mode, no command
  line) `Esc` closes the panel, as AvaloniaEdit does with vim off. In insert mode, a visual mode or with something
  pending, `Esc` is vim's first, and a second `Esc` closes the panel.
- **Tab order:** in replace mode, `Tab` in the find field focuses the replace field and `Shift+Tab` in the replace field
  goes back. Applies with vim on or off.

## PHASE01 — Ctrl+F and Ctrl+H open in every mode

**Status:** TODO

### Steps

1. `VimEditorBehavior.OnKeyDown`: before `Translate`, return without touching `e.Handled` when the key is a search
   shortcut, i.e. `F` or `H` with Control (or Meta) and without Alt. A small `IsSearchShortcut(KeyEventArgs)` helper.
   AvaloniaEdit's `TextArea` then runs Find / Replace.
2. After AvaloniaEdit has run: add a bubbling `KeyDown` handler on the editor with `handledEventsToo: true`, attached
   and detached with the other two in `Refresh`. For a search shortcut, when the panel is now open and the engine is
   not in insert mode, call a new engine method `VimEngine.Interrupt()`.
3. Core `VimEngine.Interrupt()` (public): "something outside vim took the keyboard". It resets the parser, clears
   `_windowPending`, closes the command line (as `Esc` in it would, incremental-search preview included) and leaves a
   visual mode. It is a no-op in insert mode. It is not recorded into an open macro.
4. Help (`HelpCatalog`): in *Scrolling*, `Ctrl+F` is no longer listed as a page down (keep `Ctrl+B`, `PgDn`, `PgUp`). In
   *Search & replace*, add `Ctrl+F, Ctrl+H`: "The editor's find, find and replace panel; Esc closes it".
5. XML remarks: `VimEditorBehavior` class remarks (the scroll keys paragraph) say `Ctrl+F` and `Ctrl+H` are the
   editor's and never offered to the engine.

### Acceptance criteria

- Core: `Interrupt()` drops a pending `d`, cancels an open `:` line (no ex request raised), leaves `v` / `V` (selection
  cleared, mode NORMAL), and changes nothing in insert mode.
- Desktop headless: with vim on, `Ctrl+F` opens the panel (`SearchPanel.IsOpened`) in normal, visual and insert mode,
  and `Ctrl+H` opens it in replace mode (`IsReplaceMode`) in normal and insert mode. Normal mode stays NORMAL and insert
  stays INSERT. From visual mode, the panel's `SearchPattern` is the selected word and the mode ends NORMAL. The view
  does not scroll (the caret's line is unchanged).
- `PgDn` still pages down in normal mode. With vim off, `Ctrl+F` opens the panel as before.
- Zero warnings, whole suite green.

## PHASE02 — The panel's keys are its own

**Status:** TODO

### Steps

1. `VimEditorBehavior`: a helper `IsInSearchPanel(object? source)`, true when `_editor.SearchPanel` is not null and the
   source is that panel or a visual descendant of it. `OnKeyDown`, `OnTextInput` and the PHASE01 bubbling handler
   return at once for such a source.
2. `Esc` from the text with the panel open: in `OnKeyDown`, when the key is a bare `Escape`, the panel is open, the
   engine is in NORMAL and `HasPendingSequence` is false, leave the key alone (not handled). AvaloniaEdit's
   `CloseSearchPanel` binding then closes the panel.
3. Remarks on `VimEditorBehavior` (a paragraph on the search panel).

### Acceptance criteria

- Desktop headless, with vim on, for the panel opened by `Ctrl+F` in normal mode and in insert mode:
  - `KeyTextInput("beta")` writes `beta` into the find field. The document is unchanged and the mode is unchanged.
  - `Enter` in the field selects the next match.
  - `Backspace` in the field removes a character of the pattern, not of the document.
  - `Esc` in the field closes the panel (`IsOpened` false) and the mode is unchanged (insert stays INSERT).
  - the same with `Ctrl+H`: typing in the replace field works.
- With the text focused and the panel open: `Esc` in idle normal mode closes the panel. In insert mode the first `Esc`
  goes to NORMAL with the panel still open, and a second `Esc` closes it.
- Vim's own keys in the text are unaffected (the existing `VimEditingTests` stay green).
- Zero warnings, whole suite green.

## PHASE03 — Tab from find to replace field

**Status:** TODO

### Steps

1. Confirm the panel's template part names for the two text boxes in a headless test (expected `PART_searchTextBox` /
   `PART_replaceTextBox`, or found as the panel's `TextBox` descendants in order).
2. A small view class `SearchPanelTabOrder` (`src/Enigma.TextEditor.Desktop/Views/`) attached by `EditorPaneView` to its
   editor. It has a `KeyDown` handler on the editor (tunnel, so it runs before the text box's own focus navigation).
   When the panel is in replace mode, `Tab` without modifiers from the find field focuses the replace field and
   `Shift+Tab` from the replace field focuses the find field. Both are handled. Other keys and the find-only panel are
   untouched.
3. Make sure `VimEditorBehavior` does not see that `Tab` (PHASE02 already skips panel sources).

### Acceptance criteria

- Desktop headless: `Ctrl+H`, then `Tab` in the find field → the replace field has the keyboard focus. `Shift+Tab` →
  the find field has it back. Tested with vim on (normal and insert mode) and with vim off.
- In the find-only panel (`Ctrl+F`), `Tab` keeps its default behaviour (focus never lands in the hidden replace field).
- Zero warnings, whole suite green.

## Out of scope

- Mapping the panel to vim's own `/` search, or sharing its pattern with `n` / `N`.
- Adopting a match the panel selected as a visual selection, or moving the caret to the match start when the panel
  closes.
- Replacing while an insert session's undo group is open (the replace joins that insert's undo step, as today).
- Edit menu entries for Find / Replace.
- The hex view (it has no search panel).
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `BUG` | The panel is shipped (AvaloniaEdit's default) and broken under vim. The `Tab` order is a small fix to the same panel | `FEATURE` for the Tab order |
| Shape | One item, three phases: open, keys, Tab | Each phase can be reviewed alone in well under 30 minutes and is usable on its own | One phase (mixes three independent fixes); separate items (same panel and same handlers) |
| `Ctrl+F` vs vim's page-down | `Ctrl+F` opens Find in every mode, and the desktop view never offers it to the engine | The draft asks for it explicitly. `PgDn` still pages down. The engine stays a faithful vim and its tests stay valid | Keeping page-down in normal mode (contradicts the draft); removing `<C-f>` from the Core grammar (needless churn in Core) |
| Visual mode / pending command when the panel opens | The panel opens first (takes the selection as its pattern), then `VimEngine.Interrupt()` | A stale visual anchor would make the next motion or operator act on an old range once the panel moves the selection | Leaving vim as it is; interrupting before the panel opens (loses the selection pattern) |
| How vim ignores the panel | Skip events whose source is inside `TextEditor.SearchPanel` | Targeted: only the panel's own text boxes and buttons. The text keeps every vim key | Acting only when the source is the `TextArea` (broader, could hide future in-editor controls); detaching vim while the panel is open (the text must stay vim) |
| `Esc` with the text focused | Closes the panel only in idle normal mode | Matches AvaloniaEdit with vim off, where vim's `Esc` would do nothing. Elsewhere vim's `Esc` has a meaning and keeps it | Always closing (breaks leaving insert mode); never (the draft wants `Esc` to close in every mode) |
| `Shift+Tab` back | Included | A one-way Tab order is a keyboard trap. It costs one more branch | Tab only |
| Tab order with vim off | Same rule | The draft describes the panel, not vim | Vim-only |

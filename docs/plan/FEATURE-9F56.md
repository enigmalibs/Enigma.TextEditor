# FEATURE-9F56 — Move tabs between panes

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-9f56-phaseNN-<slug>` (one per phase)
**Run:** feature/2026-10-30-move-tabs-reuse-instance

## Objective

Move a tab from one split pane to another:

- **with the keyboard**: `<Space>mh`, `<Space>mj`, `<Space>mk`, `<Space>ml` move the active tab to the pane on the
  left, below, above, on the right;
- **with the mouse**: drag a tab from its strip and drop it anywhere on another pane.

## Context & constraints

- **Panes and tabs:**
  - `WorkspaceViewModel` owns the panes (`_byId`, one `EditorPaneViewModel` per `LayoutTree` leaf), the active pane,
    and the rule that a pane losing its last tab closes unless it is the only pane (`CloseTabAsync` → `RemovePane`);
  - `EditorPaneViewModel.Tabs` is the strip, `ActiveTab` the tab showing. `Show(document)` adds a **new**
    `EditorTabViewModel`; `Remove(tab)` shows the neighbour first, then takes the tab off;
  - `EditorTabViewModel` carries the view state (caret, scroll, hex row): moving the **same instance** keeps it;
  - `DocumentRegistry` counts the tabs showing each buffer (`Retain`/`Release`/`ViewCount`); a moved tab is still
    one view.
- **Keys:**
  - the leader is Space (FEATURE-216A), a table in `KeySequenceParser.LeaderMappings` (`"rh"`… →
    `VimAction.MoveSplitter` with the letter as the argument);
  - `VimEngine` maps the letter to a `VimWindowCommand` (`SplitterCommandFor`) and raises `WindowCommandRequested`;
  - `WindowCommandHandler.ExecuteAsync` activates the pane the key was typed in, then acts;
  - the hex view has its own engine wired to the same `ExecuteWindowAsync`, so the keys work there too;
  - `Layout.FindNeighbor(pane, FocusDirection)` is the neighbour `gh`/`gj`/`gk`/`gl` use.
- **Help:** `HelpCatalog` must list every `VimWindowCommand` (`HelpTests.EveryWindowCommand_HasALine`).
- **Drag and drop:**
  - `EditorPaneView` already accepts files dropped anywhere on the pane (`DragDrop.SetAllowDrop`, handlers with
    `handledEventsToo` because the text area handles drags of its own); `DroppedFiles` reads them;
  - Avalonia 12.1.4: `DragDrop.DoDragDropAsync(PointerPressedEventArgs, IDataTransfer, DragDropEffects)`,
    `DataFormat.CreateInProcessFormat<T>(string)`, `DataTransferItem.Create(format, value)`;
  - headless tests drive drops with `window.DragDrop(point, RawDragEventType…, data, effects)`
    (`FileDropViewTests`).
- **Tab strip:** a `ListBox` named `Tabs` (selection bound to `ActiveTab`), each item a title and a close button; a
  middle click closes (`OnTabsPointerReleased`).

## PHASE01 — Space m keys move the tab

**Status:** TODO

### Steps

1. **Desktop workspace:**
   - `EditorPaneViewModel.Adopt(EditorTabViewModel tab)`: adds that tab instance at the end of the strip and shows it;
   - `WorkspaceViewModel.MoveTab(EditorPaneViewModel source, EditorTabViewModel tab, EditorPaneViewModel target)`:
     - nothing when the panes are the same, either is not in the workspace, or the tab is not on `source`;
     - when `target` already has a tab on the same buffer: show that tab, take the moved one off `source` and
       release it once in the registry;
     - otherwise: `source.Remove(tab)`, then `target.Adopt(tab)` (registry unchanged: still one view);
     - the target pane takes the keyboard; `source` closes when left without tabs; commands refresh;
       `DocumentShown` is raised;
     - returns whether the tab moved;
   - `WorkspaceViewModel.MoveTab(FocusDirection direction)`: the active tab to the neighbour in that direction;
     `false`, silently, with no tab or no neighbour.
2. **Core Vim:**
   - `VimWindowCommand`: `MoveTabLeft`, `MoveTabDown`, `MoveTabUp`, `MoveTabRight`;
   - `VimAction.MoveTab`, documented like `MoveSplitter`;
   - `KeySequenceParser.LeaderMappings`: `"mh"`, `"mj"`, `"mk"`, `"ml"` → `MoveTab` with the letter; the class
     remarks mention them;
   - `VimEngine`: `MoveTab` maps `h`/`j`/`k`/`l` to the four commands and raises `WindowCommandRequested`.
3. **Desktop handler:** `WindowCommandHandler` sends the four commands to `MoveTab(FocusDirection…)`.
4. **Help:** a Panes row `Space mh, mj, mk, ml` — "Move the tab to the pane to the left, below, above, to the right".
5. **README:** the *Tabs and panes* paragraph names the keys.
6. **Tests:**
   - core parser: `<Space>mh/mj/mk/ml` complete `MoveTab` with their letter, in normal and visual mode; `<Space>m`
     is pending; `<Space>mx` is dead;
   - core engine: each key raises its window command and leaves the text alone; `.` does not repeat it;
   - desktop workspace: the tab instance moves with its caret; the target gets the keyboard; a source left empty
     closes; a buffer already in the target is shown there, not duplicated, and its view count drops by one; no
     neighbour → nothing changes; same pane → nothing changes;
   - desktop window (typed in the real window): `<Space>ml` in the left pane moves its tab to the right pane.

### Acceptance criteria

- `<Space>mh/mj/mk/ml` move the active tab to the pane on that side, which takes the keyboard and shows it with its
  caret where it was.
- The pane the last tab left closes; with no pane on that side nothing happens.
- A buffer the target already shows is not opened twice there.
- The help and the README list the keys. Zero warnings, whole suite green.

## PHASE02 — Drag a tab onto another pane

**Status:** TODO

### Steps

1. **Desktop pane:** `EditorPaneViewModel.TakeTab(EditorTabViewModel tab)` → the host's `MoveTab(source, tab, this)`,
   the source found by the host (`IPaneHost.MoveTabTo`, or the workspace finding which pane holds the tab).
2. **View helper** `Views/DraggedTab.cs`, beside `DroppedFiles`:
   - the in-process `DataFormat<EditorTabViewModel>`;
   - `Carrying(tab)` builds the `DataTransfer`; `Read(IDataTransfer)` gets the tab or `null`.
3. **Drag source** (`EditorPaneView`): on the `Tabs` strip,
   - a left press on a tab (not on its close button) is remembered with its `PointerPressedEventArgs`;
   - once the pointer has moved past a small threshold with the button still down, `DragDrop.DoDragDropAsync(press,
     DraggedTab.Carrying(tab), DragDropEffects.Move)`;
   - release, capture loss or the drag's end forget the press.
4. **Drop target** (`EditorPaneView`'s existing handlers):
   - a dragged tab over **another** pane gets `Move`, over its own pane `None`; files keep their `Copy`;
   - dropped on another pane, the tab moves there (posted, as the file drop is);
   - a tab from another instance carries nothing this process can read and is ignored.
5. **Help:** a row "Drag a tab onto another pane" — "Move it there".
6. **README:** the *Tabs and panes* paragraph mentions dragging.
7. **Tests** (headless, real window):
   - hovering a dragged tab over the other pane offers `Move`, over its own pane `None`; files still offer `Copy`;
   - dropping it on the other pane moves the tab and gives that pane the keyboard; the pane it left closes when
     empty;
   - dropping it on its own pane changes nothing;
   - starting the drag from the strip, if the headless platform supports a drag source; otherwise recorded under
     *Deviations*.

### Acceptance criteria

- Dragging a tab onto another pane — strip, text, hex view or placeholder — moves it there, as `<Space>m` does.
- Over its own pane the cursor says no drop and nothing changes; dropped files still open as before.
- Clicking, closing (×, middle click) and selecting tabs behave as before.
- Zero warnings, whole suite green.

## Out of scope

- Reordering tabs inside a pane, or dropping at a position in the target's strip (the tab goes at the end).
- Creating a new split when no pane is on that side, or dropping on a pane's edge to split it.
- Dragging a tab out of the window, or into another instance.
- Menu entries for moving tabs; a drop-target highlight beyond the cursor.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Which tab the keys move, where | The active tab, to the neighbour `FindNeighbor` gives for that side | Same pane `gh`/`gj`/`gk`/`gl` reach, so `Space ml` lands where `gl` goes | Next/previous pane in reading order |
| Letters | `h` left, `j` down, `k` up, `l` right | Vim's motions and `gh`…`gl`; the draft names `<leader>m h/j/k/l` | Bar-like inverted semantics (only `rj`/`rk` move a bar) |
| No pane on that side | Nothing, silently | `gh` at the edge is silent; "another split group" means an existing one | Splitting to make one (VS Code) — layout work for left/up and a different feature |
| Keyboard after the move | Follows the tab | The user keeps working on the file they moved; VS Code does the same | Staying in the source pane |
| Source left without tabs | Closes | The workspace's rule: closing tabs is how panes go away | Leaving an empty pane |
| Target already shows the buffer | Show its tab; the moved one merges | One buffer is never two tabs of one pane (`Show` already enforces it) | A second tab on the same buffer |
| View state | The tab instance moves | Caret, scroll and hex row live on the tab | A new tab (state lost) |
| Drop zone and place | Anywhere on another pane; appended at the end | Mirrors the file drop; position drops imply reordering, not asked | Strip-only, insertion by position |
| Drag feedback | The cursor (Move/None) | Same as the file drop | A highlighted frame — later if wanted |
| Count, `.`, visual mode | Count ignored, `.` never repeats, works over a selection | Same as every leader and pane key | — |
| Menus | No entries | Other pane keys (`gh`, `Space r…`) are not in menus either; help and README list them | A View → Move Tab submenu |
| Shape | Two phases: keys with the workspace move, then drag and drop | Each is usable alone and reviewable in ~30 min | One phase (too large); drag first (needs the same move) |

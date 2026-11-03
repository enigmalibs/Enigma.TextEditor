# FEATURE-4107 — New tab on a tab strip double-click

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-4107-new-tab-double-click`
**Run:** feature/2026-11-02-tab-strip-file-explorer

## Objective

Double-clicking the empty part of a pane's tab strip opens a new untitled buffer in a new tab of that pane, as
**File → New** does in the active pane. A double-click that lands on a tab, its close button or the strip's scrollbar
opens nothing.

## Context & constraints

- **The strip** (`Views/EditorPaneView.axaml`):
  - `Border x:Name="Strip"` (`MinHeight="37"`, Surface background), shown only while the pane has a tab
    (`HasActiveTab`). It holds `ListBox x:Name="Tabs"`, `VerticalAlignment="Top"`, a horizontal `StackPanel` of
    `ListBoxItem`s, and a thin horizontal scrollbar (`ScrollViewer.HorizontalScrollBarVisibility="Auto"`);
  - the empty part is the list's own area to the right of the last tab, plus the strip below the list;
  - `EditorPaneView.axaml.cs` already listens on `Tabs` for pressed, moved and released, with `handledEventsToo`, for
    the tab drag and the middle-click close. A double-tap does not start a drag, which needs a move past the tap size.
- **Avalonia's `DoubleTapped`** is raised on the press's source when the click count is even, whether or not the
  press was handled, and it bubbles.
- **The workspace** (`ViewModels/WorkspaceViewModel.cs`):
  - `NewDocument()` shows a new untitled buffer in the **active** pane (`Show(_documents.CreateUntitled())`);
  - `Activate(EditorPaneViewModel pane)` gives a pane the keyboard; `SplitHost` puts the focus in the active pane's
    editor.
- **Host seam:** `IPaneHost`, implemented by `WorkspaceViewModel` and by `EditorPaneViewModelTests.RecordingHost`.
- **Help:** `HelpCatalog` *Files & tabs* (`Ex("Ctrl+N", "New untitled buffer in a new tab", VimExCommand.EditNew)`).
  **README:** the *Tabs and panes* paragraph.
- **Tests:** `ViewModels/WorkspaceViewModelTests`, `ViewModels/EditorPaneViewModelTests`; real window: `Views/TabDragViewTests`
  (headless mouse helpers).

## Decisions

- **Where:** the strip's empty area only — not a tab (title, dot or close button), not the scrollbar.
- **What:** a new untitled buffer in a new tab at the end of *that* pane's strip, which becomes its active tab. The
  pane takes the keyboard, its editor the focus.
- **A pane with no tab** shows no strip, so has nothing to double-click: out of scope.
- **Layers:**
  - `WorkspaceViewModel.NewDocument(EditorPaneViewModel pane)`: activates the pane, then `NewDocument()`;
  - `IPaneHost.NewDocument(EditorPaneViewModel pane)`, reached through `EditorPaneViewModel.NewTab()`;
  - the view decides from the event's source whether the double-click hit the empty strip.

## Steps

1. **Workspace:** `NewDocument(EditorPaneViewModel pane)` — a pane not in the workspace changes nothing and returns
   `null`; otherwise `Activate(pane)` then `NewDocument()`, returning the new tab.
2. **Host seam:** `IPaneHost.NewDocument(pane)`, documented like the other members; `EditorPaneViewModel.NewTab()`;
   `RecordingHost` follows.
3. **View** (`EditorPaneView.axaml.cs`): a `DoubleTapped` handler on `Strip` (bubble, `handledEventsToo`). When its
   source has no `ListBoxItem`, `Button` or `ScrollBar` ancestor (self included), it marks the event handled and calls
   `_pane?.NewTab()`.
4. **Help:** a *Files & tabs* row `Keys("Double-click the tab strip", "New untitled buffer in a new tab of that pane")`.
5. **README:** the *Tabs and panes* paragraph says a double-click on the empty part of a strip opens a new tab there.
6. **Tests:**
   - workspace: a new document in a pane that is not the active one lands there, and that pane becomes active; a pane
     not in the workspace is refused;
   - pane: `NewTab()` asks its host;
   - real window: a double-click right of the last tab adds one untitled tab to that pane, active and with the
     keyboard; one on a tab adds none; with two panes, a double-click on the inactive pane's strip adds the tab there
     and activates that pane.

## Acceptance criteria

- A double-click on the empty part of a pane's strip opens a new untitled tab at the end of that pane, which takes the
  keyboard.
- A double-click on a tab, its close button or the scrollbar opens no tab.
- The help and the README say so. Zero warnings, whole suite green.

## Out of scope

- A double-click on the "No file is open" placeholder of a pane with no tab.
- A new tab button on the strip.
- Release notes (written by the next release item).

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Type | `FEATURE` | New behaviour on a working strip | `BUG` |
| Which pane gets the tab | The pane whose strip was double-clicked | The gesture points at a strip; the new tab belongs where the user looked | The active pane |
| Where in the strip | Appended at the end | Matches File → New and Ctrl+N | Next to the active tab |
| What counts as a tab hit | The `ListBoxItem` (title, dot, close button) | The draft: "not if the double-click hits a tab" | Only the title |
| Scrollbar double-click | Ignored | A fast click on the thin scrollbar is scrolling, not a request for a tab | Opening a tab |
| Layering | Workspace method + host seam + view handler | Same shape as the drop and drag features; the model is testable without a window | View calling the workspace directly |
| Release notes | Left to the next release item | Release items write them | Editing `RELEASENOTES.md` now |

# FEATURE-316E-PHASE02 — Line-ending & indentation tools

**Item:** FEATURE-316E — Encoding, EOL & indent tools
**Branch:** `feature/feature-316e-phase02-eol-indent-tools`
**Run:** vibe/2026-10-15-text-editor-vim

## Summary

The last two tools, and the status bar the draft lists, are complete.

- **Tools → Line Endings ▸ LF / CRLF** (radio, reflecting the active buffer). Switching changes the
  buffer's format and marks it unsaved; the next save writes every line with the new terminator. It
  uses PHASE01's format-change path.
- **Tools → Indentation ▸ Spaces: 2 / Spaces: 4 / Tabs** (radio):
  - sets the buffer's indentation (tabs shown 4 wide);
  - every pane showing the buffer updates its editor options (`ConvertTabsToSpaces`,
    `IndentationSize`) and re-seeds Vim's `shiftwidth`/`expandtab`, so Tab and `>>` follow;
  - existing text is not converted, and the buffer is not marked unsaved.
- **Status bar**, left to right, as the draft lists it:

  | Item | Example | Clickable? |
  |---|---|---|
  | Vim mode | NORMAL | no |
  | Ln, Col | Ln 1, Col 1 | no |
  | Spaces | `Spaces: 2` / `Tab Size: 4` | flyout with the indentation radios |
  | Encoding | UTF-8 | flyout with the two encoding tools |
  | Language | Python | opens the picker |
  | Line endings | `LF` / `CRLF` | flyout with the line-ending radios |

## Files / modules touched

**Modified**

- `ViewModels/ToolsViewModel.cs`:
  - `UseLf`/`UseCrLf` and the three indentation commands, with `IsLf`, `IsCrLf`, `IsTwoSpaces`,
    `IsFourSpaces` and `IsTabs`;
  - follows the active buffer's format and indentation changes.
- `ViewModels/StatusBarViewModel.cs`: `IndentationText`, `LineEndingText`.
- `Views/MainWindow.axaml`: the two Tools submenus; the indentation and line-ending status buttons
  with their flyouts.

**Created**

- `tests/.../Desktop.UnitTests/ViewModels/LineEndingAndIndentationToolsTests.cs` (6).

## Deviations & follow-ups

- **Nothing else needed in the model.** `DocumentViewModel.ChangeLineEndings` landed in PHASE01, and
  the pane already applied indentation changes to the editor and to Vim (FEATURE-3EE8 PHASE01 and
  PHASE03).
- **Tabs always switch to a tab width of 4,** the conventional width, as planned. A tab-width setting
  of its own is a follow-up if wanted.
- **Fix cycles used: 0.**

## Decisions taken at build time

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How the radio menus are built | `ToggleType="Radio"` items with one-way `IsChecked` bindings and a command each | The checked state reads the buffer; clicking runs the command | Two-way bindings onto setter properties (a menu click could desync the model) |
| Where the indentation and line-ending status items sit | Spaces after Ln/Col, EOL last | The order the draft lists | Grouping all format items together |

## Build / test evidence

- `dotnet build Enigma.TextEditor.slnx`: **0 warnings, 0 errors**.
- `dotnet test --solution Enigma.TextEditor.slnx`: **2307 passed**, 0 failed, 0 skipped. That
  includes:
  - a CRLF switch writes CRLF and the buffer becomes clean;
  - an indentation change does not dirty the buffer and moves the radio marks;
  - headless, Tabs: `>>` typed into the editor inserts `\t`;
  - headless, Spaces: 2 in a split: both panes' editors take size 2 and `>>` inserts two spaces;
  - every draft item in the status bar, following changes (NORMAL · Ln · `Spaces: 2` · UTF-8 · Python
    · CRLF → LF / `Tab Size: 4`);
  - the Tools radio submenus.

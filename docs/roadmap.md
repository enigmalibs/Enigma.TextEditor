# Roadmap

Registry of every tracked work item in Enigma.TextEditor. Full details live in `docs/plan/<ID>.md`;
completion records live in `docs/done/<ID>.md`.

| ID           | Title                              | Status      | Plan                      |
|--------------|------------------------------------|-------------|---------------------------|
| FEATURE-55D7 | Text editor foundation             | DONE        | docs/plan/FEATURE-55D7.md |
| - PHASE01    | Solution scaffold & app shell      | DONE        | (in FEATURE-55D7.md)      |
| - PHASE02    | Vim engine port                    | DONE        | (in FEATURE-55D7.md)      |
| - PHASE03    | Text file I/O                      | DONE        | (in FEATURE-55D7.md)      |
| FEATURE-3EE8 | Editor panes, tabs & files         | DONE        | docs/plan/FEATURE-3EE8.md |
| - PHASE01    | Editor pane & tabs                 | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE02    | Unsaved changes & startup args     | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE03    | Vim integration & ex requests      | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE04    | Syntax highlighting & language     | DONE        | (in FEATURE-3EE8.md)      |
| FEATURE-04BC | Multi-split panes                  | DONE        | docs/plan/FEATURE-04BC.md |
| - PHASE01    | Layout tree & split host           | DONE        | (in FEATURE-04BC.md)      |
| - PHASE02    | Vim window commands & shared state | DONE        | (in FEATURE-04BC.md)      |
| FEATURE-5731 | Vim search highlight               | DONE        | docs/plan/FEATURE-5731.md |
| FEATURE-316E | Encoding, EOL & indent tools       | DONE        | docs/plan/FEATURE-316E.md |
| - PHASE01    | Encoding tools                     | DONE        | (in FEATURE-316E.md)      |
| - PHASE02    | Line-ending & indentation tools    | DONE        | (in FEATURE-316E.md)      |
| FEATURE-3AE6 | Read-only hex viewer               | DONE        | docs/plan/FEATURE-3AE6.md |
| - PHASE01    | Hex view                           | DONE        | (in FEATURE-3AE6.md)      |
| - PHASE02    | Binary prompt & reopen as hex      | DONE        | (in FEATURE-3AE6.md)      |
| FEATURE-6BB4 | Settings & Help menus              | DONE        | docs/plan/FEATURE-6BB4.md |
| FEATURE-37E4 | Open files by drag and drop        | DONE        | docs/plan/FEATURE-37E4.md |
| FEATURE-08A7 | Application icon                   | DONE        | docs/plan/FEATURE-08A7.md |
| FEATURE-12AF | Help dialog & ex abbreviations     | DONE        | docs/plan/FEATURE-12AF.md |
| - PHASE01    | Ex command abbreviations           | DONE        | (in FEATURE-12AF.md)      |
| - PHASE02    | Key bindings help dialog           | DONE        | (in FEATURE-12AF.md)      |
| BUG-52A2     | Empty pane after closing a split   | DONE        | docs/plan/BUG-52A2.md     |
| BUG-1529     | False highlighting error on close  | DONE        | docs/plan/BUG-1529.md     |
| BUG-0D06     | Status bar button text too high    | DONE        | docs/plan/BUG-0D06.md     |
| FEATURE-5F2B | Pane focus with gh, gj, gk, gl     | DONE        | docs/plan/FEATURE-5F2B.md |
| FEATURE-4EAA | :enew and :enew!                   | DONE        | docs/plan/FEATURE-4EAA.md |
| FEATURE-6500 | :e opens the file dialog           | DONE        | docs/plan/FEATURE-6500.md |
| FEATURE-4A9A | Split opens a new document         | DONE        | docs/plan/FEATURE-4A9A.md |
| FEATURE-5D83 | Editor context menu                | DONE        | docs/plan/FEATURE-5D83.md |
| FEATURE-21A7 | Vim shortcuts in menus             | DONE        | docs/plan/FEATURE-21A7.md |
| FEATURE-8F24 | Release 1.0.0                      | DONE        | docs/plan/FEATURE-8F24.md |
| - PHASE01    | README & SECURITY.md               | DONE        | (in FEATURE-8F24.md)      |
| - PHASE02    | Version 1.0.0, notes & runbook     | DONE        | (in FEATURE-8F24.md)      |
| FEATURE-7F58 | Linux installer                    | DONE        | docs/plan/FEATURE-7F58.md |
| FEATURE-5301 | Release 1.1.0                      | DONE        | docs/plan/FEATURE-5301.md |
| FEATURE-4217 | Dropped file replaces a new tab    | DONE        | docs/plan/FEATURE-4217.md |
| FEATURE-3546 | Editor minimap                     | DONE        | docs/plan/FEATURE-3546.md |
| - PHASE01    | Minimap beside the editor          | DONE        | (in FEATURE-3546.md)      |
| - PHASE02    | Minimap or scrollbar setting       | DONE        | (in FEATURE-3546.md)      |
| BUG-4A17     | Colours missing after a scroll     | IN PROGRESS | docs/plan/BUG-4A17.md     |
| FEATURE-2F9C | :set for the status bar items      | IN PROGRESS | docs/plan/FEATURE-2F9C.md |
| - PHASE01    | Indentation options                | DONE        | (in FEATURE-2F9C.md)      |
| - PHASE02    | ff, fenc, bomb and ft              | TODO        | (in FEATURE-2F9C.md)      |
| FEATURE-684E | :q on the last tab keeps the app   | TODO        | docs/plan/FEATURE-684E.md |
| FEATURE-2860 | Quick pane and tab keys            | TODO        | docs/plan/FEATURE-2860.md |
| - PHASE01    | g=, g+, g-, g>, g<, gt, gT         | TODO        | (in FEATURE-2860.md)      |
| - PHASE02    | Configurable resize step           | TODO        | (in FEATURE-2860.md)      |
| FEATURE-1FBE | Dialog keyboard focus              | TODO        | docs/plan/FEATURE-1FBE.md |
| FEATURE-7D6B | Status bar gives focus back        | TODO        | docs/plan/FEATURE-7D6B.md |
| FEATURE-5D8B | About dialog like GitClient        | TODO        | docs/plan/FEATURE-5D8B.md |

> `BUG-4A17` quarantined by `/vibe` run `feature/2026-10-21-minimap-keys-dialogs` — stand-in test still finds 14 plain lines after 3 fix cycles; branch `bugfix/bug-4a17-scroll-colours` unmerged. Finish with `/build BUG-4A17`.

# Roadmap

Registry of every tracked work item in Enigma.TextEditor. Full details live in `docs/plan/<ID>.md`;
completion records live in `docs/done/<ID>.md`.

| ID           | Title                                | Status      | Plan                      |
|--------------|--------------------------------------|-------------|---------------------------|
| FEATURE-55D7 | Text editor foundation               | DONE        | docs/plan/FEATURE-55D7.md |
| - PHASE01    | Solution scaffold & app shell        | DONE        | (in FEATURE-55D7.md)      |
| - PHASE02    | Vim engine port                      | DONE        | (in FEATURE-55D7.md)      |
| - PHASE03    | Text file I/O                        | DONE        | (in FEATURE-55D7.md)      |
| FEATURE-3EE8 | Editor panes, tabs & files           | DONE        | docs/plan/FEATURE-3EE8.md |
| - PHASE01    | Editor pane & tabs                   | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE02    | Unsaved changes & startup args       | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE03    | Vim integration & ex requests        | DONE        | (in FEATURE-3EE8.md)      |
| - PHASE04    | Syntax highlighting & language       | DONE        | (in FEATURE-3EE8.md)      |
| FEATURE-04BC | Multi-split panes                    | DONE        | docs/plan/FEATURE-04BC.md |
| - PHASE01    | Layout tree & split host             | DONE        | (in FEATURE-04BC.md)      |
| - PHASE02    | Vim window commands & shared state   | DONE        | (in FEATURE-04BC.md)      |
| FEATURE-5731 | Vim search highlight                 | DONE        | docs/plan/FEATURE-5731.md |
| FEATURE-316E | Encoding, EOL & indent tools         | DONE        | docs/plan/FEATURE-316E.md |
| - PHASE01    | Encoding tools                       | DONE        | (in FEATURE-316E.md)      |
| - PHASE02    | Line-ending & indentation tools      | DONE        | (in FEATURE-316E.md)      |
| FEATURE-3AE6 | Read-only hex viewer                 | DONE        | docs/plan/FEATURE-3AE6.md |
| - PHASE01    | Hex view                             | DONE        | (in FEATURE-3AE6.md)      |
| - PHASE02    | Binary prompt & reopen as hex        | DONE        | (in FEATURE-3AE6.md)      |
| FEATURE-6BB4 | Settings & Help menus                | DONE        | docs/plan/FEATURE-6BB4.md |
| FEATURE-37E4 | Open files by drag and drop          | DONE        | docs/plan/FEATURE-37E4.md |
| FEATURE-08A7 | Application icon                     | DONE        | docs/plan/FEATURE-08A7.md |
| FEATURE-12AF | Help dialog & ex abbreviations       | DONE        | docs/plan/FEATURE-12AF.md |
| - PHASE01    | Ex command abbreviations             | DONE        | (in FEATURE-12AF.md)      |
| - PHASE02    | Key bindings help dialog             | DONE        | (in FEATURE-12AF.md)      |
| BUG-52A2     | Empty pane after closing a split     | DONE        | docs/plan/BUG-52A2.md     |
| BUG-1529     | False highlighting error on close    | DONE        | docs/plan/BUG-1529.md     |
| BUG-0D06     | Status bar button text too high      | DONE        | docs/plan/BUG-0D06.md     |
| FEATURE-5F2B | Pane focus with gh, gj, gk, gl       | DONE        | docs/plan/FEATURE-5F2B.md |
| FEATURE-4EAA | :enew and :enew!                     | DONE        | docs/plan/FEATURE-4EAA.md |
| FEATURE-6500 | :e opens the file dialog             | DONE        | docs/plan/FEATURE-6500.md |
| FEATURE-4A9A | Split opens a new document           | DONE        | docs/plan/FEATURE-4A9A.md |
| FEATURE-5D83 | Editor context menu                  | DONE        | docs/plan/FEATURE-5D83.md |
| FEATURE-21A7 | Vim shortcuts in menus               | DONE        | docs/plan/FEATURE-21A7.md |
| FEATURE-8F24 | Release 1.0.0                        | DONE        | docs/plan/FEATURE-8F24.md |
| - PHASE01    | README & SECURITY.md                 | DONE        | (in FEATURE-8F24.md)      |
| - PHASE02    | Version 1.0.0, notes & runbook       | DONE        | (in FEATURE-8F24.md)      |
| FEATURE-7F58 | Linux installer                      | DONE        | docs/plan/FEATURE-7F58.md |
| FEATURE-5301 | Release 1.1.0                        | DONE        | docs/plan/FEATURE-5301.md |
| FEATURE-4217 | Dropped file replaces a new tab      | DONE        | docs/plan/FEATURE-4217.md |
| FEATURE-3546 | Editor minimap                       | DONE        | docs/plan/FEATURE-3546.md |
| - PHASE01    | Minimap beside the editor            | DONE        | (in FEATURE-3546.md)      |
| - PHASE02    | Minimap or scrollbar setting         | DONE        | (in FEATURE-3546.md)      |
| BUG-4A17     | Colours missing after a scroll       | IN PROGRESS | docs/plan/BUG-4A17.md     |
| FEATURE-2F9C | :set for the status bar items        | DONE        | docs/plan/FEATURE-2F9C.md |
| - PHASE01    | Indentation options                  | DONE        | (in FEATURE-2F9C.md)      |
| - PHASE02    | ff, fenc, bomb and ft                | DONE        | (in FEATURE-2F9C.md)      |
| FEATURE-684E | :q on the last tab keeps the app     | DONE        | docs/plan/FEATURE-684E.md |
| FEATURE-2860 | Quick pane and tab keys              | DONE        | docs/plan/FEATURE-2860.md |
| - PHASE01    | g=, g+, g-, g>, g<, gt, gT           | DONE        | (in FEATURE-2860.md)      |
| - PHASE02    | Configurable resize step             | DONE        | (in FEATURE-2860.md)      |
| FEATURE-1FBE | Dialog keyboard focus                | DONE        | docs/plan/FEATURE-1FBE.md |
| FEATURE-7D6B | Status bar gives focus back          | DONE        | docs/plan/FEATURE-7D6B.md |
| FEATURE-5D8B | About dialog like GitClient          | DONE        | docs/plan/FEATURE-5D8B.md |
| FEATURE-2B84 | Release 1.2.0                        | DONE        | docs/plan/FEATURE-2B84.md |
| FEATURE-216A | Space leader: move the pane splitter | DONE        | docs/plan/FEATURE-216A.md |
| FEATURE-3F16 | Release 1.3.0                        | DONE        | docs/plan/FEATURE-3F16.md |
| BUG-09C2     | Space rj/rk move the bar inverted    | DONE        | docs/plan/BUG-09C2.md     |
| FEATURE-6622 | Release 1.4.0                        | DONE        | docs/plan/FEATURE-6622.md |
| BUG-7624     | Text hidden under minimap/scrollbar  | DONE        | docs/plan/BUG-7624.md     |
| FEATURE-003A | Preferences dialog & fonts           | DONE        | docs/plan/FEATURE-003A.md |
| - PHASE01    | Preferences dialog                   | DONE        | (in FEATURE-003A.md)      |
| - PHASE02    | Editor font family & size            | DONE        | (in FEATURE-003A.md)      |
| FEATURE-2A5C | Release 1.5.0                        | DONE        | docs/plan/FEATURE-2A5C.md |
| FEATURE-856D | Icon rail & settings page            | DONE        | docs/plan/FEATURE-856D.md |
| - PHASE01    | Icon rail and settings page          | DONE        | (in FEATURE-856D.md)      |
| - PHASE02    | Files page follows the editor        | DONE        | (in FEATURE-856D.md)      |
| FEATURE-0242 | New settings defaults                | DONE        | docs/plan/FEATURE-0242.md |
| FEATURE-0D4A | First-run setup dialog               | DONE        | docs/plan/FEATURE-0D4A.md |
| FEATURE-4362 | Release 1.6.0                        | DONE        | docs/plan/FEATURE-4362.md |
| BUG-78F9     | Readable colours in the dark theme   | DONE        | docs/plan/BUG-78F9.md     |
| - PHASE01    | Link colours                         | DONE        | (in BUG-78F9.md)          |
| - PHASE02    | Settings hints and hex row           | DONE        | (in BUG-78F9.md)          |
| FEATURE-AEBF | Release 1.7.0                        | DONE        | docs/plan/FEATURE-AEBF.md |
| FEATURE-9A03 | New application icon                 | DONE        | docs/plan/FEATURE-9A03.md |
| FEATURE-3C6D | Release 1.8.0                        | DONE        | docs/plan/FEATURE-3C6D.md |
| FEATURE-4092 | App icon in the output directory     | DONE        | docs/plan/FEATURE-4092.md |
| FEATURE-452D | Catch unhandled exceptions           | DONE        | docs/plan/FEATURE-452D.md |
| - PHASE01    | Error policy and process hooks       | DONE        | (in FEATURE-452D.md)      |
| - PHASE02    | Error dialog and wiring              | DONE        | (in FEATURE-452D.md)      |
| - PHASE03    | Fire-and-forget tasks reach the net  | DONE        | (in FEATURE-452D.md)      |
| FEATURE-84C3 | Windows and Linux publish scripts    | DONE        | docs/plan/FEATURE-84C3.md |
| FEATURE-8EDF | Release 1.9.0                        | DONE        | docs/plan/FEATURE-8EDF.md |
| FEATURE-9F56 | Move tabs between panes              | DONE        | docs/plan/FEATURE-9F56.md |
| - PHASE01    | Space m keys move the tab            | DONE        | (in FEATURE-9F56.md)      |
| - PHASE02    | Drag a tab onto another pane         | DONE        | (in FEATURE-9F56.md)      |
| FEATURE-7B55 | Open files in the running instance   | DONE        | docs/plan/FEATURE-7B55.md |
| - PHASE01    | Instance pipe and pointer file       | DONE        | (in FEATURE-7B55.md)      |
| - PHASE02    | Forward launches to the open window  | DONE        | (in FEATURE-7B55.md)      |
| BUG-5A45     | Tab drag shows the refusal cursor    | DONE        | docs/plan/BUG-5A45.md     |
| - PHASE01    | A tab drag the pane runs itself      | DONE        | (in BUG-5A45.md)          |
| - PHASE02    | Escape cancels a tab drag            | DONE        | (in BUG-5A45.md)          |
| - PHASE03    | Highlight the pane a tab drops on    | DONE        | (in BUG-5A45.md)          |
| FEATURE-2448 | Release 1.10.0                       | DONE        | docs/plan/FEATURE-2448.md |
| FEATURE-9E4A | Drop a tab on an edge to split       | DONE        | docs/plan/FEATURE-9E4A.md |
| - PHASE01    | Split a pane with a moved tab        | DONE        | (in FEATURE-9E4A.md)      |
| - PHASE02    | Edge drop zones and their hint       | DONE        | (in FEATURE-9E4A.md)      |
| FEATURE-1CC6 | Release 1.11.0                       | DONE        | docs/plan/FEATURE-1CC6.md |
| FEATURE-A198 | Font size picker in the status bar   | DONE        | docs/plan/FEATURE-A198.md |
| FEATURE-B751 | Drop files on a pane edge to split   | DONE        | docs/plan/FEATURE-B751.md |
| - PHASE01    | Split a pane for dropped files       | DONE        | (in FEATURE-B751.md)      |
| - PHASE02    | File drop zones and their hint       | DONE        | (in FEATURE-B751.md)      |

> `BUG-4A17` quarantined by `/vibe` run `feature/2026-10-21-minimap-keys-dialogs` — stand-in test still finds 14 plain lines after 3 fix cycles; branch `bugfix/bug-4a17-scroll-colours` unmerged. Finish with `/build BUG-4A17`.

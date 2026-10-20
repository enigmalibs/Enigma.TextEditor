# FEATURE-8F24-PHASE01 — README & SECURITY.md

**Item:** FEATURE-8F24 — Release 1.0.0
**Branch:** `feature/feature-8f24-phase01-readme-security`
**Run:** feature/2026-10-20-release-1-0-0

## Summary

The repository now says what the application is, and how to report a vulnerability in it.

- `README.md` was a single title line. It is now the application README, in Enigma.Launchpad's shape:
  - an intro naming the stack (Avalonia 12, AvaloniaEdit, Enigma.Avalonia.Desktop, Enigma.Icons.Avalonia);
  - the features, in prose:
    - tabs and panes;
    - Vim: the command set, shared registers and search, file ex commands, *Help → Key Bindings*, Vim mode off,
      the Vim hints in the menus;
    - files: opening, encodings, line endings, indentation, unsaved changes;
    - syntax highlighting;
    - the hex viewer, with the 32 MB limit;
    - the status bar;
    - settings, zoom and the About dialog;
  - *Your data*: the per-user folder on each platform, `settings.json` and what happens to an unreadable one,
    and `logs/app.log` with its two weeks of history;
  - *Requirements*, *Build, run, test*, *Solution layout*, *License*.
- `SECURITY.md` is new, from the `dotnet-release` template:
  - GitHub private vulnerability reporting, with 1.0.x supported;
  - a scope naming what is sensitive in an editor:
    - reading any file: the binary and size checks, decoding, the hex viewer;
    - writing it back;
    - syntax highlighting over a file's text;
    - the Vim engine's patterns;
    - the settings file and the log;
  - the upstream projects to report to as well.

## Files / modules touched

**Created**

- `SECURITY.md`
- `docs/done/FEATURE-8F24-PHASE01.md`

**Modified**

- `README.md` — written in full
- `docs/roadmap.md`, `docs/plan/FEATURE-8F24.md` — statuses

## README verification

Every name the README gives was checked against the code.

| Kind                | Checked | Source                                                                       | Mismatches |
|---------------------|---------|------------------------------------------------------------------------------|------------|
| Menu items          | 15      | `MainWindow.axaml`                                                           | 0          |
| Ex commands         | 21      | `ExCommandParser.Definitions`, `VimEngine.RunEx`                             | 0          |
| Keys                | 21      | `HelpCatalog`                                                                | 0          |
| `:set` options      | 7       | `HelpCatalog` (*Options*)                                                    | 0          |
| Encodings           | 9       | `TextEncodings`, `ContentSniffer`                                            | 0          |
| Limits and defaults | 4       | `DocumentFile`, `AppSettings`, `HexFormatter`, `ServiceCollectionExtensions` | 0          |
| Files and folders   | 4       | `AppPaths`, `AppInfo`                                                        | 0          |

The names behind the counts:

- **Menu items:**
  - *File → Open…*, *Open as Hex…*;
  - *View → Zoom In*, *Zoom Out*, *Reset Zoom*;
  - *Tools → Set Language…*, *Reopen with Encoding…*, *Change File Encoding…*, *Line Endings*, *Indentation*,
    *Reopen as Hex*, *Reopen as Text*;
  - *Settings → Vim Mode*;
  - *Help → Key Bindings*, *About Enigma Text Editor*.
- **Ex commands:** `:w :q :wq :x :e :enew :bn :bp :wa :qa :q! :enew! :qa! :sp :vs :vsp :clo :help :noh :s :set`.
- **Keys:**
  - Ctrl+F4, Ctrl+Tab, Ctrl+Shift+Tab;
  - `Ctrl+W` with `v s h j k l`;
  - `gh gj gk gl`;
  - F1, Ctrl+=, Ctrl+-, Ctrl+0;
  - `dw ci( yiw >}`.
- **Limits and defaults:** 32 MB; zoom 6–72; 16-byte rows; 14 days of logs.
- **Files and folders:** `settings.json`, `settings.invalid.json`, `logs/app.log`, `Enigma.TextEditor`.

The draft was corrected four times before this check passed:

- `:w` has no `!` form, so the README names `:q!`, `:enew!` and `:qa!` only;
- equalizing panes has no ex command, so it is credited to the View menu and `Ctrl+W`;
- a file over 32 MB is offered the hex viewer alone, without *Open as text*;
- *Tools → Indentation* changes what Tab and `>>` insert, not the text already there.

## Deviations & follow-ups

- **No what's-new callout yet**, as planned. It lands in PHASE02 with the release notes it points at.
- **The README's macOS folder is hedged**, after Launchpad's README. Enigma.Configuration's documentation says
  `~/.config`, but .NET resolves `ApplicationData` on macOS by its own rules, which have changed between runtime
  versions.
- **Follow-up:** enable *Private vulnerability reporting* in the GitHub repository's *Settings → Security*.
  `SECURITY.md` relies on it.
- Line endings: both files are LF, per `.gitattributes`.

## Build/test evidence

Documentation only; no code changed. The build and suite were still run:

- `dotnet build Enigma.TextEditor.slnx`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2503 passed, 0 failed, 0 skipped.

## Decisions taken at build time

| Question               | Chosen                                                         | Why                                                       |
|------------------------|----------------------------------------------------------------|-----------------------------------------------------------|
| Feature section shape  | Bold-lead paragraphs per area, lists where one runs long       | Launchpad's README, the app precedent                     |
| Which commands to name | What a newcomer looks for, then *Help → Key Bindings*          | The dialog is the full, test-guarded list                 |
| External links         | Avalonia, AvaloniaEdit, TextMateSharp, the two Enigma packages | Not this repository; links into it stay relative          |
| `SECURITY.md` scope    | Reading, writing, highlighting, Vim patterns, settings, log    | What an editor does with input it did not write           |
| Documentation sweep    | Nothing else to update                                         | No `CLAUDE.md` or other prose doc; the README is the work |

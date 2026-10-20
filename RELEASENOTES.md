# Release notes

## 1.0.0 — 2026-10-20

The first release: a desktop text editor driven by Vim's keys. It has tabs, panes that split any way you like, and
syntax highlighting. It reads and writes each file in its own encoding, line endings and indentation, and has a
read-only hex viewer for files that are not text. It runs on Windows, Linux and macOS from the same code.

### Tabs and panes

- Each pane holds its documents as tabs. A dot marks unsaved changes.
- Close a tab with its **×**, a middle click or Ctrl+F4. Ctrl+Tab and Ctrl+Shift+Tab move between tabs.
- **Split** right or down from the **View** menu, `Ctrl+W v` / `Ctrl+W s`, or `:vs` / `:sp`. The new pane opens a
  new untitled document, or the file you name (`:sp notes.txt`). `:vne` and `:new` do the same without a file.
- Close a pane, keep only this one, make them all the same size, or move to the next one, from the View menu or
  with `Ctrl+W c`, `o`, `=` and `w`. `Ctrl+W +`, `-`, `>` and `<` resize.
- `Ctrl+W h`, `j`, `k`, `l`, or just `gh`, `gj`, `gk`, `gl`, move to the pane on that side. With several panes, the
  one with the keyboard has an accent frame.

### Vim

- The editor starts in NORMAL mode, with motions and counts, operators and text objects, visual and line-visual
  modes, `.` to repeat, registers and macros. The mode shows on the status bar.
- **Search** with `/` and `?`, `n`, `N`, `*` and `#`. Every match is highlighted as you type and after Enter, in
  every pane, until `:noh`.
- **Substitute** with `:s/pattern/text/[g][i]` over a range: `%`, `'<,'>` or line numbers.
- **`:set`** options: `ignorecase`, `smartcase`, `wrapscan`, `hlsearch`, `incsearch`, `expandtab`, `shiftwidth`.
- **Shared across panes:** the registers, the last search and the last `f`/`t`. Yank in one pane, put in another.
- **Ex commands** for files and tabs, with Vim's abbreviations (`:vsp`, `:clo`):
  - `:w [file]`, `:wq`, `:x`, `ZZ`, `:wa` to save;
  - `:q`, `:q!`, `ZQ`, `:qa`, `:qa!` to close or quit;
  - `:e` (the file dialog) and `:e file`;
  - `:enew` and `:enew!`;
  - `:bn` and `:bp`.
- **Help → Key Bindings** (F1, or `:help`) lists every command, by section.
- **Settings → Vim Mode** switches Vim off: the keys then type, as in any editor. While it is on, the menus and the
  editor's right-click menu show the Vim keys of the commands that have them.

### Files

- Open files with **File → Open…** (several at once), by dropping them on a pane, or by naming them on the command
  line. A name that does not exist yet, in a folder that does, opens an empty document the first save creates.
- **Encodings.** A byte-order mark is honoured; otherwise valid UTF-8 is read as UTF-8, and anything else as
  Windows-1252.
  - **Tools → Reopen with Encoding…** reads the file again in another encoding.
  - **Change File Encoding…** sets the one it is saved in.
  - On offer: UTF-8 with or without BOM, UTF-16 LE/BE, Windows-1252, ISO-8859-1, ISO-8859-15, Windows-1250 and
    Windows-1251.
  - A save into an encoding that cannot hold the text is refused, naming the first such character with its line
    and column. The file is left untouched.
- **Line endings.** LF or CRLF, as the file has them. **Tools → Line Endings** changes them for the next save.
- **Indentation.** Tabs or spaces, and how many, are read from the file, with the default from **Settings** when it
  does not say. **Tools → Indentation** changes what Tab and `>>` insert.
- **Unsaved changes are never lost silently.** Closing a document, or quitting, asks *Save*, *Don't save* or
  *Cancel* for each one with changes.
- The **right-click menu** has Undo, Redo, Cut, Copy, Paste, Delete and Select All.

### Syntax highlighting

- Every language TextMateSharp ships a TextMate grammar for, chosen by the file's extension.
- **Tools → Set Language…**, or the language on the status bar, picks another one, filtered as you type.
- Colours follow the theme.

### Hex viewer

- A file that looks binary opens with a question: the read-only **hex viewer** (the default), as text anyway, or
  not at all.
- A file over 32 MB, the largest the editor opens as text, is offered the viewer alone.
- **File → Open as Hex…** opens any file there directly, whatever its size. **Tools → Reopen as Hex** and
  **Reopen as Text** switch a tab between the two.
- Rows are 16 bytes, `hexdump -C` style, read from the file as you scroll. With Vim on, `j`, `k`, `gg` and `G` move
  through them, and `:q` and the pane commands work as in a text tab.

### Status bar, settings and help

- **The status bar** shows, left to right, the Vim mode, the line and column, the indentation, the encoding, the
  language and the line endings. The last four are buttons that change them.
- **Settings** apply at once and are remembered:
  - the theme: light, dark, or following the system;
  - Vim mode;
  - word wrap;
  - the indentation and line endings a new document starts with.
- **View → Zoom In / Zoom Out / Reset Zoom** (Ctrl+=, Ctrl+-, Ctrl+0) sets the font size, between 6 and 72, in
  every pane and the hex viewer.
- **Help → About Enigma Text Editor** shows the version and the commit the build was cut from.

### Your data

- Your preferences are kept in `settings.json`, in your per-user application-data folder (`%APPDATA%` on Windows,
  `~/.config` on Linux).
  - A key the file lacks keeps its default.
  - A file that cannot be read is moved aside to `settings.invalid.json`, and the editor starts with the defaults.
- A log is written beside it, in `logs/app.log`: one file per day, two weeks kept.
- The editor stores nothing else. Your documents are only ever the files you open and save.

### Compatibility

- Windows 10/11, Linux (X11) and macOS, from the same code.
- .NET 10: bundled with a self-contained build, or installed, for a framework-dependent one.
- Built on Avalonia 12.1.2 and AvaloniaEdit 12.0.0.
- Settings written by the development builds (0.1.0) load as they are.

### Version

- Initial release: **1.0.0**.

# Release notes

## 1.4.0 — 2026-10-21

A minor release. `Space rj` and `Space rk` now move the bar between two stacked panes the way `j` and `k` point in
Vim: `j` down, `k` up. In 1.3 they went the other way.

### Panes

- **`Space rj`** moves the bar between two stacked panes **down**: the top pane grows, typed in either of them.
- **`Space rk`** moves it **up**: the bottom pane grows.
- `Space rl` and `Space rh` are unchanged: they already moved the bar right and left. So are `Ctrl+W +`, `-`, `>`, `<`
  and `g+`, `g-`, `g>`, `g<`.
- **Help → Key Bindings** lists the keys as `Space rj, Space rk` — down, up.

### Upgrading from 1.3

- **`Space rj` and `Space rk` swap meanings.** If your fingers learned 1.3's way, `Space rj` now does what `Space rk`
  did, and the other way round.
- **Your settings carry over** unchanged. 1.4 adds no settings key, and going back to 1.3 reads the same file.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- No package had an update outside the Avalonia set. The set is held back at **12.1.2**, with Enigma.Avalonia.Desktop
  1.2.0 and AvaloniaEdit 12.0.0. Avalonia 12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.3.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.4.0** is a minor release under Semantic Versioning. It keeps every stored format and removes nothing. One
  behaviour changes: `Space rj` and `Space rk` swap directions.

## 1.3.0 — 2026-10-21

A minor release. Space is now a leader key, and four leader keys move the bar between two panes. They need no `+`,
`-`, `<` or `>`, so they are easy to reach on any keyboard layout.

### Panes

- **Space is the leader key** in normal and visual mode.
- **`Space rk` and `Space rj`** move the bar between two stacked panes down and up.
- **`Space rl` and `Space rh`** move the bar between two side-by-side panes right and left.
- **They move the bar, not the active pane's edge.** `Space rk` makes the top pane taller whether it is typed in the
  top pane or in the bottom one, and `Space rl` makes the left pane wider from either side.
- **Which bar, how far:**
  - with panes nested, the bar that moves is the nearest one on that axis, the one `Ctrl+W +` and `Ctrl+W >` resize;
  - it moves by **Settings → Pane Resize Step**, as the other resize keys do.
- `Ctrl+W +`, `-`, `>`, `<` and `g+`, `g-`, `g>`, `g<` are unchanged: they still grow or shrink the pane that has the
  keyboard.
- **Help → Key Bindings** lists the four keys under *Panes*.

### Upgrading from 1.2

- **Space no longer moves the caret right** in normal and visual mode. It waits for the rest of a leader key, and Esc
  or a key no leader key starts with cancels it.
  - `l`, or the right arrow, moves right.
  - Behind an operator Space is still the motion it was: `d<Space>` deletes the character under the caret, as `x`
    does.
- **Your settings carry over** unchanged. 1.3 adds no settings key, and going back to 1.2 reads the same file.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- No package had an update outside the Avalonia set. The set is held back at **12.1.2**, with Enigma.Avalonia.Desktop
  1.2.0 and AvaloniaEdit 12.0.0. Avalonia 12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.2.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.3.0** is a minor release under Semantic Versioning. It adds features, keeps every stored format, and removes
  nothing. One behaviour changes: Space in normal and visual mode.

## 1.2.0 — 2026-10-21

A minor release. A minimap now runs beside the text, and every item on the status bar can be set with `:set`, under
Vim's names. Panes get quicker keys, and how far the resize keys move a splitter is now a setting. Dialogs take the
keyboard when they open and give it back when they close, and the status bar buttons leave it in the pane.

### Minimap

- Every text tab has a **minimap** at the right of the text, in place of the vertical scrollbar. A window marks the
  part of the document on screen, and a mark in the accent colour shows the caret's line.
- Press or drag on it, and the document scrolls there. The wheel over it scrolls the text.
- **Settings → Minimap** switches every pane back to a plain scrollbar, at once, and the choice is remembered.
- A hex tab has no minimap: the hex view scrolls its own rows.

### Panes and tabs

- **Quicker pane keys:** `g` followed by the `Ctrl+W` key, as `gh`, `gj`, `gk` and `gl` already were.
  - `g=` makes the panes the same size;
  - `g+`, `g-`, `g>` and `g<` make the active pane taller, shorter, wider or narrower.
- **`gt` and `gT`** show the pane's next and previous tab, wrapping round, as Ctrl+Tab and Ctrl+Shift+Tab do.
- **Settings → Pane Resize Step** sets how far the resize keys move a splitter: 2, 5 or 10% of the split. Any step
  from 1 to 50 can be set in `settings.json`. The default, 5%, is the step the keys always had.
- **A dropped file takes the place of a new tab.** When the pane's tab is new — untitled, empty and unchanged, as
  **File → New** or a new split leaves it — the first file dropped on it opens there, rather than beside it. Any
  other dropped files open after it, as before.

### Vim

- **`:set` for the status bar.** Each of its items is now a `:set` option, under Vim's name:
  - `expandtab` (`et`), `shiftwidth` (`sw`) and the new `tabstop` (`ts`) set the file's indentation. The status bar,
    **Tools → Indentation**, the Tab key and every pane showing the file follow;
  - `fileformat` (`ff`), `unix` or `dos`, sets the line endings;
  - `fileencoding` (`fenc`) sets the encoding the next save writes: `utf-8`, `utf-16le`, `utf-16`, `latin1`,
    `iso-8859-15`, `cp1252`, `cp1250` or `cp1251`. `bomb` and `nobomb` add or remove UTF-8's byte-order mark;
  - `filetype` (`ft`) sets the language, by its name or an extension (`python`, `cs`).

  `:set ff?`, or a bare `:set ff`, shows the value. A value the editor cannot take is refused with
  `Invalid argument`, and the options before it in the same `:set` still apply. They are not available in the hex
  view.
- **`:q` on the last tab** closes the tab and keeps the application open, with the pane saying no file is open. The
  same goes for `:q!`, `:wq`, `:x`, `ZZ`, `ZQ` and `Ctrl+W q`. `:qa`, **File → Exit** and the window's close button
  quit, as before.

### Keyboard focus

- **Dialogs take the keyboard** when they open. The default button has it ("Save changes?" opens on *Save*), or the
  dialog's own filter box or list where it has one.
  - Tab and Shift+Tab move between the buttons, and Enter or Space answers with the one that has the focus.
  - Esc cancels.
  - When the dialog closes, the keyboard goes back where it was.
- **The status bar buttons** — indentation, encoding, language and line endings — leave the keyboard in the pane, so
  typing goes on without a click in the text.

### About

- **Help → About Enigma Text Editor** has Enigma.GitClient's look: the icon, the name, the version, the commit the
  build was cut from (selectable, to copy) and the copyright. *Close*, or Esc, closes it.

### Upgrading from 1.1

- **Your settings carry over.** `settings.json` gains two keys, `ShowMinimap` and `PaneResizeStep`. A 1.1 file lacks
  them, and starts with the minimap on and a 5% step.
- **Going back to 1.1** reads the file and ignores the two keys. If 1.1 then saves the settings, it leaves them out,
  and 1.2 starts them at their defaults again.
- **`:q` on the last tab no longer quits.** Quit with `:qa`, **File → Exit** or the window's close button.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- No package had an update outside the Avalonia set. The set is held back at **12.1.2**, with Enigma.Avalonia.Desktop
  1.2.0 and AvaloniaEdit 12.0.0. Avalonia 12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.1.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.2.0** is a minor release under Semantic Versioning. It adds features, keeps every stored format, and removes
  nothing. One behaviour changes: `:q` on the last tab.

## 1.1.0 — 2026-10-20

A minor release. The editor now installs on Linux with one command: it lands in your application launcher and in your
file manager's *Open With* for text files, and it comes out again just as easily. The editor itself is unchanged.

### Install on Linux

- **`packaging/linux/install.sh`** builds the editor from the repository and installs it for you. It needs no root
  and no `sudo`, and writes nothing outside your home directory:
  - the application, in `~/.local/share/enigma-text-editor`;
  - an `enigma-text-editor [file …]` command, in `~/.local/bin`;
  - a desktop entry, *Enigma Text Editor*;
  - the icon in six sizes.

  `XDG_DATA_HOME` and `XDG_BIN_HOME` are honoured.
- **The .NET runtime is bundled** by default (about 123 MB), so the editor runs whatever is installed on the machine.
  The options:
  - `--framework-dependent` builds against an installed .NET 10 runtime instead (about 44 MB). When that runtime is
    somewhere a launcher would not look, such as `~/.dotnet`, the desktop entry carries its location;
  - `--from DIR` installs a folder you published yourself;
  - `--rid` picks another runtime identifier.
- **Open With.** The desktop entry hands files to the editor, so it is offered for text files: plain text, source
  code, configuration. Each file opens in a tab. It does not make itself the default for anything.
- **Upgrade in place:** run the installer again. A new build is staged beside the old one and swapped in, so a failed
  build leaves the installed version as it was.
- **`packaging/linux/uninstall.sh`** removes those four things and nothing else. Your settings and the log, in
  `~/.config/Enigma.TextEditor`, stay as they are.

### Upgrading from 1.0

- **Nothing you saved changes.** Settings and the log stay where they are, in the same format, and going back to 1.0
  keeps everything.
- A 1.0 build you published yourself can be installed as it is, with `install.sh --from <folder>`.

### Dependencies

- No package had an update outside the Avalonia set. The set is held back at **12.1.2**, with Enigma.Avalonia.Desktop
  1.2.0 and AvaloniaEdit 12.0.0. Avalonia 12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.0.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.
- The installer is for Linux. On Windows and macOS, run a published folder as before.

### Version

- **1.1.0** is a minor release under Semantic Versioning. It adds the Linux installer, keeps every stored format, and
  removes nothing.

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

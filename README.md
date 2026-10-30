# Enigma.TextEditor

A cross-platform desktop text editor with **Vim key bindings**, built with [Avalonia 12](https://avaloniaui.net/),
[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit),
[Enigma.Avalonia.Desktop](https://www.nuget.org/packages/Enigma.Avalonia.Desktop) and
[Enigma.Icons.Avalonia](https://www.nuget.org/packages/Enigma.Icons.Avalonia).

> **What's new in 1.11** — drop a dragged tab near an edge of a pane, its own included, and that pane splits on that
> side with the tab in the new half; the half it will take is highlighted while you drag. 1.10 brought moving a tab to
> another pane with `Space mh`, `mj`, `mk`, `ml`, or by dragging it there; and, for a file opened from the system while
> the editor is running, a tab in the window you used last instead of a second window. 1.9 brought a dialog for an error that nothing else handles, with details to copy for a bug report,
> while the editor keeps running; the icon beside the executable as `app.ico`; and one script per platform to publish
> the editor. 1.8 brought a new application icon, an editor window with a green block cursor and a status-bar
> mode pill, in ten sizes on Windows and eight in the Linux icon theme. 1.7 brought colours that read in the dark theme: links
> (URLs and e-mail addresses) in a lighter blue instead of pure blue, the settings page's descriptions a step brighter,
> and the hex view's current row in the editor's selection colour. 1.6 brought a narrow rail on the left of the window, with the files at the top and the settings page
> at the bottom (Ctrl+, too, Esc to go back); a welcome dialog on the first start; and new defaults. 1.5 brought the editor's font, from the monospace faces the machine has, and
> kept the text clear of the scrollbars and the minimap. 1.4 made `Space rj` move the bar between stacked panes down and
> `Space rk` up. 1.3 made Space a leader key, with `Space rh`, `rj`, `rk`, `rl` to move the bar between two panes from
> either of them. 1.2 brought a minimap beside the text; `:set` for every item on the status bar; quicker pane keys
> (`g=`, `g+`, `gt`…) and a resize step of your choice; and dialogs that take the keyboard when they open. 1.1 brought a
> Linux installer. 1.0, the first release, brought tabs, split panes and Vim's keys; files read and written in their own
> encoding, line endings and indentation; syntax highlighting; and a read-only hex viewer. See
> [RELEASENOTES.md](RELEASENOTES.md).

**Tabs and panes.** Each pane holds its documents as tabs. A dot marks unsaved changes, and a tab closes with its **×**,
a middle click or Ctrl+F4. Ctrl+Tab and Ctrl+Shift+Tab, or `gt` and `gT`, move between tabs. Panes split any way you
like: right or down from the **View** menu, `Ctrl+W v` and `Ctrl+W s`, or `:vs` and `:sp`, each opening a new untitled
document in the new pane, or the file you name (`:sp notes.txt`). The View menu and `Ctrl+W` also close a pane, keep
only this one, make them all the same size (`g=` too), and move to the next one. With several panes, the one with the
keyboard has an accent frame, and `Ctrl+W h`, `j`, `k`, `l` (or `gh`, `gj`, `gk`, `gl`) move to the pane on that side.
`Ctrl+W +`, `-`, `>`, `<` (or `g+`, `g-`, `g>`, `g<`) make the active pane taller, shorter, wider or narrower.
Space is the leader key: `Space rl` and `Space rh` move the bar between side-by-side panes right and left, and
`Space rj` and `Space rk` move the bar between stacked panes down and up, from whichever of the two has the keyboard.
`Space mh`, `mj`, `mk` and `ml` move the tab you are on to the pane on the left, below, above or on the right, which
takes the keyboard; so does dragging a tab onto another pane's tabs or the middle of its text (`Escape` cancels the
drag). Drop a tab near an edge of a pane's text instead, that pane's own included, and the pane splits on that side
with the tab in the new half; the half it will take is highlighted while you drag. A pane left without tabs closes.

**Vim.** The editor starts in NORMAL mode, with a broad set of Vim's commands:

- motions and counts;
- operators and text objects (`dw`, `ci(`, `yiw`, `>}`);
- visual and line-visual modes;
- registers and macros;
- `.` to repeat;
- `/` and `?` searches that highlight every match as you type, and after Enter, until `:noh`;
- `:s` substitution over a range;
- `:set` options (`ignorecase`, `smartcase`, `wrapscan`, `hlsearch`, `incsearch`, `expandtab`, `shiftwidth`,
  `tabstop`); the last three are the file's indentation, as the status bar shows it. The bar's other items are
  `:set` options too, under Vim's names: `fileformat` (`unix`, `dos`), `fileencoding` (`utf-8`, `latin1`, `cp1252`…)
  with `bomb`, and `filetype` (`python`, `cs`…).

The registers, the last search and the last `f`/`t` are shared by every pane. Files are driven the Vim way too:
`:w`, `:q`, `:wq`, `:x`, `:e`, `:enew`, `:bn`, `:bp`, `:wa` and `:qa`, with `:q!`, `:enew!` and `:qa!` to throw
changes away, and Vim's abbreviations (`:vsp`, `:clo`). **Help → Key Bindings** (F1, or `:help`) lists every command.
Vim can be switched off in **Settings → Vim Mode**: the keys then type, as in any editor. While it is on, the menus
and the editor's right-click menu show the Vim keys of the commands that have them.

**Files.** Open them with **File → Open…** (several at once), by dropping them on a pane, or by naming them on the
command line. A name that does not exist yet, in a folder that does, opens an empty document that the first save
creates. A file opened from the system — a double-click, *Open with* — while the editor is running opens as a tab in
the window you used last, which comes to the front; started without a file, the editor opens a new window, so
several can run side by side. The editor reads each file the way it is written, and writes it back the same way:

- **Encoding.** A byte-order mark, else valid UTF-8, else Windows-1252. **Tools → Reopen with Encoding…** reads the
  file again in another encoding; **Change File Encoding…** sets the one it is saved in. On offer: UTF-8 with or
  without BOM, UTF-16 LE/BE, Windows-1252, ISO-8859-1, ISO-8859-15, Windows-1250 and Windows-1251. A save into an
  encoding that cannot hold the text is refused, naming the first such character with its line and column, and the
  file is left untouched.
- **Line endings**, LF or CRLF, as the file has them; **Tools → Line Endings** changes them.
- **Indentation**, tabs or spaces and how many, read from the file, else the default from **Settings**;
  **Tools → Indentation** changes what Tab and `>>` insert.

Closing a document with unsaved changes asks whether to save them, as does quitting with any.

**Syntax highlighting** comes from the TextMate grammars [TextMateSharp](https://github.com/danipen/TextMateSharp)
ships, chosen by the file's extension. **Tools → Set Language…**, or the language on the status bar, picks another
one. Colours follow the theme.

**Hex viewer.** A file that looks binary opens with a question: the read-only hex viewer (the default), as text
anyway, or not at all. A file over 32 MB, the largest the editor opens as text, is offered the viewer alone.
**File → Open as Hex…** opens any file there directly, whatever its size, and **Tools → Reopen as Hex** or
**Reopen as Text** switches a tab between the two. Rows are 16 bytes, `hexdump -C` style, read from the file as you
scroll.

**The status bar** shows, left to right:

- the Vim mode;
- the line and column;
- the indentation;
- the encoding;
- the language;
- the line endings.

The last four are buttons that change them.

**Settings** apply at once and are remembered: the theme (light, dark or following the system), Vim mode, word wrap,
the minimap beside the text (press or drag on it to scroll; off, a plain scrollbar is back), how far the resize keys
move a splitter (2, 5 or 10% of the split from the menu, any step from 1 to 50 on the settings page), and the
indentation and line endings a new document starts with. The rail on the left of the window has two icons: the files
at the top, the split editor itself, and the settings at the bottom, a page that shows all of them grouped into Text,
Appearance, Editing and New files, each with its full range; *Text* also sets the font the editors and the hex view
draw a file in, from the monospace faces the machine has. **Settings → All Settings** (Ctrl+,) opens that page too,
and Esc goes back to the files. **View → Zoom In / Zoom Out / Reset Zoom** (Ctrl+=, Ctrl+-, Ctrl+0) sets the editor's
font size, between 6 and 72, as the settings page's font size does. **Help → About Enigma Text Editor** shows the version
and the commit it was built from.

## Your data

The editor keeps three things of its own, in a per-user application-data folder:

- your preferences, in `settings.json`;
- its log;
- `instance.json`, which names the window you used last, so that a file opened from the system goes to it. That
  window removes the file when it closes.

Your documents are only ever the files you open and save.

| Platform | Folder                                                                  |
|----------|-------------------------------------------------------------------------|
| Windows  | `%APPDATA%\Enigma.TextEditor`                                           |
| Linux    | `$XDG_CONFIG_HOME/Enigma.TextEditor` (`~/.config` by default)           |
| macOS    | `Enigma.TextEditor` under the folder .NET resolves as `ApplicationData` |

- `settings.json` is readable JSON. A key it lacks keeps its default. A file that cannot be read is moved aside to
  `settings.invalid.json`, the log says so, and the editor starts with the defaults.
- `logs/app.log` is the log, one file per day, with two weeks kept.

## Requirements

- .NET SDK 10.0.100 or later to build (pinned in `global.json`, rolls forward to newer feature bands). The Linux
  installer bundles the .NET runtime with the application by default; a framework-dependent build needs the .NET 10
  runtime instead
- Windows 10/11, Linux (X11) or macOS

## Build, run, test

```bash
dotnet build Enigma.TextEditor.slnx
dotnet run --project src/Enigma.TextEditor.Desktop -- [file …]
dotnet test --solution Enigma.TextEditor.slnx
```

To run it on a machine without the SDK, publish it for that platform: `packaging/windows/publish.ps1` (or
`publish.bat`) on Windows, `packaging/linux/publish.sh` on Linux. `docs/RELEASE.md` says where the output lands, and how
to bundle the .NET runtime instead.

## Install on Linux

`packaging/linux/install.sh` builds the application from this repository and installs it for you. It needs no root and
no `sudo`, and writes nothing outside your home directory. **Enigma Text Editor** is then in your application launcher,
and in your file manager's *Open With* for text files:

```bash
./packaging/linux/install.sh
```

| What                                              | Where (`XDG_DATA_HOME` and `XDG_BIN_HOME` are honoured)            |
|---------------------------------------------------|--------------------------------------------------------------------|
| The application                                   | `~/.local/share/enigma-text-editor`                                |
| A launcher command, `enigma-text-editor [file …]` | `~/.local/bin/enigma-text-editor`                                  |
| The desktop entry                                 | `~/.local/share/applications/enigma-text-editor.desktop`           |
| The icon, in eight sizes                          | `~/.local/share/icons/hicolor/<N>x<N>/apps/enigma-text-editor.png` |

By default the .NET runtime is bundled with the application (about 123 MB), so it runs whatever is installed on the
machine. The installer's options:

- `--framework-dependent` builds against an installed .NET 10 runtime instead (about 44 MB);
- `--from DIR` installs a directory you published yourself;
- `--rid` picks another runtime identifier.

Run the installer again to upgrade in place. `./packaging/linux/uninstall.sh` removes those four things and nothing
else. Your settings and the log, in `~/.config/Enigma.TextEditor`, are left as they are.

## Solution layout

| Path                                        | What it is                                                             |
|---------------------------------------------|------------------------------------------------------------------------|
| `src/Enigma.TextEditor.Core`                | Vim engine, text files, hex model, pane layout, settings — no Avalonia |
| `src/Enigma.TextEditor.Desktop`             | The Avalonia app: composition root, services, ViewModels, Views        |
| `tests/Enigma.TextEditor.Core.UnitTests`    | xUnit v3 tests for the core library                                    |
| `tests/Enigma.TextEditor.Desktop.UnitTests` | xUnit v3 tests for the app, including headless Avalonia tests          |
| `packaging/icon`                            | The application icon's SVG sources                                     |
| `packaging/linux`                           | Linux install, uninstall and publish scripts, desktop entry, icons     |
| `packaging/windows`                         | The Windows publish scripts                                            |
| `docs/`                                     | Roadmap, plans, completion records and the release runbook             |

## License

MIT — see [LICENSE.md](LICENSE.md).

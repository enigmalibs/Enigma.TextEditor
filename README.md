# Enigma.TextEditor

A cross-platform desktop text editor with **Vim key bindings**, built with [Avalonia 12](https://avaloniaui.net/),
[AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit),
[Enigma.Avalonia.Desktop](https://www.nuget.org/packages/Enigma.Avalonia.Desktop) and
[Enigma.Icons.Avalonia](https://www.nuget.org/packages/Enigma.Icons.Avalonia).

> **What's new in 1.0** — the first release: tabs, split panes and Vim's keys; files read and written in their own
> encoding, line endings and indentation; syntax highlighting; a read-only hex viewer. See
> [RELEASENOTES.md](RELEASENOTES.md).

**Tabs and panes.** Each pane holds its documents as tabs. A dot marks unsaved changes, and a tab closes with its
**×**, a middle click or Ctrl+F4. Ctrl+Tab and Ctrl+Shift+Tab move between tabs. Panes split any way you like:
right or down from the **View** menu, `Ctrl+W v` and `Ctrl+W s`, or `:vs` and `:sp`, each opening a new untitled
document in the new pane, or the file you name (`:sp notes.txt`). The View menu and `Ctrl+W` also close a pane, keep
only this one, make them all the same size, and move to the next one. With several panes, the one with the keyboard
has an accent frame, and `Ctrl+W h`, `j`, `k`, `l` (or `gh`, `gj`, `gk`, `gl`) move to the pane on that side.

**Vim.** The editor starts in NORMAL mode, with a broad set of Vim's commands:

- motions and counts;
- operators and text objects (`dw`, `ci(`, `yiw`, `>}`);
- visual and line-visual modes;
- registers and macros;
- `.` to repeat;
- `/` and `?` searches that highlight every match as you type, and after Enter, until `:noh`;
- `:s` substitution over a range;
- `:set` options (`ignorecase`, `smartcase`, `wrapscan`, `hlsearch`, `incsearch`, `expandtab`, `shiftwidth`).

The registers, the last search and the last `f`/`t` are shared by every pane. Files are driven the Vim way too:
`:w`, `:q`, `:wq`, `:x`, `:e`, `:enew`, `:bn`, `:bp`, `:wa` and `:qa`, with `:q!`, `:enew!` and `:qa!` to throw
changes away, and Vim's abbreviations (`:vsp`, `:clo`). **Help → Key Bindings** (F1, or `:help`) lists every command.
Vim can be switched off in **Settings → Vim Mode**: the keys then type, as in any editor. While it is on, the menus
and the editor's right-click menu show the Vim keys of the commands that have them.

**Files.** Open them with **File → Open…** (several at once), by dropping them on a pane, or by naming them on the
command line. A name that does not exist yet, in a folder that does, opens an empty document that the first save
creates. The editor reads each file the way it is written, and writes it back the same way:

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
and the indentation and line endings a new document starts with. **View → Zoom In / Zoom Out / Reset Zoom** (Ctrl+=,
Ctrl+-, Ctrl+0) sets the editor's font size, between 6 and 72. **Help → About Enigma Text Editor** shows the version
and the commit it was built from.

## Your data

The editor keeps two things of its own, in a per-user application-data folder:

- your preferences, in `settings.json`;
- its log.

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

To run it on a machine without the SDK, publish it for that platform; `docs/RELEASE.md` has the command.

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
| The icon, in six sizes                            | `~/.local/share/icons/hicolor/<N>x<N>/apps/enigma-text-editor.png` |

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
| `packaging/linux`                           | The Linux installer and uninstaller, the desktop entry, the icons      |
| `docs/`                                     | Roadmap, plans, completion records and the release runbook             |

## License

MIT — see [LICENSE.md](LICENSE.md).

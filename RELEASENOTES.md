# Release notes

## 1.18.0 — 2026-11-09

A minor release that brings the previous session back. Quitting keeps the tabs, the splits and every unsaved change,
untitled documents included, and the next start brings them back as they were. With vim on, `Ctrl+Y` now redoes in
every mode, as the Edit menu says.

### The session comes back

- **Quitting keeps what was open:**
  - the tabs of each pane, in their order, with the one showing;
  - the splits and their sizes, and the pane you were in;
  - each tab's caret and scroll position. A hex view comes back as a hex view, on its row.

  The next start brings them all back. It holds however the window closes: the close button, Alt+F4 or
  **File → Exit**, `:qa`, or the system logging off.
- **Unsaved changes are kept too, and quitting no longer asks about them.**
  - A modified file comes back with its changes, marked unsaved, in the encoding, line endings and indentation it
    will be saved in. That holds even if the file was deleted in the meantime: a save writes it again.
  - An untitled document with text comes back with it, under its name.
  - The dot stays until you save or reload, since undo cannot reach back past the restart.
  - Closing a tab with unsaved changes still asks whether to save them.
  - `:qa!` throws the changes away: the files' tabs come back as the files are on disk, and an untitled document with
    text does not come back.
- **A file with no unsaved change is read again from disk,** in the encoding it was open in, with its indentation and
  the language you chose for it. Edits made outside the editor while it was closed therefore show. A file with kept
  changes comes back with the kept text, even if the file changed on disk since.
- **A file that cannot be read back is skipped,** with no dialog:
  - the status line names it once (*Not restored from the previous session: …*), and the log says why;
  - a pane left without tabs closes;
  - with nothing to bring back, the editor starts with a new untitled tab, as before.
- **Files named on the command line open after the session,** in the pane you were in.
- **One window keeps the session: the first one started.** A second window started while it runs starts empty, and
  asks about unsaved changes when it closes, as before. If the session cannot be written, quitting asks as before
  too.
- **The session is written when the window closes.** After a crash, the next start brings back what was open the last
  time the editor closed, not the changes made since.
- **`session.json` sits beside `settings.json`.** It is readable JSON and holds the text of every unsaved change.
  `session.lock` marks the window that keeps it. A file that cannot be used is moved aside to `session.invalid.json`,
  the log says so, and the editor starts without it.
- **Help → Key Bindings** describes the new `:qa` ("Quit; tabs and unsaved changes come back at the next start") and
  `:qa!` ("Quit, throwing every change away; the tabs come back").

### `Ctrl+Y` redoes with vim on

- **`Ctrl+Y` redoes in every mode,** as **Edit → Redo** and the editor's right-click Redo show it, and as
  `Ctrl+Shift+Z` does:
  - in normal mode, it is `Ctrl+R`;
  - in a visual mode, it leaves the selection, then redoes;
  - in insert mode, it redoes without leaving insert mode, as in 1.17.

  In 1.17 it scrolled the view up a line in normal and visual mode, and redid nothing.
- **A recording takes `Ctrl+Y` as the redo** and replays it as one.
- **Vim's scroll of one line up is given up.** `Ctrl+E` still scrolls a line down, and `Ctrl+U` and `PgUp` scroll
  up.
- **Help → Key Bindings** lists `Ctrl+E` alone under *Scrolling*, and `Ctrl+Z, Ctrl+Y` ("Undo, redo, as the Edit menu
  does") under *Editing*.
- **With vim off, nothing changes:** `Ctrl+Y` redid already.

### Upgrading from 1.17

- **Your settings carry over** unchanged. 1.18 adds no settings key.
- **Quitting no longer asks about unsaved changes:** they are kept for the next start. Save them, or use `:qa!`, to
  leave nothing behind.
- **`Ctrl+Y` no longer scrolls a line up** in normal and visual mode: it redoes.
- **Two files are new** in the application-data folder, `session.json` and `session.lock`. The first start of 1.18
  finds no session and starts as before.
- **Going back to 1.17 reads the same settings,** but 1.17 ignores `session.json`: save the changes it keeps before
  going back, or they will not come back.
- **On Windows, an MSI built from 1.18 upgrades the installed version** in place, 1.17.0 included.
  `packaging\windows\buildmsi.ps1` names it `Enigma.TextEditor.1.18.0.msi`. Uninstalling it leaves the session in
  `%APPDATA%\Enigma.TextEditor`, as it leaves the settings.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **xunit.v3 4.0.2 → 4.0.3**, in the test projects only.
- **The Avalonia set is held back at 12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.6 is out, but the
  set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- Nothing else has an update.

### Compatibility

- Unchanged from 1.17.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.18.0** is a minor release under Semantic Versioning. It brings the previous session back at the start, keeps
  unsaved changes when the editor quits, and gives `Ctrl+Y` the redo in every mode. The settings file and every
  default setting stay as they were.

## 1.17.0 — 2026-11-06

A minor release that makes find and replace and the mouse work with vim on. `Ctrl+F` and `Ctrl+H` open the find and
replace panel in every mode, and you can type in it in every mode. A click past the end of a line puts the block on
the line's last character, and a double-click selects a word in visual mode.

### Find and replace with vim on

- **`Ctrl+F` and `Ctrl+H` open the panel in every mode,** not only in insert mode. In 1.16, `Ctrl+F` paged down in
  normal and visual mode, and `Ctrl+H` opened a panel you could not type in. `PgDn` still pages down.
  - Opening the panel drops a half-typed command, cancels an open `:` or `/` line and leaves a visual mode. Insert
    mode stays as it is.
  - From visual mode, the panel takes the selected word as what to find.
- **The panel's fields take the keyboard in every mode.** Typing, `Backspace`, the arrows, `Ctrl+Z` and `Enter` (the
  next match) work as they do with vim off, and `Esc` closes the panel. In 1.16, typing did nothing outside insert
  mode, and `Esc` went to vim, so the panel stayed open.
- **`Esc` in the text closes the panel** in normal mode when nothing is half typed. In insert mode, in a visual mode
  or with a command half typed, the first `Esc` is vim's and a second one closes the panel.
- **In find and replace, `Tab` in the find field goes to the replace field,** and `Shift+Tab` comes back, with vim on
  or off. It used to stop on the panel's toggles and buttons first. The find-only panel keeps the usual order.
- **Help → Key Bindings** lists `Ctrl+F, Ctrl+H` under *Search & replace*, and `PgDn, Ctrl+B, PgUp` under
  *Scrolling*.

### The mouse with vim on

- **A click past the end of a line puts the block on the line's last character** in normal mode, so `x` deletes the
  character under the block. In 1.16 the block sat after the line, and `x` deleted the character to its left.
  - A click also ends a visual mode.
  - `j` and `k` after a click start from the column you clicked.
- **A double-click on a word selects it in visual mode.** The status bar shows VISUAL, `d`, `y`, `c` and the other
  operators act on the word, and `Esc` or a click ends it. In 1.16 the word was selected but the editor stayed in
  normal mode, so `d` started a delete instead of deleting the word.
  - A drag after the double-click extends the selection a word at a time.
  - A triple-click selects the line, in V-LINE mode.
- **Help → Key Bindings** lists the double-click under *Visual mode*.
- **In insert mode, and with vim off, the mouse works as before.**

### Upgrading from 1.16

- **Your settings carry over** unchanged. 1.17 adds no settings key and no file, and going back to 1.16 reads the same
  file.
- **`Ctrl+F` no longer pages down** in normal and visual mode: use `PgDn`.
- **On Windows, an MSI built from 1.17 upgrades the installed version** in place, 1.16.0 included.
  `packaging\windows\buildmsi.ps1` names it `Enigma.TextEditor.1.17.0.msi`.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change.** The Avalonia set is held back at **12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.5
  is out, but the set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- Nothing else has an update.

### Compatibility

- Unchanged from 1.16.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.17.0** is a minor release under Semantic Versioning. It makes the find and replace panel and the mouse work
  with vim on, gives `Ctrl+F` to the find panel, and keeps every stored format and every default setting.

## 1.16.0 — 2026-11-04

A minor release that fixes undo with vim on. `Ctrl+Z` in insert mode no longer opens the *Something went wrong*
dialog: it takes back what you typed and leaves you in insert mode. Every other way to undo or redo now works the same
way, from the keys, the menus and *Reopen with encoding*.

### Undo and redo with vim on

- **`Ctrl+Z` in insert mode undoes, and you stay in insert mode.** In 1.15 it showed an error, and the next `Esc`
  showed a second one and left the editor in insert mode.
  - The first `Ctrl+Z` takes back what you typed since you entered insert mode. Another one goes further back.
  - What you type after it is a step of its own: `Esc` then `u` takes back only that.
  - `.` repeats only what you typed after the undo, and a recording replays the undo as you made it.
- **`Ctrl+Y` and `Ctrl+Shift+Z` in insert mode redo**, the same way, without leaving insert mode.
- **In normal mode, `Ctrl+Z` is `u` and `Ctrl+Shift+Z` is `Ctrl+R`**, so the caret lands where `u` puts it.
  `Ctrl+Y` still scrolls the view up a line.
- **In a visual mode, `Ctrl+Z` and `Ctrl+Shift+Z` leave the selection**, then undo or redo.
- **Edit → Undo and Redo, and the editor's right-click Undo and Redo,** take the same path, and no longer fail in
  insert mode.
- **Reopen with encoding in insert mode** no longer fails: the editor goes back to normal mode before the file is read
  again.
- **Help → Key Bindings** lists `Ctrl+Z, Ctrl+Y` under *Inserting*.
- **With vim off, nothing changes.**

### Upgrading from 1.15

- **Your settings carry over** unchanged. 1.16 adds no settings key and no file, and going back to 1.15 reads the same
  file.
- **On Windows, an MSI built from 1.16 upgrades the installed version** in place, 1.15.0 included.
  `packaging\windows\buildmsi.ps1` names it `Enigma.TextEditor.1.16.0.msi`.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change.** The Avalonia set is held back at **12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.5
  is out, but the set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- Nothing else has an update.

### Compatibility

- Unchanged from 1.15.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.16.0** is a minor release under Semantic Versioning. It fixes undo and redo with vim on, gives `Ctrl+Z` and
  `Ctrl+Shift+Z` vim's undo and redo, and keeps every stored format and every default.

## 1.15.0 — 2026-11-03

A minor release that makes the icon bigger. Its editor window, still without a tile, is drawn larger so it fills its
frame, and it reads better on the taskbar and in a launcher. Nothing else changes.

### Application icon

- **The editor window fills its frame.** It is the same drawing as 1.14's, on the same transparent background and in
  the same blues:
  - the gutter of line marks;
  - the four lines of text with the green block cursor;
  - the status bar with its green mode pill.

  It is drawn about a quarter larger, and its longest side now runs almost from edge to edge.
- **The small sizes are redrawn too.** From 16 to 32 pixels, the simplified drawing with heavier shapes fills the same
  area.
- **It shows everywhere the icon showed:**
  - the Windows executable and taskbar;
  - the window, on every platform;
  - the About dialog;
  - the MSI's shortcuts and its *Installed apps* entry;
  - the Linux launcher.
- **The sizes are unchanged:** ten in the Windows icon, from 16 to 256 pixels, and eight in the Linux icon theme.

### Upgrading from 1.14

- **Your settings carry over** unchanged. 1.15 adds no settings key and no file, and going back to 1.14 reads the same
  file.
- **On Windows, an MSI built from 1.15 upgrades the installed version** in place, 1.14.0 included:
  - `packaging\windows\buildmsi.ps1` names it `Enigma.TextEditor.1.15.0.msi`;
  - the script takes the version from the projects, so it needs no change of its own.

  Explorer may show the old icon on a shortcut until its icon cache refreshes.
- **On Linux, run `packaging/linux/install.sh` again** to pick up the new icon. Some desktops cache icons until you log
  in again.

### Dependencies

- **No change.** The Avalonia set is held back at **12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.5
  is out, but the set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- Nothing else has an update.

### Compatibility

- Unchanged from 1.14.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.15.0** is a minor release under Semantic Versioning. It changes the icon, and keeps every stored format and every
  default.

## 1.14.0 — 2026-11-03

A minor release with a new look for the icon and a Windows installer. The icon keeps its editor window but loses the
dark tile behind it. On Windows, one script now builds an MSI that installs the editor for all users, with its
shortcuts, and replaces a version installed from an earlier MSI.

### Application icon

- **The dark rounded tile is gone.** The editor window (the gutter of line marks, the four lines of text with the green
  block cursor, and the status bar with its green mode pill) now stands on a transparent background, in the same blues.
- **It shows everywhere the icon showed:** the Windows executable and taskbar, the window on every platform, the About
  dialog, and the Linux launcher.
- **The sizes are unchanged:** ten in the Windows icon, from 16 to 256 pixels, and eight in the Linux icon theme. The
  small sizes (16 to 32 pixels) still come from a simplified drawing with heavier shapes.

### Windows installer

- **`packaging\windows\buildmsi.ps1`**, or `buildmsi.bat` from a `cmd` prompt, builds
  `artifacts\Enigma.TextEditor.X.Y.Z.msi` in one step:
  - it runs `git clean -dxf` first, which deletes **every file git does not track** in the repository, so commit or
    move anything new before running it;
  - it publishes the editor with `publish.ps1`, as before;
  - it packages the result, uncompressed, with Enigma.Msi, whose worker is in `packaging\windows\Enigma.Msi.Worker\`.
  The Enigma.Msi profile it was built from is kept beside the MSI, as `Enigma.TextEditor.X.Y.Z.msipkg.json`.
- **What the MSI installs:**
  - the editor, for all users, in `%ProgramFiles%\Enigma Text Editor` (`Program Files (x86)` on 64-bit Windows, since
    the package is 32-bit);
  - a shortcut named *Enigma Text Editor* on the desktop and one in the program menu;
  - an entry in *Installed apps*, *Enigma Text Editor* by Josué Clément, with the icon.
- **It replaces an earlier version installed from an MSI**, such as 1.9.0: every MSI carries the same upgrade code.
  Uninstalling it leaves your settings, the log and `instance.json` in `%APPDATA%\Enigma.TextEditor` alone.
- Building it needs Windows, the .NET 10 SDK and the WiX CLI (`dotnet tool install --global wix`). The script checks for
  all three, and for the worker, before it cleans anything. The installed editor needs the .NET 10 runtime.

### Upgrading from 1.13

- **Your settings carry over** unchanged. 1.14 adds no settings key and no file, and going back to 1.13 reads the same
  file.
- **On Windows, an MSI built from 1.14 upgrades the installed version** in place. Explorer may show the old icon on a
  shortcut until its icon cache refreshes.
- **On Linux, run `packaging/linux/install.sh` again** to pick up the new icon. Some desktops cache icons until you log
  in again.

### Dependencies

- **No change.** The Avalonia set is held back at **12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.5
  is out, but the set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- Nothing else has an update.
- The Windows installer's build uses Enigma.Msi 1.7.0's worker (WixSharp_wix4 2.14.2, WiX 4.0.1 assemblies) and the
  WiX CLI. None of their files goes into the MSI.

### Compatibility

- Unchanged from 1.13.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.
- The MSI is for Windows only. On Linux the installer is still `packaging/linux/install.sh`; macOS has none.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.14.0** is a minor release under Semantic Versioning. It changes the icon and adds a Windows installer, and keeps
  every stored format and every default.

## 1.13.0 — 2026-11-02

A minor release with a file explorer. A panel on the left of the panes shows a folder's files and folders as a tree.
From it you open a file with a double-click, and create, rename and delete files and folders. The tab strip changes a
little too: a double-click on its empty part opens a new tab, and the mouse wheel scrolls it.

### The file explorer

- **A panel on the left of the panes**, hidden until you show it with the first button of the status bar,
  **View → Explorer** or Ctrl+Shift+E. The same three hide it again.
  - It is 260 px wide at first. Drag its edge to widen or narrow it, down to 160 px; the panes keep at least 240.
    Hidden and shown again, it keeps its width.
  - The button is on the status bar of the files page only. Showing the explorer from the settings page brings the
    files page back.
  - Ctrl+Shift+E gives it the keyboard: the tree, or the *Open Folder…* button when no folder is open. Esc gives the
    keyboard back to the text.
- **Open Folder…**, in the panel or the **File** menu, asks for a folder, starting in the open one, else in the folder
  of the file you are on. One folder is open at a time: opening another replaces it, and opening one shows the panel.
- **Close Folder**, in the panel or the **File** menu, empties the panel.
- **The tree** shows the folder's files and folders. The folder's name heads the panel, with its path as a tooltip.
  - Folders come first, then files, each in natural order and ignoring case, so `file2` comes before `file10`.
  - Nothing is hidden: dot files and hidden entries are listed.
  - A folder is read the first time it is expanded. A folder that cannot be read shows one row saying why:
    *Not found*, *No access* or *Cannot be read*.
- **A double-click or Enter opens a file** in the pane you are in, which takes the keyboard. It opens as with
  **File → Open…**: a binary file is asked about, and a file already open is shown. On a folder, the same keys expand
  or collapse it.
- **Refresh** reads the folder again. The explorer does not watch the disk, so after changes made outside the editor,
  press it. The folders you expanded stay expanded, and the selection stays on its entry if that is still there.
- **Its right-click menu** acts on the row you click, or on the folder itself from the empty part of the tree:
  - **New File…** and **New Folder…** create an entry in the selected folder, beside the selected file, or in the
    folder itself. The name is checked as you type it: an empty name, `.` or `..`, a path separator, a character or a
    name the system does not allow, and a name already taken are refused before you can confirm. Nothing is ever
    overwritten. The new entry is selected, and a new file, empty, opens;
  - **Rename…** (F2) starts with the current name, the part before its extension selected. A change of case alone
    works. The tabs of the renamed file, or of the files under a renamed folder, follow it with their unsaved
    changes, so a save never brings the old name back;
  - **Delete** (Delete) asks first, then moves the entry to the Recycle Bin on Windows or the trash on Linux. If the
    trash refuses the entry, a second question offers to delete it for good; where there is no trash, that is the
    only question. Both questions have *Cancel* as their default, so an Enter typed too soon deletes nothing.
- **The tabs of deleted files close**, unless they hold unsaved changes: those stay open, and a save writes the file
  again.
- **When an operation fails**, for a file in use or a name taken in the meantime, an error says so, and the folder is
  read again.
- **Nothing is remembered** from one run to the next: the explorer starts hidden, at its first width, with no folder.
- **Help → Key Bindings** lists the explorer's keys under *Files & tabs*, and Ctrl+Shift+E and Esc under
  *Application*.

### The tab strip

- **A double-click on the empty part of a pane's tab strip**, right of the last tab or below the tabs, opens a new
  untitled document in a new tab at the end of that pane, as **File → New** does. That pane takes the keyboard, even
  when it was not the one you were in.
  - A double-click on a tab, its **×** or the strip's scrollbar opens nothing.
  - **Help → Key Bindings** lists it under *Files & tabs*.
- **The mouse wheel scrolls a tab strip** too narrow for its tabs, sideways: down towards the later tabs, up towards
  the first. It stops at both ends.
  - In 1.12, only Shift and the wheel, a sideways swipe or the scrollbar moved it. Those work as before, and a notch
    moves the strip as far with Shift as without.

### Upgrading from 1.12

- **Your settings carry over** unchanged. 1.13 adds no settings key and no file: the explorer remembers nothing
  between runs. Going back to 1.12 reads the same file.
- **A 1.12 window still open takes the files of a 1.13 launch**, since handing files to the running window has not
  changed. Close every window to start using 1.13.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change.** The Avalonia set is held back at **12.1.4**, with Enigma.Avalonia.Desktop 1.3.0. Avalonia 12.1.5
  is out, but the set moves as a whole, and Enigma.Avalonia.Desktop has not moved.
- AvaloniaEdit 12.0.0 is the latest. Microsoft.Extensions.Hosting stays at 10.0.14: its 11.0.0 is a release candidate
  only, and is not taken. Nothing else has an update.

### Compatibility

- Unchanged from 1.12.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.
- **Where a deleted entry goes:**
  - on Windows, the Recycle Bin;
  - on Linux, the trash, through `gio trash` where it is installed. Without it, the trash in your home folder, which
    takes only entries on the same drive as your home: for an entry elsewhere, you are offered a permanent delete;
  - on macOS, the trash is not used yet: a delete asks once, for a permanent delete.
- Handing files to the running window is still built for Windows and Linux, and has not been tried on macOS.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.13.0** is a minor release under Semantic Versioning. It adds a file explorer and a new tab on a double-click of
  the tab strip, makes the mouse wheel scroll the tab strip, and keeps every stored format and every default.

## 1.12.0 — 2026-10-31

A minor release with two additions. In 1.11 a tab dropped near an edge of a pane split that pane. Now files dropped
there from outside the editor do the same, with the same highlight. And the status bar has a button for the font size.

### Splitting a pane with dropped files

- **Drop files near an edge** of a pane's text or hex view: the top, the right, the bottom or the left. That pane
  splits on that side, and the files open in the new half, which takes the keyboard.
  - "Near" means within the outer quarter of the pane's width or height, as for a tab. In a corner, the edge the
    pointer is nearer to wins.
  - The split is even, as `Ctrl+W v` and `:sp` split.
- **One split for all of them.** Every file opens in the new pane, in the order they were dropped, and the last one is
  shown.
- **The highlight shows where they go.** Near an edge, only the half of the pane the new pane will take is
  highlighted, with the same accent wash and ring as a dragged tab. Over a pane's tabs or the middle of its text, the
  whole pane is highlighted. In 1.11, files carried over a pane were not highlighted at all.
- **Opening in a pane works as before.** Dropped on a pane's tabs, or on the middle of its text, the files open in
  that pane.
- **A pane with no tab has no edges.** The window's only pane, showing "No file is open", takes the files whole, with
  no split: a split would leave that empty pane beside them.
- **What the split pane keeps:**
  - a fresh tab stays where it is, and the files open beside it, in the new pane;
  - a file the split pane already shows opens in the new pane too, as `:vsp file` gives: one file in two panes.
- **When nothing can open, nothing splits.** If no file is among what is dropped (only folders, or paths that are
  gone), the layout stays as it is and the status line names them, as for any drop.
- **When every file is refused**, such as a binary file you choose not to open, the new pane closes again and the
  pane that was split takes the keyboard back, as after `:split file`.
- **Text dragged over a pane** is not highlighted, and is still dropped into the text where you release it.
- **Help → Key Bindings** lists it under *Files & tabs*.

### The font size on the status bar

- **A new button, last on the right**, reads `Font Size: 14`. Pressed, it lists the sizes 8 to 16, then 18, 20, 24,
  28, 32 and 36.
  - The current size is checked, and the default reads `14 (default)`: choosing it is the reset.
  - A size that is not in the list, such as 17 after a zoom, is added in its place and checked, so the list always
    shows where the text is.
- **Choosing a size resizes the text at once** in every editor and in the hex view, and the size is remembered, as
  with **View → Zoom In / Zoom Out / Reset Zoom** (Ctrl+=, Ctrl+-, Ctrl+0) and the settings page.
- **It follows the other ways in.** A zoom key, the settings page or its *Reset to defaults* changes the button and
  the check too. Sizes from 6 to 72 stay reachable through those.
- **It shows for any open tab**, text or hex, since the hex rows use the font size too. With no tab open, it hides with
  the rest of the bar's right-hand items.
- **The keyboard stays in the pane.** The button never takes the focus, and the keyboard goes back to the text when
  the list closes, as for the bar's other buttons.

### Upgrading from 1.11

- **Your settings carry over** unchanged. 1.12 adds no settings key and no file: the button writes the font size the
  zoom keys and the settings page already wrote. Going back to 1.11 reads the same file.
- **A 1.11 window still open takes the files of a 1.12 launch**, since handing files to the running window has not
  changed. Close every window to start using 1.12.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change: every package is at its latest stable version.** That includes:
  - the Avalonia set at 12.1.4, with Enigma.Avalonia.Desktop 1.3.0;
  - AvaloniaEdit 12.0.0;
  - Microsoft.Extensions.Hosting 10.0.14. Its 11.0.0 is a release candidate only, and is not taken.

### Compatibility

- Unchanged from 1.11.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.
- Handing files to the running window is still built for Windows and Linux, and has not been tried on macOS.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.12.0** is a minor release under Semantic Versioning. It adds splitting a pane by dropping files near its edge,
  and a font size button on the status bar, and keeps every stored format and every default.

## 1.11.0 — 2026-10-30

A minor release about where a dragged tab goes. In 1.10 a tab dragged onto another pane moved into it. Now, dropped
near an edge of a pane, it splits that pane on that side and goes into the new half. That works on the pane it came
from too.

### Splitting a pane with a dragged tab

- **Drop a tab near an edge** of a pane's text or hex view: the top, the right, the bottom or the left. That pane
  splits on that side, and the tab goes into the new half, which takes the keyboard.
  - "Near" means within the outer quarter of the pane's width or height. In a corner, the edge the pointer is nearer
    to wins.
  - The split is even, as `Ctrl+W v` and `:sp` split.
- **The highlight shows the new pane.** Near an edge, only the half of the pane the new pane will take is
  highlighted, with the same accent wash and ring as a move. Over the middle, the whole pane is highlighted, as in
  1.10.
- **Moving works as before.** Dropped on a pane's tabs, or on the middle of its text, a tab moves into that pane. The
  tab strip never splits.
- **The tab's own pane splits too**, as long as it keeps another tab.
  - A pane's only tab shows no highlight near that pane's edges, and dropping it there changes nothing. The pane
    would be left empty and close, and the layout would end up as it was.
  - The middle of its own pane still moves nothing.
- **The new pane gets the tab itself**, as a move does: its caret, its scroll position and, in the hex view, its row.
- **A file the pane already shows** still splits off into the new pane. That pane starts empty, so a file is never
  shown twice in one pane.
- **A pane left without tabs closes**, as after a move.
- **Escape cancels the drag**, as before, and nothing splits.
- **Help → Key Bindings** lists it under *Panes*.
- **The keys are unchanged.** `Space mh`, `mj`, `mk` and `ml` still move a tab only to a pane that is already there.
  Splitting with a tab is done by dragging.

### Upgrading from 1.10

- **Your settings carry over** unchanged. 1.11 adds no settings key and no file, and going back to 1.10 reads the
  same file.
- **A 1.10 window still open takes the files of a 1.11 launch**, since handing files to the running window has not
  changed. Close every window to start using 1.11.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change: every package is at its latest stable version.** That includes:
  - the Avalonia set at 12.1.4, with Enigma.Avalonia.Desktop 1.3.0;
  - AvaloniaEdit 12.0.0;
  - Microsoft.Extensions.Hosting 10.0.14. Its 11.0.0 is a release candidate only, and is not taken.

### Compatibility

- Unchanged from 1.10.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.
- Handing files to the running window is still built for Windows and Linux, and has not been tried on macOS.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.11.0** is a minor release under Semantic Versioning. It adds splitting a pane by dropping a tab near its edge,
  and keeps every stored format and every default.

## 1.10.0 — 2026-10-30

A minor release about where a document goes. A tab can move to another pane, with `Space` and a direction or by
dragging it there. A file opened from the system while the editor is running, by a double-click or *Open with*, now
opens as a tab in the window you used last, instead of in a second window.

### Moving tabs between panes

- **`Space mh`, `mj`, `mk` and `ml`** move the tab you are on to the pane on the left, below, above or on the right:
  the pane `gh`, `gj`, `gk`, `gl` would reach. That pane takes the keyboard. They work from a hex tab too, and over a
  selection. With no pane on that side, they do nothing.
- **Dragging a tab** from its strip onto another pane moves it there too. You can drop it anywhere on that pane: its
  tabs, its text, its hex view or its empty placeholder.
  - The drag starts once the pointer has moved a few pixels. A plain click still shows the tab, and the **×** still
    closes it.
  - While you drag, the pointer is a move cursor, and the pane the tab would land on is highlighted with a faint wash
    of the accent colour inside an accent ring.
  - **Escape cancels the drag**, without leaving Vim's insert or visual mode. Releasing the tab over its own pane, or
    outside the panes, moves nothing.
  - Files dragged in from another application still open where they are dropped, as before.
- **The tab itself moves**, with its caret, its scroll position and, in the hex view, its row. It goes to the end of
  the other pane's tabs.
- **A file the other pane already shows** does not get a second tab there. That pane's tab is shown and the moved
  one goes away. The file stays open, and nothing is asked.
- **A pane left without tabs closes**, as closing its last tab would close it.
- **Help → Key Bindings** lists both under *Panes*.

### Files opened while the editor is running

- **The open window takes them.** Some launches name files: a double-click, *Open with*, the Linux launcher, or
  `enigma-text-editor notes.txt` in a terminal. While the editor is running, such a launch hands its files to the
  window you used last and ends. The files open there as tabs, and that window comes to the front, maximised again if
  it was. Before, each such launch started a window of its own.
- **Where they open:** in the pane with the keyboard, on the files page even if the settings page was showing.
  - A fresh untitled tab gives its place to the first file, as it does for a dropped file; an untitled tab you have
    typed in is kept.
  - A name that does not exist yet opens an empty document, as it does on the command line.
- **Several windows still.** Started without a file, the editor opens a new window, so several can run side by side.
  Files go to the one you used last.
- **Nothing is lost.** If that window has gone, or does not answer within a few seconds, the launch opens its own
  window with the files, as before.
- **Only your own launches reach it.** Each window listens on a named pipe that only your user account can connect
  to, and takes nothing from it but a list of files to open.
- **Limits:**
  - files that arrive while the window shows a dialog open behind that dialog;
  - files that arrive while the window is closing close with it;
  - a folder named alongside files is left out.

### Upgrading from 1.9

- **Your settings carry over** unchanged. 1.10 adds no settings key, and going back to 1.9 reads the same file.
- **One new file, `instance.json`,** sits beside `settings.json`. It names the window used last, so that a launch
  knows where to send its files, and that window removes it when it closes. 1.9 never reads it.
- **A 1.9 window still open receives no files.** The first file you open starts a 1.10 window, and the files after it
  go there.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place. The launcher entry already hands its files to
  the editor, so nothing else changes.

### Dependencies

- **No change: every package is at its latest stable version.** That includes:
  - the Avalonia set at 12.1.4, with Enigma.Avalonia.Desktop 1.3.0;
  - AvaloniaEdit 12.0.0;
  - Microsoft.Extensions.Hosting 10.0.14. Its 11.0.0 is a release candidate only, and is not taken.

### Compatibility

- Unchanged from 1.9.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.
- Handing files to the running window is built for Windows and Linux. On macOS, Finder hands files to an application
  through Apple events, which the editor does not take, so it has not been tried there.

### Known issue

- Some lines can still lose their syntax colours after a scroll, until they are edited or scrolled out and back. The
  fix is not in this release.

### Version

- **1.10.0** is a minor release under Semantic Versioning. It adds moving tabs between panes and handing files to the
  running window, and keeps every stored format and every default.

## 1.9.0 — 2026-10-29

A minor release that brings the editor in line with the other Enigma applications. An error that nothing else handles
no longer vanishes or takes the editor down with it: it goes to the log, and a dialog says what went wrong and that the
editor is still running. The application icon now sits beside the executable as `app.ico`, and one script per platform
publishes the editor.

### Errors that nothing else handles

- **A dialog, not a silent failure or a crash.** Some failures are not handled where they happen: in a command, a key
  binding or background work. The editor now writes each one to the log and opens a *Something went wrong* dialog.
  The dialog says what failed, and that the editor is still running, so you can carry on or restart it.
- **One dialog for a burst.**
  - Errors that come while the dialog is open are counted into it ("2 more errors occurred"), rather than opening a
    dialog each.
  - After you close it, the same error is not shown again for a minute.
- **Never over a question.** While the editor is asking you something, such as whether to save changes or which
  encoding to reopen with, the dialog waits until you have answered.
- **Details for a bug report.** A collapsed *Details* section holds each error's type, message and stack trace, and
  *Copy details* puts them on the clipboard. Nothing else attached to an error is shown or copied.
- **Work started in the background reports its failures the same way.** That covers saving a setting, running an ex
  command and opening dropped files. Before, such a failure could go unseen.
- **An error that ends the process is still fatal.** It is now written to the log, and the log is flushed, before the
  process ends.
- **Cancelling is not an error.** Cancelling something, or closing the editor while work is in flight, shows nothing.

### Packaging

- **`app.ico` beside the executable.** The build and publish output now hold the application icon as `app.ico`, next
  to `Enigma.TextEditor.Desktop(.exe)`. A shortcut, a launcher or an installer can point at it.
- **Publish scripts.** Each one publishes a ReadyToRun Release build:
  - `packaging/windows/publish.ps1` and `publish.bat`, for win-x64;
  - `packaging/linux/publish.sh`, for linux-x64.

  Each runs from any directory and fails when `dotnet publish` fails. The result runs on an installed .NET 10 runtime.
  `docs/RELEASE.md` says where the output lands.

### Upgrading from 1.8

- **Your settings carry over** unchanged. 1.9 adds no settings key, and going back to 1.8 reads the same file.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- **No change: every package is at its latest stable version.** That includes:
  - the Avalonia set at 12.1.4, with Enigma.Avalonia.Desktop 1.3.0;
  - AvaloniaEdit 12.0.0;
  - Microsoft.Extensions.Hosting 10.0.14. Its 11.0.0 is a release candidate only, and is not taken.

### Compatibility

- Unchanged from 1.8.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 and AvaloniaEdit 12.0.0.

### Version

- **1.9.0** is a minor release under Semantic Versioning. It adds the error dialog and the packaging helpers, and
  keeps every stored format and every default.

## 1.8.0 — 2026-10-28

A minor release with a new application icon. The page of text gives way to an editor window: a gutter of line marks,
four lines of text with the green block cursor, and a status bar with a green mode pill along the bottom, in softer
blues on the same dark tile as the other Enigma applications. Underneath, the Avalonia set moves to 12.1.4.

### Application icon

- **The new drawing is everywhere the icon shows:** the Windows executable and taskbar, the window on every platform,
  the About dialog, and the Linux launcher.
- **The Windows icon has ten sizes instead of six.** The new ones are 20, 24, 40 and 96 pixels, so Windows no longer
  scales an image at 125 %, 150 %, 250 % or 300 % display scaling.
- **The Linux installer puts eight sizes in the icon theme instead of six**, adding 24 and 96 pixels for panels and
  launchers. The uninstaller removes all eight.
- The small sizes (16 to 32 pixels) come from a simplified drawing with heavier shapes, so they stay sharp.

### Upgrading from 1.7

- **Your settings carry over** unchanged. 1.8 adds no settings key, and going back to 1.7 reads the same file.
- **On Linux, run `packaging/linux/install.sh` again** to pick up the new icon. Some desktops cache icons until you
  log in again.
- On Windows, Explorer may show the old icon on a shortcut until its icon cache refreshes.

### Dependencies

- **The Avalonia set moves from 12.1.2 to 12.1.4**, all together:
  - Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter;
  - and, in the tests, Avalonia.Headless and Avalonia.Skia.
- **Enigma.Avalonia.Desktop moves from 1.2.0 to 1.3.0.** That release is what allows the move: it needs Avalonia
  12.1.4, and it changes no API and no theme colour.
- AvaloniaEdit stays at 12.0.0, the latest. Nothing else has an update.

### Compatibility

- Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.4 (was 12.1.2) and AvaloniaEdit 12.0.0.

### Version

- **1.8.0** is a minor release under Semantic Versioning. It changes the icon and patch-level dependencies, and keeps
  every stored format and every default.

## 1.7.0 — 2026-10-27

A minor release about colours. Links in the text — URLs and e-mail addresses — were drawn in pure blue, which is
barely legible on the dark theme's background; they now have a blue of each theme's own. The settings page's
descriptions are a step brighter, and the hex view marks its current row in the editor's selection colour.

### Colours

- **Links.** The editor still underlines URLs and e-mail addresses. Their colour changes:
  - **Dark: `#7AABFF`**, a lighter blue on the window accent's hue. It was `#0000FF`, at 1.9:1 against the
    background; it is now 7.1:1.
  - **Light: `#1F5FCC`**, 5.5:1. It was the same `#0000FF` that the light syntax colours give to keywords, so a link
    in a comment looked like a keyword.
  - Both follow a theme switch at once.
- **The settings page's descriptions** use the descriptions' grey of the rest of the window rather than the
  placeholders' fainter one. That is the line under each setting and the header's "Changes apply at once…". On a dark
  card they go from 1.7:1 to 2.9:1.
- **The hex view's current row** is filled with the editor's selection colour, in both themes. It was the operating
  system's accent colour, a saturated stripe in the dark theme.
- Nothing else changes colour: syntax highlighting, the search highlight, the selection, the minimap and the line
  numbers are as they were.

### Upgrading from 1.6

- **Your settings carry over** unchanged. 1.7 adds no settings key, and going back to 1.6 reads the same file.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- No change. The Avalonia set is held back at **12.1.2**, with Enigma.Avalonia.Desktop 1.2.0 and AvaloniaEdit 12.0.0.
  Avalonia 12.1.4 is out, but the set moves as a whole. Nothing else has an update.

### Compatibility

- Unchanged from 1.6.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.7.0** is a minor release under Semantic Versioning. It changes colours only, and keeps every stored format and
  every default.

## 1.6.0 — 2026-10-27

A minor release. A narrow rail on the left of the window holds two icons: the files, which is the split editor, and
the settings, a page that replaces the Preferences dialog. The first start asks four questions in a welcome dialog,
and a fresh install now wraps long lines, moves a splitter by 10% and writes new files with the platform's line
endings.

### Navigation rail and settings page

- **The rail** sits on the left, between the menu bar and the status bar: 44 pixels wide, icons only, each naming
  itself in a tooltip.
  - **Files**, at the top, is the split editor as before.
  - **Settings**, at the bottom, is the settings page.
- **The settings page** holds every setting, grouped into Text, Appearance, Editing and New files, each with its full
  range — what the Preferences dialog held, on a page that fills the window. It has a *Settings* header with
  *Reset to defaults*, which still asks first, and a centred column that scrolls.
- **The editor stays as it was** while the settings are shown: every tab, split, caret and scroll position is there
  when you come back.
- **Keys:** **Settings → All Settings** or **Ctrl+,** shows the page, and Esc goes back to the files. The keyboard
  follows the page: the settings page takes it, and the editor gets it back.
- **The files come back by themselves** when something opens, creates or rearranges a document while the settings are
  shown: File → New or Open, Ctrl+N, `:e`, a split, another tab. The Edit menu does nothing while the editor is hidden.
- **Help → Key Bindings** lists Ctrl+, as *Settings*, and Esc to go back to the files.

### First start

- **A welcome dialog** opens the first time the editor starts, with four choices: the theme (follow the system, light
  or dark), Vim mode, the minimap, and wrapping long lines. Each is set to its default.
- **The theme repaints the window as it is chosen.** The rest applies when the dialog closes.
- **Get started, Skip or Esc** all keep what the dialog shows, and write the settings file, so the dialog does not come
  back. Every choice can be changed later on the settings page.
- **Files named on the command line** open once the dialog has closed.
- A first start is a start with no settings file. A settings file that cannot be read is not one: it is moved aside as
  before, and no welcome is shown.

### Defaults

- A fresh install, and *Reset to defaults*, now start with:
  - **wrap long lines: on** (was off);
  - **pane resize step: 10%** (was 5%);
  - **new files' line endings: CRLF on Windows, LF on Linux and macOS** (was LF everywhere).
- The theme (follow the system), the minimap (on) and Vim mode (on) are unchanged.

### Upgrading from 1.5

- **Your settings carry over** unchanged. `settings.json` holds every setting, so the new defaults reach only a fresh
  install and *Reset to defaults*. 1.6 adds no settings key, and going back to 1.5 reads the same file.
- **The Preferences dialog is gone.** Its settings are on the settings page; **Settings → Preferences…** is now
  **Settings → All Settings**, still on Ctrl+,.
- **The editor area is 44 pixels narrower**, the width of the rail.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- Microsoft.Extensions.Hosting **10.0.13 → 10.0.14**.
- The Avalonia set is held back at **12.1.2**, with Enigma.Avalonia.Desktop 1.2.0 and AvaloniaEdit 12.0.0. Avalonia
  12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.5.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.6.0** is a minor release under Semantic Versioning. It adds features and keeps every stored format. One dialog
  becomes a page — the Preferences dialog is now the settings page, and its menu item is renamed — and three defaults
  change for a fresh install.

## 1.5.0 — 2026-10-23

A minor release. **Settings → Preferences…** (Ctrl+,) opens a dialog with every preference in one place, each with its
full range, and a new one: the font the editors draw a file in. The editor's text no longer runs under its scrollbars or
against the minimap.

### Preferences

- **Settings → Preferences…**, or **Ctrl+,**, opens the *Preferences* dialog. Its groups:
  - **Text**: the font and the font size;
  - **Appearance**: the theme, the minimap, and wrapping long lines;
  - **Editing**: Vim mode, and the pane resize step, any value from 1 to 50%;
  - **New files**: indenting with tabs or spaces, the indent size from 1 to 16, and LF or CRLF line endings.
- **Changes apply at once** and are remembered, as from the menu. The only button is *Close*; Esc closes it too, and
  the keyboard goes back to the editor.
- **The menu, the zoom keys and the dialog show the same settings.** A change made in one shows in the others while
  the dialog is open. A resize step the menu does not offer (anything but 2, 5 or 10%) leaves none of its items
  checked.
- **Reset to defaults** asks first, from a flyout: *Put every preference back to its default*.
- **Help → Key Bindings** lists Ctrl+, under *Application*.

### Fonts

- **The font** is chosen from the monospace faces the machine has. *Default* comes first: the faces the editor has
  always used, the first of Cascadia Mono, Consolas, JetBrains Mono, DejaVu Sans Mono, Menlo and Courier New that is
  installed. The font draws the text in every pane's editor and the hex view's rows.
- **A face that is not installed** — a settings file brought from another machine — still shows in the list, and the
  editors fall back to the default faces, so the text stays monospace.
- **The font size**, from 6 to 72, is the size Ctrl+=, Ctrl+- and Ctrl+0 change: the field and the keys move the same
  setting.
- The help dialog and the ex and search line keep the default font: they are part of the interface, not the file.

### Editor

- **The text is no longer drawn under the scrollbars.** The last line on screen used to sit half under the horizontal
  bar, and with the minimap off, the end of a long line or a wrapped row ran under the vertical bar.
- **The text stops short of the minimap**, and of the vertical scrollbar, with a small gap. Scrolled fully right, the
  end of the longest line shows whole, wrapped or not.
- **The scrollbars are drawn at full width.** With the minimap off, the vertical bar no longer shows as a thin line
  that widens under the pointer, and the horizontal bar, when a line is wider than the pane, takes its own row below
  the text.

### Upgrading from 1.4

- **Your settings carry over.** `settings.json` gains one key, `EditorFontFamily`. A 1.4 file lacks it, and starts
  with the default font.
- **Going back to 1.4** reads the file and ignores the key. If 1.4 then saves the settings, it leaves the key out, and
  1.5 starts with the default font again.
- **The scrollbars no longer auto-hide** in the editor (see *Editor*). The text area is a scrollbar's width narrower
  with the minimap off, and a scrollbar's height shorter when a line is wider than the pane.
- On Linux, run `packaging/linux/install.sh` again to upgrade in place.

### Dependencies

- No package had an update outside the Avalonia set. The set is held back at **12.1.2**, with Enigma.Avalonia.Desktop
  1.2.0 and AvaloniaEdit 12.0.0. Avalonia 12.1.4 is out, but the set moves as a whole.

### Compatibility

- Unchanged from 1.4.0: Windows 10/11, Linux (X11) and macOS; .NET 10; Avalonia 12.1.2 and AvaloniaEdit 12.0.0.

### Version

- **1.5.0** is a minor release under Semantic Versioning. It adds features, keeps every stored format, and removes
  nothing. One behaviour changes: the editor's scrollbars no longer auto-hide.

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

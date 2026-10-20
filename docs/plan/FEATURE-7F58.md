# FEATURE-7F58 — Linux installer

**Status:** DONE — see `docs/done/FEATURE-7F58.md`
**Type:** FEATURE
**Branch:** `feature/feature-7f58-linux-installer`
**Run:** feature/2026-10-20-linux-install-release

## Objective

Make Enigma Text Editor installable on Linux with one command, the way Enigma.GitClient is:

- the application in `~/.local/share/enigma-text-editor`;
- a launcher symlink in `~/.local/bin`;
- a desktop entry named **Enigma Text Editor**, offered in *Open With* for text files;
- its icon in the hicolor theme;
- an uninstaller that takes all of it away again and leaves the user's settings alone.

## Context & constraints

- **The reference** is Enigma.GitClient's `packaging/linux/` (FEATURE-B4C0, as it stands at GitClient 6.4.0):
  - `install.sh`:
    - bash, `set -euo pipefail`, refuses root;
    - options: `--from DIR`, `--framework-dependent`, `--rid RID`, `--help`;
    - publishes the app in Release, self-contained by default, into a staging directory beside the install
      directory, then swaps it in by a rename. A failed swap puts the previous install back;
    - symlinks the apphost into the bin directory;
    - installs six icons into hicolor;
    - writes the desktop entry from a `.desktop.in` template, substituting `@EXEC@` in bash rather than sed;
    - pins `DOTNET_ROOT` in `Exec` when a framework-dependent tree's runtime is not where an apphost looks;
    - refreshes the desktop and icon caches, best effort;
    - notes when the bin directory is not on `PATH`.
  - `uninstall.sh`:
    - removes exactly those four locations;
    - never touches the configuration;
    - refuses to remove a directory whose name is not the app id;
    - leaves a symlink alone when it points elsewhere.
  - `icons/<app-id>-<N>.png`, for N in 16, 32, 48, 64, 128 and 256.
- **Enigma.MarkdownEditor's later port** fixed one thing in GitClient's checks. GitClient's
  `[[ -n ${data_home} ]]` checks can never fail: `${XDG_DATA_HOME:-…}` already treats an empty value as unset.
  Its desktop template also records when `%F` and `MimeType=` belong in the entry: when the application opens
  files named on its command line.
- **This application:**
  - the project is `src/Enigma.TextEditor.Desktop`, and the apphost is `Enigma.TextEditor.Desktop`;
  - the configuration and logs are in `$XDG_CONFIG_HOME/Enigma.TextEditor`;
  - it **opens the files named on its command line** (`App` → `StartupFiles.Parse`), several at once;
  - `Assets/app.ico` holds six 32-bit PNG frames at exactly the hicolor sizes (pinned by `AppIconTests`);
  - `packaging/icon/enigma-text-editor.svg` already uses the id `enigma-text-editor`.
- `.gitattributes` already declares `*.sh text eol=lf` and `*.png binary`.
- **Docs that would go stale:**
  - the README *Requirements* and *Build, run, test* say nothing about installing;
  - its layout table lists only `packaging/icon`;
  - `docs/RELEASE.md` says "There is no installer" (step 5), and has a section titled *Why there is no NuGet
    step or installer*.
- `desktop-file-validate`, `update-desktop-database`, `gtk-update-icon-cache` and `kbuildsycoca6` are on this
  machine. `shellcheck` is not.

## Decisions

| Decision | Rationale |
|---|---|
| Port GitClient's scripts, adapted to this app's id, apphost, project and wording | The request names it as the model |
| App id `enigma-text-editor`: the install directory, the symlink, the desktop file, the icon name | The icon sources already carry it; mirrors `enigma-git-client` |
| Self-contained by default | A framework-dependent launcher started from a bare environment does nothing when the runtime lives in `~/.dotnet` (GitClient's and Tasks' verified finding) |
| `Exec=@EXEC@ %F` and `MimeType=text/plain;` | The editor opens the files it is given, so the launcher can pass them. *Open With* for text files is what a text editor's entry is for |

## Steps

1. `packaging/linux/icons/enigma-text-editor-<N>.png` — the six frames of `src/Enigma.TextEditor.Desktop/Assets/app.ico`,
   byte copies.
2. `packaging/linux/enigma-text-editor.desktop.in`:
   - `Type=Application`, `Version=1.5`;
   - `Name=Enigma Text Editor`, `GenericName=Text Editor`, a `Comment`;
   - `Exec=@EXEC@ %F`, `Icon=enigma-text-editor`, `Terminal=false`;
   - `MimeType=text/plain;`, `Categories=Utility;TextEditor;`, `Keywords=`;
   - `StartupNotify=true`, `StartupWMClass=Enigma.TextEditor.Desktop`.

   Comments in the template say why `Exec` is substituted and why the field code is there. They must never name the
   placeholder: the substitution is global, so a second occurrence would be rewritten too.
3. `packaging/linux/install.sh` — GitClient's installer with this application's id, apphost, project and wording:
   - the help names `~/.config/Enigma.TextEditor` as never touched;
   - the closing hint names `enigma-text-editor [file …]`;
   - GitClient's git check is dropped: this app needs no git;
   - the publish sizes in `--help` are measured, not copied;
   - the two unreachable empty-value checks are replaced by MarkdownEditor's comment.
4. `packaging/linux/uninstall.sh` — likewise. It names `~/.config/Enigma.TextEditor` (settings and logs) as left
   untouched. GitClient's former-launcher-name allowance is dropped: this app has only ever had one apphost name.
5. Both scripts are mode 755 in the index.
6. `README.md`:
   - an *Install on Linux* section: what goes where, the options, the uninstaller, *Open With*;
   - the *Requirements* line for the runtime;
   - the `packaging/linux` row in *Solution layout*.
7. `docs/RELEASE.md` — step 5 and the closing section say the Linux installer exists, what it is for, and that
   there is still no NuGet step and no MSI.

## Acceptance criteria

- `bash -n` parses both scripts.
- `desktop-file-validate` accepts the written entry with no error and no warning.
- A dry run against a scratch XDG root (`XDG_DATA_HOME`, `XDG_BIN_HOME`, `XDG_CONFIG_HOME` all inside the session
  scratchpad) installs the application:
  - the install directory holds a runnable apphost;
  - the symlink resolves to it;
  - the six icons are in their hicolor directories, byte-identical to the `app.ico` frames;
  - `Exec` is the quoted absolute installed path followed by `%F`;
  - the installed application starts from a bare environment (`env -i` with only the display variables) and keeps
    running until stopped.
- Running the installer again replaces the installation and leaves no staging or `.previous` directory behind.
- `uninstall.sh` removes the four locations, and leaves a scratch `Enigma.TextEditor` configuration directory
  untouched.
- `--help` works on both scripts. An unknown option, and `--from` with a directory that holds no launcher, are
  refused with a message.
- Neither script uses `sudo` or writes outside the XDG directories.
- `dotnet build Enigma.TextEditor.slnx` is clean with zero warnings, and the whole suite is green.

## Out of scope

- `.deb`, `.rpm`, Flatpak, AppImage, a release tarball, a system-wide install.
- Windows and macOS packaging; an MSI profile.
- Making the editor the default application for any type.
- Any change to the application's code.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| "Like made in Enigma.GitClient" | GitClient's final `install.sh` / `uninstall.sh` / template / icon layout, adapted | The draft names it; the scripts carry lessons already paid for | A new design; MarkdownEditor's tarball-aware variant (a release tarball is not asked for) |
| Application id | `enigma-text-editor` | The icon sources' name, and the house `enigma-<name>` shape | `enigma-texteditor`; a reverse-DNS id (none is configured anywhere) |
| Publish default | Self-contained, `--framework-dependent` opt-in | The verified launcher-runtime finding | Framework-dependent by default |
| Field code and MIME type | `%F` and `MimeType=text/plain;` | The app opens files from its command line; MarkdownEditor's template defers both lines to exactly that capability | No field code, as GitClient (whose app opens no files); a long list of `text/x-*` types (`text/plain` is their parent) |
| `Name` / `GenericName` | "Enigma Text Editor" / "Text Editor" | The About dialog's name; a generic name, as MarkdownEditor's "Markdown Editor" | `GenericName` equal to the name, as GitClient |
| `Categories` | `Utility;TextEditor;` | MarkdownEditor's, the registered pair for a text editor | `Development;TextEditor;` |
| `StartupWMClass` | `Enigma.TextEditor.Desktop` | The WM_CLASS Avalonia derives from the entry assembly, as every sibling | Omitting it |
| Icons | Byte copies of the `app.ico` frames | GitClient's recipe; the frames are already the hicolor sizes | Re-rendering from the SVGs |
| Empty-XDG checks | MarkdownEditor's comment instead of GitClient's unreachable checks | Dead code is misleading; the sibling already fixed it | Copying them verbatim |
| GitClient specifics | Drop the git check and the former-launcher allowance | Neither applies to this app | Keeping them |
| How it is verified | `bash -n`, `desktop-file-validate`, a real install / reinstall / uninstall into a scratch XDG root, a start from `env -i` | The house Definition of Done for scripts, without a shell-test framework the project does not have | A `bats` suite; inspection alone |

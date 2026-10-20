# FEATURE-7F58 — Linux installer

**Item:** FEATURE-7F58 — Linux installer
**Branch:** `feature/feature-7f58-linux-installer`
**Run:** feature/2026-10-20-linux-install-release

## Summary

Enigma Text Editor now installs on Linux with one command, the way Enigma.GitClient does. Nothing runs as root, and
nothing is written outside the user's XDG directories.

**`packaging/linux/install.sh`** is GitClient's installer, adapted:

- it publishes `src/Enigma.TextEditor.Desktop` in Release for this machine's runtime identifier, self-contained by
  default, into a staging directory beside the install directory, then swaps it in by a rename;
- `--framework-dependent`, `--from DIR` and `--rid RID` work as in GitClient;
- it links `~/.local/bin/enigma-text-editor` to the apphost;
- it installs the six icons into the hicolor theme;
- it writes `enigma-text-editor.desktop` from the template, pinning `DOTNET_ROOT` when a framework-dependent build's
  runtime is not where a launcher looks;
- it refreshes the desktop and icon caches, best effort.

**`packaging/linux/uninstall.sh`** removes exactly those four things. It leaves alone:

- `~/.config/Enigma.TextEditor`, the settings and the log;
- a symlink pointing anywhere else.

**`packaging/linux/enigma-text-editor.desktop.in`** names the editor *Enigma Text Editor* (generic name *Text
Editor*), in `Utility;TextEditor;`, with `StartupWMClass=Enigma.TextEditor.Desktop`. Unlike GitClient's, it carries:

- `%F`, because this application opens every file named on its command line;
- `MimeType=text/plain;`, which lists it under *Open With* for text files and their `text/x-*` subtypes.

**`packaging/linux/icons/enigma-text-editor-<N>.png`** are the six frames of `Assets/app.ico`, byte for byte.

**Docs:**

- the README gains *Install on Linux*, a *Requirements* note on the runtime, and the `packaging/linux` layout row;
- `docs/RELEASE.md`:
  - step 5 names the installer for a published folder;
  - its closing section is now *Why there is no NuGet step or MSI*, and says what the Linux installer is and why
    it is not a release step.

## Files / modules touched

**Created**

- `packaging/linux/install.sh` (mode 755)
- `packaging/linux/uninstall.sh` (mode 755)
- `packaging/linux/enigma-text-editor.desktop.in`
- `packaging/linux/icons/enigma-text-editor-{16,32,48,64,128,256}.png`
- `docs/done/FEATURE-7F58.md`

**Modified**

- `README.md` — *Install on Linux*, *Requirements*, *Solution layout*
- `docs/RELEASE.md` — step 5 and the closing section
- `docs/roadmap.md`, `docs/plan/FEATURE-7F58.md` — statuses

## Deviations & follow-ups

**Deviations from GitClient's scripts**, all as planned:

- GitClient's two empty-value checks on the XDG variables can never fail. They are replaced by the comment
  Enigma.MarkdownEditor's port uses.
- GitClient's git check is dropped from the installer, and its allowance for a former launcher name from the
  uninstaller. Neither applies to this application.
- The closing hint names `enigma-text-editor <file>...`, and the final line names *Open With*.

**Observed during the dry run, not fixed here:**

- **The editor does not end on SIGTERM.** A `kill <pid>` left the window open, and it took a SIGKILL to stop it.
  - The likely cause is the generic host's console lifetime: it takes the signal and stops the host, and nothing
    closes the Avalonia window.
  - Logout and shutdown kill with SIGKILL after their timeout anyway, but a graceful stop (with the unsaved-changes
    question) would be better.
  - Candidate for a BUG item.

**Other notes:**

- **Scratch-root side effect, as in GitClient:**
  - `dotnet publish` inherits `XDG_DATA_HOME`, so NuGet and Avalonia's build tooling put their caches (`NuGet/`,
    `AvaloniaUI/`) in whatever data directory the installer is pointed at;
  - with the default `~/.local/share`, that is where they already are.
- **Empty directories left behind:** the uninstaller leaves the empty `hicolor/<N>x<N>/apps` directories, as
  GitClient's does. They belong to the icon theme.
- **Not exercised:** running as root, which both scripts refuse first thing.
- **Line endings:** every new text file is LF, per `.gitattributes`.

## Build/test evidence

**Solution:**

- `dotnet build Enigma.TextEditor.slnx --no-incremental`: 0 warnings, 0 errors.
- `dotnet test --solution Enigma.TextEditor.slnx`: 2503 passed, 0 failed, 0 skipped.

**Scripts:**

- `bash -n` parses both scripts.
- `--help` works on both.
- Each of these is refused with a message and exit 1:
  - `--bogus`, on either script;
  - `--from /tmp`: "holds no Enigma.TextEditor.Desktop launcher";
  - `--from /nonexistent`;
  - a relative `XDG_DATA_HOME`.

**Dry run.** Every run had `XDG_DATA_HOME`, `XDG_BIN_HOME`, `XDG_CONFIG_HOME` and `XDG_CACHE_HOME` inside the session
scratchpad.

- **Install** (self-contained, `linux-x64`):
  - 123 MB in `…/data/enigma-text-editor`, with an executable apphost and `libcoreclr.so`;
  - the symlink resolves to the apphost;
  - the six installed icons are `cmp`-identical to `packaging/linux/icons`, and the 256 one to `Assets/app-256.png`;
  - `Exec="…/enigma-text-editor/Enigma.TextEditor.Desktop" %F`;
  - `desktop-file-validate` printed nothing, so no error and no warning;
  - `mimeinfo.cache` lists `text/plain=enigma-text-editor.desktop;`.
- **Start from a bare environment.** `env -i`, keeping only `DISPLAY`, `XAUTHORITY`, `XDG_RUNTIME_DIR`, a scratch
  `HOME` and the scratch `XDG_CONFIG_HOME`, ran the symlink with a file argument:
  - the process stayed up;
  - the window `sample.txt — Enigma Text Editor` mapped;
  - `xprop` read `WM_CLASS = "enigma-text-editor", "Enigma.TextEditor.Desktop"`. The class matches `StartupWMClass`;
  - the log said `Enigma Text Editor 1.0.0 started.`;
  - settings and logs went to the scratch configuration only.
- **Reinstall:**
  - "Replacing …";
  - a stale file planted in the install directory was gone;
  - no `.staging` or `.previous` directory was left;
  - with the bin directory on `PATH`, the closing hint is the run hint, not the `PATH` note.
- **`--framework-dependent`:**
  - 44 MB, with no bundled runtime;
  - the runtime here is in `~/.dotnet`, and `/usr/share/dotnet` does not exist, so the entry pinned
    `env DOTNET_ROOT="/home/jo/.dotnet"`, and still validated.
- **`--from`:** installing that tree into a third root copied it identically (`diff -rq`), and pinned the same.
- **Uninstall:**
  - removed the application, the symlink, the desktop entry and the six icons;
  - left the scratch `Enigma.TextEditor` configuration (`marker.json`, `settings.json`, `logs/`) as it was;
  - a second run said "Nothing to remove";
  - a symlink re-pointed at `/usr/bin/true` was left alone, and named.
- **Measured, not copied:** the 123 MB and 44 MB in `--help` and the README.

## Decisions taken at build time

| Question | Chosen | Why |
|---|---|---|
| `Comment` | "A text editor driven by Vim's keys, with tabs, split panes and a hex viewer" | The README's intro and the 1.0.0 notes' positioning, in one line |
| `Keywords` | `text;editor;vim;code;encoding;hex;` | What someone would type into a launcher to find it |
| Template comments | Why `Exec` is substituted, and why `%F` and `MimeType` are there; the placeholder never named | MarkdownEditor's template rule: the substitution is global |
| README section place | After *Build, run, test*, before *Solution layout* | GitClient's order |
| Runbook wording | "Neither has applied so far" instead of naming 1.0.0 | A generic sentence that the next release need not touch |
| Verifying `StartupWMClass` | Read `WM_CLASS` off the running window with `xprop` | Proves the value rather than assuming Avalonia's default |

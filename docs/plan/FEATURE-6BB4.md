# FEATURE-6BB4 — Settings & Help menus

**Status:** DONE — see `docs/done/FEATURE-6BB4.md`
**Type:** FEATURE
**Branch:** `feature/feature-6bb4-settings-help-menus`
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

Complete the menu bar the draft lists (File, Edit, View, Settings, Tools, …):

- a **Settings** menu that changes and persists the editor's preferences;
- a **Help** menu with an About dialog;
- font zoom in the View menu.

## Context & constraints

- `AppSettings` (FEATURE-55D7 PHASE01) already persists these through Enigma.Configuration
  (`IConfig<AppSettings>.Value` + `WriteAsync`): Theme, VimEnabled, WordWrap, EditorFontSize,
  DefaultIndentation, DefaultLineEnding.
- `IThemeService` maps the theme to a `ThemeVariant`.
- `VimSession` reads `VimEnabled`.
- New buffers use the default line ending; inconclusive indentation detection falls back to the
  default indentation.

## Decisions

| Decision | Rationale |
|---|---|
| Settings menu with direct, checkable items, no preferences dialog: *Theme ▸ System / Light / Dark*, *Vim mode* ✓, *Word wrap* ✓, *Default indentation ▸ Spaces: 2 / Spaces: 4 / Tabs*, *Default line endings ▸ LF / CRLF* | Every preference is a toggle or a short choice; a dialog would add a screen for no gain |
| Each change applies live and is written immediately (`WriteAsync`). A write failure is reported once through the message service and logged; the session keeps the value | Settings survive a crash; a read-only config folder does not break editing |
| View menu: *Zoom in* (Ctrl+=), *Zoom out* (Ctrl+-), *Reset zoom* (Ctrl+0) → `EditorFontSize` within 6–72, default 14; persisted | Font size without a dialog; the gestures pass through Vim (Ctrl) |
| Editor font family fixed to a monospace fallback chain: `Cascadia Mono, Consolas, JetBrains Mono, DejaVu Sans Mono, Menlo, monospace` | Works on all three OSes without a font picker |
| Help menu: *About Enigma Text Editor* → ContentDialog with name, version, build revision when present, copyright; one Close button | The sibling apps' About content |
| Vim off: the engine detaches and the editor behaves as a plain editor; the mode chip and the search highlight hide | MarkdownEditor's behaviour |

## Steps

1. `SettingsMenuViewModel` (or members on `MainWindowViewModel`) exposing checkable state and the
   commands. A `ISettingsWriter` wraps `IConfig<AppSettings>.WriteAsync` with error reporting.
2. Theme: `IThemeService.Apply(theme)`. Editor chrome and TextMate already follow the variant
   (FEATURE-3EE8 PHASE04).
3. Vim mode: `VimSession.OnSettingsChanged()`, and every pane's behavior refreshes.
4. Word wrap and font size: bound on every pane's editor.
5. Defaults: used by `DocumentRegistry.CreateUntitled` and by `IndentationDetector`'s fallback.
6. View menu zoom items.
7. Help → About: `AboutViewModel` (from `AppInfo` / assembly informational version), `AboutView` and
   `IAboutDialogService`.
8. Tests:
   - each setting toggles and persists (temp settings file);
   - theme maps to its variant;
   - Vim off detaches (keys type text);
   - zoom clamps at 6 and 72;
   - a new buffer uses the defaults;
   - a write failure is reported once;
   - About view-model fields;
   - headless: the menus contain the decided items.

## Acceptance criteria

- Every Settings item applies immediately, survives a restart and shows its current state as a
  check or radio mark.
- Zoom in, out and reset work in every pane and persist.
- Help → About shows the product name and version.
- The menu bar reads File · Edit · View · Tools · Settings · Help.
- Zero warnings, whole suite green.

## Out of scope

- A font-family picker, keymap customization, `.vimrc` loading.
- Per-language settings.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Settings UI | Direct menu items | Every setting is a toggle or short choice; zero extra screens | A Settings dialog (MarkdownEditor style; more code, same result) |
| When settings are written | Immediately on change | Nothing to lose on crash | On exit only (lost on crash) |
| Font control | Zoom commands + fixed monospace chain | Cross-platform, no picker | Font picker (needs a font catalog; not asked) |
| Menu order | File · Edit · View · Tools · Settings · Help | Conventional, with the draft's menus all present | The draft's literal order (Settings before Tools is unusual) |

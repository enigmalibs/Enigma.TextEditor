# BUG-78F9 — Readable colours in the dark theme

**Status:** DONE
**Type:** BUG
**Branch (PHASE01):** `bugfix/bug-78f9-phase01-link-colours`
**Branch (PHASE02):** `bugfix/bug-78f9-phase02-hints-hex-row`
**Run:** bugfix/2026-10-27-link-colours-release

## Objective

- URLs and e-mail addresses in the editor are hard to read in the dark theme. Paint them a lighter blue that suits
  the Enigma palette.
- Check the other colours the dark theme puts on screen, and fix the ones that are just as hard to read or clash
  with the palette.

## Context & constraints

- **Where the URL colour comes from.** AvaloniaEdit turns URLs and e-mail addresses into links
  (`TextEditorOptions.EnableHyperlinks` and `EnableEmailHyperlinks`, both on by default) and paints them with
  `TextView.LinkTextForegroundBrush`. Nothing in the app sets it, so it is AvaloniaEdit 12.0.0's default,
  `Brushes.Blue` (`#0000FF`), checked by reading the property's metadata.
  - Dark: `#0000FF` on the editor's `#1E1F22` is **1.92:1** (WCAG AA wants 4.5:1), and 1.54:1 on the current-line
    fill `#2E3035`.
  - Light: `#0000FF` is readable (8.1:1). It is, though, Light+'s keyword colour, so a URL in a comment looks like
    a keyword with an underline.
- **How the editor is painted.** `Views/EditorAppearance.cs` assigns AvaloniaEdit's plain brush properties (text,
  line numbers, caret, selection, current line) from palette keys for the variant on screen. It does this in its
  constructor and again in `Apply(variant)` on every theme switch. A key that does not resolve leaves the property
  as it was.
- **App-level colours** that are not in the Enigma palette already live in `App.axaml`, as theme-dictionary
  brushes with a `Dark` and a `Light` entry (`MinimapViewportBrush`, `MinimapViewportBorderBrush`). Both variants
  must define the same keys, or a key resolves to nothing after a switch.
- **The audit.** It used headless Skia renders of the main window, the hex view and the settings page in both
  variants, plus WCAG contrast ratios on the palette (`Enigma.Avalonia.Desktop` 1.2.0, `Themes/Colors.axaml`).
  Two more findings:
  - **Settings hint lines.** `SettingsPageView.axaml`'s `TextBlock.preferenceHint` (11 px) uses
    `EnigmaForegroundTertiaryBrush`. Dark: 1.74:1 on the card surface, 1.90:1 on the expanded content.
    Light: 2.15–2.27:1. The palette reserves the tertiary tone for placeholders and watermarks. Descriptions and
    captions are `EnigmaForegroundSecondaryBrush`, which `SettingsCardExpander` uses for its own description line.
    That tone gives 2.90–3.18:1 in dark and 4.06–4.29:1 in light.
  - **The hex view's selected row** is Fluent's `ListBoxItem:selected` fill. It is the operating system's accent,
    a saturated blue band in dark, unlike the editor's selection and the rest of the palette. The editor's tab
    strip already restyles its `ListBoxItem:selected` the same way (`EditorPaneView.axaml`).
- **Checked and left alone:**
  - search highlight (accent at 35% / 75%);
  - selection `#214283` (5.2:1 under the text);
  - current line;
  - line numbers (tertiary, deliberately quiet, JetBrains' look);
  - the empty-pane placeholder (the tertiary tone's own use);
  - the minimap's viewport wash;
  - About's build line (deliberately quiet, by its own comment);
  - the palette's secondary tone itself, about 3:1 in dark. That is `Enigma.Avalonia.Desktop`'s palette, not this
    app's.

## Decisions

- Links get one theme-dictionary brush, `EditorLinkBrush`, in `App.axaml`: **`#7AABFF`** in Dark, **`#1F5FCC`** in
  Light. `EditorAppearance` assigns it to `TextView.LinkTextForegroundBrush` along with the other chrome brushes,
  so it follows a theme switch.
  - Dark `#7AABFF`: hue 218°, the Enigma accent's own (`#3574F0`, 220°), lighter. 7.14:1 on the background, 5.72:1
    on the current line. It stays clear of Dark+'s keyword blue `#569CD6` and variable cyan `#9CDCFE`.
  - Light `#1F5FCC`: the same hue. 5.54:1 on the background, 4.88:1 on the current line. Unlike `#0000FF`, it does
    not match Light+'s keywords.
- Settings hints use `EnigmaForegroundSecondaryBrush`.
- The hex view's selected row is `EnigmaSelectionBrush`, the editor's own selection colour, in both variants.

## PHASE01 — Link colours

**Status:** DONE

### Steps

1. `App.axaml`: add `EditorLinkBrush` to the `Dark` and `Light` theme dictionaries beside the minimap brushes, with
   a comment saying what it paints and why these values.
2. `EditorAppearance`: a `LinkKey` constant. `ApplyChrome` assigns the brush to
   `_editor.TextArea.TextView.LinkTextForegroundBrush` through `Use`. Update the class remarks.
3. Tests in `EditorAppearanceTests`:
   - In each variant, the link brush is the `EditorLinkBrush` of that variant. It is not AvaloniaEdit's default
     blue. Its contrast against the variant's `EnigmaBackgroundColor` is at least 4.5:1.
   - `Apply` to the other variant repaints the links.

### Acceptance criteria

- In Dark, URLs and e-mail addresses are painted `#7AABFF`; in Light, `#1F5FCC`. Each follows a theme switch
  without a restart.
- Each variant's link colour is at least 4.5:1 against the editor background, asserted by a test.
- Build clean with zero warnings; whole suite green.

## PHASE02 — Settings hints and hex row

**Status:** DONE

### Steps

1. `SettingsPageView.axaml`: `TextBlock.preferenceHint` uses `EnigmaForegroundSecondaryBrush`.
2. `HexView.axaml`: style `ListBox#Rows ListBoxItem:selected /template/ ContentPresenter` with
   `Background = {DynamicResource EnigmaSelectionBrush}`, following the tab strip's precedent.
3. Tests:
   - On the shown settings page, every `preferenceHint` text block's foreground is the secondary brush.
   - In a hex tab, the selected row's content presenter is filled with the selection brush.

### Acceptance criteria

- On the settings page, every hint line, including the header's "Changes apply at once…", is painted in the
  secondary tone.
- The hex view's selected row is the palette's selection colour in both variants, whatever the OS accent.
- Build clean with zero warnings; whole suite green.

## Out of scope

- Changing the `Enigma.Avalonia.Desktop` palette, the TextMate Dark+ / Light+ token colours, the search highlight
  or the minimap.
- Turning hyperlinks off, or changing how they are clicked (Ctrl+click, AvaloniaEdit's default).
- A user setting for link colours.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Item type | BUG, two phases | 1.9:1 text is a readability defect; links and the audit's findings are separately reviewable | FEATURE; one phase for everything |
| Dark link colour | `#7AABFF` | Lighter blue on the accent's hue; 7.1:1, clear of Dark+'s keyword and variable colours (compared in a render of six candidates) | `#548AF7` (JetBrains, 5.0:1, too close to `#569CD6`); `#8AB4F8` (drifts toward the variable cyan) |
| Light link colour | `#1F5FCC` | Same hue, 5.5:1, no longer the keyword colour; a brush key needs both variants anyway | Keeping `#0000FF`; `#2E55A3` (duller) |
| Where the colour lives | Theme-dictionary `EditorLinkBrush` in `App.axaml`, assigned by `EditorAppearance` | Same pattern as the minimap brushes and the other chrome brushes; follows a theme switch | A literal in code; a style on `TextView` |
| Settings hints | Secondary tone | The palette's tone for descriptions; tertiary is for placeholders | A new brush of the app's own |
| Hex selected row | `EnigmaSelectionBrush` | The editor's selection colour; independent of the OS accent; tab strip precedent | Leaving Fluent's accent; the hover brush (too faint for the only cursor the view has) |
| Search highlight, minimap, line numbers, About build line | Unchanged | Readable or deliberately quiet in the renders; changing them is design, not a fix | Retuning them in this item |
| Palette's secondary tone (~3:1 in dark) | Follow-up for `Enigma.Avalonia.Desktop` | The palette belongs to the library | Overriding palette keys in the app |

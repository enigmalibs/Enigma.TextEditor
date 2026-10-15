# FEATURE-08A7 — Application icon

**Status:** TODO
**Type:** FEATURE
**Branch:** `feature/feature-08a7-app-icon`
**Run:** vibe/2026-10-15-drop-icon-help

## Objective

Give the text editor an application icon in the style of the other Enigma applications. The icon
appears:

- on the Windows executable;
- on the running window, on every platform;
- in the About dialog.

## Context & constraints

- The family layout is Enigma.GitClient's, repeated by Enigma.FileExplorer (FEATURE-743A PHASE01):
  - `packaging/icon/<app>.svg` is the master for 48–256 px; `<app>-small.svg` is hinted for 16 and
    32 px. Each carries its regeneration recipe in a header comment.
  - `Assets/app.ico` holds six 32-bit PNG frames (16, 32, 48, 64, 128, 256), smallest first.
  - `Assets/app-256.png` is the 256 frame, byte for byte.
  - The csproj has `<ApplicationIcon>Assets/app.ico</ApplicationIcon>` and
    `<AvaloniaResource Include="Assets/**" />`.
  - The window has `Icon="/Assets/app.ico"`.
  - `AppIconTests` pins the frames, the 256 copy and the window icon.
- The family palette:
  - `#1E1F22` tile, 240 units, corner 48;
  - `#3574F0` blue, `#2558C4` darker blue;
  - `#3FB950` green.
- The text editor today has no icon and no `Assets/` folder.
- Its About dialog (FEATURE-6BB4) shows the name, version, build and copyright, without an icon.
- `rsvg-convert` is not installed. Svg.Skia is in the local NuGet cache; FileExplorer rendered its
  frames with it from a throwaway script, and nothing of it was committed.

## Decisions

| Decision | Rationale |
|---|---|
| Artwork: the family tile, a page in the family blue, a few text lines on it, and a green block cursor at the end of a line | Says "text editor with Vim" in the siblings' palette |
| Two SVG masters, `packaging/icon/enigma-text-editor.svg` and `enigma-text-editor-small.svg` (hinted for 16/32 px), each with the recipe in a header comment | The family layout |
| `Assets/app.ico` (six PNG frames, 16 → 256, smallest first) and `Assets/app-256.png` (the 256 frame, byte for byte) | The family layout |
| Wiring: `ApplicationIcon` and `AvaloniaResource` in the csproj, and `Icon="/Assets/app.ico"` on `MainWindow` | Avalonia convention: exe and running window are separate concerns |
| The About dialog shows the icon at 96 px above the product name | The siblings' About shows it; it uses `app-256.png` |
| Frames rendered with a throwaway local script; no renderer or package committed | No new dependency |

## Steps

1. Draw the two SVG masters under `packaging/icon/`.
2. Render the six frames to 32-bit RGBA PNGs. Pack them, unchanged and smallest first, as the PNG
   entries of `src/Enigma.TextEditor.Desktop/Assets/app.ico`, and copy the 256 frame to
   `Assets/app-256.png`.
3. `Enigma.TextEditor.Desktop.csproj`: `ApplicationIcon` and `<AvaloniaResource Include="Assets/**" />`.
4. `MainWindow.axaml`: `Icon="/Assets/app.ico"`.
5. `AboutView.axaml`: a 96 × 96 `Image` of `/Assets/app-256.png` above the product name.
6. Tests (`Desktop.UnitTests`):
   - `app.ico` holds six square 32-bit PNG frames, 16 to 256, and each PNG header agrees with its
     directory entry;
   - `app-256.png` is the largest frame byte for byte, and decodes as 256 × 256;
   - the main window has an icon;
   - the About view shows the icon.

## Acceptance criteria

- The built `.exe` carries the icon, and the running window shows it in its title bar and taskbar.
- The six frames are legible at their size, the cursor included at 16 px.
- The About dialog shows the icon above the name.
- Zero warnings (no AVLN warning), whole suite green.

## Out of scope

- Linux/macOS packaging, launcher entries and their icon copies.
- A splash screen.
- File-type associations.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| Visual motif | A page with text lines and a green block cursor | Distinguishes the editor from Git Client (graph) and File Explorer (folder); the cursor says Vim | A pen or pencil (generic); the letter "T" (no family precedent) |
| Asset layout | GitClient/FileExplorer's | Regenerating works the same way in every repository | A single `.ico` with no masters (no way to regenerate) |
| Icon in About | Yes, 96 px | Siblings do it; it gives `app-256.png` its use | Leave About unchanged (the 256 copy would be dead weight) |
| Rasterizer | Svg.Skia via a throwaway scratchpad script | Available offline in the local cache; FileExplorer precedent | Commit a renderer tool (new dependency, not asked) |

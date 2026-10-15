# FEATURE-55D7 — Text editor foundation

**Status:** IN PROGRESS
**Type:** FEATURE
**Branch:** `feature/feature-55d7-<phase>-<slug>` (one per phase)
**Run:** vibe/2026-10-15-text-editor-vim

## Objective

Lay the foundation of **Enigma.TextEditor**, a cross-platform Avalonia 12 text editor with Vim
bindings:

1. a solution that builds clean and runs an empty, themed application window;
2. the Vim engine of Enigma.MarkdownEditor, ported into this solution with its test suite;
3. the text-file layer every later feature reads and writes through: encodings, byte-order marks,
   line endings, binary sniffing and indentation detection.

## Context & constraints

- **Greenfield.** The repository holds only `README.md` (`# Enigma.TextEditor`) on `main`.
- **Sources reused** (sibling repositories, read-only for this run):
  - `Enigma.MarkdownEditor` @ `ee8f6e9` —
    - `src/Enigma.MarkdownEditor.Core/Vim/*`: the engine, about 9.5k lines behind `IVimBuffer`;
    - `tests/Enigma.MarkdownEditor.Core.UnitTests/Vim/*`: 575 tests;
    - `Core/Documents/{TextFileFormat,LineEndings,LineEndingStyle,DocumentContent,DocumentFile}`;
    - `Core/Configuration/{ConfigRecovery,ConfigLoadResult}` over Enigma.Configuration.
  - `Enigma.FileExplorer` @ `ce34411` — the house-compliant test setup: xunit.v3 4.x,
    `Microsoft.Testing.Extensions.CodeCoverage` and a hand-rolled `HeadlessAvaloniaFixture`.
- **The engine's single markdown dependency**: `VimEngine` calls `ListContinuation.OpenBelow/Above`
  for `o`/`O` (and their `.` replay), to carry a list marker onto the new line. A text editor has no
  list items.
- House conventions (skills `dotnet-solution-setup`, `dotnet-solution-config`, `git-repo-hygiene`,
  `avalonia`, `enigma-avalonia-desktop`, `communitytoolkit-mvvm`, `xunit-v3`, `dotnet-di-design`).

## Decisions

| Decision | Rationale |
|---|---|
| Projects: `src/Enigma.TextEditor.Core` (net10.0, no Avalonia), `src/Enigma.TextEditor.Desktop` (net10.0 WinExe), `tests/Enigma.TextEditor.Core.UnitTests`, `tests/Enigma.TextEditor.Desktop.UnitTests` | The sibling apps' shape. Core is app-internal, so it targets net10.0 alone (documented deviation, as in MarkdownEditor) |
| Avalonia 12.1.2 coupled set; AvaloniaEdit + AvaloniaEdit.TextMate 12.0.0; Enigma.Avalonia.Desktop 1.2.0; Enigma.Icons.Avalonia 1.0.0 | The exact set MarkdownEditor ships the editor with; 12.1.2 ≥ the 12.1.1 floor of Enigma.Avalonia.Desktop 1.2.0 |
| xunit.v3 4.0.2 + Microsoft.Testing.Extensions.CodeCoverage 18.12.0; Avalonia.Headless + Avalonia.Skia 12.1.2; FileExplorer-style `HeadlessAvaloniaFixture` | House xunit-v3 rule. Avalonia.Headless.XUnit 12.1.x pins xunit.v3 3.x, so it is not used |
| Microsoft.Extensions.Hosting 10.0.13, Enigma.Configuration 1.0.0, Enigma.Logging.NLog 1.0.0 | MarkdownEditor's composition root, settings and file log |
| Vim engine ported verbatim into `Enigma.TextEditor.Core.Vim`, with a provenance note in the folder | No cross-repo edits are allowed in this run and no shared package exists. Extracting a shared `Enigma.Vim` package is a follow-up |
| `o`/`O` carry the current line's leading whitespace instead of a markdown marker | AvaloniaEdit's default indentation strategy does the same on Enter, so `o` and Enter-in-insert agree |
| Binary sniff: a NUL byte in the first 8000 bytes, unless the bytes start with a UTF-16/UTF-32 BOM | git's heuristic, cheap and well understood |
| Encoding detection: BOM, then strict UTF-8, then Windows-1252 | UTF-8 is the modern default; 1252 decodes every byte, so a legacy file opens readably |
| Code pages through `CodePagesEncodingProvider` (in-box on .NET 10) | No new package |
| Product: display name "Enigma Text Editor", version 0.1.0, app-data folder `Enigma.TextEditor` | Sibling naming |

## PHASE01 — Solution scaffold & app shell

**Status:** DONE — see `docs/done/FEATURE-55D7-PHASE01.md`
**Branch:** `feature/feature-55d7-phase01-solution-scaffold`

### Steps

1. Root files, created only if missing, copied from the skill templates as LF:
   - `.gitignore`, `.gitattributes` (git-repo-hygiene);
   - `.editorconfig`, `Directory.Build.props` (authors "Josué Clément", 2026), `Directory.Packages.props`
     with the package groups above, `global.json` (SDK 10.0.100, `latestFeature`, MTP runner);
   - `LICENSE.md` (MIT), empty `RELEASENOTES.md`. `README.md` stays as it is.
2. `Enigma.TextEditor.slnx` with `/src/` and `/tests/` folders.
3. `Enigma.TextEditor.Core`:
   - `AppInfo` (display name, app-data folder name, version);
   - `Settings/AppSettings` (theme, Vim on/off, word wrap, font size, default indentation, default
     line endings), `AppSettingsValidator`, `AppPaths`;
   - `Configuration/ConfigRecovery` + `ConfigLoadResult` ported (quarantine an unreadable file).
4. `Enigma.TextEditor.Desktop`:
   - `Program` (classic desktop lifetime, `WithDeveloperTools` in Debug, Inter font);
   - `App.axaml` (FluentTheme, AvaloniaEdit theme include, Enigma Fluent dictionary);
   - `App.axaml.cs`: load the settings before the container, build the IHost, register the Enigma
     services, NLog file logging and the shell, register the three hosts and storage providers,
     apply the theme, hand over the window, flush and stop on exit.
   - `IThemeService`/`ThemeService` mapping `AppTheme` to a `ThemeVariant`.
5. `MainWindow` with native decorations, `DockPanel` top to bottom:
   - `Menu` holding *File → Exit*;
   - the content area (placeholder);
   - the status bar (placeholder);
   - the three Enigma hosts last.

   `MainWindowViewModel` holds the title and `ExitCommand`.
6. Tests:
   - Core: `AppInfo`, `AppSettings` defaults/validation, `ConfigRecovery` (valid file, corrupt file
     quarantined).
   - Desktop: `HeadlessAvaloniaFixture`; the composition root resolves `MainWindow` and
     `MainWindowViewModel`; the main window shows a menu and a status bar.

### Acceptance criteria

- `dotnet build Enigma.TextEditor.slnx` succeeds with zero warnings.
- `dotnet test --solution Enigma.TextEditor.slnx` passes.
- Running the Desktop project shows a native-titled window with a menu bar (*File → Exit* works),
  an empty content area and a status bar, in the system theme.

## PHASE02 — Vim engine port

**Status:** TODO
**Branch:** `feature/feature-55d7-phase02-vim-engine-port`

### Steps

1. Copy `Enigma.MarkdownEditor.Core/Vim/*.cs` to `src/Enigma.TextEditor.Core/Vim/` and rename the
   namespace to `Enigma.TextEditor.Core.Vim`. Add a `README.md` in that folder naming the source
   repository and commit.
2. Replace the two `ListContinuation` calls with a private `LeadingWhitespace(line)` helper, so `o`/`O`
   (and the `.` replay of either) open the new line at the current line's indentation.
3. Copy the Vim tests to `tests/Enigma.TextEditor.Core.UnitTests/Vim/` with the namespace renamed.
   Adapt the markdown cases in `VimOpenLineTests`/`VimInsertTests` to the indentation rule:
   - plain line → no prefix;
   - indented line → same indentation;
   - a `- ` item is just text, so it gets no marker.
4. Add `InternalsVisibleTo` for the test project only if a ported test needs internals; otherwise
   leave visibility exactly as ported.

### Acceptance criteria

- Every ported Vim test passes. The only changed assertions are the markdown-continuation cases,
  rewritten to the indentation rule.
- New tests: `o` and `O` keep indentation (spaces and tabs), and `3o` writes the indentation on each
  copy.
- Build has zero warnings; no markdown type remains under `Core/Vim`.

## PHASE03 — Text file I/O

**Status:** TODO
**Branch:** `feature/feature-55d7-phase03-text-file-io`

### Steps

1. `Core/Documents` — port `LineEndingStyle`, `LineEndings`, `TextFileFormat`, `DocumentContent`,
   `DocumentFile` (namespace renamed).
2. `TextEncodings` — the curated list of supported encodings, each with a display name and the
   status-bar label:
   - UTF-8, UTF-8 with BOM, UTF-16 LE, UTF-16 BE;
   - Windows-1252, ISO-8859-1, ISO-8859-15, Windows-1250, Windows-1251.

   Register `CodePagesEncodingProvider` once. Add a lookup from a `TextFileFormat` back to its entry.
3. `ContentSniffer`:
   - `IsBinary(ReadOnlySpan<byte>)` (NUL in the first 8000 bytes, BOM exemption);
   - `DetectEncoding(bytes)` → BOM, then strict UTF-8 validation, then Windows-1252.
4. `DocumentFile`:
   - `ReadAsync(path, encodingOverride?)` decodes with the detected encoding or the override.
   - `WriteAsync` writes with the format's encoding and BOM, after normalizing the text and applying
     the line-ending style.
   - The write first checks the text is representable (`EncoderExceptionFallback`). If it is not, it
     throws `UnrepresentableTextException`, naming the first offending character and its line, and
     writes nothing.
   - Keep the 32 MB text cap (`MaximumBytes`), as a `FileTooLargeException` the UI can offer a
     fallback for.
5. `IndentationDetector.Detect(text, fallback)` → `Indentation(UseTabs, Size)`:
   - tab-led lines vs space-led lines;
   - for spaces, the most common positive indent delta, snapped to 2 or 4;
   - `fallback` when the file is inconclusive.
6. Tests for each, with temp files for read/write round trips (BOM kept/dropped, CRLF kept, Latin-1
   fallback, unrepresentable refusal, binary sniffing, UTF-16 with NULs not binary).

### Acceptance criteria

- Round trips preserve encoding, BOM and line-ending style byte for byte for UTF-8, UTF-8 BOM, UTF-16
  LE/BE and Windows-1252.
- Invalid UTF-8 opens as Windows-1252.
- A file with a NUL in its first 8000 bytes is binary; a UTF-16 file is not.
- Writing "日本" as Windows-1252 throws `UnrepresentableTextException`; the target file is untouched.
- Indentation detection: 2-space, 4-space, tabs, and the inconclusive fallback.
- Zero warnings, whole suite green.

## Out of scope

- Packaging/installers, app icon, README content (release time).
- Any change to Enigma.MarkdownEditor or Enigma.FileExplorer.

## Decisions taken autonomously

| Question | Chosen | Why | Alternatives rejected |
|---|---|---|---|
| How to reuse MarkdownEditor's Vim engine | Port the source into this repo | Nothing may be written outside this repo during the run; the engine is self-contained behind `IVimBuffer` | Shared NuGet package (right long term, but it is a separate work item in another repo); cross-repo `ProjectReference` (breaks clones and CI) |
| What replaces markdown list continuation in `o`/`O` | Carry the leading whitespace | Matches AvaloniaEdit's Enter; a plain text editor has no list semantics | No prefix at all (Enter and `o` would disagree); keep markdown continuation (wrong for code) |
| Test stack | xunit.v3 4.x + FileExplorer's headless fixture | House xunit-v3 skill; FileExplorer proves it with Avalonia 12 | MarkdownEditor's xunit.v3 3.2.2 + coverlet + Headless.XUnit (violates house rules) |
| Avalonia version | 12.1.2 | The set AvaloniaEdit 12.0.0 is proven with in MarkdownEditor | 12.1.1 (FileExplorer, no editor); 12.1.3 (unproven with AvaloniaEdit) |
| Settings storage | Enigma.Configuration + ported quarantine recovery | MarkdownEditor's mechanism, a house package | FileExplorer's hand-rolled JSON store (more code to own) |
| Fallback encoding | Windows-1252 | Decodes every byte and is the common legacy encoding | ISO-8859-1 (no €, curly quotes); UTF-8 with replacement characters (loses bytes on save) |
| Binary heuristic | NUL in the first 8000 bytes | git's rule; no false positives on text | Ratio of control characters (tunable, more false positives) |
| Text size cap | 32 MB | MarkdownEditor's proven limit for AvaloniaEdit | No cap (the UI freezes on huge files) |

# Security Policy

Enigma.TextEditor is a desktop text editor that opens whatever file it is given — text, binary, or neither — decodes
it, colours it with syntax grammars, and writes it back over the original. A defect in it could let a crafted file
crash or hang the editor, show you something other than what the file holds, or corrupt a file when it is saved.
Vulnerability reports are taken seriously and handled with priority.

## Supported versions

Security fixes are provided for the latest released version. Enigma.TextEditor follows
[Semantic Versioning](https://semver.org/), and users are encouraged to stay current with the newest release.

| Version | Supported          |
|---------|--------------------|
| 1.4.x   | :white_check_mark: |
| 1.3.x   | :x:                |
| 1.2.x   | :x:                |
| 1.1.x   | :x:                |
| 1.0.x   | :x:                |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**
Public disclosure before a fix is available puts every user at risk.

Instead, use **GitHub's private vulnerability reporting**:

1. Go to the repository's **Security** tab.
2. Select **Report a vulnerability** to open a private advisory.
3. Include as much detail as you can — the affected version, the component involved, a description of the issue,
   and, where possible, a minimal reproduction and its impact.

This keeps the report private between you and the maintainers while it is triaged and fixed.

## What to expect

- Your report will be acknowledged and triaged as promptly as possible.
- The issue will be investigated and, once confirmed, a fix prepared and released.
- Coordinated disclosure is preferred: please allow a reasonable period for a fix to ship before any public
  discussion of the vulnerability.
- Your contribution will be credited in the resulting advisory unless you ask to remain anonymous.

## Scope

The following are in scope:

- the way Enigma.TextEditor reads a file: the binary and size checks, the encoding detection and decoding, and the
  hex viewer;
- the way it writes a file back, in its encoding and with its line endings;
- the syntax highlighting it runs over a file's text;
- the Vim engine's handling of what is typed into it, including search and substitution patterns;
- the way it reads and writes its settings file and its log;
- the Linux install and uninstall scripts in `packaging/linux`: what they write into your home directory, what they
  remove, and the desktop entry they generate.

Issues rooted in Avalonia, AvaloniaEdit, TextMateSharp and its grammars, SkiaSharp or the .NET runtime should also be
reported to those projects.

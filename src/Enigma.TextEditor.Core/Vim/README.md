# Vim engine — provenance

This folder is a port of the Vim engine of **Enigma.MarkdownEditor**, taken at commit `ee8f6e9`
from `src/Enigma.MarkdownEditor.Core/Vim/`. Its tests were ported from
`tests/Enigma.MarkdownEditor.Core.UnitTests/Vim/` to `tests/Enigma.TextEditor.Core.UnitTests/Vim/`.

Differences from the source:

- The namespace is `Enigma.TextEditor.Core.Vim`.
- `o` and `O` (and their `.` and count replays) carry the current line's **leading whitespace**
  onto the new line. They no longer carry a markdown list marker, so `ListContinuation` is not a
  dependency here.

Later work items in this repository extend the engine: shared state across panes, window commands,
more ex commands, and search highlighting. See `docs/roadmap.md`. A fix that also applies to the
original belongs in Enigma.MarkdownEditor too. Moving the engine into a shared package would end
the duplication; that is a follow-up.

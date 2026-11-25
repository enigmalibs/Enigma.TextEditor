using System;
using System.IO;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// The unsaved text of one buffer, to be written to a file of its own beside the session: its name, and how to write it.
/// </summary>
/// <remarks>
/// <para>
/// A writer rather than a string, so that the text need not be copied to be written: the caller hands over whatever
/// holds it — an immutable snapshot of the buffer — and it is written straight from there, away from the thread that
/// edits the buffer.
/// </para>
/// <para>
/// A name stands for one text and never another: a buffer whose text changes gets a new name. A file of that name
/// already beside the session is therefore not written again.
/// </para>
/// </remarks>
/// <param name="FileName">
/// The file's name in the session's unsaved-text folder, as <see cref="SessionDocument.UnsavedTextFile"/> names it; a
/// plain name that <see cref="SessionFile.IsTextFileName"/> accepts.
/// </param>
/// <param name="WriteTo">Writes the text, with line feeds alone, as the editor holds it.</param>
public sealed record SessionText(string FileName, Action<TextWriter> WriteTo);

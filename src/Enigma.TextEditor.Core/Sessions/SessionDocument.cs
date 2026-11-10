using Enigma.TextEditor.Core.Documents;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// One buffer of a saved session: which file it was, how it is written, and — when it held changes its
/// file does not — the text itself.
/// </summary>
/// <remarks>
/// A buffer with no <see cref="UnsavedText"/> is read again from its file when restored, so that a change
/// made to the file while the editor was closed shows. The format and the indentation are kept all the
/// same: a file reopened in another encoding, or set to other indentation, comes back that way.
/// </remarks>
/// <param name="FilePath">The file's full path, or <see langword="null"/> for an untitled buffer.</param>
/// <param name="UntitledName">The name the buffer goes by while it has no file — <c>Untitled-1</c>.</param>
/// <param name="IsHex">Whether this was the read-only hex view of the file.</param>
/// <param name="CodePage">The encoding's code page.</param>
/// <param name="ByteOrderMark">Whether the text is written with a byte-order mark.</param>
/// <param name="LineEndings">The line terminator the text is written with.</param>
/// <param name="IndentWithTabs">Whether Tab and the shifts insert tabs.</param>
/// <param name="IndentSize">How many columns one level of indentation is.</param>
/// <param name="Language">The language the user chose, by id; <see langword="null"/> when the file's name decides.</param>
/// <param name="UnsavedText">
/// The text, when the buffer held changes its file does not — or any text, for an untitled buffer; with line
/// feeds alone, as the editor holds it. <see langword="null"/> when the file is to be read again.
/// </param>
public sealed record SessionDocument(
    string? FilePath,
    string UntitledName,
    bool IsHex = false,
    int CodePage = TextEncodings.Utf8CodePage,
    bool ByteOrderMark = false,
    LineEndingStyle LineEndings = LineEndingStyle.Lf,
    bool IndentWithTabs = false,
    int IndentSize = 4,
    string? Language = null,
    string? UnsavedText = null);

using System;
using System.Text;
using System.Text.Unicode;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// Looks at a file's bytes and says what it is: text or binary, and if text, in which encoding.
/// </summary>
public static class ContentSniffer
{
    /// <summary>
    /// How many leading bytes are looked at to decide whether a file is binary.
    /// </summary>
    /// <remarks>
    /// git's number, for git's reason: a text file has no NUL byte anywhere, and a binary file almost
    /// always has one near the start, in its header.
    /// </remarks>
    public const int BinaryProbeLength = 8000;

    /// <summary>
    /// Says whether a file looks binary.
    /// </summary>
    /// <param name="bytes">The file's first bytes — <see cref="BinaryProbeLength"/> of them, or all if shorter.</param>
    /// <returns>
    /// <see langword="true"/> when a NUL byte appears in the probe, unless the file starts with a
    /// UTF-16 or UTF-32 byte-order mark, whose text is full of NULs.
    /// </returns>
    public static bool IsBinary(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> probe = bytes.Length > BinaryProbeLength ? bytes[..BinaryProbeLength] : bytes;

        if (probe.IndexOf((byte)0) < 0)
        {
            return false;
        }

        Encoding marked = TextFileFormat.DetectEncoding(probe, out bool hasMark, out _);

        return !(hasMark && (marked is UnicodeEncoding or UTF32Encoding));
    }

    /// <summary>
    /// Works out which encoding a text file is in.
    /// </summary>
    /// <param name="bytes">The whole file.</param>
    /// <param name="preambleLength">Set to how many leading bytes are a byte-order mark rather than text.</param>
    /// <returns>
    /// The marked encoding when there is a byte-order mark; otherwise UTF-8 when the bytes are valid
    /// UTF-8 — which plain ASCII always is — and Windows-1252 when they are not.
    /// </returns>
    /// <remarks>
    /// Windows-1252 rather than a guess from byte statistics: it decodes every byte, so a legacy file
    /// opens readable and is written back byte for byte, and it is the legacy encoding most such
    /// files are in. Reopening with another encoding is the user's way out when it is not.
    /// </remarks>
    public static TextEncoding DetectEncoding(ReadOnlySpan<byte> bytes, out int preambleLength)
    {
        Encoding marked = TextFileFormat.DetectEncoding(bytes, out bool hasMark, out preambleLength);

        if (hasMark)
        {
            return TextEncodings.For(new TextFileFormat(marked, true, LineEndings.Default));
        }

        return Utf8.IsValid(bytes) ? TextEncodings.Utf8 : TextEncodings.Windows1252;
    }
}

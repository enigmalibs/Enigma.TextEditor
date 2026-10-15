using System;
using System.Text;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// One encoding the editor can read and write a file in, as the user sees it.
/// </summary>
/// <param name="Label">The short name the status bar shows — <c>UTF-8</c>, <c>Windows-1252</c>.</param>
/// <param name="DisplayName">The longer name a picker shows — <c>Western European (Windows-1252)</c>.</param>
/// <param name="CodePage">The code page that identifies the encoding.</param>
/// <param name="WithByteOrderMark">Whether a file in this encoding starts with a byte-order mark.</param>
/// <remarks>
/// UTF-8 is two entries — with and without a mark — because the mark is the one difference between
/// two files a user can choose between. UTF-16 is always written with a mark: a UTF-16 file without
/// one cannot be told from binary by anything that opens it later.
/// </remarks>
public sealed record TextEncoding(string Label, string DisplayName, int CodePage, bool WithByteOrderMark)
{
    /// <summary>
    /// Builds the <see cref="Encoding"/> this entry stands for, configured to emit — or not — its mark.
    /// </summary>
    /// <returns>A new encoding instance.</returns>
    public Encoding CreateEncoding()
    {
        TextEncodings.EnsureCodePages();

        return CodePage switch
        {
            TextEncodings.Utf8CodePage => new UTF8Encoding(encoderShouldEmitUTF8Identifier: WithByteOrderMark),
            TextEncodings.Utf16LeCodePage => new UnicodeEncoding(bigEndian: false, byteOrderMark: WithByteOrderMark),
            TextEncodings.Utf16BeCodePage => new UnicodeEncoding(bigEndian: true, byteOrderMark: WithByteOrderMark),
            TextEncodings.Utf32LeCodePage => new UTF32Encoding(bigEndian: false, byteOrderMark: WithByteOrderMark),
            TextEncodings.Utf32BeCodePage => new UTF32Encoding(bigEndian: true, byteOrderMark: WithByteOrderMark),
            _ => Encoding.GetEncoding(CodePage),
        };
    }

    /// <summary>
    /// Gets the bytes a file in this encoding starts with, or none.
    /// </summary>
    public ReadOnlySpan<byte> Preamble => WithByteOrderMark ? CreateEncoding().Preamble : [];
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// The encodings the editor offers, and the way back from a file's format to one of them.
/// </summary>
/// <remarks>
/// A curated list rather than every code page .NET knows: Unicode, and the legacy encodings a Western,
/// Central-European or Cyrillic text file is likely to be in. The single-byte code pages come from
/// <see cref="CodePagesEncodingProvider"/>, which ships with .NET and only has to be registered.
/// </remarks>
public static class TextEncodings
{
    /// <summary>
    /// UTF-8's code page.
    /// </summary>
    public const int Utf8CodePage = 65001;

    /// <summary>
    /// UTF-16 little-endian's code page.
    /// </summary>
    public const int Utf16LeCodePage = 1200;

    /// <summary>
    /// UTF-16 big-endian's code page.
    /// </summary>
    public const int Utf16BeCodePage = 1201;

    /// <summary>
    /// UTF-32 little-endian's code page.
    /// </summary>
    public const int Utf32LeCodePage = 12000;

    /// <summary>
    /// UTF-32 big-endian's code page.
    /// </summary>
    public const int Utf32BeCodePage = 12001;

    /// <summary>
    /// Windows-1252's code page — the legacy encoding a file that is not valid UTF-8 is read in.
    /// </summary>
    public const int Windows1252CodePage = 1252;

    private static int _registered;

    /// <summary>
    /// Gets UTF-8 without a byte-order mark: what a new file is written in.
    /// </summary>
    public static TextEncoding Utf8 { get; } = new("UTF-8", "Unicode (UTF-8)", Utf8CodePage, false);

    /// <summary>
    /// Gets UTF-8 with a byte-order mark.
    /// </summary>
    public static TextEncoding Utf8WithBom { get; } = new("UTF-8 with BOM", "Unicode (UTF-8 with BOM)", Utf8CodePage, true);

    /// <summary>
    /// Gets UTF-16 little-endian, with its mark.
    /// </summary>
    public static TextEncoding Utf16Le { get; } = new("UTF-16 LE", "Unicode (UTF-16 LE)", Utf16LeCodePage, true);

    /// <summary>
    /// Gets UTF-16 big-endian, with its mark.
    /// </summary>
    public static TextEncoding Utf16Be { get; } = new("UTF-16 BE", "Unicode (UTF-16 BE)", Utf16BeCodePage, true);

    /// <summary>
    /// Gets Windows-1252: the fallback for an unmarked file that is not valid UTF-8.
    /// </summary>
    public static TextEncoding Windows1252 { get; } = new("Windows-1252", "Western European (Windows-1252)", Windows1252CodePage, false);

    /// <summary>
    /// Gets every encoding the pickers offer, in the order they offer them.
    /// </summary>
    public static IReadOnlyList<TextEncoding> All { get; } =
    [
        Utf8,
        Utf8WithBom,
        Utf16Le,
        Utf16Be,
        Windows1252,
        new("ISO-8859-1", "Western European (ISO-8859-1)", 28591, false),
        new("ISO-8859-15", "Western European (ISO-8859-15)", 28605, false),
        new("Windows-1250", "Central European (Windows-1250)", 1250, false),
        new("Windows-1251", "Cyrillic (Windows-1251)", 1251, false),
    ];

    /// <summary>
    /// The entry a file's format is in.
    /// </summary>
    /// <param name="format">The format, as read from a file or chosen by the user.</param>
    /// <returns>
    /// The matching entry of <see cref="All"/>, or — for an encoding read from a mark but not offered,
    /// UTF-32 — an entry made up on the spot, so the status bar always has something to say.
    /// </returns>
    public static TextEncoding For(TextFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);

        int codePage = format.Encoding.CodePage;

        foreach (TextEncoding entry in All)
        {
            if ((entry.CodePage == codePage) && (entry.WithByteOrderMark == format.HasByteOrderMark))
            {
                return entry;
            }
        }

        foreach (TextEncoding entry in All)
        {
            // A UTF-16 file read without a mark: still the UTF-16 entry, which writes one.
            if (entry.CodePage == codePage)
            {
                return entry;
            }
        }

        return codePage switch
        {
            Utf32LeCodePage => new("UTF-32 LE", "Unicode (UTF-32 LE)", codePage, format.HasByteOrderMark),
            Utf32BeCodePage => new("UTF-32 BE", "Unicode (UTF-32 BE)", codePage, format.HasByteOrderMark),
            _ => new(format.Encoding.WebName.ToUpperInvariant(), format.Encoding.EncodingName, codePage, format.HasByteOrderMark),
        };
    }

    /// <summary>
    /// Registers the code-page provider, once per process, before a single-byte encoding is asked for.
    /// </summary>
    internal static void EnsureCodePages()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 0)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}

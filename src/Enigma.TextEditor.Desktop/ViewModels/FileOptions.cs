using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// <c>:set</c> of the options about the file — <c>fileformat</c>, <c>fileencoding</c>, <c>bomb</c> and
/// <c>filetype</c> — which the Vim engine hands on, because their values are the application's.
/// </summary>
/// <remarks>
/// <para>
/// Each is one of the status bar's items under Vim's name, and changes the buffer the way that item's
/// tool does: <c>ff</c> its line endings, <c>fenc</c> and <c>bomb</c> the encoding the next save writes
/// (Vim's <c>fileencoding</c>, not a re-read), <c>ft</c> its language.
/// </para>
/// <para>
/// The spellings are the engine's: <c>name=value</c> sets, <c>name?</c> or a bare <c>name</c> shows,
/// and <c>bomb</c> is a switch (<c>bomb</c>, <c>nobomb</c>, <c>bomb!</c>). The first bad word stops the
/// rest, with what came before it done — Vim's rule.
/// </para>
/// </remarks>
internal static class FileOptions
{
    private const string Invalid = "Invalid argument: ";

    // Vim's name first — what '?' shows — then the other spellings it is known by.
    private static readonly (int CodePage, string[] Names)[] EncodingNames =
    [
        (TextEncodings.Utf8CodePage, ["utf-8", "utf8"]),
        (TextEncodings.Utf16LeCodePage, ["utf-16le", "utf16le"]),
        (TextEncodings.Utf16BeCodePage, ["utf-16", "utf-16be", "utf16", "utf16be"]),
        (TextEncodings.Utf32LeCodePage, ["utf-32le"]),
        (TextEncodings.Utf32BeCodePage, ["utf-32"]),
        (TextEncodings.Windows1252CodePage, ["cp1252", "windows-1252"]),
        (28591, ["latin1", "iso-8859-1"]),
        (28605, ["iso-8859-15", "latin9"]),
        (1250, ["cp1250", "windows-1250"]),
        (1251, ["cp1251", "windows-1251"]),
    ];

    /// <summary>
    /// Sets or shows the file options a <c>:set</c> named.
    /// </summary>
    /// <param name="words">The words, as typed, separated by spaces.</param>
    /// <param name="document">The buffer the <c>:set</c> was typed in.</param>
    /// <param name="languages">The languages <c>filetype</c> can name.</param>
    /// <returns>What to show: the values asked for, or why a word was refused; <see langword="null"/> for nothing.</returns>
    public static string? Apply(string words, DocumentViewModel document, LanguageCatalog languages)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(languages);

        List<string> shown = [];

        foreach (string word in words.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            // The hex view shows bytes: it has no encoding, line endings or language to set.
            if (document.IsHex)
            {
                return $"Not available in the hex view: {word}";
            }

            if (!ApplyOne(word, document, languages, shown))
            {
                return Invalid + word;
            }
        }

        return shown.Count > 0 ? string.Join(' ', shown) : null;
    }

    private static bool ApplyOne(string word, DocumentViewModel document, LanguageCatalog languages, List<string> shown)
    {
        int equals = word.IndexOf('=', StringComparison.Ordinal);
        string name = equals >= 0 ? word[..equals] : word.TrimEnd('?', '!');
        string? value = equals >= 0 ? word[(equals + 1)..] : null;
        char suffix = equals >= 0 ? '=' : ((word.Length > name.Length) ? word[^1] : '\0');

        switch (name)
        {
            case "fileformat" or "ff":
                return Valued(suffix, () => shown.Add("fileformat=" + FormatName(document)), () => SetFormat(document, value!));

            case "fileencoding" or "fenc":
                return Valued(suffix, () => shown.Add("fileencoding=" + EncodingName(document.Encoding)), () => SetEncoding(document, value!));

            case "filetype" or "ft":
                return Valued(suffix, () => shown.Add("filetype=" + document.Language.Id), () => SetLanguage(document, value!, languages));

            case "bomb":
                switch (suffix)
                {
                    case '?':
                        shown.Add(document.Encoding.WithByteOrderMark ? "bomb" : "nobomb");
                        return true;

                    case '!':
                        return SetByteOrderMark(document, !document.Encoding.WithByteOrderMark);

                    case '\0':
                        return SetByteOrderMark(document, true);

                    default:
                        return false;
                }

            case "nobomb" when suffix == '\0':
                return SetByteOrderMark(document, false);

            default:
                return false;
        }
    }

    // An option with a value: '?' or nothing shows it, '=' sets it, and it cannot be flipped.
    private static bool Valued(char suffix, Action show, Func<bool> set)
    {
        switch (suffix)
        {
            case '?':
            case '\0':
                show();
                return true;

            case '=':
                return set();

            default:
                return false;
        }
    }

    private static string FormatName(DocumentViewModel document)
        => document.Format.LineEndings == LineEndingStyle.CrLf ? "dos" : "unix";

    // CR alone ('mac') is not a line ending the editor writes.
    private static bool SetFormat(DocumentViewModel document, string value)
    {
        LineEndingStyle? style = value.ToLowerInvariant() switch
        {
            "unix" => LineEndingStyle.Lf,
            "dos" => LineEndingStyle.CrLf,
            _ => null,
        };

        if (style is not { } lineEndings)
        {
            return false;
        }

        document.ChangeLineEndings(lineEndings);

        return true;
    }

    private static string EncodingName(TextEncoding encoding)
        => EncodingNames.FirstOrDefault(entry => entry.CodePage == encoding.CodePage).Names?[0]
           ?? encoding.Label.ToLowerInvariant();

    /// <summary>
    /// Sets the encoding the next save writes, keeping the byte-order mark as it is for UTF-8 —
    /// <c>fileencoding</c> and <c>bomb</c> are two options in Vim.
    /// </summary>
    private static bool SetEncoding(DocumentViewModel document, string value)
    {
        (int CodePage, string[] Names) named = EncodingNames.FirstOrDefault(
            entry => entry.Names.Contains(value, StringComparer.OrdinalIgnoreCase));

        if (named.Names is null)
        {
            return false;
        }

        bool keepMark = (document.Encoding.CodePage == TextEncodings.Utf8CodePage) && document.Encoding.WithByteOrderMark;
        TextEncoding? encoding = named.CodePage == TextEncodings.Utf8CodePage
            ? (keepMark ? TextEncodings.Utf8WithBom : TextEncodings.Utf8)
            : TextEncodings.All.FirstOrDefault(entry => entry.CodePage == named.CodePage);

        // Read from a file, never offered for writing: UTF-32.
        if (encoding is null)
        {
            return false;
        }

        document.ChangeEncoding(encoding);

        return true;
    }

    // UTF-8 has a mark or not; UTF-16 is always written with one here, and the single-byte encodings
    // have none to write.
    private static bool SetByteOrderMark(DocumentViewModel document, bool mark)
    {
        TextEncoding current = document.Encoding;

        if (current.WithByteOrderMark == mark)
        {
            return true;
        }

        if (current.CodePage != TextEncodings.Utf8CodePage)
        {
            return false;
        }

        document.ChangeEncoding(mark ? TextEncodings.Utf8WithBom : TextEncodings.Utf8);

        return true;
    }

    private static bool SetLanguage(DocumentViewModel document, string value, LanguageCatalog languages)
    {
        LanguageInfo? language = value.ToLowerInvariant() is "" or "text"
            ? LanguageInfo.PlainText
            : languages.Lookup(value);

        if (language is null)
        {
            return false;
        }

        document.ChooseLanguage(language);

        return true;
    }
}

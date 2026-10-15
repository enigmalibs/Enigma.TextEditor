using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// Reading a text file into the editor, and writing it back exactly as it was found.
/// </summary>
/// <remarks>
/// <para>
/// The pair is deliberately one class: a read that detects an encoding is only correct if the write
/// that mirrors it lives beside it, and splitting them is how the two drift apart.
/// </para>
/// <para>
/// The editor always holds text with line feeds alone. A read normalizes whatever the file used and
/// records it in the <see cref="TextFileFormat"/>; a write applies the recorded style again, so a file
/// keeps its endings — and a line typed into a CRLF file gets CRLF too.
/// </para>
/// </remarks>
public static class DocumentFile
{
    /// <summary>
    /// The largest file the editor opens as text.
    /// </summary>
    /// <remarks>
    /// A limit rather than no limit, because the whole file is held as one string and again as a
    /// document with a line index over it. Anything past it is offered to the hex viewer instead of
    /// being left to freeze the window.
    /// </remarks>
    public const long MaximumBytes = 32L * 1024 * 1024;

    /// <summary>
    /// Looks at a file without reading it whole: its size, and whether it looks binary.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <param name="cancellationToken">Abandons the read.</param>
    /// <returns>What the first <see cref="ContentSniffer.BinaryProbeLength"/> bytes say about it.</returns>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static async Task<FileProbe> ProbeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using FileStream stream = new(
            filePath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous,
            });

        byte[] head = new byte[ContentSniffer.BinaryProbeLength];
        int read = 0;

        while (read < head.Length)
        {
            int chunk = await stream.ReadAsync(head.AsMemory(read), cancellationToken).ConfigureAwait(false);

            if (chunk == 0)
            {
                break;
            }

            read += chunk;
        }

        return new FileProbe(stream.Length, ContentSniffer.IsBinary(head.AsSpan(0, read)));
    }

    /// <summary>
    /// Reads a text file, normalizing its line breaks and remembering how to write it back.
    /// </summary>
    /// <param name="filePath">The file to read.</param>
    /// <param name="encoding">
    /// The encoding to read it in — <em>Reopen with encoding</em> — or <see langword="null"/> to detect
    /// one: the byte-order mark, then UTF-8, then Windows-1252 (see <see cref="ContentSniffer"/>).
    /// </param>
    /// <param name="cancellationToken">Abandons the read.</param>
    /// <returns>The text with line feeds alone, plus the format it was stored in.</returns>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty or white-space.</exception>
    /// <exception cref="FileTooLargeException">The file is larger than <see cref="MaximumBytes"/>.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    /// <remarks>
    /// A forced encoding drops a byte-order mark of its own code page from the front of the text, so
    /// reopening a marked UTF-8 file as UTF-8 does not put an invisible character on its first line. A
    /// byte the forced encoding cannot decode becomes U+FFFD — reading never fails over the user's
    /// choice of encoding.
    /// </remarks>
    public static async Task<DocumentContent> ReadAsync(
        string filePath,
        TextEncoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FileInfo file = new(filePath);

        if (file.Exists && (file.Length > MaximumBytes))
        {
            throw new FileTooLargeException(filePath, file.Length, MaximumBytes);
        }

        // Read as bytes rather than through a StreamReader: the byte-order mark has to survive the
        // round trip, and a reader consumes it without ever saying what it was.
        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);

        return Decode(bytes, encoding);
    }

    /// <summary>
    /// Decodes a file's bytes the way <see cref="ReadAsync"/> does.
    /// </summary>
    /// <param name="bytes">The whole file.</param>
    /// <param name="encoding">The encoding to force, or <see langword="null"/> to detect one.</param>
    /// <returns>The text with line feeds alone, plus the format it was stored in.</returns>
    public static DocumentContent Decode(byte[] bytes, TextEncoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        int preambleLength;
        bool hasMark;

        if (encoding is null)
        {
            encoding = ContentSniffer.DetectEncoding(bytes, out preambleLength);
            hasMark = preambleLength > 0;
        }
        else
        {
            Encoding marked = TextFileFormat.DetectEncoding(bytes, out bool fileHasMark, out int markLength);
            preambleLength = fileHasMark && (marked.CodePage == encoding.CodePage) ? markLength : 0;
            hasMark = encoding.WithByteOrderMark;
        }

        Encoding decoder = encoding.CreateEncoding();
        string raw = decoder.GetString(bytes, preambleLength, bytes.Length - preambleLength);

        return new DocumentContent(
            LineEndings.Normalize(raw),
            new TextFileFormat(decoder, hasMark, LineEndings.Detect(raw)));
    }

    /// <summary>
    /// Writes text to a file in the given format: its encoding, its byte-order mark and its line
    /// terminator.
    /// </summary>
    /// <param name="filePath">The file to write; created if it does not exist.</param>
    /// <param name="text">The text, with any line breaks — they are normalized and re-applied.</param>
    /// <param name="format">How to store it.</param>
    /// <param name="cancellationToken">Abandons the write.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    /// <exception cref="UnrepresentableTextException">
    /// A character of <paramref name="text"/> does not exist in the format's encoding. Nothing is written.
    /// </exception>
    /// <exception cref="IOException">The file could not be written.</exception>
    public static async Task WriteAsync(
        string filePath,
        string text,
        TextFileFormat format,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(format);

        byte[] bytes = Encode(text, format);

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Encodes text the way <see cref="WriteAsync"/> writes it.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="format">How to store it.</param>
    /// <returns>The file's bytes, byte-order mark included.</returns>
    /// <exception cref="UnrepresentableTextException">A character does not exist in the format's encoding.</exception>
    public static byte[] Encode(string text, TextFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(format);

        string normalized = LineEndings.Normalize(text);

        // A copy that throws instead of writing '?': the format's own encoding is shared and keeps the
        // framework's default fallback.
        Encoding strict = (Encoding)format.Encoding.Clone();
        strict.EncoderFallback = EncoderFallback.ExceptionFallback;

        try
        {
            byte[] body = strict.GetBytes(LineEndings.Apply(normalized, format.LineEndings));
            ReadOnlySpan<byte> preamble = format.HasByteOrderMark ? format.Encoding.Preamble : [];

            return [.. preamble, .. body];
        }
        catch (EncoderFallbackException)
        {
            throw Unrepresentable(normalized, strict, TextEncodings.For(format).Label);
        }
    }

    /// <summary>
    /// Finds the first character that cannot be encoded, on the normalized text, so that its line and
    /// column are the ones the editor shows.
    /// </summary>
    private static UnrepresentableTextException Unrepresentable(string normalized, Encoding strict, string label)
    {
        int line = 1;
        int lineStart = 0;

        for (int index = 0; index < normalized.Length; index++)
        {
            int length = char.IsHighSurrogate(normalized[index]) && ((index + 1) < normalized.Length) ? 2 : 1;

            try
            {
                _ = strict.GetByteCount(normalized.AsSpan(index, length));
            }
            catch (EncoderFallbackException)
            {
                return new UnrepresentableTextException(normalized.Substring(index, length), line, (index - lineStart) + 1, label);
            }

            if (normalized[index] == '\n')
            {
                line++;
                lineStart = index + 1;
            }

            index += length - 1;
        }

        // The whole text failed but no single character did — a surrogate split across the scan. Point
        // at the start rather than claiming a place that is not wrong.
        return new UnrepresentableTextException("?", 1, 1, label);
    }
}

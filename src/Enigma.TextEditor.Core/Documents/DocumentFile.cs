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
/// <para>
/// A write never truncates the file before the new bytes are safe: they go to a temporary file beside it, flushed to
/// the disk, which is then moved over it (see <see cref="WriteBytesAsync(string, ReadOnlyMemory{byte}, CancellationToken)"/>).
/// A full disk, a dropped network share or a crash half-way leaves the file as it was, rather than empty or cut short.
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
    /// <exception cref="IOException">The file could not be written; it is left as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The file may not be written.</exception>
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

        await WriteBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a file's bytes — as <see cref="Encode"/> made them — without ever leaving the file empty or cut short.
    /// </summary>
    /// <param name="filePath">The file to write; created if it does not exist.</param>
    /// <param name="bytes">The whole file.</param>
    /// <param name="cancellationToken">Abandons the write; the file is left as it was.</param>
    /// <returns>A task that completes when the file has been written and is on the disk.</returns>
    /// <exception cref="IOException">The file could not be written; it is left as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The file may not be written.</exception>
    /// <remarks>
    /// <para>
    /// The bytes go to a temporary file in the file's own folder, flushed to the disk, which is then moved over the
    /// file: until that move, the file is untouched. A symbolic link is followed, so the file it points at is written
    /// and the link stays a link. The file's attributes and creation time (Windows) or its permissions (Linux, macOS)
    /// are carried over to the new one.
    /// </para>
    /// <para>
    /// A file that cannot be written is refused as a write in place would refuse it — read-only, or held by another
    /// program — before anything is written. When the folder takes no new file, or the file cannot be moved over while
    /// it can be written, the bytes are written in place, as before: a file that could be saved can still be saved,
    /// only without that protection.
    /// </para>
    /// </remarks>
    public static Task WriteBytesAsync(string filePath, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        => WriteBytesAsync(filePath, bytes, static (stream, data, token) => stream.WriteAsync(data, token), cancellationToken);

    /// <summary>
    /// <see cref="WriteBytesAsync(string, ReadOnlyMemory{byte}, CancellationToken)"/> with the step that puts the bytes
    /// into the file handed in — for a test to make it fail half-way.
    /// </summary>
    /// <param name="filePath">The file to write.</param>
    /// <param name="bytes">The whole file.</param>
    /// <param name="write">Puts the bytes into a stream.</param>
    /// <param name="cancellationToken">Abandons the write.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    internal static async Task WriteBytesAsync(
        string filePath,
        ReadOnlyMemory<byte> bytes,
        Func<Stream, ReadOnlyMemory<byte>, CancellationToken, ValueTask> write,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(write);

        string target = FinalTarget(Path.GetFullPath(filePath));
        bool exists = File.Exists(target);

        if (exists)
        {
            // Refused here as the write in place would have refused it, before anything is written.
            ProbeWritable(target);
        }

        string temporary = Path.Combine(
            Path.GetDirectoryName(target) ?? string.Empty,
            $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");
        FileStream stream;

        try
        {
            stream = new FileStream(
                temporary,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous,
                });
        }
        catch (UnauthorizedAccessException)
        {
            // The folder takes no new file, though the file may be written: in place, as before.
            await WriteInPlaceAsync(target, bytes, write, cancellationToken).ConfigureAwait(false);

            return;
        }

        bool moved = false;

        try
        {
            await using (stream.ConfigureAwait(false))
            {
                await write(stream, bytes, cancellationToken).ConfigureAwait(false);

                // On the disk before the move, or a power cut just after it could leave the name on an empty file.
                stream.Flush(flushToDisk: true);
            }

            if (exists)
            {
                CopyMetadata(target, temporary);
            }

            try
            {
                File.Move(temporary, target, overwrite: true);
                moved = true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Held by a program that lets it be written but not replaced: in place, as before.
                await WriteInPlaceAsync(target, bytes, write, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!moved)
            {
                TryDelete(temporary);
            }
        }
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

    // The file a path ends up at, through any symbolic links; the path itself when it is none, or a link to nothing.
    private static string FinalTarget(string path)
    {
        try
        {
            return (new FileInfo(path).LinkTarget is not null) && (File.ResolveLinkTarget(path, returnFinalTarget: true) is { } final)
                ? final.FullName
                : path;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return path;
        }
    }

    // Opens the file for writing and lets go at once, truncating nothing: whatever would stop a write in place — the
    // read-only attribute, permissions, a program holding it — throws here.
    private static void ProbeWritable(string path)
    {
        using FileStream probe = new(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
    }

    // What the move would lose otherwise. Best effort: the text is what matters, and it is already safe.
    private static void CopyMetadata(string from, string to)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                File.SetCreationTimeUtc(to, File.GetCreationTimeUtc(from));
                File.SetAttributes(to, File.GetAttributes(from) & ~FileAttributes.ReadOnly);
            }
            else
            {
                File.SetUnixFileMode(to, File.GetUnixFileMode(from));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // The new file keeps the folder's defaults.
        }
    }

    // The write without the protection: over the old bytes, cut to the new length once they are all there. Opened
    // without truncating, so a hidden file — which Windows will not open to be created anew — is written too.
    private static async Task WriteInPlaceAsync(
        string path,
        ReadOnlyMemory<byte> bytes,
        Func<Stream, ReadOnlyMemory<byte>, CancellationToken, ValueTask> write,
        CancellationToken cancellationToken)
    {
        FileStream stream = new(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.OpenOrCreate,
                Access = FileAccess.Write,

                // As File.WriteAllBytes shares it: a program reading the file meanwhile does not stop the save.
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous,
            });

        await using (stream.ConfigureAwait(false))
        {
            await write(stream, bytes, cancellationToken).ConfigureAwait(false);
            stream.SetLength(bytes.Length);
            stream.Flush(flushToDisk: true);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A stray temporary file is harmless, and hidden: its name starts with a dot.
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

using System;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// A file opened in the hex viewer: its bytes and their rows.
/// </summary>
public sealed class HexDocument : IDisposable
{
    private readonly IByteSource _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="HexDocument"/> class.
    /// </summary>
    /// <param name="source">The bytes; the document owns and disposes it.</param>
    public HexDocument(IByteSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        Rows = new HexRows(source);
    }

    /// <summary>
    /// Gets the file's size, in bytes.
    /// </summary>
    public long Length => _source.Length;

    /// <summary>
    /// Gets the rows of the dump.
    /// </summary>
    public HexRows Rows { get; }

    /// <summary>
    /// Opens a file in the hex viewer.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <returns>The document.</returns>
    public static HexDocument Open(string filePath) => new(new FileByteSource(filePath));

    /// <summary>
    /// Writes a size the way the status bar shows it: <c>512 bytes</c>, <c>1.5 KB</c>, <c>3.2 MB</c>.
    /// </summary>
    /// <param name="length">The size, in bytes.</param>
    /// <returns>The text.</returns>
    public static string FormatSize(long length)
    {
        string[] units = ["KB", "MB", "GB", "TB"];

        if (length < 1024)
        {
            return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{length} bytes");
        }

        double size = length;
        int unit = -1;

        while ((size >= 1024) && (unit < (units.Length - 1)))
        {
            size /= 1024;
            unit++;
        }

        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{size:0.#} {units[unit]}");
    }

    /// <inheritdoc />
    public void Dispose() => _source.Dispose();
}

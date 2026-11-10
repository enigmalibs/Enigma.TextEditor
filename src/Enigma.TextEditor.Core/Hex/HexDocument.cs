using System;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// A file opened in the hex viewer: its bytes and their rows.
/// </summary>
public sealed class HexDocument : IDisposable
{
    /// <summary>
    /// The most bytes a selection may hold to be copied as hex or as text: 1 MiB, which is three million characters
    /// of hex.
    /// </summary>
    /// <remarks>
    /// A selection can reach across a whole file of any size, and a copy builds its text in memory before the
    /// clipboard takes it: a few gigabytes selected would be several times that in characters.
    /// </remarks>
    public const int MaxCopyBytes = 1024 * 1024;

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
    /// Gets how many hex digits the rows write their offsets with: 8, or 16 beyond 4 GB.
    /// </summary>
    public int OffsetDigits => HexFormatter.OffsetDigits(Length);

    /// <summary>
    /// Gets the rows of the dump.
    /// </summary>
    public HexRows Rows { get; }

    /// <summary>
    /// Reads a run of bytes.
    /// </summary>
    /// <param name="start">The offset of the first.</param>
    /// <param name="count">How many.</param>
    /// <returns>The bytes: fewer than asked when the run reaches past the end, none past it.</returns>
    /// <exception cref="System.IO.IOException">The file could not be read.</exception>
    public byte[] Read(long start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        byte[] bytes = new byte[count];
        int read = _source.Read(start, bytes);

        return read == count ? bytes : bytes[..read];
    }

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

using System;
using System.Globalization;
using System.Text;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// Lays out bytes as rows of a hex dump, the way <c>hexdump -C</c> does.
/// </summary>
/// <remarks>
/// <c>00000010  48 65 6C 6C 6F 20 77 6F  72 6C 64 0A 00 01 02 03  Hello world.....</c> — the offset,
/// sixteen bytes in two groups of eight, and the same bytes as text, where everything outside
/// printable ASCII is a dot. A short last row keeps the text column where the full rows have it.
/// </remarks>
public static class HexFormatter
{
    /// <summary>
    /// How many bytes one row shows.
    /// </summary>
    public const int BytesPerRow = 16;

    /// <summary>
    /// How many hex digits the offsets of a file of a given size are written with: eight up to 4 GB,
    /// sixteen beyond.
    /// </summary>
    /// <param name="length">The file's size, in bytes.</param>
    /// <returns>8 or 16.</returns>
    public static int OffsetDigits(long length) => length > 0x1_0000_0000L ? 16 : 8;

    /// <summary>
    /// How many rows a file of a given size takes.
    /// </summary>
    /// <param name="length">The file's size, in bytes.</param>
    /// <returns>The row count; an empty file has none.</returns>
    public static long RowCount(long length) => (length + BytesPerRow - 1) / BytesPerRow;

    /// <summary>
    /// Formats one row.
    /// </summary>
    /// <param name="offset">The offset of the row's first byte.</param>
    /// <param name="bytes">The row's bytes, at most <see cref="BytesPerRow"/>.</param>
    /// <param name="offsetDigits">How many hex digits the offset is written with.</param>
    /// <returns>The row.</returns>
    public static string FormatRow(long offset, ReadOnlySpan<byte> bytes, int offsetDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, BytesPerRow);

        StringBuilder row = new((offsetDigits + 2) + (BytesPerRow * 3) + 2 + BytesPerRow);
        row.Append(offset.ToString(offsetDigits == 16 ? "X16" : "X8", CultureInfo.InvariantCulture));
        row.Append("  ");

        for (int index = 0; index < BytesPerRow; index++)
        {
            if (index == (BytesPerRow / 2))
            {
                row.Append(' ');
            }

            if (index < bytes.Length)
            {
                row.Append(bytes[index].ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
            }
            else
            {
                row.Append("   ");
            }
        }

        row.Append(' ');

        foreach (byte value in bytes)
        {
            row.Append(value is >= 0x20 and < 0x7F ? (char)value : '.');
        }

        return row.ToString();
    }
}

using System;
using System.Globalization;
using System.Text;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// Lays out bytes as rows of a hex dump, the way <c>hexdump -C</c> does.
/// </summary>
/// <remarks>
/// <para>
/// <c>00000010  48 65 6C 6C 6F 20 77 6F  72 6C 64 0A 00 01 02 03  Hello world.....</c> — the offset,
/// sixteen bytes in two groups of eight, and the same bytes as text, where everything outside
/// printable ASCII is a dot. A short last row keeps the text column where the full rows have it.
/// </para>
/// <para>
/// The layout's positions are public too — where a byte's hex pair and its character stand in a row, and which byte
/// a character of the row belongs to — so what paints and hit-tests a selection reads them from the class that
/// writes the rows, and the two cannot drift apart.
/// </para>
/// </remarks>
public static class HexFormatter
{
    /// <summary>
    /// How many bytes one row shows.
    /// </summary>
    public const int BytesPerRow = 16;

    private const string HexDigits = "0123456789ABCDEF";

    // Two spaces after the offset; three characters per byte, and one more space between the two groups of eight.
    private const int OffsetGap = 2;
    private const int CharsPerByte = 3;
    private const int GroupBytes = BytesPerRow / 2;
    private const int GroupWidth = GroupBytes * CharsPerByte;

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
    /// Writes an offset the way the rows do: <c>0000001F</c>.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="offsetDigits">How many hex digits it is written with: 8 or 16.</param>
    /// <returns>The offset, in upper-case hex, without a prefix.</returns>
    public static string FormatOffset(long offset, int offsetDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        return offset.ToString(offsetDigits == 16 ? "X16" : "X8", CultureInfo.InvariantCulture);
    }

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

        StringBuilder row = new(TextCharIndex(0, offsetDigits) + BytesPerRow);
        row.Append(FormatOffset(offset, offsetDigits));
        row.Append("  ");

        for (int index = 0; index < BytesPerRow; index++)
        {
            if (index == GroupBytes)
            {
                row.Append(' ');
            }

            if (index < bytes.Length)
            {
                row.Append(HexDigits[bytes[index] >> 4]).Append(HexDigits[bytes[index] & 0xF]).Append(' ');
            }
            else
            {
                row.Append("   ");
            }
        }

        row.Append(' ');

        foreach (byte value in bytes)
        {
            row.Append(TextOf(value));
        }

        return row.ToString();
    }

    /// <summary>
    /// Where a byte's hex pair starts in its row.
    /// </summary>
    /// <param name="byteInRow">The byte, counted from the row's first, 0 to 15.</param>
    /// <param name="offsetDigits">How many hex digits the row's offset is written with.</param>
    /// <returns>The index of the pair's first digit in the row's text.</returns>
    public static int HexCharIndex(int byteInRow, int offsetDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteInRow);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(byteInRow, BytesPerRow);

        return offsetDigits + OffsetGap + (byteInRow * CharsPerByte) + (byteInRow >= GroupBytes ? 1 : 0);
    }

    /// <summary>
    /// Where a byte's character stands in its row.
    /// </summary>
    /// <param name="byteInRow">The byte, counted from the row's first, 0 to 15.</param>
    /// <param name="offsetDigits">How many hex digits the row's offset is written with.</param>
    /// <returns>The index of the character in the row's text.</returns>
    public static int TextCharIndex(int byteInRow, int offsetDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteInRow);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(byteInRow, BytesPerRow);

        // Past the hex column — sixteen pairs and the space between the groups — and one more space.
        return offsetDigits + OffsetGap + (BytesPerRow * CharsPerByte) + 2 + byteInRow;
    }

    /// <summary>
    /// Which column and which byte a character of a row belongs to.
    /// </summary>
    /// <remarks>
    /// Every character answers, so a pointer anywhere on a row lands on a byte: the offset and anything left of the
    /// row are the first hex pair; a space between pairs is the pair before it, the gap between the two groups
    /// included; the space before the text column is its first character; anything past the text is its last.
    /// Whether the row has that byte — the last row may be short — is the caller's to check.
    /// </remarks>
    /// <param name="charIndex">The character's index in the row's text; negative is left of the row.</param>
    /// <param name="offsetDigits">How many hex digits the row's offset is written with.</param>
    /// <returns>The column, and the byte counted from the row's first, 0 to 15.</returns>
    public static (HexColumn Column, int ByteInRow) ByteAt(int charIndex, int offsetDigits)
    {
        int textStart = TextCharIndex(0, offsetDigits);

        if (charIndex >= (textStart - 1))
        {
            return (HexColumn.Text, Math.Clamp(charIndex - textStart, 0, BytesPerRow - 1));
        }

        int position = charIndex - (offsetDigits + OffsetGap);

        int byteInRow = position switch
        {
            < 0 => 0,
            < GroupWidth => position / CharsPerByte,
            GroupWidth => GroupBytes - 1,
            _ => GroupBytes + ((position - GroupWidth - 1) / CharsPerByte),
        };

        return (HexColumn.Hex, Math.Min(byteInRow, BytesPerRow - 1));
    }

    /// <summary>
    /// Writes bytes as the hex column shows them: <c>48 65 6C 6C 6F</c>.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>Their pairs of upper-case hex digits, one space apart, on one line.</returns>
    public static string FormatHexString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        char[] text = new char[(bytes.Length * CharsPerByte) - 1];

        for (int index = 0; index < bytes.Length; index++)
        {
            int at = index * CharsPerByte;

            if (index > 0)
            {
                text[at - 1] = ' ';
            }

            text[at] = HexDigits[bytes[index] >> 4];
            text[at + 1] = HexDigits[bytes[index] & 0xF];
        }

        return new string(text);
    }

    /// <summary>
    /// Writes bytes as the text column shows them: printable ASCII as itself, anything else as a dot.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>One character per byte.</returns>
    public static string FormatText(ReadOnlySpan<byte> bytes)
    {
        char[] text = new char[bytes.Length];

        for (int index = 0; index < bytes.Length; index++)
        {
            text[index] = TextOf(bytes[index]);
        }

        return new string(text);
    }

    /// <summary>
    /// Writes the offsets of a run of bytes: <c>0x00000001 - 0x00000004</c>.
    /// </summary>
    /// <param name="start">The offset of the first byte.</param>
    /// <param name="end">The offset of the last byte, included; the first again for one byte.</param>
    /// <param name="offsetDigits">How many hex digits the offsets are written with, as the rows write them.</param>
    /// <returns>The range.</returns>
    public static string FormatAddressRange(long start, long end, int offsetDigits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start);

        return $"0x{FormatOffset(start, offsetDigits)} - 0x{FormatOffset(end, offsetDigits)}";
    }

    private static char TextOf(byte value) => value is >= 0x20 and < 0x7F ? (char)value : '.';
}

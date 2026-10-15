using System;
using System.IO;
using System.Linq;
using System.Text;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Core.UnitTests.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Hex;

public sealed class HexTests
{
    [Fact]
    public void AFullRow_IsTheOffsetSixteenBytesInTwoGroupsAndTheirText()
    {
        byte[] bytes = [.. Encoding.ASCII.GetBytes("Hello world\n"), 0x00, 0x01, 0x02, 0x03];

        Assert.Equal(
            "00000010  48 65 6C 6C 6F 20 77 6F  72 6C 64 0A 00 01 02 03  Hello world.....",
            HexFormatter.FormatRow(0x10, bytes, 8));
    }

    [Fact]
    public void AShortRow_KeepsItsTextWhereAFullRowHasIt()
    {
        string full = HexFormatter.FormatRow(0, new byte[16], 8);
        string shortRow = HexFormatter.FormatRow(0x20, "AB"u8, 8);

        Assert.Equal("00000020  41 42                                             AB", shortRow);
        Assert.Equal(full.IndexOf("  ....", StringComparison.Ordinal) + 2, shortRow.IndexOf("AB", 10, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData((byte)0x1F, '.')]
    [InlineData((byte)0x20, ' ')]
    [InlineData((byte)0x7E, '~')]
    [InlineData((byte)0x7F, '.')]
    [InlineData((byte)0xE9, '.')]
    public void OnlyPrintableAsciiIsShownAsItself(byte value, char shown)
        => Assert.Equal(shown, HexFormatter.FormatRow(0, [value], 8)[^1]);

    [Fact]
    public void OffsetsPastFourGigabytes_TakeSixteenDigits()
    {
        Assert.Equal(8, HexFormatter.OffsetDigits(0xFFFF_FFFFL));
        Assert.Equal(16, HexFormatter.OffsetDigits(0x1_0000_0001L));
        Assert.StartsWith("0000000100000000  ", HexFormatter.FormatRow(0x1_0000_0000L, [0], 16), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(1L, 1L)]
    [InlineData(16L, 1L)]
    [InlineData(17L, 2L)]
    public void TheRowCount_RoundsUp(long length, long rows)
        => Assert.Equal(rows, HexFormatter.RowCount(length));

    [Fact]
    public void AFileByteSource_ReadsAtOffsetsAndShortAtTheEnd()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("data.bin", [.. Enumerable.Range(0, 100).Select(value => (byte)value)]);
        using FileByteSource source = new(path);
        byte[] buffer = new byte[10];

        Assert.Equal(100, source.Length);
        Assert.Equal(10, source.Read(50, buffer));
        Assert.Equal(50, buffer[0]);
        Assert.Equal(5, source.Read(95, buffer));
        Assert.Equal(99, buffer[4]);
        Assert.Equal(0, source.Read(100, buffer));
    }

    [Fact]
    public void AFileByteSource_DoesNotStopOthersFromWritingOrDeletingTheFile()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("data.bin", [1, 2, 3]);
        using FileByteSource source = new(path);

        File.WriteAllBytes(path, [9, 9, 9]);
        File.Delete(path);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Rows_AreCountedFromTheLengthAndReadOnlyWhenAskedFor()
    {
        byte[] bytes = new byte[(HexRows.RowsPerPage * HexFormatter.BytesPerRow * 3) + 5];
        HexRows rows = new(new MemoryByteSource(bytes));

        Assert.Equal((HexRows.RowsPerPage * 3) + 1, rows.Count);
        Assert.Equal(0, rows.PagesRead);

        _ = rows[0];
        _ = rows[HexRows.RowsPerPage - 1];
        Assert.Equal(1, rows.PagesRead);

        string last = rows[^1];
        Assert.Equal(2, rows.PagesRead);
        Assert.StartsWith("00000C00  00 00 00 00 00 ", last, StringComparison.Ordinal);
    }

    [Fact]
    public void Rows_AreFoundByTheirTextWithoutAScan()
    {
        byte[] bytes = [.. Enumerable.Range(0, 64).Select(value => (byte)value)];
        System.Collections.IList rows = new HexRows(new MemoryByteSource(bytes));

        Assert.Equal(2, rows.IndexOf(rows[2]));
        Assert.Equal(-1, rows.IndexOf("not a row"));
        Assert.True(rows.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => rows.Add("x"));
    }

    [Fact]
    public void AnEmptyFile_HasNoRows()
        => Assert.Empty(new HexRows(new MemoryByteSource(Array.Empty<byte>())));

    [Theory]
    [InlineData(0L, "0 bytes")]
    [InlineData(512L, "512 bytes")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(3221225472L, "3 GB")]
    public void Sizes_ReadTheWayTheStatusBarShowsThem(long length, string text)
        => Assert.Equal(text, HexDocument.FormatSize(length));
}

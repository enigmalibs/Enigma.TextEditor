using System;
using System.Globalization;
using System.Linq;
using Enigma.TextEditor.Core.Hex;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Hex;

/// <summary>
/// Selecting bytes in the hex viewer: the selection, where its bytes stand in a row, and what a copy of it reads.
/// </summary>
public sealed class HexSelectionTests
{
    [Fact]
    public void ASelection_RunsFromItsLowerEndToItsHigherEnd_WhicheverWayItWasMade()
    {
        HexSelection down = HexSelection.At(3, HexColumn.Hex).ExtendedTo(9);
        HexSelection up = HexSelection.At(9, HexColumn.Text).ExtendedTo(3);

        Assert.Equal((3L, 9L, 7L), (down.Start, down.End, down.Length));
        Assert.Equal((3L, 9L, 7L), (up.Start, up.End, up.Length));
        Assert.Equal((9L, 3L, HexColumn.Text), (up.Anchor, up.Active, up.Column));
        Assert.True(up.Contains(3));
        Assert.True(up.Contains(9));
        Assert.False(up.Contains(2));
        Assert.False(up.Contains(10));
    }

    [Fact]
    public void ASelectionOfOneByte_StartsAndEndsOnIt()
    {
        HexSelection one = HexSelection.At(0x10, HexColumn.Hex);

        Assert.Equal((0x10L, 0x10L, 1L), (one.Start, one.End, one.Length));
    }

    [Fact]
    public void ExtendingASelection_KeepsItsAnchorAndColumn()
    {
        HexSelection selection = HexSelection.At(5, HexColumn.Text).ExtendedTo(40).ExtendedTo(1);

        Assert.Equal(new HexSelection(5, 1, HexColumn.Text), selection);
    }

    [Fact]
    public void ASelection_RefusesNegativeOffsetsAndUnknownColumns()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexSelection(-1, 0, HexColumn.Hex));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexSelection(0, -1, HexColumn.Hex));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexSelection(0, 0, (HexColumn)7));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void EveryPairAndCharacterOfARow_StandsWhereTheLayoutSays_AndMapsBackToItsByte(int digits)
    {
        byte[] bytes = [.. Enumerable.Range(0, HexFormatter.BytesPerRow).Select(index => (byte)('A' + index))];
        string row = HexFormatter.FormatRow(0x20, bytes, digits);

        for (int index = 0; index < HexFormatter.BytesPerRow; index++)
        {
            int hex = HexFormatter.HexCharIndex(index, digits);
            int text = HexFormatter.TextCharIndex(index, digits);

            Assert.Equal(bytes[index].ToString("X2", CultureInfo.InvariantCulture), row.Substring(hex, 2));
            Assert.Equal((char)bytes[index], row[text]);
            Assert.Equal((HexColumn.Hex, index), HexFormatter.ByteAt(hex, digits));
            Assert.Equal((HexColumn.Hex, index), HexFormatter.ByteAt(hex + 1, digits));
            Assert.Equal((HexColumn.Hex, index), HexFormatter.ByteAt(hex + 2, digits));
            Assert.Equal((HexColumn.Text, index), HexFormatter.ByteAt(text, digits));
        }

        Assert.Equal(row.Length - 1, HexFormatter.TextCharIndex(HexFormatter.BytesPerRow - 1, digits));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void TheCharactersBetweenTheBytes_BelongToTheNearestByteOfTheirColumn(int digits)
    {
        int textStart = HexFormatter.TextCharIndex(0, digits);

        // The offset, and left of the row, are the first pair.
        Assert.Equal((HexColumn.Hex, 0), HexFormatter.ByteAt(-5, digits));
        Assert.Equal((HexColumn.Hex, 0), HexFormatter.ByteAt(0, digits));
        Assert.Equal((HexColumn.Hex, 0), HexFormatter.ByteAt(digits + 1, digits));

        // The gap between the groups is the last pair of the first group.
        Assert.Equal((HexColumn.Hex, 7), HexFormatter.ByteAt(HexFormatter.HexCharIndex(8, digits) - 1, digits));

        // The space before the text is its first character; past the text is its last.
        Assert.Equal((HexColumn.Hex, 15), HexFormatter.ByteAt(textStart - 2, digits));
        Assert.Equal((HexColumn.Text, 0), HexFormatter.ByteAt(textStart - 1, digits));
        Assert.Equal((HexColumn.Text, 15), HexFormatter.ByteAt(textStart + 40, digits));
    }

    [Fact]
    public void TheLayoutPositions_RefuseBytesOutsideARow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HexFormatter.HexCharIndex(-1, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => HexFormatter.HexCharIndex(16, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => HexFormatter.TextCharIndex(16, 8));
    }

    [Fact]
    public void AHexString_IsUpperCasePairsOneSpaceApart()
    {
        Assert.Equal("48 65 6C 6C 6F 00 FF", HexFormatter.FormatHexString([0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF]));
        Assert.Equal("0A", HexFormatter.FormatHexString([0x0A]));
        Assert.Equal(string.Empty, HexFormatter.FormatHexString([]));
    }

    [Fact]
    public void Text_IsWhatTheTextColumnShows()
    {
        Assert.Equal("Hi .~..", HexFormatter.FormatText([0x48, 0x69, 0x20, 0x0A, 0x7E, 0x7F, 0xE9]));
        Assert.Equal(string.Empty, HexFormatter.FormatText([]));
    }

    [Fact]
    public void AnAddressRange_IsBothOffsetsInclusive_AsWideAsTheRowsWriteThem()
    {
        Assert.Equal("0x00000001 - 0x00000004", HexFormatter.FormatAddressRange(1, 4, 8));
        Assert.Equal("0x0000001F - 0x0000001F", HexFormatter.FormatAddressRange(0x1F, 0x1F, 8));
        Assert.Equal("0x0000000100000000 - 0x00000001000000FF", HexFormatter.FormatAddressRange(0x1_0000_0000L, 0x1_0000_00FFL, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => HexFormatter.FormatAddressRange(4, 1, 8));
    }

    [Fact]
    public void AnOffset_IsWrittenAsTheRowsWriteIt()
    {
        Assert.Equal("0000001F", HexFormatter.FormatOffset(0x1F, 8));
        Assert.Equal("000000000000001F", HexFormatter.FormatOffset(0x1F, 16));
    }

    [Fact]
    public void ADocument_ReadsARunAcrossRows_AndShortAtTheEnd()
    {
        byte[] bytes = [.. Enumerable.Range(0, 40).Select(index => (byte)index)];
        using HexDocument document = new(new MemoryByteSource(bytes));

        Assert.Equal(bytes[14..18], document.Read(14, 4));
        Assert.Equal(bytes[36..], document.Read(36, 10));
        Assert.Empty(document.Read(40, 4));
        Assert.Equal(8, document.OffsetDigits);
    }
}

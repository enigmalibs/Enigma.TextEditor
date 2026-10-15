using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Documents;

public sealed class ContentSnifferTests
{
    [Fact]
    public void PlainText_IsNotBinary()
        => Assert.False(ContentSniffer.IsBinary(Encoding.UTF8.GetBytes("hello\nworld\t\r\n")));

    [Fact]
    public void AnEmptyFile_IsNotBinary()
        => Assert.False(ContentSniffer.IsBinary([]));

    [Fact]
    public void ANulByte_MakesAFileBinary()
        => Assert.True(ContentSniffer.IsBinary([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00]));

    [Fact]
    public void ANulPastTheProbe_IsNotLookedAt()
    {
        byte[] bytes = [.. Enumerable.Repeat((byte)'a', ContentSniffer.BinaryProbeLength), 0x00];

        Assert.False(ContentSniffer.IsBinary(bytes));
    }

    [Fact]
    public void Utf16TextWithItsMark_IsNotBinaryDespiteItsNuls()
    {
        byte[] bytes = [.. new UnicodeEncoding(false, true).GetPreamble(), .. Encoding.Unicode.GetBytes("text")];

        Assert.Contains((byte)0, bytes);
        Assert.False(ContentSniffer.IsBinary(bytes));
    }

    [Fact]
    public void Utf32TextWithItsMark_IsNotBinary()
    {
        UTF32Encoding utf32 = new(false, true);
        byte[] bytes = [.. utf32.GetPreamble(), .. utf32.GetBytes("text")];

        Assert.False(ContentSniffer.IsBinary(bytes));
    }

    [Fact]
    public void Utf16TextWithoutAMark_IsTakenForBinary()
    {
        // Nothing tells it from binary: this is why the UTF-16 entries always write their mark.
        Assert.True(ContentSniffer.IsBinary(Encoding.Unicode.GetBytes("text")));
    }

    [Theory]
    [InlineData(new byte[] { 0x61, 0x62 }, "UTF-8", 0)]
    [InlineData(new byte[] { 0xC3, 0xA9 }, "UTF-8", 0)]
    [InlineData(new byte[] { 0xE9 }, "Windows-1252", 0)]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x61 }, "UTF-8 with BOM", 3)]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, "UTF-16 LE", 2)]
    [InlineData(new byte[] { 0xFE, 0xFF, 0x00, 0x61 }, "UTF-16 BE", 2)]
    public void TheEncoding_IsTheMarkThenUtf8ThenWindows1252(byte[] bytes, string label, int preamble)
    {
        TextEncoding encoding = ContentSniffer.DetectEncoding(bytes, out int preambleLength);

        Assert.Equal(label, encoding.Label);
        Assert.Equal(preamble, preambleLength);
    }

    [Fact]
    public async Task AProbe_ReportsTheSizeAndWhetherTheFileLooksBinary()
    {
        using TempDocumentFolder folder = new();
        string text = folder.WriteBytes("a.txt", Encoding.ASCII.GetBytes("text"));
        string binary = folder.WriteBytes("a.bin", [0x00, 0x01, 0x02]);

        FileProbe textProbe = await DocumentFile.ProbeAsync(text, TestContext.Current.CancellationToken);
        FileProbe binaryProbe = await DocumentFile.ProbeAsync(binary, TestContext.Current.CancellationToken);

        Assert.Equal(new FileProbe(4, false), textProbe);
        Assert.Equal(new FileProbe(3, true), binaryProbe);
        Assert.False(textProbe.IsTooLargeForText);
    }

    [Fact]
    public async Task AProbe_SaysWhenAFileIsTooLargeForText()
    {
        using TempDocumentFolder folder = new();
        string path = folder.Combine("big.log");

        using (FileStream stream = File.Create(path))
        {
            stream.SetLength(DocumentFile.MaximumBytes + 1);
        }

        FileProbe probe = await DocumentFile.ProbeAsync(path, TestContext.Current.CancellationToken);

        Assert.True(probe.IsTooLargeForText);
    }
}

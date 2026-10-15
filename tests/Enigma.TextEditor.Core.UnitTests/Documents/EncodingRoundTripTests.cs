using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Documents;

/// <summary>
/// Encodings beyond the round trips the ported <see cref="DocumentFileTests"/> cover: detection with
/// the Windows-1252 fallback, forced encodings, conversions, and the refusal to write what an encoding
/// cannot hold.
/// </summary>
public sealed class EncodingRoundTripTests
{
    [Fact]
    public async Task AFileThatIsNotValidUtf8_IsReadAsWindows1252AndWrittenBackByteForByte()
    {
        using TempDocumentFolder folder = new();

        // "café €" in Windows-1252: 0xE9 and 0x80 are not valid UTF-8 on their own.
        byte[] original = [0x63, 0x61, 0x66, 0xE9, 0x20, 0x80, 0x0A];
        string path = folder.WriteBytes("legacy.txt", original);

        DocumentContent content = await DocumentFile.ReadAsync(path, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("café €\n", content.Text);
        Assert.Equal(TextEncodings.Windows1252, TextEncodings.For(content.Format));

        await DocumentFile.WriteAsync(path, content.Text, content.Format, TestContext.Current.CancellationToken);

        Assert.Equal(original, folder.ReadBytes("legacy.txt"));
    }

    [Fact]
    public async Task ValidUtf8WithoutAMark_IsReadAsUtf8()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("modern.txt", Encoding.UTF8.GetBytes("naïve 日本\n"));

        DocumentContent content = await DocumentFile.ReadAsync(path, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("naïve 日本\n", content.Text);
        Assert.Equal(TextEncodings.Utf8, TextEncodings.For(content.Format));
    }

    [Theory]
    [InlineData(0, "UTF-8")]
    [InlineData(1, "UTF-8 with BOM")]
    [InlineData(2, "UTF-16 LE")]
    [InlineData(3, "UTF-16 BE")]
    [InlineData(4, "Windows-1252")]
    public async Task EveryOfferedUnicodeAndWesternEncoding_RoundTripsByteForByte(int index, string label)
    {
        using TempDocumentFolder folder = new();
        TextEncoding encoding = TextEncodings.All[index];
        Assert.Equal(label, encoding.Label);

        TextFileFormat format = TextFileFormat.Default.With(encoding).With(LineEndingStyle.CrLf);
        byte[] original = DocumentFile.Encode("Grüße\nline two\n", format);
        string path = folder.WriteBytes("file.txt", original);

        DocumentContent content = await DocumentFile.ReadAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        await DocumentFile.WriteAsync(path, content.Text, content.Format, TestContext.Current.CancellationToken);

        Assert.Equal("Grüße\nline two\n", content.Text);
        Assert.Equal(LineEndingStyle.CrLf, content.Format.LineEndings);
        Assert.Equal(original, folder.ReadBytes("file.txt"));
    }

    [Fact]
    public async Task ReopeningWithAnotherEncoding_DecodesTheSameBytesDifferently()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("utf8.txt", Encoding.UTF8.GetBytes("é\n"));

        DocumentContent content = await DocumentFile.ReadAsync(path, TextEncodings.Windows1252, TestContext.Current.CancellationToken);

        // The two bytes of UTF-8 'é', each read as a Windows-1252 character.
        Assert.Equal("Ã©\n", content.Text);
        Assert.Equal(TextEncodings.Windows1252, TextEncodings.For(content.Format));
    }

    [Fact]
    public async Task ReopeningAMarkedFileInItsOwnEncoding_DropsTheMarkFromTheText()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("marked.txt", [0xEF, 0xBB, 0xBF, 0x61]);

        DocumentContent content = await DocumentFile.ReadAsync(path, TextEncodings.Utf8, TestContext.Current.CancellationToken);

        Assert.Equal("a", content.Text);
        Assert.False(content.Format.HasByteOrderMark);
    }

    [Fact]
    public async Task ConvertingToUtf16_WritesAMarkedUtf16File()
    {
        using TempDocumentFolder folder = new();
        string path = folder.Combine("converted.txt");
        TextFileFormat format = TextFileFormat.Default.With(TextEncodings.Utf16Le);

        await DocumentFile.WriteAsync(path, "hi", format, TestContext.Current.CancellationToken);

        Assert.Equal(new byte[] { 0xFF, 0xFE, 0x68, 0x00, 0x69, 0x00 }, folder.ReadBytes("converted.txt"));
    }

    [Fact]
    public async Task TextTheEncodingCannotHold_IsRefusedAndTheFileIsLeftUntouched()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("target.txt", Encoding.ASCII.GetBytes("untouched"));
        TextFileFormat format = TextFileFormat.Default.With(TextEncodings.Windows1252);

        UnrepresentableTextException failure = await Assert.ThrowsAsync<UnrepresentableTextException>(
            () => DocumentFile.WriteAsync(path, "first line\nab日本", format, TestContext.Current.CancellationToken));

        Assert.Equal("日", failure.Character);
        Assert.Equal(2, failure.Line);
        Assert.Equal(3, failure.Column);
        Assert.Equal("Windows-1252", failure.EncodingLabel);
        Assert.Equal("untouched", Encoding.ASCII.GetString(folder.ReadBytes("target.txt")));
    }

    [Fact]
    public void ACharacterOutsideTheBasicPlane_IsReportedWhole()
    {
        TextFileFormat format = TextFileFormat.Default.With(TextEncodings.Windows1252);

        UnrepresentableTextException failure = Assert.Throws<UnrepresentableTextException>(
            () => DocumentFile.Encode("x😀", format));

        Assert.Equal("😀", failure.Character);
        Assert.Equal(2, failure.Column);
    }

    [Fact]
    public void EveryOfferedEncoding_CanBeCreated()
    {
        foreach (TextEncoding entry in TextEncodings.All)
        {
            Encoding encoding = entry.CreateEncoding();

            Assert.Equal(entry.CodePage, encoding.CodePage);
            Assert.Equal(entry, TextEncodings.For(TextFileFormat.Default.With(entry)));
        }
    }

    [Fact]
    public void AUtf32File_HasAnEntryEvenThoughItIsNotOffered()
    {
        TextFileFormat format = new(new UTF32Encoding(false, true), true, LineEndingStyle.Lf);

        Assert.Equal("UTF-32 LE", TextEncodings.For(format).Label);
    }

    [Fact]
    public async Task AFileTooLarge_ReportsItsSizeAndTheLimit()
    {
        using TempDocumentFolder folder = new();
        string path = folder.Combine("huge.log");

        using (FileStream stream = File.Create(path))
        {
            stream.SetLength(DocumentFile.MaximumBytes + 1);
        }

        FileTooLargeException failure = await Assert.ThrowsAsync<FileTooLargeException>(
            () => DocumentFile.ReadAsync(path, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(DocumentFile.MaximumBytes + 1, failure.Length);
        Assert.Equal(path, failure.FilePath);
        Assert.Contains("huge.log", failure.Message, StringComparison.Ordinal);
    }
}

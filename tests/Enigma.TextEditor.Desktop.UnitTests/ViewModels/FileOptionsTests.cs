using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The file's options set or shown by <c>:set</c>: <c>fileformat</c>, <c>fileencoding</c>, <c>bomb</c>,
/// <c>filetype</c>, applied to a buffer as the status bar's tools apply them.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileOptionsTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData("ff=dos", LineEndingStyle.CrLf)]
    [InlineData("fileformat=dos", LineEndingStyle.CrLf)]
    [InlineData("ff=DOS", LineEndingStyle.CrLf)]
    [InlineData("ff=unix", LineEndingStyle.Lf)]
    public void FileFormat_SetsTheLineEndings(string words, LineEndingStyle expected)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "one\ntwo\n");

            Assert.Null(FileOptions.Apply(words, document, harness.Languages));
            Assert.Equal(expected, document.Format.LineEndings);
        });

    [Fact]
    public void FileFormat_IsShownUnderItsLongName()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "one\r\ntwo\r\n");

            Assert.Equal("fileformat=dos", FileOptions.Apply("ff?", document, harness.Languages));
            Assert.Equal("fileformat=dos", FileOptions.Apply("ff", document, harness.Languages));
        });

    [Theory]
    [InlineData("ff=mac")]
    [InlineData("ff=")]
    [InlineData("ff!")]
    public void AnUnsupportedFileFormat_IsRefused_AndNothingChanges(string words)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "one\n");

            Assert.Equal($"Invalid argument: {words}", FileOptions.Apply(words, document, harness.Languages));
            Assert.Equal(LineEndingStyle.Lf, document.Format.LineEndings);
            Assert.False(document.IsDirty);
        });

    [Theory]
    [InlineData("fenc=latin1", "ISO-8859-1")]
    [InlineData("fenc=iso-8859-1", "ISO-8859-1")]
    [InlineData("fenc=cp1252", "Windows-1252")]
    [InlineData("fenc=windows-1252", "Windows-1252")]
    [InlineData("fenc=latin9", "ISO-8859-15")]
    [InlineData("fenc=cp1250", "Windows-1250")]
    [InlineData("fenc=cp1251", "Windows-1251")]
    [InlineData("fenc=utf-16le", "UTF-16 LE")]
    [InlineData("fenc=utf-16", "UTF-16 BE")]
    [InlineData("fileencoding=UTF-16BE", "UTF-16 BE")]
    public void FileEncoding_SetsTheEncodingTheNextSaveWrites(string words, string label)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");

            Assert.Null(FileOptions.Apply(words, document, harness.Languages));
            Assert.Equal(label, document.Encoding.Label);
            Assert.True(document.IsDirty);
        });

    [Fact]
    public void FileEncodingUtf8_KeepsTheByteOrderMarkAsItIs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");
            document.ChangeEncoding(TextEncodings.Utf8WithBom);

            Assert.Null(FileOptions.Apply("fenc=latin1 fenc=utf-8", document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8, document.Encoding);

            document.ChangeEncoding(TextEncodings.Utf8WithBom);
            Assert.Null(FileOptions.Apply("fenc=utf8", document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8WithBom, document.Encoding);
        });

    [Theory]
    [InlineData("fenc?", "fileencoding=utf-8")]
    [InlineData("fenc=latin1 fenc?", "fileencoding=latin1")]
    [InlineData("fenc=utf-16 fenc", "fileencoding=utf-16")]
    public void FileEncoding_IsShownUnderVimsName(string words, string shown)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");

            Assert.Equal(shown, FileOptions.Apply(words, document, harness.Languages));
        });

    [Theory]
    [InlineData("fenc=klingon")]
    [InlineData("fenc=utf-32")]
    [InlineData("fenc!")]
    public void AnUnknownEncoding_IsRefused(string words)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");

            Assert.Equal($"Invalid argument: {words}", FileOptions.Apply(words, document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8, document.Encoding);
        });

    [Fact]
    public void Bomb_SwitchesUtf8sByteOrderMark()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");

            Assert.Equal("nobomb", FileOptions.Apply("bomb?", document, harness.Languages));
            Assert.Null(FileOptions.Apply("bomb", document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8WithBom, document.Encoding);
            Assert.Equal("bomb", FileOptions.Apply("bomb?", document, harness.Languages));
            Assert.Null(FileOptions.Apply("nobomb", document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8, document.Encoding);
            Assert.Null(FileOptions.Apply("bomb!", document, harness.Languages));
            Assert.Equal(TextEncodings.Utf8WithBom, document.Encoding);
        });

    [Fact]
    public void Bomb_OnlyMeansSomethingForUtf8()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "abc");

            FileOptions.Apply("fenc=latin1", document, harness.Languages);
            Assert.Equal("Invalid argument: bomb", FileOptions.Apply("bomb", document, harness.Languages));
            Assert.Null(FileOptions.Apply("nobomb", document, harness.Languages));

            // UTF-16 is always written with its mark here.
            FileOptions.Apply("fenc=utf-16le", document, harness.Languages);
            Assert.Null(FileOptions.Apply("bomb", document, harness.Languages));
            Assert.Equal("Invalid argument: nobomb", FileOptions.Apply("nobomb", document, harness.Languages));
            Assert.Equal("Invalid argument: bomb=1", FileOptions.Apply("bomb=1", document, harness.Languages));
        });

    [Theory]
    [InlineData("ft=python", "Python")]
    [InlineData("filetype=PYTHON", "Python")]
    [InlineData("ft=cs", "C#")]
    [InlineData("ft=c#", "C#")]
    [InlineData("ft=csharp", "C#")]
    [InlineData("ft=json", "JSON")]
    [InlineData("ft=", "Plain Text")]
    [InlineData("ft=text", "Plain Text")]
    [InlineData("ft=plaintext", "Plain Text")]
    public void FileType_ChoosesTheLanguage(string words, string displayName)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "x", "a.md");

            Assert.Null(FileOptions.Apply(words, document, harness.Languages));
            Assert.Equal(displayName, document.Language.DisplayName);
        });

    [Fact]
    public void FileType_IsShownAsTheLanguagesId()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "x", "a.py");

            Assert.Equal("filetype=python", FileOptions.Apply("ft?", document, harness.Languages));
            FileOptions.Apply("ft=", document, harness.Languages);
            Assert.Equal("filetype=plaintext", FileOptions.Apply("ft", document, harness.Languages));
        });

    [Fact]
    public void AnUnknownFileType_IsRefused()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "x", "a.py");

            Assert.Equal("Invalid argument: ft=klingon", FileOptions.Apply("ft=klingon", document, harness.Languages));
            Assert.Equal("python", document.Language.Id);
        });

    [Fact]
    public void TheFirstBadWord_StopsTheRest_WithWhatCameBeforeItDone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = await OpenAsync(harness, "x", "a.txt");

            Assert.Equal("Invalid argument: fenc=nope", FileOptions.Apply("ff=dos fenc=nope ft=python", document, harness.Languages));
            Assert.Equal(LineEndingStyle.CrLf, document.Format.LineEndings);
            Assert.Equal(LanguageInfo.PlainText, document.Language);
        });

    [Fact]
    public void AHexView_HasNoFileOptions()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = (await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", new byte[8])))!.Document;

            Assert.Equal("Not available in the hex view: ff=dos", FileOptions.Apply("ff=dos", document, harness.Languages));
        });

    private static async Task<DocumentViewModel> OpenAsync(EditorHarness harness, string text, string name = "a.txt")
        => (await harness.Workspace.OpenAsync(harness.Folder.Write(name, Encoding.UTF8.GetBytes(text))))!.Document;
}

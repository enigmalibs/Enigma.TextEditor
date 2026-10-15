using System.IO;
using System.Text;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// Opening and saving through the real file layer, with the pickers and messages scripted. Run on the
/// headless UI thread: a buffer belongs to the thread that built it, and the service resumes on the
/// thread that called it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class DocumentServiceTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Opening_ReadsTheFileAndDetectsItsIndentation()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("code.txt", "a:\n  b: 1\n  c:\n    d: 2\n");

            DocumentViewModel? document = await harness.Documents.OpenAsync(path);

            Assert.NotNull(document);
            Assert.Equal("a:\n  b: 1\n  c:\n    d: 2\n", document.Document.Text);
            Assert.Equal(Indentation.Spaces(2), document.Indentation);
            Assert.Equal("code.txt", document.Title);
        });

    [Fact]
    public void OpeningTheSameFileTwice_GivesTheSameBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("once.txt", "x");

            DocumentViewModel? first = await harness.Documents.OpenAsync(path);
            DocumentViewModel? second = await harness.Documents.OpenAsync(path);

            Assert.Same(first, second);
            Assert.Single(harness.Registry.Documents);
        });

    [Fact]
    public void AFileThatCannotBeRead_IsReportedAndOpensNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            DocumentViewModel? document = await harness.Documents.OpenAsync(harness.Folder.PathOf("missing.txt"));

            Assert.Null(document);
            Assert.Equal("This file could not be opened", Assert.Single(harness.Messages.Errors).Title);
            Assert.Empty(harness.Registry.Documents);
        });

    [Fact]
    public void Saving_WritesTheTextInTheFilesOwnFormat()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("crlf.txt", "one\r\n");
            DocumentViewModel document = (await harness.Documents.OpenAsync(path))!;
            document.Document.Insert(document.Document.TextLength, "two\n");

            bool saved = await harness.Documents.SaveAsync(document);

            Assert.True(saved);
            Assert.False(document.IsDirty);
            Assert.Equal("one\r\ntwo\r\n", File.ReadAllText(path));
        });

    [Fact]
    public void SavingAnUntitledBuffer_AsksWhereAndAdoptsThatFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = harness.Documents.CreateUntitled();
            document.Document.Text = "new text";
            string target = harness.Folder.PathOf("named.txt");
            harness.Prompts.SavePaths.Enqueue(target);

            bool saved = await harness.Documents.SaveAsync(document);

            Assert.True(saved);
            Assert.Equal("Untitled-1", harness.Prompts.LastSuggestedName);
            Assert.Equal(target, document.FilePath);
            Assert.Equal("new text", File.ReadAllText(target));
            Assert.Same(document, harness.Registry.Find(target));
        });

    [Fact]
    public void CancellingTheSavePicker_WritesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel document = harness.Documents.CreateUntitled();
            document.Document.Text = "unsaved";

            bool saved = await harness.Documents.SaveAsync(document);

            Assert.False(saved);
            Assert.True(document.IsUntitled);
            Assert.True(document.IsDirty);
        });

    [Fact]
    public void SavingOverAFileOpenInAnotherBuffer_IsRefused()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string other = harness.Folder.Write("other.txt", "keep me");
            await harness.Documents.OpenAsync(other);
            DocumentViewModel document = harness.Documents.CreateUntitled();

            bool saved = await harness.Documents.SaveAsAsync(document, other);

            Assert.False(saved);
            Assert.Equal("keep me", File.ReadAllText(other));
            Assert.Single(harness.Messages.Errors);
        });

    [Fact]
    public void TextTheEncodingCannotHold_IsReportedAndTheBufferStaysDirty()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("latin.txt", new byte[] { 0xE9, 0x0A });
            DocumentViewModel document = (await harness.Documents.OpenAsync(path))!;
            document.Document.Insert(0, "日本");

            bool saved = await harness.Documents.SaveAsync(document);

            Assert.False(saved);
            Assert.True(document.IsDirty);
            Assert.Equal(new byte[] { 0xE9, 0x0A }, File.ReadAllBytes(path));
            Assert.Contains("Windows-1252", Assert.Single(harness.Messages.Errors).Message, System.StringComparison.Ordinal);
        });

    [Fact]
    public void ANewBuffer_UsesTheDefaultLineEndingAndIndentationFromTheSettings()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new(settings =>
            {
                settings.DefaultLineEnding = LineEndingStyle.CrLf;
                settings.IndentWithTabs = true;
                settings.IndentSize = 8;
            });

            DocumentViewModel document = harness.Documents.CreateUntitled();

            Assert.Equal(LineEndingStyle.CrLf, document.Format.LineEndings);
            Assert.Equal(Indentation.Tabs(8), document.Indentation);
            Assert.Equal(Encoding.UTF8.CodePage, document.Format.Encoding.CodePage);
            Assert.False(document.Format.HasByteOrderMark);
        });
}

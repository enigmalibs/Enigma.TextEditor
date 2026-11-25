using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The document service's touches of the disk away from the UI thread: what is at a path, a save whose encoding and
/// write run on the thread pool while the text and its version are taken first, and hex views opened off it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class DocumentServiceIoTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheProbe_SaysFile_Folder_OrMissing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string file = harness.Folder.Write("a.txt", "a");
            string folder = Directory.CreateDirectory(harness.Folder.PathOf("notes")).FullName;

            Assert.Equal(PathKind.File, await harness.Documents.ProbeAsync(file));
            Assert.Equal(PathKind.Folder, await harness.Documents.ProbeAsync(folder));
            Assert.Equal(PathKind.Missing, await harness.Documents.ProbeAsync(harness.Folder.PathOf("gone.txt")));
            Assert.Equal(PathKind.Missing, await harness.Documents.ProbeAsync(string.Empty));
        });

    [Fact]
    public void ASave_WritesTheTextItTookFirst_AndAKeystrokeMeanwhileLeavesTheBufferDirty()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            tab.Document.Document.Insert(1, "b");

            Task<bool> saving = harness.Documents.SaveAsync(tab.Document);

            // The write is under way on the thread pool: this lands after the text was taken.
            tab.Document.Document.Insert(2, "c");
            Assert.True(await saving);

            Assert.Equal("ab", File.ReadAllText(path));
            Assert.True(tab.Document.IsDirty);
            Assert.Equal("abc", tab.Document.Document.Text);
        });

    [Fact]
    public void AHexViewOpenedTwiceAtOnce_IsOneBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("data.bin", new byte[] { 1, 2, 3 });

            Task<DocumentViewModel?> first = harness.Documents.OpenHexAsync(path);
            Task<DocumentViewModel?> second = harness.Documents.OpenHexAsync(path);
            DocumentViewModel?[] opened = await Task.WhenAll(first, second);

            Assert.NotNull(opened[0]);
            Assert.Same(opened[0], opened[1]);
            Assert.Equal(3, opened[0]!.Hex!.Length);
        });

    [Fact]
    public void AStartUpFile_InAFolderThatDoesNotExist_IsNamed_AndOneInAFolderThatDoes_IsANewBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string newFile = harness.Folder.PathOf("new.txt");
            string nowhere = Path.Combine(harness.Folder.PathOf("nowhere"), "new.txt");

            await harness.Intake.OpenStartupFilesAsync(
                harness.Workspace,
                new Core.Documents.StartupFiles([new(newFile, false), new(nowhere, false)], []));

            Assert.Contains(harness.Workspace.ActivePane.Tabs, tab => string.Equals(tab.Document.FilePath, newFile, StringComparison.Ordinal));
            Assert.Contains("is in a folder that does not exist", harness.StatusLine.Current, StringComparison.Ordinal);
        });
}

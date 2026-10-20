using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Files dropped on a pane: where they open, in which order, what is skipped and what is said about it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileDropTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Dropping_OpensTheFilesInThePaneDroppedOn_AndGivesItTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            EditorPaneViewModel right = harness.Workspace.ActivePane;
            string a = harness.Folder.Write("a.txt", "a");
            string b = harness.Folder.Write("b.txt", "b");

            await left.OpenDroppedAsync([a, b]);

            Assert.Same(left, harness.Workspace.ActivePane);
            Assert.Equal(["a.txt", "b.txt"], left.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("b.txt", left.ActiveTab?.Document.Title);
            Assert.Null(Assert.Single(right.Tabs).Document.FilePath);
            Assert.Null(harness.StatusLine.Current);
        });

    [Fact]
    public void Dropping_SkipsFoldersAndPathsThatHaveGone_AndNamesThemOnce()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string folder = Directory.CreateDirectory(harness.Folder.PathOf("notes")).FullName;
            string a = harness.Folder.Write("a.txt", "a");

            await harness.Workspace.ActivePane.OpenDroppedAsync([folder, a, harness.Folder.PathOf("gone.txt")]);

            Assert.Equal(["a.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("Not opened: notes is a folder; gone.txt does not exist.", harness.StatusLine.Current);
        });

    [Fact]
    public void DroppingAFileAlreadyOpen_ShowsItsBuffer_RatherThanReadingItAgain()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(a))!;
            tab.Document.Document.Insert(1, " edited");
            harness.Workspace.NewDocument();

            await harness.Workspace.ActivePane.OpenDroppedAsync([a]);

            Assert.Same(tab, harness.Workspace.ActiveTab);
            Assert.Equal("a edited", tab.Document.Document.Text);
            Assert.Equal(2, harness.Registry.Documents.Count);
        });

    [Fact]
    public void DroppingABinaryFile_AsksAboutItAsOpenDoes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string image = harness.Folder.Write("image.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D]);

            await harness.Workspace.ActivePane.OpenDroppedAsync([image]);

            Assert.Equal(["binary:image.png"], harness.Binary.AskedAbout);
            Assert.Empty(harness.Workspace.ActivePane.Tabs);
        });
}

using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Splitting a pane to open dropped files in a pane of their own beside it, as files dropped on a pane's edge do: where
/// the new pane goes, what it holds, what the split pane keeps — and the splits that are refused or undone.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SplitWithFilesTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    [Theory]
    [InlineData(FocusDirection.Left)]
    [InlineData(FocusDirection.Down)]
    [InlineData(FocusDirection.Up)]
    [InlineData(FocusDirection.Right)]
    public void AFile_SplitsThePaneOnThatSide_IntoANewPaneWithTheFileAndTheKeyboard(FocusDirection side)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            string b = harness.Folder.Write("b.txt", "b");

            Assert.True(pane.CanSplitWithFiles());
            EditorPaneViewModel created = (await pane.SplitWithFilesAsync([b], side))!;

            Assert.NotNull(created);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(pane)!.Value, side));
            Assert.Equal(["b.txt"], created.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal([a], pane.Tabs);
            Assert.Same(created, harness.Workspace.ActivePane);
            Assert.True(created.IsActive);
            Assert.True(created.IsFramed);
            Assert.False(pane.IsActive);
            Assert.Null(harness.StatusLine.Current);
        });

    [Fact]
    public void SeveralFiles_MakeOneSplit_AndOpenInTheNewPaneInDropOrder_TheLastShown()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            string b = harness.Folder.Write("b.txt", "b");
            string c = harness.Folder.Write("c.txt", "c");

            EditorPaneViewModel created = (await pane.SplitWithFilesAsync([b, c], FocusDirection.Right))!;

            Assert.Equal([pane, created], harness.Workspace.Panes);
            Assert.Equal(["b.txt", "c.txt"], created.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("c.txt", created.ActiveTab?.Document.Title);
            Assert.Equal("c.txt", harness.Workspace.ActiveTab?.Document.Title);
        });

    [Fact]
    public void AFreshTab_OfTheSplitPane_StaysThere()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel fresh = harness.Workspace.NewDocument();
            EditorPaneViewModel pane = harness.Workspace.ActivePane;

            EditorPaneViewModel created = (await pane.SplitWithFilesAsync([harness.Folder.Write("b.txt", "b")], FocusDirection.Down))!;

            Assert.Same(fresh, Assert.Single(pane.Tabs));
            Assert.Equal(["b.txt"], created.Tabs.Select(tab => tab.Document.Title));
        });

    [Fact]
    public void AFileTheSplitPaneShows_GetsATabOfItsOwnInTheNewPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(path))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;

            EditorPaneViewModel created = (await pane.SplitWithFilesAsync([path], FocusDirection.Right))!;

            EditorTabViewModel copy = Assert.Single(created.Tabs);
            Assert.NotSame(a, copy);
            Assert.Same(a.Document, copy.Document);
            Assert.Equal([a], pane.Tabs);
            Assert.Equal(2, harness.Registry.ViewCount(a.Document));
            Assert.Single(harness.Registry.Documents);
        });

    [Fact]
    public void APaneWithNoTab_IsNotSplit()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            string b = harness.Folder.Write("b.txt", "b");

            Assert.False(pane.CanSplitWithFiles());
            Assert.Null(await pane.SplitWithFilesAsync([b], FocusDirection.Right));

            Assert.Same(pane, Assert.Single(harness.Workspace.Panes));
            Assert.Empty(pane.Tabs);
            Assert.Empty(harness.Registry.Documents);
        });

    [Fact]
    public void NoFileAmongThePaths_SplitsNothing_AndNamesThemOnce()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            string folder = Directory.CreateDirectory(harness.Folder.PathOf("notes")).FullName;
            string gone = harness.Folder.PathOf("gone.txt");

            Assert.Null(await pane.SplitWithFilesAsync([folder, gone], FocusDirection.Right));

            Assert.Same(pane, Assert.Single(harness.Workspace.Panes));
            Assert.Equal([a], pane.Tabs);
            Assert.Equal("Not opened: notes is a folder; gone.txt does not exist.", harness.StatusLine.Current);
        });

    [Fact]
    public void ABinaryFileDeclined_ClosesTheNewPaneAgain_AndGivesTheKeyboardBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            string image = harness.Folder.Write("image.png", Png);

            Assert.Null(await pane.SplitWithFilesAsync([image], FocusDirection.Left));

            Assert.Equal(["binary:image.png"], harness.Binary.AskedAbout);
            Assert.Same(pane, Assert.Single(harness.Workspace.Panes));
            Assert.Equal([a], pane.Tabs);
            Assert.Same(pane, harness.Workspace.ActivePane);
            Assert.True(pane.IsActive);
            Assert.False(pane.IsFramed);
        });

    [Fact]
    public void APaneOutsideTheWorkspace_IsNotSplit()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorPaneViewModel stray = new(harness.Workspace.Preferences, harness.Workspace);

            Assert.False(stray.CanSplitWithFiles());
            Assert.Null(await stray.SplitWithFilesAsync([harness.Folder.Write("b.txt", "b")], FocusDirection.Right));

            Assert.Single(harness.Workspace.Panes);
            Assert.Single(harness.Registry.Documents);
        });
}

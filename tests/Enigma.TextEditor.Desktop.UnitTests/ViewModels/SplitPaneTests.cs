using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The workspace's panes: splitting, closing, keeping one, moving the keyboard between them — and a
/// buffer two of them show.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SplitPaneTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ASplitOfAFile_OpensANewUntitledBufferInANewActivePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!;
            EditorPaneViewModel first = harness.Workspace.ActivePane;

            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Same(second, harness.Workspace.ActivePane);
            DocumentViewModel created = Assert.Single(second.Tabs).Document;
            Assert.NotSame(tab.Document, created);
            Assert.Null(created.FilePath);
            Assert.Equal(string.Empty, created.Document.Text);
            Assert.Same(tab, Assert.Single(first.Tabs));
            Assert.Equal(1, harness.Registry.ViewCount(tab.Document));
            Assert.False(first.IsActive);
            Assert.True(second.IsActive);
            Assert.True(second.IsFramed);
        });

    [Fact]
    public void ASplitOfAnUntitledBuffer_OpensAnotherOne()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            DocumentViewModel untitled = harness.Workspace.NewDocument().Document;
            untitled.Document.Text = "typed";

            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Rows);

            DocumentViewModel created = Assert.Single(second.Tabs).Document;
            Assert.NotSame(untitled, created);
            Assert.Null(created.FilePath);
            Assert.NotEqual(untitled.Title, created.Title);
            Assert.Equal(string.Empty, created.Document.Text);
            Assert.Equal("typed", untitled.Document.Text);
        });

    [Fact]
    public void ASplitOfAHexTab_OpensAnUntitledTextBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", "abc"));

            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

            DocumentViewModel created = Assert.Single(second.Tabs).Document;
            Assert.False(created.IsHex);
            Assert.Null(created.FilePath);
        });

    [Fact]
    public void ABufferShownInTwoPanes_TakesAnEditInEitherInBoth()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!.Document;
            EditorPaneViewModel first = harness.Workspace.ActivePane;
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Rows);
            harness.Workspace.Show(a);

            second.ActiveTab!.Document.Document.Insert(0, "x");

            Assert.Equal("xabc", first.ActiveTab!.Document.Document.Text);
            Assert.Equal(2, harness.Registry.ViewCount(a));
        });

    [Fact]
    public void ClosingAPane_WhoseDirtyBufferIsShownElsewhere_AsksNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!.Document;
            a.Document.Insert(0, "x");
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Show(a);

            bool closed = await harness.Workspace.ClosePaneAsync(second);

            Assert.True(closed);
            Assert.Single(harness.Workspace.Panes);
            Assert.Empty(harness.Unsaved.AskedAbout);
            Assert.True(harness.Workspace.ActiveDocument?.IsDirty);
        });

    [Fact]
    public void ClosingAPane_WithTheOnlyViewOfADirtyBuffer_AsksAndCancelKeepsThePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);
            (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!.Document.Document.Insert(0, "x");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);

            bool closed = await harness.Workspace.ClosePaneAsync(second);

            Assert.False(closed);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Equal(["b.txt"], harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void TheLastPane_DoesNotClose()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            Assert.False(await harness.Workspace.ClosePaneAsync(harness.Workspace.ActivePane));
            Assert.False(harness.Workspace.ClosePaneCommand.CanExecute(null));
        });

    [Fact]
    public void ClosingThePanesLastTab_ClosesThePane_AndTheLastPaneStaysEmpty()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            harness.Workspace.Split(SplitAxis.Columns);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.Quit));

            Assert.Single(harness.Workspace.Panes);
            Assert.Equal(0, harness.Shutdown.ExitRequests);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.Quit));

            Assert.Single(harness.Workspace.Panes);
            Assert.False(harness.Workspace.HasTabs);
            Assert.Equal(0, harness.Shutdown.ExitRequests);
        });

    [Theory]
    [InlineData(VimExCommand.Split)]
    [InlineData(VimExCommand.VerticalSplit)]
    public void ASplitOnAFileThatCannotBeOpened_LeavesNoEmptyPane(VimExCommand command)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            EditorPaneViewModel original = harness.Workspace.ActivePane;

            await original.ExecuteExAsync(new VimExRequest(command, "missing.txt"));

            Assert.Same(original, Assert.Single(harness.Workspace.Panes));
            Assert.Same(original, harness.Workspace.ActivePane);
            Assert.True(original.IsActive);
            Assert.Single(harness.Messages.Errors);
        });

    [Fact]
    public void TheSplitCommands_NeedATabToShow()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            Assert.False(harness.Workspace.SplitRightCommand.CanExecute(null));
            Assert.False(harness.Workspace.SplitDownCommand.CanExecute(null));

            EditorTabViewModel tab = harness.Workspace.NewDocument();
            Assert.True(harness.Workspace.SplitRightCommand.CanExecute(null));
            Assert.True(harness.Workspace.SplitDownCommand.CanExecute(null));

            await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);
            Assert.False(harness.Workspace.SplitRightCommand.CanExecute(null));
            Assert.False(harness.Workspace.SplitDownCommand.CanExecute(null));
        });

    [Fact]
    public void Only_ClosesEveryOtherPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Split(SplitAxis.Rows);
            EditorPaneViewModel keep = harness.Workspace.ActivePane;

            bool done = await harness.Workspace.OnlyAsync();

            Assert.True(done);
            Assert.Same(keep, Assert.Single(harness.Workspace.Panes));
            Assert.Same(keep, harness.Workspace.ActivePane);
            Assert.False(keep.IsFramed);
            Assert.Equal(1, harness.Registry.ViewCount(keep.ActiveTab!.Document));
        });

    [Fact]
    public void TheKeyboard_MovesByDirectionAndInReadingOrder()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);

            Assert.True(harness.Workspace.Focus(FocusDirection.Left));
            Assert.Same(left, harness.Workspace.ActivePane);
            Assert.False(harness.Workspace.Focus(FocusDirection.Up));

            harness.Workspace.FocusNext();
            Assert.Same(right, harness.Workspace.ActivePane);

            harness.Workspace.FocusPrevious();
            Assert.Same(left, harness.Workspace.ActivePane);
        });

    [Fact]
    public void ResizeAndEqualize_MoveTheSplit()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);

            Assert.True(harness.Workspace.Resize(SplitAxis.Columns, harness.Workspace.ResizeStep));
            Assert.Equal(0.45, split.Ratio, 9);
            Assert.False(harness.Workspace.Resize(SplitAxis.Rows, harness.Workspace.ResizeStep));

            harness.Workspace.Equalize();
            Assert.Equal(0.5, split.Ratio, 9);
        });

    [Fact]
    public void TheFileCommands_ActOnTheActivePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));

            Assert.Equal([null, harness.Folder.PathOf("b.txt")], second.Tabs.Select(tab => tab.Document.FilePath));
            Assert.Equal(["a.txt"], harness.Workspace.Panes[0].Tabs.Select(tab => tab.Document.Title));
            Assert.StartsWith("b.txt", harness.Window.Title, System.StringComparison.Ordinal);
        });
}

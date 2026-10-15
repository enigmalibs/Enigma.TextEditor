using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The workspace's panes: splitting, closing, keeping one, moving the keyboard between them — and the
/// buffers they share.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SplitPaneTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ASplit_ShowsTheSameBufferInANewActivePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!;
            tab.CaretOffset = 2;
            EditorPaneViewModel first = harness.Workspace.ActivePane;

            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Same(second, harness.Workspace.ActivePane);
            Assert.Same(tab.Document, second.ActiveTab?.Document);
            Assert.Equal(2, second.ActiveTab?.CaretOffset);
            Assert.Equal(2, harness.Registry.ViewCount(tab.Document));
            Assert.False(first.IsActive);
            Assert.True(second.IsActive);
            Assert.True(second.IsFramed);
        });

    [Fact]
    public void AnEditInOnePane_IsTheEditInTheOther()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            EditorPaneViewModel first = harness.Workspace.ActivePane;
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Rows);

            second.ActiveTab!.Document.Document.Insert(0, "x");

            Assert.Equal("xabc", first.ActiveTab!.Document.Document.Text);
        });

    [Fact]
    public void ClosingAPane_WhoseDirtyBufferIsShownElsewhere_AsksNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc")))!.Document.Document.Insert(0, "x");
            EditorPaneViewModel second = harness.Workspace.Split(SplitAxis.Columns);

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
    public void ClosingThePanesLastTab_ClosesThePane_AndQuitOnlyQuitsWithTheLastPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            harness.Workspace.Split(SplitAxis.Columns);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.Quit));

            Assert.Single(harness.Workspace.Panes);
            Assert.Equal(0, harness.Shutdown.ExitRequests);

            await harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(VimExCommand.Quit));

            Assert.Equal(1, harness.Shutdown.ExitRequests);
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

            Assert.True(harness.Workspace.Resize(SplitAxis.Columns, WorkspaceViewModel.ResizeStep));
            Assert.Equal(0.45, split.Ratio, 9);
            Assert.False(harness.Workspace.Resize(SplitAxis.Rows, WorkspaceViewModel.ResizeStep));

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

            Assert.Equal(["a.txt", "b.txt"], second.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal(["a.txt"], harness.Workspace.Panes[0].Tabs.Select(tab => tab.Document.Title));
            Assert.StartsWith("b.txt", harness.Window.Title, System.StringComparison.Ordinal);
        });
}

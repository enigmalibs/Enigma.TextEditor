using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Splitting a pane to give a tab a pane of its own beside it, as a tab dropped on a pane's edge does: where the new
/// pane goes, what it holds, the pane the tab leaves — and the splits that are refused.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SplitWithTabTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(FocusDirection.Left)]
    [InlineData(FocusDirection.Down)]
    [InlineData(FocusDirection.Up)]
    [InlineData(FocusDirection.Right)]
    public void AnotherPanesTab_SplitsTheTargetOnThatSide_IntoANewPaneWithTheKeyboard(FocusDirection side)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "bbb")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            EditorTabViewModel untitled = right.Tabs.Single();
            harness.Workspace.Activate(left);
            b.CaretOffset = 2;

            Assert.True(right.CanSplitWithTab(b));
            EditorPaneViewModel created = right.SplitWithTab(b, side)!;

            Assert.NotNull(created);
            Assert.Equal(3, harness.Workspace.Panes.Count);
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(right)!.Value, side));
            Assert.Same(b, Assert.Single(created.Tabs));
            Assert.Same(b, created.ActiveTab);
            Assert.Equal(2, b.CaretOffset);
            Assert.Equal(["a.txt"], left.Tabs.Select(tab => tab.Document.Title));
            Assert.Same(untitled, Assert.Single(right.Tabs));
            Assert.Same(created, harness.Workspace.ActivePane);
            Assert.True(created.IsActive);
            Assert.False(left.IsActive);
            Assert.Equal(1, harness.Registry.ViewCount(b.Document));
        });

    [Fact]
    public void APanesLastTab_SplitBesideAnotherPane_ClosesThePaneItLeft()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);

            EditorPaneViewModel created = right.SplitWithTab(a, FocusDirection.Down)!;

            Assert.Equal([right, created], harness.Workspace.Panes);
            Assert.Null(harness.Workspace.IdOf(left));
            Assert.Same(a, Assert.Single(created.Tabs));
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(right)!.Value, FocusDirection.Down));
            Assert.Same(created, harness.Workspace.ActivePane);
        });

    [Fact]
    public void ATab_SplitsItsOwnPane_WhenThePaneKeepsAnother()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;

            Assert.True(pane.CanSplitWithTab(b));
            EditorPaneViewModel created = pane.SplitWithTab(b, FocusDirection.Right)!;

            Assert.Equal([pane, created], harness.Workspace.Panes);
            Assert.Equal([a], pane.Tabs);
            Assert.Same(b, Assert.Single(created.Tabs));
            Assert.Same(created, harness.Workspace.ActivePane);
        });

    [Fact]
    public void APanesOnlyTab_CannotSplitThatPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;

            Assert.False(pane.CanSplitWithTab(a));
            Assert.Null(pane.SplitWithTab(a, FocusDirection.Right));

            Assert.Same(pane, Assert.Single(harness.Workspace.Panes));
            Assert.Equal([a], pane.Tabs);
            Assert.IsType<PaneNode>(harness.Workspace.Layout.Root);
        });

    [Fact]
    public void ABufferTheTargetAlsoShows_StillMovesToTheNewPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            EditorTabViewModel otherA = harness.Workspace.Show(a.Document);
            harness.Workspace.Activate(left);

            EditorPaneViewModel created = right.SplitWithTab(a, FocusDirection.Right)!;

            Assert.Same(a, Assert.Single(created.Tabs));
            Assert.Contains(otherA, right.Tabs);
            Assert.Equal(["b.txt"], left.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal(2, harness.Registry.ViewCount(a.Document));
        });

    [Fact]
    public void ATabNoLongerOpen_OrAPaneOutsideTheWorkspace_SplitsNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!;
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            EditorPaneViewModel stray = new(harness.Workspace.Preferences, harness.Workspace);
            Assert.True(await harness.Workspace.CloseTabAsync(pane, b));

            Assert.False(pane.CanSplitWithTab(b));
            Assert.Null(pane.SplitWithTab(b, FocusDirection.Right));
            Assert.False(stray.CanSplitWithTab(a));
            Assert.Null(stray.SplitWithTab(a, FocusDirection.Right));

            Assert.Same(pane, Assert.Single(harness.Workspace.Panes));
            Assert.Equal([a], pane.Tabs);
        });
}

using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Moving a tab from one pane to another: the tab itself, with its place in the buffer; the pane it
/// leaves; a buffer the other pane already shows — and <c>Space m</c> and a direction, typed in the window.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MoveTabTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void MovingATab_TakesTheTabItselfToTheEndOfTheOtherStrip_WhichTakesTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "bbb")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(left);
            b.CaretOffset = 2;
            b.VerticalScrollOffset = 40;

            bool moved = harness.Workspace.MoveTab(FocusDirection.Right);

            Assert.True(moved);
            Assert.Equal(["a.txt"], left.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("a.txt", left.ActiveTab?.Document.Title);
            Assert.Same(b, right.Tabs[^1]);
            Assert.Same(b, right.ActiveTab);
            Assert.Equal(2, b.CaretOffset);
            Assert.Equal(40, b.VerticalScrollOffset);
            Assert.Same(right, harness.Workspace.ActivePane);
            Assert.Same(b, harness.Workspace.ActiveTab);
            Assert.Equal(1, harness.Registry.ViewCount(b.Document));
        });

    [Fact]
    public void MovingThePanesLastTab_ClosesThePaneItLeft()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel top = harness.Workspace.ActivePane;
            EditorPaneViewModel bottom = harness.Workspace.Split(SplitAxis.Rows);
            harness.Workspace.Activate(top);

            Assert.True(harness.Workspace.MoveTab(FocusDirection.Down));

            Assert.Same(bottom, Assert.Single(harness.Workspace.Panes));
            Assert.IsType<PaneNode>(harness.Workspace.Layout.Root);
            Assert.Same(a, bottom.ActiveTab);
            Assert.Equal(2, bottom.Tabs.Count);
            Assert.Same(bottom, harness.Workspace.ActivePane);
        });

    [Fact]
    public void ABufferTheOtherPaneAlreadyShows_IsShownThere_NotTwice()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            EditorTabViewModel otherA = harness.Workspace.Show(a.Document);
            EditorTabViewModel untitled = right.Tabs[0];
            right.ActiveTab = untitled;
            harness.Workspace.Activate(left);
            left.ActiveTab = a;
            a.Document.Document.Insert(0, "x");

            Assert.True(harness.Workspace.MoveTab(FocusDirection.Right));

            Assert.Equal(["b.txt"], left.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal([untitled, otherA], right.Tabs);
            Assert.Same(otherA, right.ActiveTab);
            Assert.Equal(1, harness.Registry.ViewCount(a.Document));

            // The buffer stays open, changes and all: nothing was closed that would lose them.
            Assert.Empty(harness.Unsaved.AskedAbout);
            Assert.True(otherA.Document.IsDirty);
        });

    [Fact]
    public void ADirtyBufferShownOnlyByTheMovedTab_MovesWithoutAQuestion()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            a.Document.Document.Insert(0, "x");
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(left);

            Assert.True(harness.Workspace.MoveTab(FocusDirection.Right));

            Assert.Empty(harness.Unsaved.AskedAbout);
            Assert.Same(a, right.ActiveTab);
            Assert.Equal("xa", a.Document.Document.Text);
            Assert.Equal(1, harness.Registry.ViewCount(a.Document));
        });

    [Fact]
    public void WithNoPaneOnThatSide_NothingMoves()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(left);

            Assert.False(harness.Workspace.MoveTab(FocusDirection.Left));
            Assert.False(harness.Workspace.MoveTab(FocusDirection.Up));
            Assert.False(harness.Workspace.MoveTab(FocusDirection.Down));

            Assert.Equal([a], left.Tabs);
            Assert.Single(right.Tabs);
            Assert.Same(left, harness.Workspace.ActivePane);
        });

    [Fact]
    public void WithOnePane_NothingMoves()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            Assert.False(harness.Workspace.MoveTab(FocusDirection.Right));

            Assert.Same(a, Assert.Single(Assert.Single(harness.Workspace.Panes).Tabs));
        });

    [Fact]
    public void ATabMovedToItsOwnPane_OrFromAPaneItIsNotOn_StaysWhereItIs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);

            Assert.False(harness.Workspace.MoveTab(left, a, left));
            Assert.False(harness.Workspace.MoveTab(right, a, left));

            Assert.Equal([a], left.Tabs);
            Assert.Single(right.Tabs);
            Assert.Same(right, harness.Workspace.ActivePane);
        });

    [Theory]
    [InlineData(SplitAxis.Columns, VimWindowCommand.MoveTabRight, VimWindowCommand.MoveTabLeft)]
    [InlineData(SplitAxis.Rows, VimWindowCommand.MoveTabDown, VimWindowCommand.MoveTabUp)]
    public void TheWindowCommands_MoveTheTabThereAndBack(SplitAxis axis, VimWindowCommand there, VimWindowCommand back)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!;
            EditorPaneViewModel first = harness.Workspace.ActivePane;
            EditorPaneViewModel second = harness.Workspace.Split(axis);

            // Typed in the first pane, whatever had the keyboard before.
            await first.ExecuteWindowAsync(there);
            Assert.Same(b, second.ActiveTab);
            Assert.Same(second, harness.Workspace.ActivePane);

            await second.ExecuteWindowAsync(back);
            Assert.Same(b, first.ActiveTab);
            Assert.Same(first, harness.Workspace.ActivePane);
            Assert.Equal(2, harness.Workspace.Panes.Count);
        });

    [Fact]
    public void TypedInTheWindow_SpaceMLAndMH_MoveTheTabBetweenSideBySidePanes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "bbb\n")))!;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();

            await FocusAsync(window, left);
            window.KeyTextInput("ll");
            await Pump();
            Assert.Equal(2, b.CaretOffset);

            window.KeyTextInput(" ml");
            await Pump();

            Assert.Same(b, right.ActiveTab);
            Assert.Same(right, harness.Workspace.ActivePane);
            EditorPaneView rightView = ViewOf(window, right);
            Assert.Same(b, rightView.CurrentTab);
            Assert.Equal(2, rightView.EditorControl.CaretOffset);
            Assert.Equal("bbb\n", b.Document.Document.Text);

            // The keyboard came with the tab: the next keys are typed in the right pane, and send it back.
            window.KeyTextInput(" mh");
            await Pump();

            Assert.Same(b, left.ActiveTab);
            Assert.Same(left, harness.Workspace.ActivePane);
            Assert.Equal(["a.txt", "b.txt"], left.Tabs.Select(tab => tab.Document.Title));
            window.Close();
        });

    private static EditorPaneView ViewOf(MainWindow window, EditorPaneViewModel pane)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single(view => ReferenceEquals(view.DataContext, pane));

    private static async Task FocusAsync(MainWindow window, EditorPaneViewModel pane)
    {
        await Pump();
        ViewOf(window, pane).FocusEditor();
        await Pump();
    }

    private static async Task Pump()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }
}

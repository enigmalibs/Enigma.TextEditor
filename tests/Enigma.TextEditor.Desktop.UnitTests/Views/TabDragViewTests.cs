using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Tabs dragged between panes on the real window, through the real input system: where a drag starts, the cursor
/// it wears, where a dropped tab goes, and the panes that split for a tab dropped near their edge.
/// </summary>
/// <remarks>
/// The drag is the pane's own gesture, not a platform session, so the whole of it — press, moves, release — is played
/// with the headless mouse.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class TabDragViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void PressingATab_AndMovingPastTheDragDistance_StartsADrag_AndLettingGoEndsIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Point title = TitleOf(window, pane, b);

            // A small move is still a click.
            window.MouseDown(title, MouseButton.Left);
            window.MouseMove(title + new Point(2, 1), RawInputModifiers.LeftMouseButton);
            Assert.False(pane.IsDraggingTab);

            window.MouseMove(title + new Point(30, 0), RawInputModifiers.LeftMouseButton);
            Assert.True(pane.IsDraggingTab);

            window.MouseUp(title + new Point(30, 0), MouseButton.Left);
            await DrainAsync();

            Assert.False(pane.IsDraggingTab);
            Assert.Equal(["a.txt", "b.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            window.Close();
        });

    [Fact]
    public void Dropping_ATab_OnTheOtherPanesText_MovesItThere_AndGivesThatPaneTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);

            Carry(window, TitleOf(window, left, b), Middle(right, window));
            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Equal(["a.txt"], leftPane.Tabs.Select(tab => tab.Document.Title));
            Assert.Same(b, rightPane.ActiveTab);
            Assert.Same(rightPane, harness.Workspace.ActivePane);
            Assert.Same(b, right.CurrentTab);
            window.Close();
        });

    [Fact]
    public void Dropping_APanesLastTab_OnTheOtherPanesStrip_MovesIt_AndClosesThePaneItLeft()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            harness.Workspace.Split(SplitAxis.Rows);
            MainWindow window = Show(harness);
            (EditorPaneView top, EditorPaneView bottom) = Panes(window);
            EditorPaneViewModel bottomPane = (EditorPaneViewModel)bottom.DataContext!;

            Carry(window, TitleOf(window, top, a), OnStrip(bottom, window));
            window.MouseUp(OnStrip(bottom, window), MouseButton.Left);
            await DrainAsync();

            Assert.Same(bottomPane, Assert.Single(harness.Workspace.Panes));
            Assert.Same(a, bottomPane.ActiveTab);
            Assert.Single(window.GetVisualDescendants().OfType<EditorPaneView>());
            window.Close();
        });

    [Fact]
    public void LettingGoOfATab_OverItsOwnPane_OrOffThePanes_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, _) = Panes(window);
            Point outside = new(-40, -40);

            Carry(window, TitleOf(window, left, a), Middle(left, window));
            window.MouseUp(Middle(left, window), MouseButton.Left);
            await DrainAsync();

            Carry(window, TitleOf(window, left, a), outside);
            window.MouseUp(outside, MouseButton.Left);
            await DrainAsync();

            Assert.Equal([a], leftPane.Tabs);
            Assert.Single(rightPane.Tabs);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.False(left.IsDraggingTab);
            window.Close();
        });

    [Fact]
    public void ACarriedTab_WearsTheDragCursorOverEveryPane_ItsOwnOffThem_AndTheStripsOwnAfterwards()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            ListBox strip = StripOf(left);
            Cursor? before = strip.Cursor;
            Point outside = new(-40, -40);

            Carry(window, TitleOf(window, left, a), Middle(left, window));
            Assert.Same(EditorPaneView.CarryingCursor, strip.Cursor);

            window.MouseMove(Middle(right, window), RawInputModifiers.LeftMouseButton);
            Assert.Same(EditorPaneView.CarryingCursor, strip.Cursor);

            window.MouseMove(outside, RawInputModifiers.LeftMouseButton);
            Assert.Same(before, strip.Cursor);

            window.MouseMove(OnStrip(right, window), RawInputModifiers.LeftMouseButton);
            Assert.Same(EditorPaneView.CarryingCursor, strip.Cursor);

            window.MouseUp(OnStrip(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Same(before, strip.Cursor);
            window.Close();
        });

    [Fact]
    public void ADragFromTheCloseButton_OrWithTheRightButton_IsNotATabDrag()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            EditorPaneViewModel leftPane = (EditorPaneViewModel)left.DataContext!;
            Button close = ItemOf(left, a).GetVisualDescendants().OfType<Button>().Single();
            Point onClose = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            Point title = TitleOf(window, left, a);

            window.MouseDown(onClose, MouseButton.Left);
            window.MouseMove(onClose + new Point(40, 0), RawInputModifiers.LeftMouseButton);
            Assert.False(left.IsDraggingTab);
            window.MouseMove(Middle(right, window), RawInputModifiers.LeftMouseButton);
            window.MouseUp(Middle(right, window), MouseButton.Left);

            window.MouseDown(title, MouseButton.Right);
            window.MouseMove(title + new Point(40, 0), RawInputModifiers.RightMouseButton);
            Assert.False(left.IsDraggingTab);
            window.MouseMove(Middle(right, window), RawInputModifiers.RightMouseButton);
            window.MouseUp(Middle(right, window), MouseButton.Right);
            await DrainAsync();

            Assert.Same(a, Assert.Single(leftPane.Tabs));
            Assert.Equal(2, harness.Workspace.Panes.Count);
            window.Close();
        });

    [Fact]
    public void Escape_DuringADrag_CancelsIt_WithoutReachingVim()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            ListBox strip = StripOf(left);
            Cursor? before = strip.Cursor;
            await InsertModeAsync(window, left);

            Carry(window, TitleOf(window, left, a), Middle(right, window));
            Assert.True(left.IsDraggingTab);

            // The keyboard in the editor, where Vim would take an Escape for its own.
            left.FocusEditor();
            Dispatcher.UIThread.RunJobs();
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

            Assert.False(left.IsDraggingTab);
            Assert.Same(before, strip.Cursor);
            Assert.Equal(VimMode.Insert, left.Vim!.Mode);

            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Equal([a], leftPane.Tabs);
            Assert.Single(rightPane.Tabs);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            window.Close();
        });

    [Fact]
    public void ADragCancelledWithEscape_CanBeStartedAgain_AndDropped()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);

            Carry(window, TitleOf(window, left, b), Middle(right, window));
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();
            Assert.Equal(2, leftPane.Tabs.Count);

            Carry(window, TitleOf(window, left, b), Middle(right, window));
            Assert.True(left.IsDraggingTab);
            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Equal(["a.txt"], leftPane.Tabs.Select(tab => tab.Document.Title));
            Assert.Same(b, rightPane.ActiveTab);
            window.Close();
        });

    [Fact]
    public void Escape_WithNoDrag_StillReachesTheEditor()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            await InsertModeAsync(window, pane);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await DrainAsync();

            Assert.Equal(VimMode.Normal, pane.Vim!.Mode);
            window.Close();
        });

    [Fact]
    public void ACarriedTab_MarksThePaneItWouldMoveTo_AndNoOther()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            Point outside = new(-40, -40);

            Assert.False(left.IsDropTarget);
            Assert.False(right.IsDropTarget);

            Carry(window, TitleOf(window, left, b), Middle(right, window));
            Assert.True(right.IsDropTarget);
            Assert.False(left.IsDropTarget);

            // Over the pane it came from, the tab has nowhere to go; off the panes, nothing to say.
            window.MouseMove(Middle(left, window), RawInputModifiers.LeftMouseButton);
            Assert.False(right.IsDropTarget);
            Assert.False(left.IsDropTarget);

            window.MouseMove(outside, RawInputModifiers.LeftMouseButton);
            Assert.False(right.IsDropTarget);
            Assert.False(left.IsDropTarget);

            window.MouseMove(OnStrip(right, window), RawInputModifiers.LeftMouseButton);
            Assert.True(right.IsDropTarget);

            window.MouseUp(OnStrip(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Same(b, ((EditorPaneViewModel)right.DataContext!).ActiveTab);
            Assert.False(right.IsDropTarget);
            Assert.False(left.IsDropTarget);
            window.Close();
        });

    [Fact]
    public void TheMark_NeverStaysBehind_AfterEscape_OrALostCapture()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            IPointer? mouse = null;
            window.AddHandler(InputElement.PointerMovedEvent, (_, e) => mouse = e.Pointer, RoutingStrategies.Tunnel, handledEventsToo: true);

            Carry(window, TitleOf(window, left, a), Middle(right, window));
            Assert.True(right.IsDropTarget);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Assert.False(right.IsDropTarget);
            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();

            // Another control taking the pointer ends the drag where it is.
            Carry(window, TitleOf(window, left, a), Middle(right, window));
            Assert.True(right.IsDropTarget);
            mouse!.Capture(right);
            Assert.False(left.IsDraggingTab);
            Assert.False(right.IsDropTarget);
            window.MouseUp(Middle(right, window), MouseButton.Left);
            await DrainAsync();

            Assert.Equal([a], leftPane.Tabs);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            window.Close();
        });

    [Fact]
    public void ACarriedTab_NearAnotherPanesEdge_MarksTheHalfThatWouldSplitOff_AndInItsMiddle_TheWholePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            Point outside = new(-40, -40);

            Carry(window, TitleOf(window, left, b), NearEdge(right, window, FocusDirection.Right));
            Assert.True(right.IsDropTarget);
            Assert.Equal(FocusDirection.Right, right.DropSide);
            Assert.False(left.IsDropTarget);
            AssertHintCovers(right, new Rect(0.5, 0, 0.5, 1));

            window.MouseMove(Middle(right, window), RawInputModifiers.LeftMouseButton);
            Assert.True(right.IsDropTarget);
            Assert.Null(right.DropSide);
            AssertHintCovers(right, new Rect(0, 0, 1, 1));

            window.MouseMove(NearEdge(right, window, FocusDirection.Up), RawInputModifiers.LeftMouseButton);
            Assert.Equal(FocusDirection.Up, right.DropSide);
            AssertHintCovers(right, new Rect(0, 0, 1, 0.5));

            window.MouseMove(NearEdge(right, window, FocusDirection.Down), RawInputModifiers.LeftMouseButton);
            Assert.Equal(FocusDirection.Down, right.DropSide);
            AssertHintCovers(right, new Rect(0, 0.5, 1, 0.5));

            window.MouseMove(NearEdge(right, window, FocusDirection.Left), RawInputModifiers.LeftMouseButton);
            Assert.Equal(FocusDirection.Left, right.DropSide);
            AssertHintCovers(right, new Rect(0, 0, 0.5, 1));

            window.MouseUp(outside, MouseButton.Left);
            await DrainAsync();

            Assert.False(right.IsDropTarget);
            Assert.Null(right.DropSide);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            window.Close();
        });

    [Fact]
    public void LettingGoNearAnotherPanesEdge_SplitsThatPane_WithTheTabInTheNewHalf()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);

            Carry(window, TitleOf(window, left, b), NearEdge(right, window, FocusDirection.Right));
            window.MouseUp(NearEdge(right, window, FocusDirection.Right), MouseButton.Left);
            await DrainAsync();

            EditorPaneViewModel created = harness.Workspace.ActivePane;
            Assert.Equal(3, harness.Workspace.Panes.Count);
            Assert.Same(b, Assert.Single(created.Tabs));
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(rightPane)!.Value, FocusDirection.Right));
            Assert.Equal(["a.txt"], leftPane.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal(3, window.GetVisualDescendants().OfType<EditorPaneView>().Count());
            Assert.False(right.IsDropTarget);
            window.Close();
        });

    [Fact]
    public void LettingGoNearItsOwnPanesBottomEdge_SplitsItBelow_WhenThePaneKeepsAnotherTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel panePane = harness.Workspace.ActivePane;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            Carry(window, TitleOf(window, pane, b), NearEdge(pane, window, FocusDirection.Down));
            Assert.True(pane.IsDropTarget);
            Assert.Equal(FocusDirection.Down, pane.DropSide);

            window.MouseUp(NearEdge(pane, window, FocusDirection.Down), MouseButton.Left);
            await DrainAsync();

            EditorPaneViewModel created = harness.Workspace.ActivePane;
            Assert.Equal([panePane, created], harness.Workspace.Panes);
            Assert.Equal([a], panePane.Tabs);
            Assert.Same(b, Assert.Single(created.Tabs));
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(panePane)!.Value, FocusDirection.Down));
            Assert.False(pane.IsDropTarget);
            window.Close();
        });

    [Fact]
    public void APanesOnlyTab_NearItsOwnEdge_IsMarkedNowhere_AndSplitsNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorPaneViewModel panePane = harness.Workspace.ActivePane;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            Carry(window, TitleOf(window, pane, a), NearEdge(pane, window, FocusDirection.Right));
            Assert.True(pane.IsDraggingTab);
            Assert.False(pane.IsDropTarget);

            window.MouseUp(NearEdge(pane, window, FocusDirection.Right), MouseButton.Left);
            await DrainAsync();

            Assert.Same(panePane, Assert.Single(harness.Workspace.Panes));
            Assert.Equal([a], panePane.Tabs);
            window.Close();
        });

    [Fact]
    public void ATabLetGoOnAnotherPanesStrip_NearItsLeftEnd_MovesThere_RatherThanSplitting()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            Point stripStart = right.TranslatePoint(new Point(10, 15), window)!.Value;

            Carry(window, TitleOf(window, left, b), stripStart);
            Assert.True(right.IsDropTarget);
            Assert.Null(right.DropSide);

            window.MouseUp(stripStart, MouseButton.Left);
            await DrainAsync();

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Same(b, rightPane.ActiveTab);
            Assert.Equal(["a.txt"], leftPane.Tabs.Select(tab => tab.Document.Title));
            window.Close();
        });

    [Fact]
    public void Escape_NearAnEdge_ClearsTheHalf_AndSplitsNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);

            Carry(window, TitleOf(window, left, b), NearEdge(right, window, FocusDirection.Left));
            Assert.Equal(FocusDirection.Left, right.DropSide);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Assert.False(right.IsDropTarget);
            Assert.Null(right.DropSide);

            window.MouseUp(NearEdge(right, window, FocusDirection.Left), MouseButton.Left);
            await DrainAsync();

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Equal(2, leftPane.Tabs.Count);
            window.Close();
        });

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(4, 4, false)] // On the edge of the distance: still a click.
    [InlineData(-4, 0, false)]
    [InlineData(5, 0, true)]
    [InlineData(0, -5, true)]
    [InlineData(-30, 2, true)]
    public void APress_IsADrag_OncePastTheDistanceOnEitherAxis(double x, double y, bool expected)
        => Assert.Equal(expected, TabDragGesture.IsDrag(new Vector(x, y), new Size(4, 4)));

    [Theory]
    [InlineData(true, StandardCursorType.DragMove)] // Over a pane, its own included: the drag is under way here.
    [InlineData(false, null)] // Off the panes: the strip keeps its own.
    public void TheDragPointer_SaysTheDragIsUnderWay_AndNeverRefuses(bool overAPane, StandardCursorType? expected)
    {
        // Never StandardCursorType.No: the refusal pointer is what the gesture exists to be rid of.
        Assert.Equal(expected, TabDragGesture.CursorFor(overAPane));
        Assert.NotEqual(StandardCursorType.No, TabDragGesture.CursorFor(overAPane));
    }

    [Theory]
    [InlineData(50, 50, 100, 100, null)] // The middle.
    [InlineData(10, 50, 100, 100, FocusDirection.Left)]
    [InlineData(90, 50, 100, 100, FocusDirection.Right)]
    [InlineData(50, 10, 100, 100, FocusDirection.Up)]
    [InlineData(50, 90, 100, 100, FocusDirection.Down)]
    [InlineData(5, 20, 100, 100, FocusDirection.Left)] // A corner goes to the nearer side.
    [InlineData(20, 5, 100, 100, FocusDirection.Up)]
    [InlineData(95, 85, 100, 100, FocusDirection.Right)]
    [InlineData(85, 95, 100, 100, FocusDirection.Down)]
    [InlineData(25, 50, 100, 100, null)] // On the boundary: still the middle.
    [InlineData(75, 50, 100, 100, null)]
    [InlineData(50, 75, 100, 100, null)]
    [InlineData(40, 30, 200, 100, FocusDirection.Left)] // Measured as shares: 20 % from the left beats 30 % from the top.
    [InlineData(60, 20, 200, 100, FocusDirection.Up)] // 20 % from the top beats 30 % from the left.
    public void TheSideATabWouldSplitAPaneOn_IsTheNearerEdgeWithinAQuarter(double x, double y, double width, double height, FocusDirection? expected)
        => Assert.Equal(expected, TabDragGesture.SideFor(new Point(x, y), new Size(width, height)));

    [Fact]
    public void ContentWithNoWidthOrHeight_HasNoSides()
    {
        Assert.Null(TabDragGesture.SideFor(new Point(0, 0), new Size(0, 100)));
        Assert.Null(TabDragGesture.SideFor(new Point(0, 0), new Size(100, 0)));
    }

    // Presses the point, carries it past the drag distance along the strip, then on to where it is going, without
    // letting go.
    private static void Carry(MainWindow window, Point from, Point to)
    {
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(from + new Point(30, 0), RawInputModifiers.LeftMouseButton);
        window.MouseMove(to, RawInputModifiers.LeftMouseButton);
    }

    // Gives the pane's editor the keyboard and types 'i': an Escape that reaches Vim from there leaves insert mode.
    private static async Task InsertModeAsync(MainWindow window, EditorPaneView pane)
    {
        await DrainAsync();
        pane.FocusEditor();
        await DrainAsync();
        window.KeyTextInput("i");
        await DrainAsync();
        Assert.Equal(VimMode.Insert, pane.Vim!.Mode);
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static (EditorPaneView First, EditorPaneView Second) Panes(MainWindow window)
    {
        EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];

        return (panes[0], panes[1]);
    }

    private static ListBox StripOf(EditorPaneView pane) => pane.FindControl<ListBox>("Tabs")!;

    private static ListBoxItem ItemOf(EditorPaneView pane, EditorTabViewModel tab)
        => pane.GetVisualDescendants().OfType<ListBoxItem>().Single(item => ReferenceEquals(item.DataContext, tab));

    private static Point TitleOf(MainWindow window, EditorPaneView pane, EditorTabViewModel tab)
    {
        TextBlock title = ItemOf(pane, tab).GetVisualDescendants().OfType<TextBlock>().First();

        return title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
    }

    private static Point Middle(EditorPaneView pane, MainWindow window)
        => pane.TranslatePoint(new Point(pane.Bounds.Width / 2, pane.Bounds.Height / 2), window)!.Value;

    // On the tab strip, right of the tabs: the strip is the top band of the pane.
    private static Point OnStrip(EditorPaneView pane, MainWindow window)
        => pane.TranslatePoint(new Point(pane.Bounds.Width - 20, 15), window)!.Value;

    // Near one edge of the pane's text — below the strip — well within the quarter that splits the pane on that side.
    private static Point NearEdge(EditorPaneView pane, MainWindow window, FocusDirection side)
    {
        Border strip = pane.FindControl<Border>("Strip")!;
        double top = strip.IsVisible ? strip.TranslatePoint(new Point(0, strip.Bounds.Height), pane)!.Value.Y : 0;
        double middle = (top + pane.Bounds.Height) / 2;

        Point inPane = side switch
        {
            FocusDirection.Left => new Point(10, middle),
            FocusDirection.Right => new Point(pane.Bounds.Width - 10, middle),
            FocusDirection.Up => new Point(pane.Bounds.Width / 2, top + 10),
            _ => new Point(pane.Bounds.Width / 2, pane.Bounds.Height - 10),
        };

        return pane.TranslatePoint(inPane, window)!.Value;
    }

    // The hint, laid out, covers that part of the pane: shares of its width and height, to within the pane's 1 px frame.
    private static void AssertHintCovers(EditorPaneView pane, Rect share)
    {
        Panel hint = pane.FindControl<Panel>("DropHint")!;
        pane.UpdateLayout();
        Point origin = hint.TranslatePoint(new Point(0, 0), pane)!.Value;
        double width = pane.Bounds.Width;
        double height = pane.Bounds.Height;

        Assert.Equal(share.X * width, origin.X, 2.0);
        Assert.Equal(share.Y * height, origin.Y, 2.0);
        Assert.Equal(share.Width * width, hint.Bounds.Width, 2.0);
        Assert.Equal(share.Height * height, hint.Bounds.Height, 2.0);
    }

    private static async Task DrainAsync()
    {
        for (int i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }

        Dispatcher.UIThread.RunJobs();
    }
}

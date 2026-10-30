using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Tabs dragged between panes on the real window: where a drag starts, the cursor it gets over each pane, and
/// where a dropped tab goes.
/// </summary>
/// <remarks>
/// The headless platform has no drag source, so the start of a drag is read from the pane's
/// <see cref="EditorPaneView.StartDrag"/>, and the drop is played as the platform would play it.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class TabDragViewTests(HeadlessAvaloniaFixture fixture)
{
    private const DragDropEffects Allowed = DragDropEffects.Move;

    [Fact]
    public void Hovering_ADraggedTab_OffersAMoveOverTheOtherPane_AndNothingOverItsOwn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);

            Assert.Equal(DragDropEffects.Move, Hover(window, Middle(right, window), DraggedTab.Carrying(a)));
            Assert.Equal(DragDropEffects.Move, Hover(window, OnStrip(right, window), DraggedTab.Carrying(a)));
            Assert.Equal(DragDropEffects.None, Hover(window, Middle(left, window), DraggedTab.Carrying(a)));
            Assert.Equal(DragDropEffects.None, Hover(window, OnStrip(left, window), DraggedTab.Carrying(a)));
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
            (_, EditorPaneView right) = Panes(window);
            DragDropEffects reported = DragDropEffects.None;
            window.AddHandler(DragDrop.DropEvent, (_, e) => reported = e.DragEffects, RoutingStrategies.Bubble, handledEventsToo: true);

            Drop(window, Middle(right, window), DraggedTab.Carrying(b));
            await DrainAsync();

            Assert.Equal(DragDropEffects.Move, reported);
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
            (_, EditorPaneView bottom) = Panes(window);
            EditorPaneViewModel bottomPane = (EditorPaneViewModel)bottom.DataContext!;

            Drop(window, OnStrip(bottom, window), DraggedTab.Carrying(a));
            await DrainAsync();

            Assert.Same(bottomPane, Assert.Single(harness.Workspace.Panes));
            Assert.Same(a, bottomPane.ActiveTab);
            Assert.Single(window.GetVisualDescendants().OfType<EditorPaneView>());
            window.Close();
        });

    [Fact]
    public void Dropping_ATab_OnItsOwnPane_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, _) = Panes(window);

            Drop(window, Middle(left, window), DraggedTab.Carrying(a));
            await DrainAsync();

            Assert.Equal([a], leftPane.Tabs);
            Assert.Single(rightPane.Tabs);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Same(rightPane, harness.Workspace.ActivePane);
            window.Close();
        });

    [Fact]
    public void PressingATab_AndMovingPastTheDragDistance_StartsAMoveOfThatTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorTabViewModel b = (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            List<(EditorTabViewModel? Tab, DragDropEffects Allowed)> started = Record(pane);
            Point title = TitleOf(window, pane, b);

            // A small move is still a click.
            window.MouseDown(title, MouseButton.Left);
            window.MouseMove(title + new Point(2, 1), RawInputModifiers.LeftMouseButton);
            Assert.Empty(started);

            window.MouseMove(title + new Point(30, 0), RawInputModifiers.LeftMouseButton);
            window.MouseMove(title + new Point(60, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(title + new Point(60, 0), MouseButton.Left);
            await DrainAsync();

            (EditorTabViewModel? tab, DragDropEffects allowed) = Assert.Single(started);
            Assert.Same(b, tab);
            Assert.Equal(DragDropEffects.Move, allowed);
            window.Close();
        });

    [Fact]
    public void ADragFromTheCloseButton_OrWithTheRightButton_IsNotATabDrag()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            List<(EditorTabViewModel? Tab, DragDropEffects Allowed)> started = Record(pane);
            Button close = ItemOf(pane, a).GetVisualDescendants().OfType<Button>().Single();
            Point onClose = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            Point title = TitleOf(window, pane, a);

            window.MouseDown(onClose, MouseButton.Left);
            window.MouseMove(onClose + new Point(40, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(onClose + new Point(40, 0), MouseButton.Left);

            window.MouseDown(title, MouseButton.Right);
            window.MouseMove(title + new Point(40, 0), RawInputModifiers.RightMouseButton);
            window.MouseUp(title + new Point(40, 0), MouseButton.Right);
            await DrainAsync();

            Assert.Empty(started);
            Assert.Same(a, Assert.Single(harness.Workspace.ActivePane.Tabs));
            window.Close();
        });

    private static List<(EditorTabViewModel? Tab, DragDropEffects Allowed)> Record(EditorPaneView pane)
    {
        List<(EditorTabViewModel? Tab, DragDropEffects Allowed)> started = [];
        pane.StartDrag = (_, data, allowed) =>
        {
            started.Add((DraggedTab.Read(data), allowed));

            return Task.FromResult(DragDropEffects.None);
        };

        return started;
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

    private static DragDropEffects Hover(MainWindow window, Point point, DataTransfer data)
    {
        DragDropEffects last = DragDropEffects.None;
        void Record(object? sender, DragEventArgs e) => last = e.DragEffects;
        window.AddHandler(DragDrop.DragOverEvent, Record, RoutingStrategies.Bubble, handledEventsToo: true);
        window.DragDrop(point, RawDragEventType.DragEnter, data, Allowed);
        window.DragDrop(point, RawDragEventType.DragOver, data, Allowed);
        window.DragDrop(point, RawDragEventType.DragLeave, data, Allowed);
        window.RemoveHandler(DragDrop.DragOverEvent, Record);

        return last;
    }

    private static void Drop(MainWindow window, Point point, DataTransfer data)
    {
        window.DragDrop(point, RawDragEventType.DragEnter, data, Allowed);
        window.DragDrop(point, RawDragEventType.DragOver, data, Allowed);
        window.DragDrop(point, RawDragEventType.Drop, data, Allowed);
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

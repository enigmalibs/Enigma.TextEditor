using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Drops on the real window: the cursor a drag gets over a pane, and where dropped files open.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileDropViewTests(HeadlessAvaloniaFixture fixture)
{
    private const DragDropEffects Allowed = DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link;

    [Fact]
    public void Hovering_OffersACopyForFiles_AndLeavesTextToTheEditor()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n"));
            harness.Workspace.Split(SplitAxis.Columns, newDocument: false);
            MainWindow window = Show(harness);
            (EditorPaneView withText, EditorPaneView empty) = Panes(window);
            DataTransfer files = await CarryingAsync(window, harness.Folder.Write("b.txt", "b"));

            DragDropEffects overText = Hover(window, Middle(withText, window), files);
            DragDropEffects overEmpty = Hover(window, Middle(empty, window), files);
            DragDropEffects textOverEmpty = Hover(window, Middle(empty, window), CarryingText("hello"));
            DragDropEffects textOverText = Hover(window, Middle(withText, window), CarryingText("hello"));

            // A copy, never a move, though the source allows one: a file manager deletes what it moved.
            Assert.Equal(DragDropEffects.Copy, overText);
            Assert.Equal(DragDropEffects.Copy, overEmpty);
            Assert.Equal(DragDropEffects.None, textOverEmpty);

            // The editor's own text drag-and-drop is untouched: it still takes text dropped on it.
            Assert.NotEqual(DragDropEffects.None, textOverText);
            window.Close();
        });

    [Fact]
    public void Dropping_OnThePaneWithoutTheKeyboard_OpensTheFileThere_AndGivesItTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n"));
            harness.Workspace.Split(SplitAxis.Columns, newDocument: false);
            MainWindow window = Show(harness);
            (EditorPaneView withText, _) = Panes(window);
            EditorPaneViewModel target = (EditorPaneViewModel)withText.DataContext!;
            DragDropEffects reported = DragDropEffects.None;
            window.AddHandler(DragDrop.DropEvent, (_, e) => reported = e.DragEffects, RoutingStrategies.Bubble, handledEventsToo: true);

            Drop(window, Middle(withText, window), await CarryingAsync(window, harness.Folder.Write("b.txt", "b")));
            await DrainAsync();

            Assert.Equal(DragDropEffects.Copy, reported);
            Assert.Same(target, harness.Workspace.ActivePane);
            Assert.Equal(["a.txt", "b.txt"], target.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("b.txt", target.ActiveTab?.Document.Title);
            window.Close();
        });

    [Fact]
    public void DroppingText_OnAnEmptyPane_OpensNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            Drop(window, Middle(pane, window), CarryingText(harness.Folder.Write("a.txt", "a")));
            await DrainAsync();

            Assert.Empty(harness.Workspace.ActivePane.Tabs);
            window.Close();
        });

    [Fact]
    public void CarriedFiles_NearAnotherPanesEdge_MarkTheHalfThatWouldSplitOff_AndInItsMiddleOrOnItsStrip_TheWholePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n"));
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            (EditorPaneView left, EditorPaneView right) = Panes(window);
            DataTransfer files = await CarryingAsync(window, harness.Folder.Write("b.txt", "b"));

            Enter(window, Middle(left, window), files);
            Assert.True(left.IsDropTarget);
            Assert.Null(left.DropSide);

            (FocusDirection Side, Rect Share)[] halves =
            [
                (FocusDirection.Right, new Rect(0.5, 0, 0.5, 1)),
                (FocusDirection.Up, new Rect(0, 0, 1, 0.5)),
                (FocusDirection.Down, new Rect(0, 0.5, 1, 0.5)),
                (FocusDirection.Left, new Rect(0, 0, 0.5, 1)),
            ];

            foreach ((FocusDirection side, Rect share) in halves)
            {
                Over(window, NearEdge(right, window, side), files);

                Assert.True(right.IsDropTarget);
                Assert.Equal(side, right.DropSide);
                Assert.False(left.IsDropTarget);
                AssertHintCovers(right, share);
            }

            Over(window, Middle(right, window), files);
            Assert.True(right.IsDropTarget);
            Assert.Null(right.DropSide);
            AssertHintCovers(right, new Rect(0, 0, 1, 1));

            Over(window, OnStrip(right, window), files);
            Assert.True(right.IsDropTarget);
            Assert.Null(right.DropSide);

            Leave(window, OnStrip(right, window), files);
            Assert.False(right.IsDropTarget);
            Assert.False(left.IsDropTarget);
            window.Close();
        });

    [Fact]
    public void CarriedText_MarksNoPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            DataTransfer text = CarryingText("hello");

            Enter(window, Middle(pane, window), text);
            Over(window, NearEdge(pane, window, FocusDirection.Right), text);

            Assert.False(pane.IsDropTarget);
            Leave(window, NearEdge(pane, window, FocusDirection.Right), text);
            window.Close();
        });

    [Fact]
    public void DroppingFiles_NearAPanesRightEdge_SplitsIt_WithTheFilesInTheNewHalf_AndLeavesNoMark()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView view = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            EditorPaneViewModel pane = (EditorPaneViewModel)view.DataContext!;
            DataTransfer files = await CarryingAsync(window, harness.Folder.Write("b.txt", "b"), harness.Folder.Write("c.txt", "c"));

            Drop(window, NearEdge(view, window, FocusDirection.Right), files);
            await DrainAsync();

            Assert.False(view.IsDropTarget);
            Assert.Equal(2, harness.Workspace.Panes.Count);
            EditorPaneViewModel created = harness.Workspace.Panes.Single(candidate => !ReferenceEquals(candidate, pane));
            Assert.Equal(harness.Workspace.IdOf(created), harness.Workspace.Layout.FindNeighbor(harness.Workspace.IdOf(pane)!.Value, FocusDirection.Right));
            Assert.Equal(["b.txt", "c.txt"], created.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal([a], pane.Tabs);
            Assert.Same(created, harness.Workspace.ActivePane);
            window.Close();
        });

    [Fact]
    public void ThePaneWithNoTab_TakesFilesWhole_EvenNearItsEdge()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            EditorPaneView view = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            DataTransfer files = await CarryingAsync(window, harness.Folder.Write("b.txt", "b"));

            Enter(window, NearEdge(view, window, FocusDirection.Right), files);
            Assert.True(view.IsDropTarget);
            Assert.Null(view.DropSide);

            Drop(window, NearEdge(view, window, FocusDirection.Right), files);
            await DrainAsync();

            Assert.False(view.IsDropTarget);
            EditorPaneViewModel pane = Assert.Single(harness.Workspace.Panes);
            Assert.Equal(["b.txt"], pane.Tabs.Select(tab => tab.Document.Title));
            window.Close();
        });

    [Fact]
    public void AFolderDropped_NearAnEdge_SplitsNothing_AndIsNamed()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "some text\n"));
            MainWindow window = Show(harness);
            EditorPaneView view = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            string folder = Directory.CreateDirectory(harness.Folder.PathOf("notes")).FullName;
            DataTransfer data = new();
            IStorageFolder item = await window.StorageProvider.TryGetFolderFromPathAsync(new Uri(folder))
                ?? throw new InvalidOperationException($"No storage folder for {folder}.");
            data.Add(DataTransferItem.CreateFile(item));

            Drop(window, NearEdge(view, window, FocusDirection.Down), data);
            await DrainAsync();

            Assert.Single(harness.Workspace.Panes);
            Assert.Equal("Not opened: notes is a folder.", harness.StatusLine.Current);
            window.Close();
        });

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

    private static Point Middle(EditorPaneView pane, MainWindow window)
        => pane.TranslatePoint(new Point(pane.Bounds.Width / 2, pane.Bounds.Height / 2), window)!.Value;

    /// <summary>
    /// What a file manager's drag carries: the files themselves, as the platform hands them over.
    /// </summary>
    private static async Task<DataTransfer> CarryingAsync(MainWindow window, params string[] paths)
    {
        DataTransfer data = new();

        foreach (string path in paths)
        {
            IStorageFile file = await window.StorageProvider.TryGetFileFromPathAsync(new Uri(path))
                ?? throw new InvalidOperationException($"No storage file for {path}.");
            data.Add(DataTransferItem.CreateFile(file));
        }

        return data;
    }

    private static DataTransfer CarryingText(string text)
    {
        DataTransfer data = new();
        data.Add(DataTransferItem.Create(DataFormat.Text, text));

        return data;
    }

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

    private static void Enter(MainWindow window, Point point, DataTransfer data)
    {
        window.DragDrop(point, RawDragEventType.DragEnter, data, Allowed);
        window.DragDrop(point, RawDragEventType.DragOver, data, Allowed);
    }

    // Moved on within the same drag: the platform says over, and the pane it has left hears that it was left.
    private static void Over(MainWindow window, Point point, DataTransfer data)
        => window.DragDrop(point, RawDragEventType.DragOver, data, Allowed);

    private static void Leave(MainWindow window, Point point, DataTransfer data)
        => window.DragDrop(point, RawDragEventType.DragLeave, data, Allowed);

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

        Assert.Equal(share.X * pane.Bounds.Width, origin.X, 2.0);
        Assert.Equal(share.Y * pane.Bounds.Height, origin.Y, 2.0);
        Assert.Equal(share.Width * pane.Bounds.Width, hint.Bounds.Width, 2.0);
        Assert.Equal(share.Height * pane.Bounds.Height, hint.Bounds.Height, 2.0);
    }

    private static void Drop(MainWindow window, Point point, DataTransfer data)
    {
        // As a platform does: the drop goes to whatever the last DragEnter or DragOver reached.
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

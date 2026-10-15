using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
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
            harness.Workspace.Split(SplitAxis.Columns, showActiveBuffer: false);
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
            harness.Workspace.Split(SplitAxis.Columns, showActiveBuffer: false);
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

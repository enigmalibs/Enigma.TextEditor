using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The editor's context menu in the real window: what a right-click opens, what it offers, and which
/// text it acts on.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EditorContextMenuTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ARightClickInTheText_OpensTheEditingCommands()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = Assert.Single(Panes(window));

            ContextMenu menu = RightClick(window, pane);

            Assert.True(menu.IsOpen);
            Assert.Equal(
                ["_Undo", "_Redo", null, "Cu_t", "_Copy", "_Paste", "_Delete", null, "Select _All"],
                menu.Items.Select(item => (item as MenuItem)?.Header as string));
            Assert.Equal(
                ["Ctrl+Z", "Ctrl+Y", "Ctrl+X", "Ctrl+C", "Ctrl+V", "Delete", "Ctrl+A"],
                menu.Items.OfType<MenuItem>().Select(item => item.InputGesture?.ToString()));
            menu.Close();
            window.Close();
        });

    [Fact]
    public void WhatCanRun_IsReadAsTheMenuOpens()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = Assert.Single(Panes(window));

            ContextMenu menu = RightClick(window, pane);
            Assert.False(Item(pane, "MenuUndo").IsEnabled);
            Assert.False(Item(pane, "MenuRedo").IsEnabled);
            Assert.True(Item(pane, "MenuSelectAll").IsEnabled);
            AssertMatchesTheEditor(pane);
            menu.Close();

            pane.EditorControl.Document.Insert(0, "x");
            pane.EditorControl.Select(0, 5);
            menu = RightClick(window, pane);

            Assert.True(Item(pane, "MenuUndo").IsEnabled);
            Assert.True(Item(pane, "MenuCut").IsEnabled);
            Assert.True(Item(pane, "MenuCopy").IsEnabled);
            Assert.True(Item(pane, "MenuDelete").IsEnabled);
            AssertMatchesTheEditor(pane);
            menu.Close();
            window.Close();
        });

    [Fact]
    public void Delete_TakesTheSelectionOut_AndUndo_PutsItBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = Assert.Single(Panes(window));
            pane.EditorControl.Select(0, 6);

            RightClick(window, pane);
            Click(Item(pane, "MenuDelete"));
            Assert.Equal("beta\n", pane.EditorControl.Document.Text);

            RightClick(window, pane);
            Click(Item(pane, "MenuUndo"));
            Assert.Equal("alpha beta\n", pane.EditorControl.Document.Text);

            RightClick(window, pane);
            Click(Item(pane, "MenuRedo"));
            Assert.Equal("beta\n", pane.EditorControl.Document.Text);
            window.Close();
        });

    [Fact]
    public void SelectAll_SelectsTheWholeText_AndTheTextHasTheKeyboardAgain()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = Assert.Single(Panes(window));

            RightClick(window, pane);
            Click(Item(pane, "MenuSelectAll"));

            Assert.Equal("alpha beta\n", pane.EditorControl.SelectedText);
            Assert.True(pane.EditorControl.TextArea.IsFocused);
            window.Close();
        });

    [Fact]
    public void Cut_ThenPaste_MovesTheSelectionThroughTheClipboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = Assert.Single(Panes(window));
            pane.EditorControl.Select(0, 6);

            RightClick(window, pane);
            Click(Item(pane, "MenuCut"));
            await PumpAsync();
            Assert.Equal("beta\n", pane.EditorControl.Document.Text);

            pane.EditorControl.CaretOffset = 4;
            RightClick(window, pane);
            Click(Item(pane, "MenuPaste"));
            await PumpAsync();

            Assert.Equal("betaalpha \n", pane.EditorControl.Document.Text);
            window.Close();
        });

    [Fact]
    public void InASplit_TheMenuActsOnThePaneItWasOpenedIn_AndGivesItTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha beta\n"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "other text\n"));
            MainWindow window = Show(harness);
            EditorPaneView leftView = Panes(window).Single(view => ReferenceEquals(view.DataContext, left));
            Assert.Same(right, harness.Workspace.ActivePane);

            RightClick(window, leftView);
            Click(Item(leftView, "MenuSelectAll"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("alpha beta\n", leftView.EditorControl.SelectedText);
            Assert.Same(left, harness.Workspace.ActivePane);
            Assert.Equal("other text\n", right.ActiveTab?.Document.Document.Text);
            window.Close();
        });

    private static void AssertMatchesTheEditor(EditorPaneView pane)
    {
        AvaloniaEdit.TextEditor editor = pane.EditorControl;

        Assert.Equal(editor.CanUndo, Item(pane, "MenuUndo").IsEnabled);
        Assert.Equal(editor.CanRedo, Item(pane, "MenuRedo").IsEnabled);
        Assert.Equal(editor.CanCut, Item(pane, "MenuCut").IsEnabled);
        Assert.Equal(editor.CanCopy, Item(pane, "MenuCopy").IsEnabled);
        Assert.Equal(editor.CanPaste, Item(pane, "MenuPaste").IsEnabled);
        Assert.Equal(editor.CanDelete, Item(pane, "MenuDelete").IsEnabled);
        Assert.Equal(editor.CanSelectAll, Item(pane, "MenuSelectAll").IsEnabled);
    }

    private static ContextMenu RightClick(MainWindow window, EditorPaneView pane)
    {
        AvaloniaEdit.TextEditor editor = pane.EditorControl;
        Point inside = editor.TranslatePoint(new Point(editor.Bounds.Width / 2, editor.Bounds.Height / 2), window)!.Value;

        window.MouseDown(inside, MouseButton.Right);
        window.MouseUp(inside, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();

        return editor.ContextMenu ?? throw new Xunit.Sdk.XunitException("The editor has no context menu.");
    }

    private static MenuItem Item(EditorPaneView pane, string name)
        => pane.FindControl<MenuItem>(name) ?? throw new Xunit.Sdk.XunitException($"No menu item {name}.");

    private static void Click(MenuItem item)
    {
        item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        item.FindLogicalAncestorOfType<ContextMenu>()?.Close();
        Dispatcher.UIThread.RunJobs();
    }

    private static EditorPaneView[] Panes(MainWindow window) => [.. window.GetVisualDescendants().OfType<EditorPaneView>()];

    private static async Task PumpAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

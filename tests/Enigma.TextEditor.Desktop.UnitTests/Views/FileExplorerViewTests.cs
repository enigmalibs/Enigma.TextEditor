using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The explorer on the real window, through the real input system: its toggle, its width, its folder buttons, and
/// opening files from its tree.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileExplorerViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheExplorer_StartsHidden_TakingNoRoom()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);

            Assert.False(ExplorerOf(window).IsVisible);
            Assert.False(SplitterOf(window).IsVisible);
            Assert.Equal(0, window.ExplorerColumn.ActualWidth);
            Assert.True(ToggleOf(window).IsVisible);
            Assert.False(ToggleOf(window).IsChecked);
            window.Close();
        });

    [Fact]
    public void TheStatusBarToggle_ShowsTheExplorer_AtItsDefaultWidth_ThenHidesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);

            Click(window, ToggleOf(window));
            await DrainAsync();

            Assert.True(harness.Explorer.IsVisible);
            Assert.True(ExplorerOf(window).IsVisible);
            Assert.Equal(MainWindow.ExplorerDefaultWidth, window.ExplorerColumn.ActualWidth, 1.0);
            Assert.True(ToggleOf(window).IsChecked);

            Click(window, ToggleOf(window));
            await DrainAsync();

            Assert.False(harness.Explorer.IsVisible);
            Assert.Equal(0, window.ExplorerColumn.ActualWidth);
            window.Close();
        });

    [Fact]
    public void ViewExplorer_IsACheckableItem_ThatTogglesTheExplorer()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            MenuItem item = MenuItemOf(MenuItemOf(window.FindControl<Menu>("MainMenu")!, "_View"), "E_xplorer");

            item.Command!.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(MenuItemToggleType.CheckBox, item.ToggleType);
            Assert.Equal(new KeyGesture(Key.E, KeyModifiers.Control | KeyModifiers.Shift), item.InputGesture);
            Assert.True(harness.Explorer.IsVisible);
            Assert.True(item.IsChecked);
            window.Close();
        });

    [Fact]
    public void CtrlShiftE_FromTheEditor_WithVimOn_ShowsTheExplorer_AndAgainHidesIt_GivingTheTextTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            pane.FocusEditor();
            Assert.True(harness.Settings.Value.VimEnabled);

            window.KeyPress(Key.E, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.E, "E");
            await DrainAsync();

            Assert.True(harness.Explorer.IsVisible);
            Assert.True(ExplorerOf(window).IsKeyboardFocusWithin);
            Assert.Equal("a\n", harness.Workspace.ActiveDocument?.Document.Text);

            window.KeyPress(Key.E, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.E, "E");
            await DrainAsync();

            Assert.False(harness.Explorer.IsVisible);
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void DraggingTheEdge_WidensTheExplorer_AndHidingThenShowingItKeepsThatWidth()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            harness.Explorer.IsVisible = true;
            await DrainAsync();
            GridSplitter splitter = SplitterOf(window);
            Point grip = splitter.TranslatePoint(new Point(splitter.Bounds.Width / 2, splitter.Bounds.Height / 2), window)!.Value;

            window.MouseDown(grip, MouseButton.Left);
            window.MouseMove(grip + new Point(60, 0), RawInputModifiers.LeftMouseButton);
            window.MouseMove(grip + new Point(120, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(grip + new Point(120, 0), MouseButton.Left);
            await DrainAsync();
            double dragged = window.ExplorerColumn.ActualWidth;

            harness.Explorer.IsVisible = false;
            await DrainAsync();
            harness.Explorer.IsVisible = true;
            await DrainAsync();

            Assert.Equal(MainWindow.ExplorerDefaultWidth + 120, dragged, 2.0);
            Assert.Equal(dragged, window.ExplorerColumn.ActualWidth, 1.0);
            window.Close();
        });

    [Fact]
    public void OpenFolder_AsksForAFolder_ShowsItsTree_AndCloseFolderEmptiesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            harness.Prompts.FoldersToOpen.Enqueue(project);
            MainWindow window = Show(harness);
            harness.Explorer.IsVisible = true;
            await DrainAsync();

            Click(window, window.FindControl<FileExplorerView>("Explorer")!.FindControl<Button>("PlaceholderOpenFolderButton")!);
            await UntilAsync(() => harness.Explorer.Roots.Count == 2);

            Assert.Equal(1, harness.Prompts.FolderQuestions);
            Assert.Equal(["docs", "readme.txt"], Rows(window).Select(row => row.Name));
            Assert.Equal("project", ExplorerOf(window).FindControl<TextBlock>("Header")!.Text);
            Assert.False(ExplorerOf(window).FindControl<StackPanel>("Placeholder")!.IsVisible);

            Click(window, ExplorerOf(window).FindControl<Button>("CloseFolderButton")!);
            await DrainAsync();

            Assert.False(harness.Explorer.HasFolder);
            Assert.True(ExplorerOf(window).FindControl<StackPanel>("Placeholder")!.IsVisible);
            Assert.Empty(Rows(window));
            window.Close();
        });

    [Fact]
    public void FileOpenFolder_AndCloseFolder_AreTheExplorersCommands()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            MenuItem file = MenuItemOf(window.FindControl<Menu>("MainMenu")!, "_File");

            Assert.Same(harness.Explorer.OpenFolderCommand, MenuItemOf(file, "Open _Folder…").Command);
            Assert.Same(harness.Explorer.CloseFolderCommand, MenuItemOf(file, "Close Fol_der").Command);
            window.Close();
        });

    [Fact]
    public void DoubleClickingAFile_OpensItInTheActivePane_WithTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await DrainAsync();
            Control readme = RowOf(window, "readme.txt");
            Point middle = readme.TranslatePoint(new Point(readme.Bounds.Width / 2, readme.Bounds.Height / 2), window)!.Value;

            window.MouseDown(middle, MouseButton.Left);
            window.MouseUp(middle, MouseButton.Left);
            window.MouseDown(middle, MouseButton.Left);
            window.MouseUp(middle, MouseButton.Left);
            await UntilAsync(() => harness.Workspace.ActiveDocument is not null);
            await DrainAsync();

            Assert.Equal(Path.Combine(project, "readme.txt"), harness.Workspace.ActiveDocument?.FilePath);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void Enter_OnTheSelectedFile_OpensIt_AndEscapeGivesTheTextTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await DrainAsync();
            Assert.True(ExplorerOf(window).IsKeyboardFocusWithin);
            harness.Explorer.SelectedNode = harness.Explorer.Roots.Single(row => row.Name == "readme.txt");
            await DrainAsync();

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await DrainAsync();
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);

            ExplorerOf(window).FocusContent();
            await DrainAsync();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UntilAsync(() => harness.Workspace.ActiveDocument?.Title == "readme.txt");
            await DrainAsync();

            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void TheArrowKeys_ThenEnter_OpenAFile_FromTheKeyboardAlone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await DrainAsync();

            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            await DrainAsync();
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            await DrainAsync();
            Assert.Equal("readme.txt", harness.Explorer.SelectedNode?.Name);

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UntilAsync(() => harness.Workspace.ActiveDocument?.Title == "readme.txt");
            window.Close();
        });

    [Fact]
    public void TheToggle_IsNotOnTheSettingsPage()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);

            harness.Shell.ShowSettings();
            Dispatcher.UIThread.RunJobs();

            Assert.False(ToggleOf(window).IsVisible);
            window.Close();
        });

    [Fact]
    public void ShowingTheExplorer_FromTheSettingsPage_BringsTheFilesBack()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            harness.Shell.ShowSettings();
            Dispatcher.UIThread.RunJobs();

            harness.Explorer.ToggleCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(harness.Shell.IsFilesPage);
            window.Close();
        });

    // project/{docs/, readme.txt}
    private static string Project(EditorHarness harness)
    {
        Directory.CreateDirectory(harness.Folder.PathOf(Path.Combine("project", "docs")));
        harness.Folder.Write(Path.Combine("project", "readme.txt"), "read me\n");

        return harness.Folder.PathOf("project");
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static FileExplorerView ExplorerOf(MainWindow window) => window.FindControl<FileExplorerView>("Explorer")!;

    private static GridSplitter SplitterOf(MainWindow window) => window.FindControl<GridSplitter>("ExplorerSplitter")!;

    private static ToggleButton ToggleOf(MainWindow window) => window.FindControl<ToggleButton>("ExplorerToggle")!;

    private static FileTreeNodeViewModel[] Rows(MainWindow window)
        => [.. ExplorerOf(window).TreeControl.GetVisualDescendants().OfType<TreeViewItem>()
            .Select(item => item.DataContext).OfType<FileTreeNodeViewModel>()];

    private static Control RowOf(MainWindow window, string name)
        => ExplorerOf(window).TreeControl.GetVisualDescendants().OfType<Grid>()
            .First(row => row.DataContext is FileTreeNodeViewModel node && (node.Name == name));

    private static MenuItem MenuItemOf(ItemsControl parent, string header)
        => parent.Items.OfType<MenuItem>().Single(item => (string?)item.Header == header);

    private static void Click(MainWindow window, Control control)
    {
        Point middle = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(middle, MouseButton.Left);
        window.MouseUp(middle, MouseButton.Left);
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

    // The listing runs on the thread pool; its result comes back through the dispatcher.
    private static async Task UntilAsync(Func<bool> condition)
    {
        Stopwatch clock = Stopwatch.StartNew();

        while (!condition())
        {
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), "Timed out waiting for the condition.");
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }

        Dispatcher.UIThread.RunJobs();
    }
}

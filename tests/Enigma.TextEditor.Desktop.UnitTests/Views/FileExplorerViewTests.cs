using System;
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
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;
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
            await UiWait.UntilAsync(() => harness.Explorer.IsVisible && ExplorerOf(window).IsVisible, "the toggle showed the explorer");

            Assert.True(harness.Explorer.IsVisible);
            Assert.True(ExplorerOf(window).IsVisible);
            Assert.Equal(MainWindow.ExplorerDefaultWidth, window.ExplorerColumn.ActualWidth, 1.0);
            Assert.True(ToggleOf(window).IsChecked);

            Click(window, ToggleOf(window));
            await UiWait.UntilAsync(() => !harness.Explorer.IsVisible && window.ExplorerColumn.ActualWidth == 0, "the toggle hid the explorer");

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
            await UiWait.UntilAsync(() => harness.Explorer.IsVisible && ExplorerOf(window).IsKeyboardFocusWithin, "Ctrl+Shift+E showed the explorer and gave it the keyboard");

            Assert.True(harness.Explorer.IsVisible);
            Assert.True(ExplorerOf(window).IsKeyboardFocusWithin);
            Assert.Equal("a\n", harness.Workspace.ActiveDocument?.Document.Text);

            window.KeyPress(Key.E, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.E, "E");
            await UiWait.UntilAsync(() => !harness.Explorer.IsVisible && pane.EditorControl.TextArea.IsKeyboardFocusWithin, "Ctrl+Shift+E hid the explorer and gave the editor the keyboard back");

            Assert.False(harness.Explorer.IsVisible);
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void TheExplorerThePreviousSessionLeftShown_ComesBackShown_LeavingTheKeyboardInThePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a\n"));
            await before.Explorer.OpenFolderAsync(Project(before));
            Assert.True(before.Window.SaveSession());
            before.Session.Dispose();

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            MainWindow window = Show(after);
            await after.Window.StartAsync(new StartupFiles([], []));
            await UiWait.UntilAsync(() => Rows(window).Length > 0);
            await UiWait.UntilAsync(() => Math.Abs(window.ExplorerColumn.ActualWidth - MainWindow.ExplorerDefaultWidth) <= 1.0, "the explorer took its width");

            Assert.True(ExplorerOf(window).IsVisible);
            Assert.Equal(MainWindow.ExplorerDefaultWidth, window.ExplorerColumn.ActualWidth, 1.0);
            Assert.False(ExplorerOf(window).IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void DraggingTheEdge_WidensTheExplorer_AndHidingThenShowingItKeepsThatWidth()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            harness.Explorer.IsVisible = true;
            await UiWait.UntilAsync(() => SplitterOf(window).Bounds.Width > 0, "the explorer and its splitter were laid out");
            GridSplitter splitter = SplitterOf(window);
            Point grip = splitter.TranslatePoint(new Point(splitter.Bounds.Width / 2, splitter.Bounds.Height / 2), window)!.Value;

            window.MouseDown(grip, MouseButton.Left);
            window.MouseMove(grip + new Point(60, 0), RawInputModifiers.LeftMouseButton);
            window.MouseMove(grip + new Point(120, 0), RawInputModifiers.LeftMouseButton);
            window.MouseUp(grip + new Point(120, 0), MouseButton.Left);
            await UiWait.UntilAsync(() => Math.Abs(window.ExplorerColumn.ActualWidth - (MainWindow.ExplorerDefaultWidth + 120)) <= 2.0, "the drag widened the explorer");
            double dragged = window.ExplorerColumn.ActualWidth;

            harness.Explorer.IsVisible = false;
            await UiWait.UntilAsync(() => window.ExplorerColumn.ActualWidth == 0, "the explorer was hidden");
            harness.Explorer.IsVisible = true;
            await UiWait.UntilAsync(() => window.ExplorerColumn.ActualWidth > 0, "the explorer was shown again");

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
            await UiWait.UntilAsync(() => ExplorerOf(window).IsVisible, "the explorer was shown");

            Click(window, window.FindControl<FileExplorerView>("Explorer")!.FindControl<Button>("PlaceholderOpenFolderButton")!);
            await UiWait.UntilAsync(() => harness.Explorer.Roots.Count == 2);

            Assert.Equal(1, harness.Prompts.FolderQuestions);
            Assert.Equal(["docs", "readme.txt"], Rows(window).Select(row => row.Name));
            Assert.Equal("project", ExplorerOf(window).FindControl<TextBlock>("Header")!.Text);
            Assert.False(ExplorerOf(window).FindControl<StackPanel>("Placeholder")!.IsVisible);

            Click(window, ExplorerOf(window).FindControl<Button>("CloseFolderButton")!);
            await UiWait.UntilAsync(() => !harness.Explorer.HasFolder && ExplorerOf(window).FindControl<StackPanel>("Placeholder")!.IsVisible, "the folder closed");

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
            await UiWait.UntilAsync(() => Rows(window).Length > 0, "the folder was listed");
            Control readme = RowOf(window, "readme.txt");
            Point middle = readme.TranslatePoint(new Point(readme.Bounds.Width / 2, readme.Bounds.Height / 2), window)!.Value;

            window.MouseDown(middle, MouseButton.Left);
            window.MouseUp(middle, MouseButton.Left);
            window.MouseDown(middle, MouseButton.Left);
            window.MouseUp(middle, MouseButton.Left);
            await UiWait.UntilAsync(() => harness.Workspace.ActiveDocument is not null);
            await UiWait.UntilAsync(() => EditorOf(window).TextArea.IsKeyboardFocusWithin, "the editor took the keyboard");

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
            await UiWait.UntilAsync(() => ExplorerOf(window).IsKeyboardFocusWithin, "the explorer took the keyboard");
            Assert.True(ExplorerOf(window).IsKeyboardFocusWithin);
            harness.Explorer.SelectedNode = harness.Explorer.Roots.Single(row => row.Name == "readme.txt");
            await UiWait.SettleAsync();

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => EditorOf(window).TextArea.IsKeyboardFocusWithin, "Escape gave the editor the keyboard");
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);

            ExplorerOf(window).FocusContent();
            await UiWait.UntilAsync(() => ExplorerOf(window).IsKeyboardFocusWithin, "the explorer took the keyboard back");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => harness.Workspace.ActiveDocument?.Title == "readme.txt");
            await UiWait.UntilAsync(() => EditorOf(window).TextArea.IsKeyboardFocusWithin, "the file opened with the keyboard in the editor");

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
            await UiWait.UntilAsync(() => Rows(window).Length > 0, "the folder was listed");

            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            await UiWait.UntilAsync(() => harness.Explorer.SelectedNode?.Name == "docs", "Down selected the first row");
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            await UiWait.UntilAsync(() => harness.Explorer.SelectedNode?.Name == "readme.txt", "Down selected the second row");
            Assert.Equal("readme.txt", harness.Explorer.SelectedNode?.Name);

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => harness.Workspace.ActiveDocument?.Title == "readme.txt");
            window.Close();
        });

    [Fact]
    public void RightClickingARow_SelectsIt_AndRightClickingBelowTheRows_SelectsNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await UiWait.UntilAsync(() => Rows(window).Length > 0, "the folder was listed");
            ContextMenu menu = ExplorerOf(window).TreeControl.ContextMenu!;

            RightClick(window, RowOf(window, "readme.txt"));
            await UiWait.UntilAsync(() => menu.IsOpen, "the right-click opened the menu");
            Assert.Equal("readme.txt", harness.Explorer.SelectedNode?.Name);
            Assert.True(menu.IsOpen);
            menu.Close();

            TreeView tree = ExplorerOf(window).TreeControl;
            RightClick(window, tree, new Point(tree.Bounds.Width / 2, tree.Bounds.Height - 10));
            await UiWait.UntilAsync(() => menu.IsOpen, "the right-click below the rows opened the menu");
            Assert.Null(harness.Explorer.SelectedNode);
            Assert.Equal(project, harness.Explorer.TargetFolder);
            menu.Close();

            MenuItem[] items = [.. menu.Items.OfType<MenuItem>()];
            Assert.Same(harness.Explorer.NewFileCommand, items.Single(item => (string?)item.Header == "New _File…").Command);
            Assert.Same(harness.Explorer.NewFolderCommand, items.Single(item => (string?)item.Header == "New F_older…").Command);
            window.Close();
        });

    [Fact]
    public void TheNameDialog_ChecksTheNameAsItIsTyped_AndEnterAnswersWithIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task<string?> asked = new FileExplorerPrompts(dialogs).AskNewFileNameAsync(project);
            await UiWait.UntilAsync(() => window.HostDialog.Content is FileNamePromptView, "the name prompt opened");
            TextBox box = Assert.IsType<FileNamePromptView>(window.HostDialog.Content).FindControl<TextBox>("NameBox")!;

            Assert.Equal("New file", window.HostDialog.Title);
            Assert.True(box.IsFocused);
            Assert.False(window.HostDialog.IsPrimaryButtonEnabled);

            window.KeyTextInput("readme.txt");
            await UiWait.SettleAsync();
            Assert.False(window.HostDialog.IsPrimaryButtonEnabled);

            box.Text = " notes.txt";
            await UiWait.UntilAsync(() => window.HostDialog.IsPrimaryButtonEnabled, "a free name enabled Create");
            Assert.True(window.HostDialog.IsPrimaryButtonEnabled);

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Enter answered the prompt");
            Assert.Equal("notes.txt", await asked);
            window.Close();
        });

    [Fact]
    public void TheNameDialog_AnswersNothing_WhenEscaped()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task<string?> asked = new FileExplorerPrompts(dialogs).AskNewFolderNameAsync(project);
            await UiWait.UntilAsync(() => window.HostDialog.Title == "New folder", "the folder prompt opened");
            Assert.Equal("New folder", window.HostDialog.Title);
            window.KeyTextInput("assets");
            await UiWait.SettleAsync();

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Escape answered the prompt");

            Assert.Null(await asked);
            window.Close();
        });

    [Fact]
    public void F2_InTheTree_RenamesTheSelectedRow_AsTheMenusRenameDoes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await UiWait.UntilAsync(() => Rows(window).Length > 0, "the folder was listed");
            harness.Explorer.SelectedNode = harness.Explorer.Roots.Single(row => row.Name == "readme.txt");
            await UiWait.SettleAsync();
            harness.ExplorerPrompts.NewNames.Enqueue("about.txt");

            window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.F2, null);
            await UiWait.UntilAsync(() => harness.Explorer.SelectedNode?.Name == "about.txt");

            Assert.True(File.Exists(Path.Combine(project, "about.txt")));

            // The menu's items bind once it has opened.
            ContextMenu menu = ExplorerOf(window).TreeControl.ContextMenu!;
            RightClick(window, RowOf(window, "about.txt"));
            await UiWait.UntilAsync(() => menu.IsOpen, "the right-click opened the menu");
            MenuItem rename = menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Rename…");
            Assert.Same(harness.Explorer.RenameCommand, rename.Command);
            Assert.Equal(new KeyGesture(Key.F2), rename.InputGesture);
            menu.Close();
            window.Close();
        });

    [Fact]
    public void TheRenameDialog_StartsOnTheName_WithItsStemSelected()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task<string?> asked = new FileExplorerPrompts(dialogs).AskNewNameAsync(Path.Combine(project, "readme.txt"), isFolder: false);
            await UiWait.UntilAsync(() => window.HostDialog.Content is FileNamePromptView, "the rename prompt opened");
            TextBox box = Assert.IsType<FileNamePromptView>(window.HostDialog.Content).FindControl<TextBox>("NameBox")!;

            Assert.Equal("Rename file", window.HostDialog.Title);
            Assert.Equal("readme.txt", box.Text);
            Assert.Equal("readme", box.SelectedText);
            Assert.True(window.HostDialog.IsPrimaryButtonEnabled);

            window.KeyTextInput("about");
            await UiWait.SettleAsync();
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Enter answered the prompt");

            Assert.Equal("about.txt", await asked);
            window.Close();
        });

    [Fact]
    public void Delete_InTheTree_DeletesTheSelectedRow_AsTheMenusDeleteDoes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            MainWindow window = Show(harness);
            await harness.Explorer.OpenFolderAsync(project);
            await UiWait.UntilAsync(() => Rows(window).Length > 0, "the folder was listed");
            harness.Explorer.SelectedNode = harness.Explorer.Roots.Single(row => row.Name == "readme.txt");
            await UiWait.SettleAsync();

            ContextMenu menu = ExplorerOf(window).TreeControl.ContextMenu!;
            RightClick(window, RowOf(window, "readme.txt"));
            await UiWait.UntilAsync(() => menu.IsOpen, "the right-click opened the menu");
            MenuItem delete = menu.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Delete");
            Assert.Same(harness.Explorer.DeleteCommand, delete.Command);
            Assert.Equal(new KeyGesture(Key.Delete), delete.InputGesture);
            menu.Close();
            await UiWait.UntilAsync(() => !menu.IsOpen, "the menu closed");
            ExplorerOf(window).FocusContent();
            await UiWait.UntilAsync(() => ExplorerOf(window).IsKeyboardFocusWithin, "the explorer took the keyboard");

            window.KeyPress(Key.Delete, RawInputModifiers.None, PhysicalKey.Delete, null);
            await UiWait.UntilAsync(() => Rows(window).Length == 1);

            Assert.Equal(["trash readme.txt"], harness.ExplorerPrompts.Confirmations);
            Assert.False(File.Exists(Path.Combine(project, "readme.txt")));
            Assert.Equal(["docs"], Rows(window).Select(row => row.Name));
            window.Close();
        });

    [Fact]
    public void TheTrashQuestion_DefaultsToCancel_AndOnlyDeleteConfirms()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);
            FileExplorerPrompts prompts = new(dialogs);

            Task<bool> asked = prompts.ConfirmMoveToTrashAsync("docs", isFolder: true);
            await UiWait.UntilAsync(() => (window.FocusManager?.GetFocusedElement() as Button)?.Content is "Cancel", "the trash question opened with Cancel focused");

            Assert.Equal("Delete 'docs'?", window.HostDialog.Title);
            Assert.Equal("Delete", window.HostDialog.PrimaryButtonText);
            Assert.Equal("Cancel", (window.FocusManager?.GetFocusedElement() as Button)?.Content);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Enter answered the trash question");
            Assert.False(await asked);

            Task<bool> forGood = prompts.ConfirmPermanentDeleteAsync("readme.txt", isFolder: false);
            await UiWait.UntilAsync(() => window.HostDialog.Title == "Delete 'readme.txt' permanently?" && window.HostDialog.IsOpen, "the permanent-delete question opened");
            Assert.Equal("Delete 'readme.txt' permanently?", window.HostDialog.Title);
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => forGood.IsCompleted, "Escape answered the permanent-delete question");
            Assert.False(await forGood);
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

    private static void RightClick(MainWindow window, Control control, Point? at = null)
    {
        Point point = control.TranslatePoint(at ?? new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Right);
        window.MouseUp(point, MouseButton.Right);
    }

    private static AvaloniaEdit.TextEditor EditorOf(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl;
}

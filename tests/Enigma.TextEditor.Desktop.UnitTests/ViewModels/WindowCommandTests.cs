using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Input;
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
/// Vim's window commands and window ex commands carried out on the workspace, and the registers they
/// share across panes.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class WindowCommandTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void CtrlWV_AndCtrlWS_Split()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));

            await Window(harness, VimWindowCommand.SplitVertical);
            await Window(harness, VimWindowCommand.SplitHorizontal);

            Assert.Equal(3, harness.Workspace.Panes.Count);
            SplitNode root = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);
            Assert.Equal(SplitAxis.Columns, root.Axis);
            Assert.Equal(SplitAxis.Rows, Assert.IsType<SplitNode>(root.Second).Axis);
        });

    [Fact]
    public void CtrlWHJKL_MoveTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);

            await Window(harness, VimWindowCommand.FocusLeft);
            Assert.Same(left, harness.Workspace.ActivePane);

            await Window(harness, VimWindowCommand.FocusRight);
            Assert.Same(right, harness.Workspace.ActivePane);

            await Window(harness, VimWindowCommand.FocusUp);
            Assert.Same(right, harness.Workspace.ActivePane);

            await Window(harness, VimWindowCommand.FocusNext);
            Assert.Same(left, harness.Workspace.ActivePane);
        });

    [Fact]
    public void CtrlWC_ClosesThePane_ButNotTheLastOne()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            harness.Workspace.Split(SplitAxis.Columns);

            await Window(harness, VimWindowCommand.Close);
            Assert.Single(harness.Workspace.Panes);

            await Window(harness, VimWindowCommand.Close);
            Assert.Single(harness.Workspace.Panes);
            Assert.Equal("Cannot close last window", harness.StatusLine.Current);
            Assert.Equal(0, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void CtrlWQ_IsColonQ()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));

            await Window(harness, VimWindowCommand.Quit);

            Assert.Empty(harness.Workspace.ActivePane.Tabs);
            Assert.Equal(0, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void CtrlWO_CtrlWEquals_AndTheResizeKeys()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);

            await Window(harness, VimWindowCommand.Wider);
            Assert.Equal(0.45, split.Ratio, 9);

            await Window(harness, VimWindowCommand.Narrower);
            await Window(harness, VimWindowCommand.Narrower);
            Assert.Equal(0.55, split.Ratio, 9);

            await Window(harness, VimWindowCommand.Equalize);
            Assert.Equal(0.5, split.Ratio, 9);

            await Window(harness, VimWindowCommand.Only);
            Assert.Single(harness.Workspace.Panes);
        });

    [Fact]
    public void TheResizeKeys_MoveTheSplitterByTheStepInTheSettings()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 4);
            harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);

            // The new pane is the right-hand one: wider moves the splitter left, 50/50 to 46/54.
            await Window(harness, VimWindowCommand.Wider);
            Assert.Equal(0.46, split.Ratio, 9);

            harness.Preferences.PaneResizeStep = 10;
            await Window(harness, VimWindowCommand.Narrower);
            Assert.Equal(0.56, split.Ratio, 9);
        });

    [Fact]
    public void TheSplitterCommands_MoveTheBarByTheStep_TheSameWayFromEitherPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 4);
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);

            harness.Workspace.Activate(left);
            await Window(harness, VimWindowCommand.MoveSplitterRight);
            Assert.Equal(0.54, split.Ratio, 9);

            harness.Workspace.Activate(right);
            await Window(harness, VimWindowCommand.MoveSplitterRight);
            Assert.Equal(0.58, split.Ratio, 9);

            await Window(harness, VimWindowCommand.MoveSplitterLeft);
            await Window(harness, VimWindowCommand.MoveSplitterLeft);
            Assert.Equal(0.5, split.Ratio, 9);

            // No bar between stacked panes: nothing moves.
            await Window(harness, VimWindowCommand.MoveSplitterDown);
            Assert.Equal(0.5, split.Ratio, 9);
            Assert.Same(right, harness.Workspace.ActivePane);
        });

    [Fact]
    public void ColonSplitWithAFile_OpensOnlyThatFileInTheNewPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            harness.Folder.Write("b.txt", "b");

            await Ex(harness, VimExCommand.VerticalSplit, "b.txt");

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Equal(["b.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
        });

    [Theory]
    [InlineData(VimExCommand.Split)]
    [InlineData(VimExCommand.VerticalSplit)]
    [InlineData(VimExCommand.New)]
    [InlineData(VimExCommand.VerticalNew)]
    public void TheSplitExCommandsAlone_OpenANewBuffer(VimExCommand command)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await Ex(harness, command);

            Assert.Equal(2, harness.Workspace.Panes.Count);
            Assert.Null(Assert.Single(harness.Workspace.ActivePane.Tabs).Document.FilePath);
            Assert.Same(tab, Assert.Single(harness.Workspace.Panes.Single(pane => !pane.IsActive).Tabs));
        });

    [Theory]
    [InlineData(VimWindowCommand.SplitHorizontal)]
    [InlineData(VimWindowCommand.SplitVertical)]
    public void CtrlWSAndV_OpenANewBuffer(VimWindowCommand command)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await Window(harness, command);

            Assert.Null(Assert.Single(harness.Workspace.ActivePane.Tabs).Document.FilePath);
            Assert.Equal(1, harness.Registry.ViewCount(tab.Document));
        });

    [Fact]
    public void TheViewMenusSplits_OpenANewBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));

            harness.Workspace.SplitRightCommand.Execute(null);
            Assert.Null(Assert.Single(harness.Workspace.ActivePane.Tabs).Document.FilePath);

            harness.Workspace.SplitDownCommand.Execute(null);
            Assert.Null(Assert.Single(harness.Workspace.ActivePane.Tabs).Document.FilePath);

            Assert.Equal(3, harness.Workspace.Panes.Count);
            Assert.Equal(3, harness.Registry.Documents.Count);
        });

    [Fact]
    public void ColonNew_AndColonVnew_SplitOntoAnUntitledBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));

            await Ex(harness, VimExCommand.New);
            Assert.Equal("Untitled-1", Assert.Single(harness.Workspace.ActivePane.Tabs).Document.Title);

            await Ex(harness, VimExCommand.VerticalNew);
            Assert.Equal("Untitled-2", Assert.Single(harness.Workspace.ActivePane.Tabs).Document.Title);
            Assert.Equal(3, harness.Workspace.Panes.Count);
        });

    [Fact]
    public void ColonOnlyAndColonClose_ActOnPanes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Split(SplitAxis.Rows);

            await Ex(harness, VimExCommand.Close);
            Assert.Equal(2, harness.Workspace.Panes.Count);

            await Ex(harness, VimExCommand.Only);
            Assert.Single(harness.Workspace.Panes);
        });

    [Fact]
    public void TypedInTheWindow_CtrlWVSplits_AndAYankInOnePaneIsPutInTheOther()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "first line\n"));
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
            Dispatcher.UIThread.RunJobs();

            window.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            window.KeyTextInput("v");
            await Pump();
            Assert.Equal(2, harness.Workspace.Panes.Count);

            // Back to the left pane to yank its line, then to the new (right) one — another engine — to
            // put it into a file opened there.
            EditorPaneViewModel left = harness.Workspace.Panes[0];
            EditorPaneViewModel right = harness.Workspace.Panes[1];
            window.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            window.KeyTextInput("h");
            await Pump();
            Assert.Same(left, harness.Workspace.ActivePane);
            window.KeyTextInput("yy");
            window.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            window.KeyTextInput("l");
            await Pump();
            Assert.Same(right, harness.Workspace.ActivePane);

            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "other\n"));
            await Pump();
            window.KeyTextInput("p");
            await Pump();

            Assert.Equal("b.txt", right.ActiveTab?.Document.Title);
            Assert.Equal("other\nfirst line\n", right.ActiveTab?.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TypedInTheWindow_GHJKL_MoveTheKeyboardBetweenPanes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();
            await FocusAsync(window, right);

            window.KeyTextInput("gh");
            await Pump();
            Assert.Same(left, harness.Workspace.ActivePane);

            window.KeyTextInput("gl");
            await Pump();
            Assert.Same(right, harness.Workspace.ActivePane);

            // The right-hand pane split in two: one above the other.
            EditorPaneViewModel below = harness.Workspace.Split(SplitAxis.Rows);
            await FocusAsync(window, below);

            window.KeyTextInput("gk");
            await Pump();
            Assert.Same(right, harness.Workspace.ActivePane);

            window.KeyTextInput("gj");
            await Pump();
            Assert.Same(below, harness.Workspace.ActivePane);

            // Nothing below the bottom pane: the keyboard stays, and nothing was typed.
            string text = below.ActiveTab!.Document.Document.Text;
            window.KeyTextInput("gj");
            await Pump();
            Assert.Same(below, harness.Workspace.ActivePane);
            Assert.Equal(text, below.ActiveTab.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TypedInTheWindow_GSizeKeys_ResizeAndEqualizeThePanes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();
            await FocusAsync(window, left);

            window.KeyTextInput("g>");
            await Pump();
            Assert.Equal(0.55, split.Ratio, 9);

            window.KeyTextInput("g<g<");
            await Pump();
            Assert.Equal(0.45, split.Ratio, 9);

            window.KeyTextInput("g=");
            await Pump();
            Assert.Equal(0.5, split.Ratio, 9);
            Assert.Equal("text\n", left.ActiveTab!.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TypedInTheWindow_SpaceRKAndRJ_MoveTheBarBetweenStackedPanes_FromEitherOfThem()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            EditorPaneViewModel top = harness.Workspace.ActivePane;
            EditorPaneViewModel bottom = harness.Workspace.Split(SplitAxis.Rows);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();

            // k moves the bar down, typed in the top pane and in the bottom one: the top pane grows both times.
            await FocusAsync(window, top);
            window.KeyTextInput(" rk");
            await Pump();
            Assert.Equal(0.55, split.Ratio, 9);

            await FocusAsync(window, bottom);
            string below = bottom.ActiveTab!.Document.Document.Text;
            window.KeyTextInput(" rk");
            await Pump();
            Assert.Equal(0.6, split.Ratio, 9);

            // j moves it up, and the bottom pane grows.
            window.KeyTextInput(" rj rj rj");
            await Pump();
            Assert.Equal(0.45, split.Ratio, 9);
            Assert.Same(bottom, harness.Workspace.ActivePane);
            Assert.Equal(below, bottom.ActiveTab.Document.Document.Text);

            await FocusAsync(window, top);
            window.KeyTextInput(" rj");
            await Pump();
            Assert.Equal(0.4, split.Ratio, 9);
            Assert.Equal("text\n", top.ActiveTab!.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TypedInTheWindow_SpaceRLAndRH_MoveTheBarBetweenSideBySidePanes_FromEitherOfThem()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            SplitNode split = Assert.IsType<SplitNode>(harness.Workspace.Layout.Root);
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();

            // l moves the bar right, typed in the left pane and in the right one: the left pane grows both times.
            await FocusAsync(window, left);
            window.KeyTextInput(" rl");
            await Pump();
            Assert.Equal(0.55, split.Ratio, 9);

            await FocusAsync(window, right);
            window.KeyTextInput(" rl");
            await Pump();
            Assert.Equal(0.6, split.Ratio, 9);

            // h moves it left, and the right pane grows.
            window.KeyTextInput(" rh rh rh");
            await Pump();
            Assert.Equal(0.45, split.Ratio, 9);

            await FocusAsync(window, left);
            window.KeyTextInput(" rh");
            await Pump();
            Assert.Equal(0.4, split.Ratio, 9);
            Assert.Equal("text\n", left.ActiveTab!.Document.Document.Text);

            // The old keys still grow the pane that has the keyboard: g> from the left one widens it.
            window.KeyTextInput("g>");
            await Pump();
            Assert.Equal(0.45, split.Ratio, 9);
            window.Close();
        });

    [Fact]
    public void TypedInTheWindow_GTAndGCapitalT_MoveBetweenThePanesTabs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("c.txt", "c"));
            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
            window.Show();
            await FocusAsync(window, pane);

            window.KeyTextInput("gt");
            await Pump();
            Assert.Equal("a.txt", pane.ActiveTab!.Document.Title);

            window.KeyTextInput("gT");
            await Pump();
            Assert.Equal("c.txt", pane.ActiveTab!.Document.Title);

            window.KeyTextInput("gT");
            await Pump();
            Assert.Equal("b.txt", pane.ActiveTab!.Document.Title);
            window.Close();
        });

    private static async Task FocusAsync(MainWindow window, EditorPaneViewModel pane)
    {
        await Pump();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single(view => ReferenceEquals(view.DataContext, pane)).FocusEditor();
        await Pump();
    }

    private static Task Window(EditorHarness harness, VimWindowCommand command)
        => harness.Workspace.ActivePane.ExecuteWindowAsync(command);

    private static Task Ex(EditorHarness harness, VimExCommand command, string? argument = null)
        => harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(command, argument));

    private static async Task Pump()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }
}

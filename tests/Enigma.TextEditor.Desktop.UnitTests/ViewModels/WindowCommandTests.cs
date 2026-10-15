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

            Assert.Equal(1, harness.Shutdown.ExitRequests);
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

    [Fact]
    public void ColonSplitAlone_ShowsTheSameBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await Ex(harness, VimExCommand.Split);

            Assert.Same(tab.Document, harness.Workspace.ActiveDocument);
            Assert.Equal(2, harness.Workspace.Panes.Count);
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

            // Yank in the new (right) pane, move to the left one — another engine — and put there.
            EditorPaneViewModel left = harness.Workspace.Panes[0];
            window.KeyTextInput("yy");
            window.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.Control);
            window.KeyTextInput("h");
            await Pump();
            Assert.Same(left, harness.Workspace.ActivePane);

            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "other\n"));
            await Pump();
            window.KeyTextInput("p");
            await Pump();

            Assert.Equal("b.txt", left.ActiveTab?.Document.Title);
            Assert.Equal("other\nfirst line\n", left.ActiveTab?.Document.Document.Text);
            window.Close();
        });

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

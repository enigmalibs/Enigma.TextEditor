using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The tab strip on the real window, through the real input system: the double-click on its empty part that opens a
/// new tab, and the wheel that scrolls it when its tabs do not fit.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class TabStripViewTests(HeadlessAvaloniaFixture fixture)
{
    // How far one notch moves the strip: Avalonia's step for Shift and the wheel, which the plain wheel matches.
    private const double TabStripWheelStep = 50;

    [Fact]
    public void DoubleClicking_RightOfTheLastTab_OpensANewUntitledTabThere_WithTheKeyboard()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            DoubleClick(window, OnStrip(pane, window));
            await DrainAsync();

            EditorPaneViewModel model = harness.Workspace.ActivePane;
            Assert.Equal(["a.txt", "Untitled-1"], model.Tabs.Select(tab => tab.Document.Title));
            Assert.Same(model.Tabs[^1], model.ActiveTab);
            Assert.Same(model.ActiveTab, pane.CurrentTab);
            Assert.True(pane.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void DoubleClicking_ATab_OpensNoTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            DoubleClick(window, TitleOf(window, pane, a));
            await DrainAsync();

            Assert.Equal([a], harness.Workspace.ActivePane.Tabs);
            window.Close();
        });

    [Fact]
    public void DoubleClicking_TheStripOfThePaneWithoutTheKeyboard_OpensTheTabThere_AndActivatesThatPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            EditorPaneViewModel leftPane = harness.Workspace.ActivePane;
            EditorPaneViewModel rightPane = harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Activate(leftPane);
            MainWindow window = Show(harness);
            EditorPaneView right = window.GetVisualDescendants().OfType<EditorPaneView>().Single(view => view.DataContext == rightPane);

            DoubleClick(window, OnStrip(right, window));
            await DrainAsync();

            Assert.Single(leftPane.Tabs);
            Assert.Equal(2, rightPane.Tabs.Count);
            Assert.Same(rightPane.Tabs[^1], rightPane.ActiveTab);
            Assert.Same(rightPane, harness.Workspace.ActivePane);
            Assert.True(right.EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    [Fact]
    public void TheWheel_OverAStripTooNarrowForItsTabs_ScrollsItSideways_AndStopsAtBothEnds()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", string.Concat(Enumerable.Repeat("line\n", 200))));
            OpenUntitled(harness, 30);
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            ScrollViewer strip = ScrollOf(pane);
            strip.Offset = default;
            Dispatcher.UIThread.RunJobs();
            Assert.True(strip.Extent.Width > strip.Viewport.Width);
            double end = strip.Extent.Width - strip.Viewport.Width;
            double textOffset = pane.EditorControl.VerticalOffset;

            window.MouseWheel(OnStrip(pane, window), new Vector(0, -1));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(TabStripWheelStep, strip.Offset.X, 0.5);

            window.MouseWheel(OnStrip(pane, window), new Vector(0, 1));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, strip.Offset.X, 0.5);

            window.MouseWheel(OnStrip(pane, window), new Vector(0, 1));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, strip.Offset.X, 0.5);

            for (int notch = 0; notch < 200; notch++)
            {
                window.MouseWheel(OnStrip(pane, window), new Vector(0, -1));
            }

            Dispatcher.UIThread.RunJobs();
            Assert.Equal(end, strip.Offset.X, 0.5);
            Assert.Equal(textOffset, pane.EditorControl.VerticalOffset);
            window.Close();
        });

    [Fact]
    public void TheWheel_OverAStripThatHoldsItsTabs_LeavesItWhereItIs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            ScrollViewer strip = ScrollOf(pane);

            window.MouseWheel(OnStrip(pane, window), new Vector(0, -1));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(0, strip.Offset.X);
            window.Close();
        });

    [Fact]
    public void ShiftAndTheWheel_OverAStripTooNarrowForItsTabs_StillScrollItSideways()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            OpenUntitled(harness, 30);
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            ScrollViewer strip = ScrollOf(pane);
            strip.Offset = default;
            Dispatcher.UIThread.RunJobs();

            window.MouseWheel(OnStrip(pane, window), new Vector(0, -1), RawInputModifiers.Shift);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(TabStripWheelStep, strip.Offset.X, 0.5);
            window.Close();
        });

    private static void OpenUntitled(EditorHarness harness, int count)
    {
        for (int i = 0; i < count; i++)
        {
            harness.Workspace.NewDocument();
        }
    }

    private static ScrollViewer ScrollOf(EditorPaneView pane)
        => pane.FindControl<ListBox>("Tabs")!.GetVisualDescendants().OfType<ScrollViewer>().First();

    private static void DoubleClick(MainWindow window, Point point)
    {
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static Point TitleOf(MainWindow window, EditorPaneView pane, EditorTabViewModel tab)
    {
        ListBoxItem item = pane.GetVisualDescendants().OfType<ListBoxItem>().Single(candidate => ReferenceEquals(candidate.DataContext, tab));
        TextBlock title = item.GetVisualDescendants().OfType<TextBlock>().First();

        return title.TranslatePoint(new Point(title.Bounds.Width / 2, title.Bounds.Height / 2), window)!.Value;
    }

    // On the tab strip, right of the tabs: the strip is the top band of the pane.
    private static Point OnStrip(EditorPaneView pane, MainWindow window)
        => pane.TranslatePoint(new Point(pane.Bounds.Width - 20, 15), window)!.Value;

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

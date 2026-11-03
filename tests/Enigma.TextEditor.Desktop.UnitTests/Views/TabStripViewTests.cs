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
/// new tab.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class TabStripViewTests(HeadlessAvaloniaFixture fixture)
{
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

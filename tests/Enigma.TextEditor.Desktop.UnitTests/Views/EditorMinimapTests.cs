using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The minimap beside the text: where it puts the view when pressed, what it is told about the view
/// and the caret, and when it shows.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EditorMinimapTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(0, 100, 0.2, 0)]
    [InlineData(50, 100, 0.2, 0.4)]
    [InlineData(100, 100, 0.2, 0.8)]
    [InlineData(5, 100, 0.2, 0)]
    [InlineData(50, 0, 0.2, 0)]
    [InlineData(50, 100, 1, 0)]
    [InlineData(-10, 100, 0.5, 0)]
    public void StartFor_CentresTheViewOnThePointer_WithinTheDocument(double y, double height, double visible, double expected)
        => Assert.Equal(expected, EditorMinimap.StartFor(y, height, visible), 6);

    [Fact]
    public void APress_AsksForTheViewCentredThere()
        => fixture.Run(() =>
        {
            EditorMinimap map = new() { ViewportStart = 0, ViewportEnd = 0.1 };
            map.Measure(new Size(36, 200));
            map.Arrange(new Rect(0, 0, 36, 200));
            double? asked = null;
            map.ScrollRequested += (_, start) => asked = start;

            map.RequestScrollTo(100);

            Assert.Equal(0.45, asked!.Value, 6);
        });

    [Fact]
    public void ATextTab_ShowsTheMinimap_InsteadOfTheVerticalScrollbar()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(10)));
            (MainWindow window, EditorPaneView pane) = Show(harness);

            Assert.True(pane.MinimapControl.IsEffectivelyVisible);
            Assert.True(pane.MinimapControl.Bounds.Width > 0);
            Assert.Equal(ScrollBarVisibility.Hidden, pane.EditorControl.VerticalScrollBarVisibility);
            window.Close();
        });

    [Fact]
    public void AHexTab_HasNoMinimap()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", new byte[64]));
            (MainWindow window, EditorPaneView pane) = Show(harness);

            Assert.False(pane.MinimapControl.IsEffectivelyVisible);
            window.Close();
        });

    [Fact]
    public void AShortDocument_IsAllOnScreen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(3)));
            (MainWindow window, EditorPaneView pane) = Show(harness);

            Assert.Equal(0, pane.MinimapControl.ViewportStart, 6);
            Assert.Equal(1, pane.MinimapControl.ViewportEnd, 6);
            window.Close();
        });

    [Fact]
    public void PressingNearTheBottom_ScrollsTheTextToItsEnd_AndTheWindowFollows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(2000)));
            (MainWindow window, EditorPaneView pane) = Show(harness);
            EditorMinimap map = pane.MinimapControl;
            Assert.Equal(0, map.ViewportStart, 6);
            Assert.True(map.ViewportEnd < 0.1);

            map.RequestScrollTo(map.Bounds.Height);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(pane.EditorControl.ExtentHeight - pane.EditorControl.ViewportHeight, pane.EditorControl.VerticalOffset, 3);
            Assert.Equal(1, map.ViewportEnd, 3);
            Assert.True(map.ViewportStart > 0.9);
            window.Close();
        });

    [Fact]
    public void TheWheelOverTheMinimap_ScrollsTheText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(2000)));
            (MainWindow window, EditorPaneView pane) = Show(harness);
            EditorMinimap map = pane.MinimapControl;
            Point centre = map.TranslatePoint(new Point(map.Bounds.Width / 2, map.Bounds.Height / 2), window)!.Value;

            window.MouseWheel(centre, new Vector(0, -2));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(100, pane.EditorControl.VerticalOffset, 3);
            Assert.True(map.ViewportStart > 0);
            window.Close();
        });

    [Fact]
    public void TheCaretsLine_IsMarkedWhereItIsInTheDocument()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(1000)));
            (MainWindow window, EditorPaneView pane) = Show(harness);
            Assert.Equal(0, pane.MinimapControl.CaretPosition, 6);

            pane.EditorControl.TextArea.Caret.Line = 501;
            Dispatcher.UIThread.RunJobs();

            // Against the scroll extent, as the window is, so the mark sits inside the window around it.
            double top = pane.EditorControl.TextArea.TextView.GetVisualTopByDocumentLine(501);
            Assert.Equal(top / pane.EditorControl.ExtentHeight, pane.MinimapControl.CaretPosition, 6);
            Assert.InRange(pane.MinimapControl.CaretPosition, 0.4, 0.5);
            window.Close();
        });

    private static string Lines(int count)
        => string.Join('\n', Enumerable.Range(1, count).Select(number => $"line {number}"));

    private static (MainWindow Window, EditorPaneView Pane) Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, window.GetVisualDescendants().OfType<EditorPaneView>().Single());
    }
}

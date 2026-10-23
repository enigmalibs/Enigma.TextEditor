using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Rendering;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The editor's text is never drawn under what stands at its edges: the minimap, the vertical
/// scrollbar that replaces it, and the horizontal scrollbar — and a gap keeps it off the strip.
/// </summary>
/// <remarks>
/// The text area clips what it draws to its own bounds, so the text is clear of a strip whenever the
/// text area is.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class EditorTextClearanceTests(HeadlessAvaloniaFixture fixture)
{
    // The third line, far wider than the window; enough lines around it for the vertical bar to show.
    private const int LongLineNumber = 3;

    [Theory]
    [InlineData(14)]
    [InlineData(40)]
    public void WithTheMinimap_TheTextStopsShortOfIt_AndAboveTheHorizontalBar(int fontSize)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.EditorFontSize = fontSize);
            (MainWindow window, EditorPaneView pane) = await ShowAsync(harness);
            ScrollToTheRight(pane);

            Rect text = BoundsIn(window, pane.EditorControl.TextArea);
            Rect map = BoundsIn(window, pane.MinimapControl);
            ScrollBar horizontal = Bar(pane, Orientation.Horizontal);

            Assert.True(text.Right < map.Left, $"The text ends at {text.Right}, the minimap starts at {map.Left}.");
            Assert.True(RightOfLongLine(window, pane) <= text.Right);
            Assert.True(horizontal.IsEffectivelyVisible);
            Assert.True(text.Bottom <= BoundsIn(window, horizontal).Top);
            window.Close();
        });

    [Theory]
    [InlineData(14)]
    [InlineData(40)]
    public void WithTheScrollbar_TheTextStopsShortOfBothBars(int fontSize)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings =>
            {
                settings.ShowMinimap = false;
                settings.EditorFontSize = fontSize;
            });
            (MainWindow window, EditorPaneView pane) = await ShowAsync(harness);
            ScrollToTheRight(pane);

            Rect text = BoundsIn(window, pane.EditorControl.TextArea);
            ScrollBar vertical = Bar(pane, Orientation.Vertical);
            ScrollBar horizontal = Bar(pane, Orientation.Horizontal);

            Assert.True(vertical.IsEffectivelyVisible);
            Assert.True(text.Right < BoundsIn(window, vertical).Left, $"The text ends at {text.Right}, the bar starts at {BoundsIn(window, vertical).Left}.");
            Assert.True(RightOfLongLine(window, pane) <= text.Right);
            Assert.True(horizontal.IsEffectivelyVisible);
            Assert.True(text.Bottom <= BoundsIn(window, horizontal).Top);
            window.Close();
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WrappedAtALargeSize_NoRowReachesTheStripBesideIt(bool minimap)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings =>
            {
                settings.ShowMinimap = minimap;
                settings.WordWrap = true;
                settings.EditorFontSize = 40;
            });
            (MainWindow window, EditorPaneView pane) = await ShowAsync(harness);

            Rect text = BoundsIn(window, pane.EditorControl.TextArea);
            Visual strip = minimap ? pane.MinimapControl : Bar(pane, Orientation.Vertical);

            Assert.True(pane.EditorControl.TextArea.TextView.GetVisualLine(LongLineNumber)!.TextLines.Count > 1);
            Assert.True(RightOfLongLine(window, pane) <= text.Right);
            Assert.True(text.Right < BoundsIn(window, strip).Left);
            window.Close();
        });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheEditorsScrollbars_StandBesideTheText_RatherThanOverIt(bool minimap)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.ShowMinimap = minimap);
            (MainWindow window, EditorPaneView pane) = await ShowAsync(harness);

            // Left to auto-hide, the bars float over the text and widen under the pointer.
            Assert.False(EditorScrollViewer(pane).AllowAutoHide);
            window.Close();
        });

    private static async Task<(MainWindow Window, EditorPaneView Pane)> ShowAsync(EditorHarness harness)
    {
        string text = string.Join(
            '\n',
            Enumerable.Range(1, 200).Select(number => number == LongLineNumber
                ? string.Concat(Enumerable.Repeat("word ", 120)) + "end"
                : $"line {number}"));
        await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", text));

        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, window.GetVisualDescendants().OfType<EditorPaneView>().Single());
    }

    // As far right as the view goes, on the first lines: the long line is the widest on screen.
    private static void ScrollToTheRight(EditorPaneView pane)
    {
        ScrollViewer scroll = EditorScrollViewer(pane);
        scroll.Offset = new Vector(scroll.Extent.Width, 0);
        Dispatcher.UIThread.RunJobs();
    }

    // Where the long line's widest row ends on screen, in window coordinates.
    private static double RightOfLongLine(MainWindow window, EditorPaneView pane)
    {
        TextView view = pane.EditorControl.TextArea.TextView;
        VisualLine line = view.GetVisualLine(LongLineNumber)!;

        return BoundsIn(window, view).Left + line.TextLines.Max(row => row.Width) - view.ScrollOffset.X;
    }

    private static ScrollViewer EditorScrollViewer(EditorPaneView pane)
        => pane.EditorControl.GetVisualDescendants().OfType<ScrollViewer>().First(scroll => scroll.Name == "PART_ScrollViewer");

    private static ScrollBar Bar(EditorPaneView pane, Orientation orientation)
        => EditorScrollViewer(pane).GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Orientation == orientation);

    private static Rect BoundsIn(MainWindow window, Visual visual)
        => new(visual.TranslatePoint(default, window)!.Value, visual.Bounds.Size);
}

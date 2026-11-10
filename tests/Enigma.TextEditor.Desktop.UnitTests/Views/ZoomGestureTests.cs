using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// Zooming the text with the pointer on the real window: Ctrl+wheel over the panes.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ZoomGestureTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void CtrlWheelUpOverTheText_ZoomsInOneSize_AndDownZoomsOut()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, EditorPaneView pane) = Show(harness);
            Button status = window.FindControl<Button>("FontSizeStatus")!;

            window.MouseWheel(OverText(pane, window), new Vector(0, 1), RawInputModifiers.Control);
            await DrainAsync();

            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Settings.Value.EditorFontSize);
            Assert.Equal(AppSettings.DefaultFontSize + 1, pane.EditorControl.FontSize);
            Assert.Equal("Font Size: 15", status.Content);

            window.MouseWheel(OverText(pane, window), new Vector(0, -1), RawInputModifiers.Control);
            window.MouseWheel(OverText(pane, window), new Vector(0, -1), RawInputModifiers.Control);
            await DrainAsync();

            Assert.Equal(AppSettings.DefaultFontSize - 1, harness.Settings.Value.EditorFontSize);
            Assert.Equal(AppSettings.DefaultFontSize - 1, pane.EditorControl.FontSize);
            window.Close();
        });

    [Fact]
    public void CtrlWheel_DoesNotScrollTheText_ButAPlainWheelStillDoes_AndDoesNotZoom()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Lines(2000)));
            (MainWindow window, EditorPaneView pane) = Show(harness);

            window.MouseWheel(OverText(pane, window), new Vector(0, -2), RawInputModifiers.Control);
            await DrainAsync();

            Assert.Equal(0, pane.EditorControl.VerticalOffset, 3);
            Assert.Equal(AppSettings.DefaultFontSize - 2, harness.Settings.Value.EditorFontSize);

            window.MouseWheel(OverText(pane, window), new Vector(0, -2));
            await DrainAsync();

            Assert.True(pane.EditorControl.VerticalOffset > 0);
            Assert.Equal(AppSettings.DefaultFontSize - 2, harness.Settings.Value.EditorFontSize);
            window.Close();
        });

    [Fact]
    public void FractionsOfANotch_ZoomOnceTheyAddUpToOne()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, EditorPaneView pane) = Show(harness);

            for (int step = 0; step < 3; step++)
            {
                window.MouseWheel(OverText(pane, window), new Vector(0, 0.25), RawInputModifiers.Control);
            }

            await DrainAsync();
            Assert.Equal(AppSettings.DefaultFontSize, harness.Settings.Value.EditorFontSize);

            window.MouseWheel(OverText(pane, window), new Vector(0, 0.25), RawInputModifiers.Control);
            await DrainAsync();
            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Settings.Value.EditorFontSize);
            window.Close();
        });

    [Fact]
    public void CtrlWheelOverTheHexView_ZoomsToo()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", new byte[256]));
            (MainWindow window, EditorPaneView pane) = Show(harness);
            HexView hex = pane.HexView;
            Point centre = hex.TranslatePoint(new Point(hex.Bounds.Width / 2, hex.Bounds.Height / 2), window)!.Value;

            window.MouseWheel(centre, new Vector(0, 1), RawInputModifiers.Control);
            await DrainAsync();

            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Settings.Value.EditorFontSize);
            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Preferences.FontSize);
            window.Close();
        });

    [Fact]
    public void CtrlWheelOutsideThePanes_DoesNotZoom()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, _) = Show(harness);
            Control bar = window.FindControl<Control>("StatusBar")!;
            Point onBar = bar.TranslatePoint(new Point(bar.Bounds.Width / 2, bar.Bounds.Height / 2), window)!.Value;

            window.MouseWheel(onBar, new Vector(0, 1), RawInputModifiers.Control);
            await DrainAsync();

            Assert.Equal(AppSettings.DefaultFontSize, harness.Settings.Value.EditorFontSize);
            window.Close();
        });

    private static Point OverText(EditorPaneView pane, MainWindow window)
    {
        Control text = pane.EditorControl.TextArea.TextView;

        return text.TranslatePoint(new Point(text.Bounds.Width / 3, text.Bounds.Height / 2), window)!.Value;
    }

    private static string Lines(int count)
    {
        StringBuilder text = new();

        for (int line = 1; line <= count; line++)
        {
            text.Append("line ").Append(line).Append('\n');
        }

        return text.ToString();
    }

    private static (MainWindow Window, EditorPaneView Pane) Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, window.GetVisualDescendants().OfType<EditorPaneView>().Single());
    }

    // The zoom writes the settings without being awaited: let each write finish before the next step or the end.
    private static async Task DrainAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }
}

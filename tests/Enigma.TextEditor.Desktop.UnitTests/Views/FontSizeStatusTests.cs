using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The status bar's font size item on the real window: what it says, the sizes it lists, and when it shows.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FontSizeStatusTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheItem_ListsTheSizes_TheCurrentOneChecked_AndChoosingOneResizesTheText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Button button = window.FindControl<Button>("FontSizeStatus")!;

            Assert.Equal("Font Size: 14", button.Content);

            MenuItem[] items = await OpenAsync(window, button);

            Assert.Equal(["8", "9", "10", "11", "12", "13", "14 (default)", "15", "16", "18", "20", "24", "28", "32", "36"],
                items.Select(item => (string?)item.Header));
            Assert.Equal("14 (default)", (string?)Assert.Single(items, item => item.IsChecked).Header);
            Assert.All(items, item => Assert.Equal(MenuItemToggleType.Radio, item.ToggleType));

            // Chosen while the list is open, as a click does: the list follows at once, then the flyout closes.
            MenuItem eighteen = items.Single(item => (string?)item.Header == "18");
            eighteen.Command!.Execute(eighteen.CommandParameter);
            await DrainAsync();
            Assert.Equal("18", (string?)Assert.Single(Live(items), item => item.IsChecked).Header);
            Assert.IsType<MenuFlyout>(button.Flyout).Hide();
            await DrainAsync();

            Assert.Equal(18, pane.EditorControl.FontSize);
            Assert.Equal("Font Size: 18", button.Content);
            Assert.Equal(18, harness.Settings.Value.EditorFontSize);

            // Opened again, the list still checks where the text is.
            Assert.Equal("18", (string?)Assert.Single(await OpenAsync(window, button), item => item.IsChecked).Header);
            window.Close();
        });

    [Fact]
    public void TheItem_FollowsTheZoomKeys()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);
            Button button = window.FindControl<Button>("FontSizeStatus")!;

            await harness.SettingsMenu.ZoomInCommand.ExecuteAsync(null);
            await DrainAsync();

            Assert.Equal("Font Size: 15", button.Content);
            window.Close();
        });

    [Fact]
    public void TheItem_ShowsOnAHexTab_ButNotWithNoTabOpen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);
            Button button = window.FindControl<Button>("FontSizeStatus")!;

            Assert.False(button.IsEffectivelyVisible);

            await harness.Workspace.OpenHexAsync(harness.Folder.Write("data.bin", [0x00, 0x01, 0x02]));
            await DrainAsync();

            Assert.True(harness.StatusBar.IsHexTab);
            Assert.True(button.IsEffectivelyVisible);
            window.Close();
        });

    /// <summary>
    /// Opens the item's flyout, and returns the items it made from the list.
    /// </summary>
    /// <remarks>
    /// They live in the popup that hosts the flyout, a tree of its own the window does not reach, so they are caught as
    /// they arrive on screen.
    /// </remarks>
    private static async Task<MenuItem[]> OpenAsync(MainWindow window, Button button)
    {
        List<MenuItem> loaded = [];

        using (Control.LoadedEvent.AddClassHandler<MenuItem>((item, _) => loaded.Add(item), handledEventsToo: true))
        {
            Click(window, button);
            await DrainAsync();
        }

        Assert.True(Assert.IsType<MenuFlyout>(button.Flyout).IsOpen);

        return [.. loaded.Where(item => item.DataContext is FontSizeChoice).Distinct()];
    }

    // The items still in the flyout: an entry whose check changed is shown by a new one.
    private static MenuItem[] Live(MenuItem[] items)
        => [.. items[0].Parent!.GetLogicalChildren().OfType<MenuItem>()];

    private static void Click(MainWindow window, Button button)
    {
        Point centre = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseDown(centre, MouseButton.Left);
        window.MouseUp(centre, MouseButton.Left);
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static async Task DrainAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }
}

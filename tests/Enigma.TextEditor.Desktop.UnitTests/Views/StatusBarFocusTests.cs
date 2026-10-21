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
/// The status bar's buttons leave the keyboard with the active pane, so typing goes on without a click
/// in the text.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class StatusBarFocusTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData("IndentationStatus")]
    [InlineData("EncodingStatus")]
    [InlineData("LanguageStatus")]
    [InlineData("LineEndingStatus")]
    public void TheStatusButtons_TakeNoFocus(string name)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);

            Assert.False(window.FindControl<Button>(name)!.Focusable);
            window.Close();
        });

    [Theory]
    [InlineData("IndentationStatus")]
    [InlineData("LineEndingStatus")]
    [InlineData("EncodingStatus")]
    public void AStatusFlyout_ThatCloses_GivesTheKeyboardBackToTheEditor(string name)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Button button = window.FindControl<Button>(name)!;

            Click(window, button);
            await DrainAsync();
            MenuFlyout flyout = Assert.IsType<MenuFlyout>(button.Flyout);
            Assert.True(flyout.IsOpen);

            flyout.Hide();
            await DrainAsync();

            Assert.True(pane.EditorControl.TextArea.IsFocused);
            window.Close();
        });

    [Fact]
    public void ChoosingInTheIndentationFlyout_LeavesTheKeyboardInTheActivePane_OfASplit()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            EditorPaneViewModel right = harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            EditorPaneView rightView = window.GetVisualDescendants().OfType<EditorPaneView>().Single(view => ReferenceEquals(view.DataContext, right));
            Button button = window.FindControl<Button>("IndentationStatus")!;

            Click(window, button);
            await DrainAsync();
            MenuFlyout flyout = Assert.IsType<MenuFlyout>(button.Flyout);
            MenuItem twoSpaces = flyout.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "Spaces: 2");
            twoSpaces.Command!.Execute(null);
            flyout.Hide();
            await DrainAsync();

            Assert.Equal("Spaces: 2", harness.StatusBar.IndentationText);
            Assert.Same(right, harness.Workspace.ActivePane);
            Assert.True(rightView.EditorControl.TextArea.IsFocused);
            window.Close();
        });

    [Fact]
    public void TheLanguageButton_LeavesTheKeyboardInTheEditor()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            pane.FocusEditor();
            await DrainAsync();

            Click(window, window.FindControl<Button>("LanguageStatus")!);
            await DrainAsync();

            Assert.Single(harness.LanguagePicker.OpenedWith);
            Assert.True(pane.EditorControl.TextArea.IsFocused);
            window.Close();
        });

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

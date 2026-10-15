using System.Linq;
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
/// The split host in the real window: one editor per pane, the active one framed, and a click moving
/// the keyboard — and the status bar — to the pane clicked.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SplitHostTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void SplittingRight_ShowsTwoEditorsSideBySideOverOneBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "shared"));
            MainWindow window = Show(harness);

            harness.Workspace.SplitRightCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];
            Assert.Equal(2, panes.Length);
            Assert.Same(panes[0].EditorControl.Document, panes[1].EditorControl.Document);
            Assert.True(panes[0].Bounds.X < panes[1].Bounds.X);
            Assert.Contains("framed", panes[1].GetVisualDescendants().OfType<Border>().First().Classes);
            window.Close();
        });

    [Fact]
    public void ClickingAPane_MakesItActive_AndTheStatusBarFollows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "line one\nline two\n"));
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            EditorPaneView left = window.GetVisualDescendants().OfType<EditorPaneView>().First();
            EditorPaneViewModel leftPane = (EditorPaneViewModel)left.DataContext!;

            // A caret column no click can produce, so the status bar can only show it while the right
            // pane is the active one.
            harness.Workspace.ActivePane.ActiveTab!.CaretColumn = 99;
            Assert.EndsWith("Col 99", window.FindControl<TextBlock>("CaretStatus")?.Text, System.StringComparison.Ordinal);

            // The click itself moves the left pane's caret, so the status bar is checked against
            // wherever it put it.
            Point inside = left.TranslatePoint(new Point(left.Bounds.Width / 2, 60), window)!.Value;
            window.MouseDown(inside, MouseButton.Left);
            window.MouseUp(inside, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            EditorTabViewModel tab = leftPane.ActiveTab!;
            Assert.Same(leftPane, harness.Workspace.ActivePane);
            Assert.Equal($"Ln {tab.CaretLine}, Col {tab.CaretColumn}", window.FindControl<TextBlock>("CaretStatus")?.Text);
            window.Close();
        });

    [Fact]
    public void ClosingAPane_TakesItsEditorOffTheScreen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            harness.Workspace.Split(SplitAxis.Rows);
            MainWindow window = Show(harness);

            await harness.Workspace.ClosePaneCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Single(window.GetVisualDescendants().OfType<EditorPaneView>());
            window.Close();
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

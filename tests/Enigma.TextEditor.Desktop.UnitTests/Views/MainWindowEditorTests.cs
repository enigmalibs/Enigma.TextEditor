using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The real window over the editor view-models: what the pane shows, what the status bar says, and
/// what the window's shortcuts do.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class MainWindowEditorTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheActiveTabsText_IsWhatTheEditorShows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "alpha"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "beta"));
            MainWindow window = Show(harness);

            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Assert.Equal("beta", pane.EditorControl.Document.Text);

            harness.Workspace.ActivePane.ActiveTab = harness.Workspace.ActivePane.Tabs[0];
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("alpha", pane.EditorControl.Document.Text);
            window.Close();
        });

    [Fact]
    public void MovingTheCaret_UpdatesLnAndColInTheStatusBar()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "one\ntwo three\n"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            pane.EditorControl.CaretOffset = 8;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Ln 2, Col 5", window.FindControl<TextBlock>("CaretStatus")?.Text);
            window.Close();
        });

    [Fact]
    public void EachTab_KeepsItsOwnCaret()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "aaaaaaaa"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "bbbbbbbb"));
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            pane.EditorControl.CaretOffset = 6;
            harness.Workspace.ActivePane.PreviousTab();
            Dispatcher.UIThread.RunJobs();
            pane.EditorControl.CaretOffset = 2;
            harness.Workspace.ActivePane.NextTab();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(6, pane.EditorControl.CaretOffset);
            window.Close();
        });

    [Fact]
    public void CtrlTab_ShowsTheNextTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel? a = await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            MainWindow window = Show(harness);
            window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
            Dispatcher.UIThread.RunJobs();

            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(a, harness.Workspace.ActiveTab);
            window.Close();
        });

    [Fact]
    public void WithNoTab_ThePaneShowsAPlaceholderAndTheStatusBarHidesTheCaret()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            MainWindow window = Show(harness);

            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            Assert.False(pane.EditorControl.IsVisible);
            Assert.False(window.FindControl<TextBlock>("CaretStatus")?.IsEffectivelyVisible);
            window.Close();
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

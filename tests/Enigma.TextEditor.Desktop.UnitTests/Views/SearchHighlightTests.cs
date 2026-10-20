using System.Linq;
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
/// The search highlight drawn in the real window: while typing, after Enter, after <c>:noh</c>, and
/// in every pane.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SearchHighlightTests(HeadlessAvaloniaFixture fixture)
{
    private const string Text = "a foo here\nand foo there\nno match\n";

    [Fact]
    public void WhileTyping_EveryMatchOnScreenIsHighlighted()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            EditorPaneView pane = Pane(window);

            window.KeyTextInput("/foo");
            Render(window);

            Assert.Equal([(2, 3), (15, 3)], pane.Vim!.Highlight.LastMatches);
            Assert.Equal(0, pane.EditorControl.CaretOffset);
            window.Close();
        });

    [Fact]
    public void AfterEnter_TheMatchesStayHighlighted_AndNohClearsThem()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            EditorPaneView pane = Pane(window);

            window.KeyTextInput("/foo");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Render(window);

            Assert.Equal(2, pane.EditorControl.CaretOffset);
            Assert.Equal([(2, 3), (15, 3)], pane.Vim!.Highlight.LastMatches);

            window.KeyTextInput(":noh");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Render(window);

            Assert.Empty(pane.Vim.Highlight.LastMatches);

            window.KeyTextInput("n");
            Render(window);

            Assert.Equal(2, pane.Vim.Highlight.LastMatches.Count);
            window.Close();
        });

    [Fact]
    public void Escape_PutsBackWhatWasHighlightedBefore()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            EditorPaneView pane = Pane(window);

            window.KeyTextInput("/match");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyTextInput("/foo");
            Render(window);
            Assert.Equal(2, pane.Vim!.Highlight.LastMatches.Count);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Render(window);

            Assert.Equal([(28, 5)], pane.Vim.Highlight.LastMatches);
            window.Close();
        });

    [Fact]
    public void TheHighlight_ShowsInEveryPane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text)))!;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Show(tab.Document);
            MainWindow window = Show(harness);
            EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];
            panes[1].FocusEditor();
            Dispatcher.UIThread.RunJobs();

            window.KeyTextInput("/there");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Render(window);

            Assert.Equal([(19, 5)], panes[0].Vim!.Highlight.LastMatches);
            Assert.Equal([(19, 5)], panes[1].Vim!.Highlight.LastMatches);
            window.Close();
        });

    [Fact]
    public void WithVimOff_NothingIsHighlighted()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            harness.VimShared.HlSearch = true;
            MainWindow window = Show(harness);

            Render(window);

            Assert.Empty(Pane(window).Vim!.Highlight.LastMatches);
            window.Close();
        });

    private static EditorPaneView Pane(MainWindow window) => window.GetVisualDescendants().OfType<EditorPaneView>().First();

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Pane(window).FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    // A frame has to be drawn for the renderer to run.
    private static void Render(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        _ = window.CaptureRenderedFrame();
    }
}

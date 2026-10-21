using System.Linq;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// <c>:set</c> typed in the real window, for what the status bar shows: the buffer follows, and so does
/// everything that reads the buffer.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SetCommandTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void SetNoExpandTabAndAWidth_IndentsTheBufferWithTabs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x")))!;
            MainWindow window = Show(harness);

            Ex(window, "set noet sw=8");

            Assert.Equal(Indentation.Tabs(8), tab.Document.Indentation);
            Assert.Equal("Tab Size: 8", harness.StatusBar.IndentationText);
            Assert.True(harness.Tools.IsTabs);
            Assert.False(Pane(window).EditorControl.Options.ConvertTabsToSpaces);
            Assert.Equal(8, Pane(window).EditorControl.Options.IndentationSize);
            window.Close();
        });

    [Fact]
    public void SetExpandTabAndAWidth_IndentsTheBufferWithSpaces_AndTheShiftFollows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x")))!;
            MainWindow window = Show(harness);

            Ex(window, "set ts=2");
            window.KeyTextInput(">>");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(Indentation.Spaces(2), tab.Document.Indentation);
            Assert.Equal("Spaces: 2", harness.StatusBar.IndentationText);
            Assert.True(harness.Tools.IsTwoSpaces);
            Assert.Equal("  x", tab.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void ASetThatChangesNoIndentation_LeavesTheBufferAlone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x")))!;
            tab.Document.Indentation = Indentation.Spaces(2);
            MainWindow window = Show(harness);

            Ex(window, "set ic");

            Assert.Equal(Indentation.Spaces(2), tab.Document.Indentation);
            window.Close();
        });

    [Fact]
    public void TheIndentationSet_IsTheBuffers_SoAnotherPaneOnItFollows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x")))!;
            harness.Workspace.Split(Core.Layout.SplitAxis.Columns, newDocument: false);
            harness.Workspace.Show(tab.Document);
            MainWindow window = Show(harness);

            Ex(window, "set noet");

            Assert.All(
                window.GetVisualDescendants().OfType<EditorPaneView>(),
                pane => Assert.False(pane.EditorControl.Options.ConvertTabsToSpaces));
            window.Close();
        });

    private static void Ex(MainWindow window, string command)
    {
        window.KeyTextInput(":" + command);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static EditorPaneView Pane(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().First();

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

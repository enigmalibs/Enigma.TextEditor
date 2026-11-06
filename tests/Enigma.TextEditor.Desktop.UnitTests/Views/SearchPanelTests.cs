using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Search;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// AvaloniaEdit's find and replace panel with vim on, in every mode, driven by keystrokes through the
/// real window (BUG-B026).
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SearchPanelTests(HeadlessAvaloniaFixture fixture)
{
    private const string Text = "alpha\nbeta\ngamma\n";

    [Fact]
    public void CtrlF_InNormalMode_OpensFind_StaysInNormalMode_AndDoesNotScroll()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);

            SearchPanel panel = Editor(window).SearchPanel;
            Assert.True(panel.IsOpened);
            Assert.False(panel.IsReplaceMode);
            Assert.Equal("NORMAL", ModeText(window));
            Assert.Equal(0, Editor(window).CaretOffset);
            Assert.Equal(Text, Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void CtrlH_InNormalMode_OpensReplace_AndStaysInNormalMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.H);

            SearchPanel panel = Editor(window).SearchPanel;
            Assert.True(panel.IsOpened);
            Assert.True(panel.IsReplaceMode);
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Theory]
    [InlineData(PhysicalKey.F, false)]
    [InlineData(PhysicalKey.H, true)]
    public void TheShortcuts_InInsertMode_OpenThePanel_AndStayInInsertMode(PhysicalKey key, bool replace)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            window.KeyTextInput("i");
            PressCtrl(window, key);

            SearchPanel panel = Editor(window).SearchPanel;
            Assert.True(panel.IsOpened);
            Assert.Equal(replace, panel.IsReplaceMode);
            Assert.Equal("INSERT", ModeText(window));
            Assert.Equal(Text, Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void CtrlF_InVisualMode_LooksForTheSelection_AndLeavesVisualMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            window.KeyTextInput("jviw");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("VISUAL", ModeText(window));

            PressCtrl(window, PhysicalKey.F);

            SearchPanel panel = Editor(window).SearchPanel;
            Assert.True(panel.IsOpened);
            Assert.Equal("beta", panel.SearchPattern);
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void CtrlF_AfterAHalfTypedOperator_DropsIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            window.KeyTextInput("d");
            PressCtrl(window, PhysicalKey.F);
            Assert.True(Editor(window).SearchPanel.IsOpened);

            Editor(window).SearchPanel.Close();
            window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("w");
            Dispatcher.UIThread.RunJobs();

            // 'w' alone moves to the next word: the 'd' is not waiting to delete it any more.
            Assert.Equal(Text, Editor(window).Document.Text);
            Assert.Equal(6, Editor(window).CaretOffset);
            window.Close();
        });

    [Fact]
    public void PageDown_StillPagesDownInNormalMode()
        => fixture.RunAsync(async () =>
        {
            StringBuilder lines = new();

            for (int line = 1; line <= 300; line++)
            {
                lines.Append("line ").Append(line).Append('\n');
            }

            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", lines.ToString()));
            MainWindow window = Show(harness);

            window.KeyPress(Key.PageDown, RawInputModifiers.None, PhysicalKey.PageDown, null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(Editor(window).TextArea.Caret.Line > 1);
            Assert.False(Editor(window).SearchPanel.IsOpened);
            window.Close();
        });

    [Fact]
    public void WithVimOff_CtrlF_StillOpensFind()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);

            Assert.True(Editor(window).SearchPanel.IsOpened);
            window.Close();
        });

    private static void PressCtrl(MainWindow window, PhysicalKey key)
    {
        window.KeyPressQwerty(key, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
    }

    private static string? ModeText(MainWindow window) => window.FindControl<TextBlock>("ModeText")?.Text;

    private static AvaloniaEdit.TextEditor Editor(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl;

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

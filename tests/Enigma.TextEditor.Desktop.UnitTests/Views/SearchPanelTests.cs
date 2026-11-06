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

    [Theory]
    [InlineData("")]
    [InlineData("i")]
    public void TypingInTheFindField_WritesThePattern_AndLeavesTheTextAndTheModeAlone(string keysFirst)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            window.KeyTextInput(keysFirst);
            string mode = ModeText(window)!;

            PressCtrl(window, PhysicalKey.F);
            window.KeyTextInput("betx");
            Press(window, Key.Back, PhysicalKey.Backspace);

            Assert.Equal("bet", Editor(window).SearchPanel.SearchPattern);
            Assert.Equal(Text, Editor(window).Document.Text);
            Assert.Equal(mode, ModeText(window));
            window.Close();
        });

    [Fact]
    public void EnterInTheFindField_SelectsTheNextMatch()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "one two one two\n"));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);
            window.KeyTextInput("two");
            Dispatcher.UIThread.RunJobs();
            int first = Editor(window).SelectionStart;

            Press(window, Key.Enter, PhysicalKey.Enter);

            Assert.Equal("two", Editor(window).SelectedText);
            Assert.NotEqual(first, Editor(window).SelectionStart);
            Assert.Equal("one two one two\n", Editor(window).Document.Text);
            window.Close();
        });

    [Theory]
    [InlineData("", "NORMAL")]
    [InlineData("i", "INSERT")]
    [InlineData("v", "NORMAL")]
    public void EscInThePanel_ClosesIt_AndOnlyThat(string keysFirst, string mode)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            window.KeyTextInput(keysFirst);

            PressCtrl(window, PhysicalKey.F);
            window.KeyTextInput("beta");
            Press(window, Key.Escape, PhysicalKey.Escape);

            Assert.False(Editor(window).SearchPanel.IsOpened);
            Assert.Equal(mode, ModeText(window));
            Assert.Equal(Text, Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void TypingInTheReplaceField_WritesTheReplacement_InNormalMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.H);
            window.KeyTextInput("beta");
            TextBox replace = Fields(window)[1];
            replace.Focus();
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("delta");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("beta", Editor(window).SearchPanel.SearchPattern);
            Assert.Equal("delta", Editor(window).SearchPanel.ReplacePattern);
            Assert.Equal(Text, Editor(window).Document.Text);
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void EscFromTheText_InNormalMode_ClosesTheOpenPanel()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);
            FocusText(window);
            Press(window, Key.Escape, PhysicalKey.Escape);

            Assert.False(Editor(window).SearchPanel.IsOpened);
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void EscFromTheText_InInsertMode_LeavesInsertFirst_ThenClosesThePanel()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            window.KeyTextInput("i");
            PressCtrl(window, PhysicalKey.F);
            FocusText(window);

            Press(window, Key.Escape, PhysicalKey.Escape);
            Assert.True(Editor(window).SearchPanel.IsOpened);
            Assert.Equal("NORMAL", ModeText(window));

            Press(window, Key.Escape, PhysicalKey.Escape);
            Assert.False(Editor(window).SearchPanel.IsOpened);
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void VimKeysInTheText_StillWork_WhileThePanelIsOpen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);
            FocusText(window);
            Editor(window).CaretOffset = 0;
            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("lpha\nbeta\ngamma\n", Editor(window).Document.Text);
            Assert.True(Editor(window).SearchPanel.IsOpened);
            window.Close();
        });

    [Theory]
    [InlineData(true, "")]
    [InlineData(true, "i")]
    [InlineData(false, "")]
    public void TabInTheFindField_GoesToTheReplaceField_AndShiftTabComesBack(bool vim, string keysFirst)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = vim);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);
            window.KeyTextInput(keysFirst);

            PressCtrl(window, PhysicalKey.H);
            Assert.Equal(SearchPanelTabOrder.FindFieldName, FocusedName(window));

            Press(window, Key.Tab, PhysicalKey.Tab);
            Assert.Equal(SearchPanelTabOrder.ReplaceFieldName, FocusedName(window));

            window.KeyPress(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(SearchPanelTabOrder.FindFieldName, FocusedName(window));

            Assert.Equal(Text, Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void TabInTheFindOnlyPanel_KeepsThePlatformsOrder()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", Text));
            MainWindow window = Show(harness);

            PressCtrl(window, PhysicalKey.F);
            Press(window, Key.Tab, PhysicalKey.Tab);

            Control? focused = window.FocusManager?.GetFocusedElement() as Control;
            Assert.NotNull(focused);
            Assert.NotEqual(SearchPanelTabOrder.FindFieldName, focused.Name);
            Assert.NotEqual(SearchPanelTabOrder.ReplaceFieldName, focused.Name);
            Assert.True(Editor(window).SearchPanel.IsVisualAncestorOf(focused));
            window.Close();
        });

    private static void PressCtrl(MainWindow window, PhysicalKey key)
    {
        window.KeyPressQwerty(key, RawInputModifiers.Control);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Press(MainWindow window, Key key, PhysicalKey physical)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void FocusText(MainWindow window)
    {
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        Dispatcher.UIThread.RunJobs();
    }

    // The panel's two fields, the find field first.
    private static TextBox[] Fields(MainWindow window)
        => [.. Editor(window).SearchPanel.GetVisualDescendants().OfType<TextBox>()];

    private static string? FocusedName(MainWindow window)
        => (window.FocusManager?.GetFocusedElement() as Control)?.Name;

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

using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// <em>Tools → Line Endings</em>, <em>Tools → Indentation</em>, and the complete status bar.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class LineEndingAndIndentationToolsTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void SwitchingToCrLf_MarksTheBufferUnsaved_AndTheSaveWritesCrLf()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "one\ntwo\n");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            Assert.True(harness.Tools.LineEndingChoices.Choice("LF").IsCurrent);

            harness.Tools.LineEndingChoices.Choice("CRLF").Command.Execute(null);

            Assert.True(tab.Document.IsDirty);
            Assert.True(harness.Tools.LineEndingChoices.Choice("CRLF").IsCurrent);
            Assert.False(harness.Tools.LineEndingChoices.Choice("LF").IsCurrent);

            await harness.Workspace.SaveAsync(tab.Document);

            Assert.Equal("one\r\ntwo\r\n", File.ReadAllText(path));
            Assert.False(tab.Document.IsDirty);
        });

    [Fact]
    public void ChangingTheIndentation_DoesNotMarkTheBufferUnsaved()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x")))!;

            harness.Tools.IndentationChoices.Choice("Tabs").Command.Execute(null);

            Assert.Equal(Indentation.Tabs(ToolsViewModel.TabSize), tab.Document.Indentation);
            Assert.True(harness.Tools.IndentationChoices.Choice("Tabs").IsCurrent);
            Assert.False(tab.Document.IsDirty);

            harness.Tools.IndentationChoices.Choice("Spaces: 2").Command.Execute(null);
            Assert.True(harness.Tools.IndentationChoices.Choice("Spaces: 2").IsCurrent);
            Assert.False(harness.Tools.IndentationChoices.Choice("Tabs").IsCurrent);
        });

    [Fact]
    public void Tabs_MakeTheEditorAndVimShiftWithATab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "line\n")))!;
            MainWindow window = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();

            harness.Tools.IndentationChoices.Choice("Tabs").Command.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.False(pane.EditorControl.Options.ConvertTabsToSpaces);

            window.KeyTextInput(">>");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("\tline\n", tab.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TwoSpaces_MakeVimShiftByTwo_InEveryPaneShowingTheBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "line\n")))!;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Show(tab.Document);
            MainWindow window = Show(harness);
            EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];

            harness.Tools.IndentationChoices.Choice("Spaces: 2").Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.All(panes, pane => Assert.Equal(2, pane.EditorControl.Options.IndentationSize));

            panes[0].FocusEditor();
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput(">>");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("  line\n", tab.Document.Document.Text);
            window.Close();
        });

    [Fact]
    public void TheStatusBar_ShowsEveryItemTheDraftAsksFor()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("code.py", "def a():\r\n  return 1\r\n"));
            MainWindow window = Show(harness);

            Assert.Equal("NORMAL", window.FindControl<TextBlock>("ModeText")?.Text);
            Assert.StartsWith("Ln ", window.FindControl<TextBlock>("CaretStatus")?.Text, System.StringComparison.Ordinal);
            Assert.Equal("Spaces: 2", window.FindControl<Button>("IndentationStatus")?.Content);
            Assert.Equal("UTF-8", window.FindControl<Button>("EncodingStatus")?.Content);
            Assert.Equal("Python", window.FindControl<Button>("LanguageStatus")?.Content);
            Assert.Equal("CRLF", window.FindControl<Button>("LineEndingStatus")?.Content);

            harness.Tools.LineEndingChoices.Choice("LF").Command.Execute(null);
            harness.Tools.IndentationChoices.Choice("Tabs").Command.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("LF", window.FindControl<Button>("LineEndingStatus")?.Content);
            Assert.Equal("Tab Size: 4", window.FindControl<Button>("IndentationStatus")?.Content);
            window.Close();
        });

    [Fact]
    public void TheToolsMenu_HasTheRadioSubmenus()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);

            MenuItem tools = window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Tools");
            MenuItem lineEndings = tools.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "Line E_ndings");
            MenuItem indentation = tools.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Indentation");

            Assert.Equal([("LF", true), ("CRLF", false)], MenuItems.Shown(lineEndings));
            Assert.Equal(["Spaces: 2", "Spaces: 4", "Tabs"], MenuItems.Shown(indentation).Select(item => item.Header));
            window.Close();
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().First().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

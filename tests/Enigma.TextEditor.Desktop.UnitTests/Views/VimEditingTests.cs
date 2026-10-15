using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The Vim engine driven through the real window by keystrokes, the way a user drives it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class VimEditingTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheEditorStartsInNormalMode_AndTypingDoesNotEditTheText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();

            // 'x' in normal mode deletes the character under the caret rather than typing one.
            Assert.Equal("bc", Editor(window).Document.Text);
            Assert.Equal("NORMAL", window.FindControl<TextBlock>("ModeText")?.Text);
            window.Close();
        });

    [Fact]
    public void Insert_TypesText_AndEscapeReturnsToNormal()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "world"));
            MainWindow window = Show(harness);

            window.KeyTextInput("i");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("INSERT", window.FindControl<TextBlock>("ModeText")?.Text);

            window.KeyTextInput("hello ");
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("hello world", Editor(window).Document.Text);
            Assert.Equal("NORMAL", window.FindControl<TextBlock>("ModeText")?.Text);
            window.Close();
        });

    [Fact]
    public void ColonW_SavesTheFile_AndTheCommandLineShowsWhileItIsTyped()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "abc");
            await harness.Workspace.OpenAsync(path);
            MainWindow window = Show(harness);

            window.KeyTextInput("x");
            window.KeyTextInput(":w");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(":w", window.FindControl<TextBlock>("CommandLine")?.Text);
            Assert.True(window.FindControl<TextBlock>("CommandLine")?.IsVisible);

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await WaitFor(() => File.ReadAllText(path) == "bc");

            Assert.Equal("bc", File.ReadAllText(path));
            Assert.False(window.FindControl<TextBlock>("CommandLine")?.IsVisible);
            window.Close();
        });

    [Fact]
    public void CtrlS_SavesInNormalMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "abc");
            await harness.Workspace.OpenAsync(path);
            MainWindow window = Show(harness);

            window.KeyTextInput("x");
            window.KeyPressQwerty(PhysicalKey.S, RawInputModifiers.Control);
            await WaitFor(() => File.ReadAllText(path) == "bc");

            Assert.Equal("bc", File.ReadAllText(path));
            window.Close();
        });

    [Fact]
    public void AnEngineMessage_ShowsOnTheMessageLine()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput(":nonsense");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Not an editor command: nonsense", window.FindControl<TextBlock>("MessageLine")?.Text);
            window.Close();
        });

    [Fact]
    public void WithVimOff_KeysTypeAndTheChipIsHidden()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("xabc", Editor(window).Document.Text);
            Assert.False(window.FindControl<Border>("ModeChip")?.IsVisible);
            window.Close();
        });

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

    // A save is awaited off a keystroke; pump the dispatcher until it has landed.
    private static async System.Threading.Tasks.Task WaitFor(System.Func<bool> condition)
    {
        for (int attempt = 0; (attempt < 200) && !condition(); attempt++)
        {
            Dispatcher.UIThread.RunJobs();
            await System.Threading.Tasks.Task.Delay(10);
        }
    }
}

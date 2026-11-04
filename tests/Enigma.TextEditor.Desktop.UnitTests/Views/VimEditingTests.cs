using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
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

    // BUG-375B: an insert session holds an undo group open until Esc, and AvaloniaEdit's undo throws
    // inside one — then forgets the group, so that the Esc after it threw too. Every route to an undo
    // goes through the engine now, and each one is driven here as the user drives it.
    [Fact]
    public void CtrlZInInsertMode_TakesBackWhatWasTyped_StaysInInsertMode_AndEscStillLeaves()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            // The 'x' gives the undo stack something before the session: the crash needed a step to take.
            window.KeyTextInput("x");
            window.KeyTextInput("i");
            window.KeyTextInput("hello");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("hellobc", Editor(window).Document.Text);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("bc", Editor(window).Document.Text);
            Assert.Equal("INSERT", ModeText(window));

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("abc", Editor(window).Document.Text);

            window.KeyTextInput("Q");
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Qabc", Editor(window).Document.Text);
            Assert.Equal("NORMAL", ModeText(window));

            // 'u' takes back only what was typed after the last Ctrl+Z.
            window.KeyTextInput("u");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("abc", Editor(window).Document.Text);
            window.Close();
        });

    [Fact]
    public void TheRedoKeysInInsertMode_PutBackWhatWasTaken_AndStayInInsertMode()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("xu");
            window.KeyTextInput("i");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("abc", Editor(window).Document.Text);

            window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("bc", Editor(window).Document.Text);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control | RawInputModifiers.Shift);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("bc", Editor(window).Document.Text);
            Assert.Equal("INSERT", ModeText(window));
            window.Close();
        });

    [Theory]
    [InlineData("UndoItem")]
    [InlineData("MenuUndo")]
    public void UndoFromAMenuInInsertMode_TakesBackWhatWasTyped_AndStaysInInsertMode(string item)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("i");
            window.KeyTextInput("hello");
            Dispatcher.UIThread.RunJobs();

            // The Edit menu's item is the window's, the editor's own menu's is the pane's.
            MenuItem menu = window.FindControl<MenuItem>(item)
                ?? window.GetVisualDescendants().OfType<EditorPaneView>().Single().FindControl<MenuItem>(item)
                ?? throw new Xunit.Sdk.XunitException($"No menu item {item}.");
            menu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("abc", Editor(window).Document.Text);
            Assert.Equal("INSERT", ModeText(window));

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void ReopeningWithAnotherEncodingInInsertMode_GoesBackToNormalMode_AndReloads()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("i");
            window.KeyTextInput("X");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Xabc", Editor(window).Document.Text);

            DocumentViewModel document = harness.Workspace.ActivePane.ActiveTab!.Document;
            Assert.True(await harness.Documents.ReopenWithEncodingAsync(document, TextEncodings.Windows1252));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("abc", Editor(window).Document.Text);
            Assert.Equal("NORMAL", ModeText(window));
            Assert.False(document.Document.UndoStack.CanUndo);

            // The group was closed before the reload, not lost under it: Esc has nothing left to throw on.
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("NORMAL", ModeText(window));
            window.Close();
        });

    [Fact]
    public void WithVimOff_CtrlZIsTheEditorsUndo()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.VimEnabled = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("xabc", Editor(window).Document.Text);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("abc", Editor(window).Document.Text);
            window.Close();
        });

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

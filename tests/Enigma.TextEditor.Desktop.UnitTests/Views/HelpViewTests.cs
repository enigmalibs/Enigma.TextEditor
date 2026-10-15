using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The help in the real window: what opens it from the editor, and the dialog itself — a card per section
/// in one scroll area, the editor's scrolling keys, and <c>q</c> giving the keyboard back to the text.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HelpViewTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ColonHelp_AndF1_TypedInTheEditor_AskForTheHelp()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);

            window.KeyTextInput(":help");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await DrainAsync();
            int afterHelp = harness.Help.Shown;

            window.KeyTextInput(":h");
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await DrainAsync();
            int afterH = harness.Help.Shown;

            // F1 is no Vim key: the editor lets it through to the window's own binding.
            window.KeyPress(Key.F1, RawInputModifiers.None, PhysicalKey.F1, null);
            await DrainAsync();

            Assert.Equal(1, afterHelp);
            Assert.Equal(2, afterH);
            Assert.Equal(3, harness.Help.Shown);
            Assert.Equal("text\n", harness.Workspace.ActiveDocument?.Document.Text);
            window.Close();
        });

    [Fact]
    public void TheHelp_ShowsACardPerSection_ScrollsWithJkGG_InOneScrollArea_AndQGivesTheEditorTheKeyboardBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task shown = new HelpDialogService(dialogs).ShowAsync();
            await DrainAsync();

            Assert.Equal("Key bindings", window.HostDialog.Title);
            Assert.Equal("Close", window.HostDialog.CloseButtonText);

            HelpView help = window.HostDialog.GetVisualDescendants().OfType<HelpView>().Single();
            ScrollViewer scroller = help.ScrollArea;
            ScrollViewer? card = help.FindAncestorOfType<ScrollViewer>();
            double bottom = scroller.Extent.Height - scroller.Viewport.Height;

            Assert.True(scroller.IsFocused, "The help takes the keyboard as it opens.");
            Assert.Equal(HelpCatalog.Sections.Count, help.GetVisualDescendants().OfType<SettingsCardExpander>().Count());
            Assert.True(bottom > HelpView.LineStep * 2, "The help is too short to scroll.");
            Assert.True((card is null) || (card.Extent.Height - card.Viewport.Height <= 0.5), "The dialog card scrolls as well as the help.");

            window.KeyPress(Key.J, RawInputModifiers.None, PhysicalKey.J, "j");
            window.KeyPress(Key.J, RawInputModifiers.None, PhysicalKey.J, "j");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(HelpView.LineStep * 2, scroller.Offset.Y);

            window.KeyPress(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(HelpView.LineStep, scroller.Offset.Y);

            window.KeyPress(Key.G, RawInputModifiers.Shift, PhysicalKey.G, "G");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(bottom, scroller.Offset.Y);

            window.KeyPress(Key.U, RawInputModifiers.Control, PhysicalKey.U, null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(bottom - (scroller.Viewport.Height / 2), scroller.Offset.Y, 3);

            window.KeyPress(Key.G, RawInputModifiers.None, PhysicalKey.G, "g");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(0, scroller.Offset.Y);

            window.KeyPress(Key.Q, RawInputModifiers.None, PhysicalKey.Q, "q");
            await DrainAsync();
            bool open = window.HostDialog.IsOpen;

            // A key that failed to close the dialog must fail the test, not hang it on the open dialog.
            if (open)
            {
                await dialogs.HideAsync();
            }

            await shown;
            await DrainAsync();

            Assert.False(open, "q leaves the help open.");
            Assert.True(
                window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl.TextArea.IsKeyboardFocusWithin,
                "Closing the help gives the keyboard back to the editor.");
            Assert.Equal("text\n", harness.Workspace.ActiveDocument?.Document.Text);
            window.Close();
        });

    [Fact]
    public void Esc_ClosesTheHelp_AndTheEditorHasTheKeyboardAgain()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task shown = new HelpDialogService(dialogs).ShowAsync();
            await DrainAsync();
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await DrainAsync();
            bool open = window.HostDialog.IsOpen;

            if (open)
            {
                await dialogs.HideAsync();
            }

            await shown;
            await DrainAsync();

            Assert.False(open, "Esc leaves the help open.");
            Assert.True(window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl.TextArea.IsKeyboardFocusWithin);
            window.Close();
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static async Task DrainAsync()
    {
        for (int i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
        }

        Dispatcher.UIThread.RunJobs();
    }
}

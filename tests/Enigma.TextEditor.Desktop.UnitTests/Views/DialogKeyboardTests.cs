using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The window's dialog and the keyboard: which button it opens on, Tab between its buttons, Esc as Cancel,
/// and the keyboard back in the editor once it has closed.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class DialogKeyboardTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void SaveChanges_OpensOnSave_AndTabWalksTheButtonsRoundAndRound()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, ContentDialogService dialogs) = Show(harness);

            Task<UnsavedChangesChoice> asked = new UnsavedChangesPrompt(dialogs).AskAsync("a.txt");
            await UiWait.UntilAsync(() => Focused(window) == "Save", "the question opened with Save focused");

            Assert.Equal("Save", Focused(window));

            Tab(window);
            Assert.Equal("Don't save", Focused(window));

            Tab(window);
            Assert.Equal("Cancel", Focused(window));

            Tab(window);
            Assert.Equal("Save", Focused(window));

            window.KeyPress(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, null);
            await UiWait.UntilAsync(() => Focused(window) == "Cancel", "Shift+Tab wrapped round to Cancel");
            Assert.Equal("Cancel", Focused(window));

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Escape answered the question");
            Assert.Equal(UnsavedChangesChoice.Cancel, await asked);
            window.Close();
        });

    [Fact]
    public void Escape_IsCancel_AndTheEditorHasTheKeyboardAgain()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, ContentDialogService dialogs) = Show(harness);
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            pane.FocusEditor();
            await UiWait.UntilAsync(() => pane.EditorControl.TextArea.IsFocused, "the editor took the keyboard");

            Task<UnsavedChangesChoice> asked = new UnsavedChangesPrompt(dialogs).AskAsync("a.txt");
            await UiWait.UntilAsync(() => Focused(window) == "Save", "the question opened with Save focused");
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => asked.IsCompleted && !window.HostDialog.IsOpen && pane.EditorControl.TextArea.IsFocused, "Escape closed the question and gave the editor its keyboard back");

            Assert.Equal(UnsavedChangesChoice.Cancel, await asked);
            Assert.False(window.HostDialog.IsOpen);
            Assert.True(pane.EditorControl.TextArea.IsFocused);
            window.Close();
        });

    [Fact]
    public void Enter_OnTheFocusedButton_AnswersWithIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            (MainWindow window, ContentDialogService dialogs) = Show(harness);

            Task<UnsavedChangesChoice> asked = new UnsavedChangesPrompt(dialogs).AskAsync("a.txt");
            await UiWait.UntilAsync(() => Focused(window) == "Save", "the question opened with Save focused");
            Tab(window);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Enter answered the question");

            Assert.Equal(UnsavedChangesChoice.Discard, await asked);
            window.Close();
        });

    [Fact]
    public void AConfirmationWhoseDefaultIsCancel_OpensOnCancel()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (MainWindow window, ContentDialogService dialogs) = Show(harness);

            Task<bool> asked = new ConfirmationPrompt(dialogs).ConfirmAsync("Discard unsaved changes?", "They are lost.", "Discard and reopen");
            await UiWait.UntilAsync(() => Focused(window) == "Cancel", "the confirmation opened with Cancel focused");

            Assert.Equal("Cancel", Focused(window));

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => asked.IsCompleted, "Escape answered the confirmation");
            Assert.False(await asked);
            window.Close();
        });

    [Fact]
    public void ADialogWhoseContentTakesTheKeyboard_KeepsItThere()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (MainWindow window, ContentDialogService dialogs) = Show(harness);

            Task<LanguageInfo?> picked = new LanguagePicker(dialogs, harness.Languages).PickAsync(LanguageInfo.PlainText);
            await UiWait.UntilAsync(() => window.FocusManager?.GetFocusedElement() is TextBox, "the language list opened with its filter focused");

            Assert.IsType<TextBox>(window.FocusManager!.GetFocusedElement());

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => picked.IsCompleted, "Escape closed the language list");
            Assert.Null(await picked);
            window.Close();
        });

    private static void Tab(MainWindow window)
    {
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static string? Focused(MainWindow window)
        => (window.FocusManager?.GetFocusedElement() as Button)?.Content as string;

    private static (MainWindow Window, ContentDialogService Dialogs) Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        ContentDialogService dialogs = new();
        dialogs.RegisterHost(window.HostDialog);

        return (window, dialogs);
    }
}

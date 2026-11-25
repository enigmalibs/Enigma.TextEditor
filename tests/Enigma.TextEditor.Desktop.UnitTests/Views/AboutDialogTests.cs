using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// <em>Help → About</em> in the real window: Enigma.GitClient's dialog, with no title over the view.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class AboutDialogTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheAboutDialog_HasNoTitle_AndOneCloseButton_ItOpensOn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task shown = new AboutDialogService(dialogs).ShowAsync();
            await UiWait.UntilAsync(() => (window.FocusManager?.GetFocusedElement() as Button)?.Content is "Close", "the About dialog opened with Close focused");

            Assert.Null(window.HostDialog.Title);
            Assert.IsType<AboutView>(window.HostDialog.Content);
            Assert.Null(window.HostDialog.PrimaryButtonText);
            Assert.Null(window.HostDialog.SecondaryButtonText);
            Assert.Equal("Close", window.HostDialog.CloseButtonText);
            Assert.Equal(DefaultButton.Close, window.HostDialog.DefaultButton);
            Assert.Equal("Close", (window.FocusManager?.GetFocusedElement() as Button)?.Content);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await UiWait.UntilAsync(() => shown.IsCompleted, "Escape closed the About dialog");
            Assert.True(shown.IsCompleted);
            window.Close();
        });
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The welcome dialog in the real window: what it shows, and that every way out of it applies its
/// choices and writes the settings file.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FirstRunDialogTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheWelcome_ShowsTheFourChoices_SetToTheDefaults_WithGetStartedAndSkip()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (MainWindow window, ContentDialogService dialogs) = Show(harness);

            Task shown = Service(harness, dialogs).ShowAsync();
            await DrainAsync();

            Assert.Equal("Welcome", window.HostDialog.Title);
            Assert.NotNull(window.HostDialog.IconData);
            Assert.Equal("Get started", window.HostDialog.PrimaryButtonText);
            Assert.Equal("Skip", window.HostDialog.CloseButtonText);
            Assert.Null(window.HostDialog.SecondaryButtonText);
            Assert.Equal(DefaultButton.Primary, window.HostDialog.DefaultButton);

            FirstRunView view = Assert.IsType<FirstRunView>(window.HostDialog.Content);
            Assert.Equal(
                ["Theme", "Vim mode", "Minimap", "Wrap long lines"],
                view.GetVisualDescendants().OfType<SettingsCard>().Select(card => card.Header));

            AppSettings defaults = new();
            Assert.Equal(defaults.Theme, view.FindControl<ComboBox>("ThemeChoice")!.SelectedItem);
            Assert.Equal(defaults.VimEnabled, view.FindControl<ToggleSwitch>("VimSwitch")!.IsChecked);
            Assert.Equal(defaults.ShowMinimap, view.FindControl<ToggleSwitch>("MinimapSwitch")!.IsChecked);
            Assert.Equal(defaults.WordWrap, view.FindControl<ToggleSwitch>("WordWrapSwitch")!.IsChecked);

            await dialogs.HideAsync();
            await shown;
            window.Close();
        });

    [Fact]
    public void GetStarted_AppliesWhatTheDialogShows_AndWritesTheSettingsFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (MainWindow window, ContentDialogService dialogs) = Show(harness);
            Task shown = Service(harness, dialogs).ShowAsync();
            await DrainAsync();
            FirstRunView view = (FirstRunView)window.HostDialog.Content!;

            view.FindControl<ComboBox>("ThemeChoice")!.SelectedItem = AppTheme.Dark;
            view.FindControl<ToggleSwitch>("VimSwitch")!.IsChecked = false;
            view.FindControl<ToggleSwitch>("MinimapSwitch")!.IsChecked = false;
            view.FindControl<ToggleSwitch>("WordWrapSwitch")!.IsChecked = false;
            await DrainAsync();

            // The theme repaints the window at once; nothing else is applied before the dialog closes.
            Assert.Equal([AppTheme.Dark], harness.Theme.Applied);
            Assert.True(harness.Vim.IsEnabled);
            Assert.False(File.Exists(harness.Settings.FilePath));

            // The keyboard is on the default button, Get started.
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await DrainAsync();
            bool open = window.HostDialog.IsOpen;
            if (open)
            {
                await dialogs.HideAsync();
            }

            await shown;

            Assert.False(open, "Enter leaves the welcome dialog open.");
            Assert.False(harness.Vim.IsEnabled);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.False(harness.Preferences.WordWrap);
            AppSettings written = SettingsLoader.Load(harness.Settings.FilePath).Config.Value;
            Assert.Equal(AppTheme.Dark, written.Theme);
            Assert.False(written.VimEnabled);
            Assert.False(written.ShowMinimap);
            Assert.False(written.WordWrap);
            window.Close();
        });

    [Fact]
    public void EscOrSkip_StillWritesTheSettingsFile_SoTheWelcomeDoesNotComeBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (MainWindow window, ContentDialogService dialogs) = Show(harness);
            Task shown = Service(harness, dialogs).ShowAsync();
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

            Assert.False(open, "Esc leaves the welcome dialog open.");
            Assert.Same(FirstRunState.Returning, FirstRunState.Detect(harness.Settings.FilePath));
            Assert.Equal(new AppSettings().WordWrap, SettingsLoader.Load(harness.Settings.FilePath).Config.Value.WordWrap);
            Assert.Empty(harness.Messages.Errors);
            window.Close();
        });

    private static FirstRunDialogService Service(EditorHarness harness, IContentDialogService dialogs)
        => new(
            dialogs,
            new FirstRunViewModel(harness.Settings, harness.Theme, harness.SettingsMenu, NullLogger<FirstRunViewModel>.Instance),
            NullLogger<FirstRunDialogService>.Instance);

    private static (MainWindow Window, ContentDialogService Dialogs) Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        ContentDialogService dialogs = new();
        dialogs.RegisterHost(window.HostDialog);

        return (window, dialogs);
    }

    private static async Task DrainAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }
}

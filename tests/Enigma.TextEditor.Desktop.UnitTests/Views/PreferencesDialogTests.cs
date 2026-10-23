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
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// <em>Settings → Preferences…</em> in the real window: what opens it, the cards it shows, and its
/// controls changing the same settings the menu does.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class PreferencesDialogTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Preferences_IsTheSettingsMenusLastItem_OnCtrlComma_AndOpensTheDialog()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);
            MenuItem settings = window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Settings");
            MenuItem preferences = settings.Items.OfType<MenuItem>().Last();

            Assert.Equal("_Preferences…", preferences.Header);
            Assert.Equal(new KeyGesture(Key.OemComma, KeyModifiers.Control), preferences.InputGesture);
            Assert.IsType<Separator>(settings.Items[settings.Items.Count - 2]);

            preferences.Command!.Execute(null);
            await DrainAsync();

            // Typed in the editor: Ctrl+, is no Vim key, and the window's own binding has it.
            window.KeyPress(Key.OemComma, RawInputModifiers.Control, PhysicalKey.Comma, null);
            await DrainAsync();

            Assert.Equal(2, harness.PreferencesDialog.Shown);
            Assert.Equal("text\n", harness.Workspace.ActiveDocument?.Document.Text);
            window.Close();
        });

    [Fact]
    public void TheDialog_ShowsTextAppearanceEditingAndNewFiles_Open_InOneScrollArea_AndEscClosesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);

            Task shown = new PreferencesDialogService(dialogs, harness.PreferencesPage).ShowAsync();
            await DrainAsync();

            Assert.Equal("Preferences", window.HostDialog.Title);
            Assert.Equal("Close", window.HostDialog.CloseButtonText);
            Assert.Null(window.HostDialog.PrimaryButtonText);
            Assert.Equal(DefaultButton.Close, window.HostDialog.DefaultButton);

            PreferencesView view = Assert.IsType<PreferencesView>(window.HostDialog.Content);
            SettingsCardExpander[] cards = [.. view.GetVisualDescendants().OfType<SettingsCardExpander>()];
            ScrollViewer? card = view.FindAncestorOfType<ScrollViewer>();

            Assert.Equal(["Text", "Appearance", "Editing", "New files"], cards.Select(expander => expander.Header));
            Assert.All(cards, expander => Assert.True(expander.IsExpanded));
            Assert.True((card is null) || (card.Extent.Height - card.Viewport.Height <= 0.5), "The dialog card scrolls as well as the preferences.");

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await DrainAsync();
            bool open = window.HostDialog.IsOpen;

            // A key that failed to close the dialog must fail the test, not hang it on the open dialog.
            if (open)
            {
                await dialogs.HideAsync();
            }

            await shown;
            await DrainAsync();

            Assert.False(open, "Esc leaves the preferences open.");
            Assert.True(
                window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl.TextArea.IsKeyboardFocusWithin,
                "Closing the preferences gives the keyboard back to the editor.");
            window.Close();
        });

    [Fact]
    public void TheControls_ShowTheSettings_ChangeThem_AndFollowTheMenu()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = Show(harness);
            ContentDialogService dialogs = new();
            dialogs.RegisterHost(window.HostDialog);
            Task shown = new PreferencesDialogService(dialogs, harness.PreferencesPage).ShowAsync();
            await DrainAsync();
            PreferencesView view = (PreferencesView)window.HostDialog.Content!;

            Assert.Equal(PreferencesViewModel.DefaultFontLabel, view.FindControl<ComboBox>("FontChoice")!.SelectedItem);
            Assert.Equal(AppSettings.DefaultFontSize, view.FindControl<NumericUpDown>("FontSizeEditor")!.Value);
            Assert.Equal(AppTheme.System, view.FindControl<ComboBox>("ThemeChoice")!.SelectedItem);
            Assert.True(view.FindControl<ToggleSwitch>("MinimapSwitch")!.IsChecked);
            Assert.Equal(AppSettings.DefaultPaneResizeStep, view.FindControl<NumericUpDown>("ResizeStepEditor")!.Value);
            Assert.Equal(LineEndingStyle.Lf, view.FindControl<ComboBox>("LineEndingChoice")!.SelectedItem);

            view.FindControl<NumericUpDown>("FontSizeEditor")!.Value = 18;
            view.FindControl<ComboBox>("ThemeChoice")!.SelectedItem = AppTheme.Dark;
            view.FindControl<ToggleSwitch>("MinimapSwitch")!.IsChecked = false;
            view.FindControl<ToggleSwitch>("VimSwitch")!.IsChecked = false;
            view.FindControl<NumericUpDown>("ResizeStepEditor")!.Value = 7;
            view.FindControl<ToggleSwitch>("TabsSwitch")!.IsChecked = true;
            view.FindControl<NumericUpDown>("IndentSizeEditor")!.Value = 2;
            view.FindControl<ComboBox>("LineEndingChoice")!.SelectedItem = LineEndingStyle.CrLf;
            await DrainAsync();

            AppSettings settings = harness.Settings.Value;
            Assert.Equal(AppTheme.Dark, settings.Theme);
            Assert.Equal(18, harness.Preferences.FontSize);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.False(harness.Vim.IsEnabled);
            Assert.Equal(7, harness.Preferences.PaneResizeStep);
            Assert.True(settings.IndentWithTabs);
            Assert.Equal(2, settings.IndentSize);
            Assert.Equal(LineEndingStyle.CrLf, settings.DefaultLineEnding);
            Assert.True(harness.SettingsMenu.IsDarkTheme);

            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(view.FindControl<ToggleSwitch>("WordWrapSwitch")!.IsChecked);

            await dialogs.HideAsync();
            await shown;
            window.Close();
        });

    [Fact]
    public void TheNumberFields_AreLimitedToTheSettingsOwnRanges_AndResetAsksFirst()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 20);
            PreferencesView view = new() { DataContext = harness.PreferencesPage };
            Window window = new() { Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            NumericUpDown step = view.FindControl<NumericUpDown>("ResizeStepEditor")!;
            NumericUpDown size = view.FindControl<NumericUpDown>("IndentSizeEditor")!;
            NumericUpDown fontSize = view.FindControl<NumericUpDown>("FontSizeEditor")!;
            Assert.Equal(AppSettings.MinimumFontSize, fontSize.Minimum);
            Assert.Equal(AppSettings.MaximumFontSize, fontSize.Maximum);
            Assert.Equal(AppSettings.MinimumPaneResizeStep, step.Minimum);
            Assert.Equal(AppSettings.MaximumPaneResizeStep, step.Maximum);
            Assert.Equal(AppSettings.MinimumIndentSize, size.Minimum);
            Assert.Equal(AppSettings.MaximumIndentSize, size.Maximum);

            // The button only opens a flyout; its one item resets.
            Button reset = view.FindControl<Button>("ResetButton")!;
            MenuFlyout flyout = Assert.IsType<MenuFlyout>(reset.Flyout);
            Assert.Null(reset.Command);

            flyout.ShowAt(reset);
            await DrainAsync();
            MenuItem confirm = Assert.Single(flyout.Items.OfType<MenuItem>());
            Assert.Equal(20, harness.Settings.Value.PaneResizeStep);

            confirm.Command!.Execute(null);
            await DrainAsync();

            Assert.Equal(AppSettings.DefaultPaneResizeStep, harness.Settings.Value.PaneResizeStep);
            Assert.Equal(AppSettings.DefaultPaneResizeStep, step.Value);
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

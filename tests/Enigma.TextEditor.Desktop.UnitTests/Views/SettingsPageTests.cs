using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls;
using Enigma.Avalonia.Desktop.Controls.Navigation;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The rail and the settings page in the real window: what the rail looks like, what shows the page and
/// what leaves it, and the page's controls changing the same settings the menu does.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SettingsPageTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheRail_IsNarrow_BetweenTheMenuAndTheStatusBar_WithIconsAndNoText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = await ShowAsync(harness);
            NavigationView rail = window.FindControl<NavigationView>("Rail")!;
            Control menuBar = window.FindControl<Menu>("MainMenu")!;
            Control statusBar = window.FindControl<Border>("StatusBar")!;

            Assert.Equal(44, rail.Bounds.Width);
            double railTop = rail.TranslatePoint(default, window)!.Value.Y;
            double railBottom = railTop + rail.Bounds.Height;
            double menuBottom = menuBar.TranslatePoint(default, window)!.Value.Y + menuBar.Bounds.Height;
            double statusTop = statusBar.TranslatePoint(default, window)!.Value.Y;
            Assert.True(railTop >= menuBottom, "The rail starts under the menu bar.");
            Assert.True(railBottom <= statusTop + 0.5, "The rail stops above the status bar.");

            NavigationItem[] items = [.. rail.GetVisualDescendants().OfType<NavigationItem>().OrderBy(item => item.TranslatePoint(default, rail)!.Value.Y)];
            Assert.Equal([ShellViewModel.FilesHeader, ShellViewModel.SettingsHeader], items.Select(item => item.Header));

            // Files at the top of the rail, Settings at its foot.
            Assert.True(items[0].TranslatePoint(default, rail)!.Value.Y < rail.Bounds.Height / 2);
            Assert.True(items[1].TranslatePoint(default, rail)!.Value.Y > rail.Bounds.Height / 2);

            Assert.All(items, item =>
            {
                Assert.Empty(item.GetVisualDescendants().OfType<TextBlock>());
                PathIcon icon = Assert.Single(item.GetVisualDescendants().OfType<PathIcon>());
                Assert.Equal(20, icon.Bounds.Width);
                Assert.Equal(item.Header, ToolTip.GetTip(item));
                Assert.Equal(item.Header, AutomationProperties.GetName(item));
            });

            window.Close();
        });

    [Fact]
    public void Settings_HidesTheEditor_ShowsThePageWithTheKeyboard_AndFilesBringsTheEditorBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = await ShowAsync(harness);
            NavigationView rail = window.FindControl<NavigationView>("Rail")!;
            Assert.Null(window.SettingsPage);

            rail.SelectedItem = harness.Shell.SettingsItem;
            await DrainAsync();

            Assert.True(harness.Shell.IsSettingsPage);
            Assert.False(window.FindControl<Panel>("FilesPage")!.IsVisible);
            SettingsPageView page = Assert.IsType<SettingsPageView>(window.SettingsPage);
            Assert.True(page.IsEffectivelyVisible);
            Assert.True(page.IsKeyboardFocusWithin, "The settings page takes the keyboard.");
            Assert.False(Editor(window).TextArea.IsKeyboardFocusWithin);
            Assert.Equal("Settings", page.FindControl<TextBlock>("PageTitle")!.Text);

            SettingsCardExpander[] cards = [.. page.GetVisualDescendants().OfType<SettingsCardExpander>()];
            Assert.Equal(["Text", "Appearance", "Editing", "New files"], cards.Select(expander => expander.Header));
            Assert.All(cards, expander => Assert.True(expander.IsExpanded));

            rail.SelectedItem = harness.Shell.FilesItem;
            await DrainAsync();

            Assert.True(harness.Shell.IsFilesPage);
            Assert.False(page.IsEffectivelyVisible);
            Assert.True(window.FindControl<Panel>("FilesPage")!.IsVisible);
            Assert.Equal("text\n", harness.Workspace.ActiveDocument?.Document.Text);
            Assert.True(Editor(window).TextArea.IsKeyboardFocusWithin, "The files page gives the keyboard back to the editor.");
            window.Close();
        });

    [Fact]
    public void ThePage_IsBuiltTheFirstTimeItIsShown_AndKept()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = await ShowAsync(harness);

            harness.Shell.ShowSettings();
            await DrainAsync();
            SettingsPageView? first = window.SettingsPage;
            harness.Shell.ShowFiles();
            await DrainAsync();
            harness.Shell.ShowSettings();
            await DrainAsync();

            Assert.NotNull(first);
            Assert.Same(first, window.SettingsPage);
            window.Close();
        });

    [Fact]
    public void AllSettings_IsTheSettingsMenusLastItem_OnCtrlComma_AndEscGoesBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = await ShowAsync(harness);
            MenuItem settings = window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Settings");
            MenuItem allSettings = settings.Items.OfType<MenuItem>().Last();

            Assert.Equal("_All Settings", allSettings.Header);
            Assert.Equal(new KeyGesture(Key.OemComma, KeyModifiers.Control), allSettings.InputGesture);
            Assert.IsType<Separator>(settings.Items[settings.Items.Count - 2]);

            allSettings.Command!.Execute(null);
            await DrainAsync();
            Assert.True(harness.Shell.IsSettingsPage);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await DrainAsync();
            Assert.True(harness.Shell.IsFilesPage, "Esc on the settings page goes back to the files.");
            Assert.True(Editor(window).TextArea.IsKeyboardFocusWithin);

            // Typed in the editor: Ctrl+, is no Vim key, and the window's own binding has it.
            window.KeyPress(Key.OemComma, RawInputModifiers.Control, PhysicalKey.Comma, null);
            await DrainAsync();

            Assert.True(harness.Shell.IsSettingsPage);
            Assert.Equal("text\n", harness.Workspace.ActiveDocument?.Document.Text);
            window.Close();
        });

    [Fact]
    public void TheControls_ShowTheSettings_ChangeThem_AndFollowTheMenu()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = await ShowAsync(harness);
            harness.Shell.ShowSettings();
            await DrainAsync();
            SettingsPageView view = window.SettingsPage!;

            Assert.Equal(PreferencesViewModel.DefaultFontLabel, view.FindControl<ComboBox>("FontChoice")!.SelectedItem);
            Assert.Equal(AppSettings.DefaultFontSize, view.FindControl<NumericUpDown>("FontSizeEditor")!.Value);
            Assert.Equal(AppTheme.System, view.FindControl<ComboBox>("ThemeChoice")!.SelectedItem);
            Assert.True(view.FindControl<ToggleSwitch>("MinimapSwitch")!.IsChecked);
            Assert.Equal(AppSettings.DefaultPaneResizeStep, view.FindControl<NumericUpDown>("ResizeStepEditor")!.Value);
            Assert.Equal(harness.Settings.Value.DefaultLineEnding, view.FindControl<ComboBox>("LineEndingChoice")!.SelectedItem);

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
            Assert.True(harness.Shell.IsSettingsPage, "Changing a setting stays on the page.");

            bool wrap = settings.WordWrap;
            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(!wrap, view.FindControl<ToggleSwitch>("WordWrapSwitch")!.IsChecked);

            window.Close();
        });

    [Fact]
    public void TheNumberFields_AreLimitedToTheSettingsOwnRanges_AndResetAsksFirst()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 20);
            SettingsPageView view = new() { DataContext = harness.PreferencesPage };
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

    [Fact]
    public void InAShortWindow_ThePageScrolls_AndItsColumnIsCentred()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            MainWindow window = await ShowAsync(harness, width: 1600, height: 500);
            harness.Shell.ShowSettings();
            await DrainAsync();
            SettingsPageView page = window.SettingsPage!;
            ScrollViewer scroller = page.ScrollArea;
            StackPanel cards = page.FindControl<StackPanel>("Cards")!;

            Assert.True(scroller.Extent.Height > scroller.Viewport.Height, "The cards are taller than a short window, so the page scrolls.");
            Assert.Equal(900, cards.Bounds.Width);
            double left = cards.TranslatePoint(default, scroller)!.Value.X;
            double right = scroller.Viewport.Width - left - cards.Bounds.Width;
            Assert.True(Math.Abs(left - right) <= 1, "The column is centred in the page.");
            window.Close();
        });

    [Fact]
    public void CtrlN_OnTheSettingsPage_ShowsTheFilesWithTheNewTab_AndTheKeyboardInIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = await ShowAsync(harness);
            harness.Shell.ShowSettings();
            await DrainAsync();
            Assert.True(window.SettingsPage!.IsKeyboardFocusWithin);

            window.KeyPress(Key.N, RawInputModifiers.Control, PhysicalKey.N, null);
            await DrainAsync();

            Assert.True(harness.Shell.IsFilesPage);
            Assert.True(window.FindControl<Panel>("FilesPage")!.IsVisible);
            Assert.Null(harness.Workspace.ActiveDocument?.FilePath);
            Assert.True(Editor(window).TextArea.IsKeyboardFocusWithin, "The new tab has the keyboard.");
            window.Close();
        });

    [Fact]
    public void TheEditMenu_OnTheSettingsPage_EditsNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            MainWindow window = await ShowAsync(harness);
            harness.Shell.ShowSettings();
            await DrainAsync();

            Assert.Null(window.ActiveEditor);
            MenuItem edit = window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "_Edit");
            MenuItem selectAll = edit.Items.OfType<MenuItem>().Single(item => (string?)item.Header == "Select _All");
            selectAll.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            await DrainAsync();

            Assert.Equal(0, Editor(window).SelectionLength);
            Assert.True(harness.Shell.IsSettingsPage);

            harness.Shell.ShowFiles();
            await DrainAsync();
            Assert.Same(Editor(window), window.ActiveEditor);
            window.Close();
        });

    private static AvaloniaEdit.TextEditor Editor(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl;

    private static async Task<MainWindow> ShowAsync(EditorHarness harness, double width = 1000, double height = 700)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = width, Height = height };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        await DrainAsync();

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

using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The Settings menu, the View menu's zoom, and <em>Help → About</em>.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SettingsMenuTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void EveryToggle_AppliesAtOnce_AndIsWrittenToTheSettingsFile()
        => fixture.RunAsync(async () =>
        {
            // Wrap off and LF, so that every toggle below is a change.
            using EditorHarness harness = new(settings =>
            {
                settings.WordWrap = false;
                settings.DefaultLineEnding = LineEndingStyle.Lf;
            });
            SettingsMenuViewModel menu = harness.SettingsMenu;

            await menu.UseDarkThemeCommand.ExecuteAsync(null);
            await menu.ToggleVimCommand.ExecuteAsync(null);
            await menu.ToggleWordWrapCommand.ExecuteAsync(null);
            await menu.ToggleMinimapCommand.ExecuteAsync(null);
            await menu.DefaultTabsCommand.ExecuteAsync(null);
            await menu.DefaultCrLfCommand.ExecuteAsync(null);

            Assert.Equal([AppTheme.Dark], harness.Theme.Applied);
            Assert.False(harness.Vim.IsEnabled);
            Assert.True(harness.Preferences.WordWrap);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.False(menu.IsMinimap);

            AppSettings written = SettingsLoader.Load(harness.Settings.FilePath).Config.Value;
            Assert.Equal(AppTheme.Dark, written.Theme);
            Assert.False(written.VimEnabled);
            Assert.True(written.WordWrap);
            Assert.False(written.ShowMinimap);
            Assert.True(written.IndentWithTabs);
            Assert.Equal(LineEndingStyle.CrLf, written.DefaultLineEnding);
            Assert.Empty(harness.Messages.Errors);
        });

    [Fact]
    public void ThePaneResizeStep_AppliesAtOnce_AndIsWritten()
        => fixture.RunAsync(async () =>
        {
            // The steps below are counted from a 5% step.
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 5);
            SettingsMenuViewModel menu = harness.SettingsMenu;
            Assert.True(menu.IsResizeStep5);

            await menu.ResizeStep10Command.ExecuteAsync(null);

            Assert.True(menu.IsResizeStep10);
            Assert.False(menu.IsResizeStep5);
            Assert.Equal(10, harness.Preferences.PaneResizeStep);
            Assert.Equal(0.1, harness.Workspace.ResizeStep, 9);
            Assert.Equal(10, SettingsLoader.Load(harness.Settings.FilePath).Config.Value.PaneResizeStep);

            await menu.ResizeStep2Command.ExecuteAsync(null);
            Assert.True(menu.IsResizeStep2);
            Assert.Equal(2, harness.Preferences.PaneResizeStep);
        });

    [Fact]
    public void AStepTheMenuDoesNotOffer_ChecksNoneOfItsThree()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new(settings => settings.PaneResizeStep = 4);

            Assert.False(harness.SettingsMenu.IsResizeStep2);
            Assert.False(harness.SettingsMenu.IsResizeStep5);
            Assert.False(harness.SettingsMenu.IsResizeStep10);
            Assert.Equal(0.04, harness.Workspace.ResizeStep, 9);
        });

    [Fact]
    public void TheRadioMarks_FollowTheChoice()
        => fixture.RunAsync(async () =>
        {
            // LF, so that CRLF moves the mark.
            using EditorHarness harness = new(settings => settings.DefaultLineEnding = LineEndingStyle.Lf);
            SettingsMenuViewModel menu = harness.SettingsMenu;
            Assert.True(menu.IsSystemTheme);
            Assert.True(menu.IsDefaultFourSpaces);
            Assert.True(menu.IsDefaultLf);

            await menu.UseLightThemeCommand.ExecuteAsync(null);
            await menu.DefaultTwoSpacesCommand.ExecuteAsync(null);
            await menu.DefaultCrLfCommand.ExecuteAsync(null);

            Assert.True(menu.IsLightTheme);
            Assert.False(menu.IsSystemTheme);
            Assert.False(menu.IsDarkTheme);
            Assert.True(menu.IsDefaultTwoSpaces);
            Assert.False(menu.IsDefaultFourSpaces);
            Assert.False(menu.IsDefaultTabs);
            Assert.True(menu.IsDefaultCrLf);
            Assert.False(menu.IsDefaultLf);
        });

    [Fact]
    public void Zoom_StepsByOnePoint_StaysWithinTheLimits_AndResetsToTheDefault()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            SettingsMenuViewModel menu = harness.SettingsMenu;

            await menu.ZoomInCommand.ExecuteAsync(null);
            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Preferences.FontSize);

            for (int step = 0; step < 100; step++)
            {
                await menu.ZoomInCommand.ExecuteAsync(null);
            }

            Assert.Equal(AppSettings.MaximumFontSize, harness.Preferences.FontSize);

            for (int step = 0; step < 100; step++)
            {
                await menu.ZoomOutCommand.ExecuteAsync(null);
            }

            Assert.Equal(AppSettings.MinimumFontSize, harness.Preferences.FontSize);
            Assert.Equal(AppSettings.MinimumFontSize, SettingsLoader.Load(harness.Settings.FilePath).Config.Value.EditorFontSize);

            await menu.ResetZoomCommand.ExecuteAsync(null);
            Assert.Equal(AppSettings.DefaultFontSize, harness.Preferences.FontSize);
            Assert.Equal(AppSettings.DefaultFontSize, harness.Settings.Value.EditorFontSize);
        });

    [Fact]
    public void ANewBuffer_TakesTheDefaultIndentationAndLineEndings()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.SettingsMenu.DefaultTabsCommand.ExecuteAsync(null);
            await harness.SettingsMenu.DefaultCrLfCommand.ExecuteAsync(null);
            EditorTabViewModel tab = harness.Workspace.NewDocument();

            Assert.True(tab.Document.Indentation.UseTabs);
            Assert.Equal(LineEndingStyle.CrLf, tab.Document.Format.LineEndings);
        });

    [Fact]
    public void AFailedWrite_IsReportedOnce_AndTheChangeStillApplies()
        => fixture.RunAsync(async () =>
        {
            // Wrap off, so that the toggle turns it on.
            using EditorHarness harness = new(settings => settings.WordWrap = false);

            // A folder where the file should go cannot be replaced by the file.
            Directory.CreateDirectory(harness.Settings.FilePath);

            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            await harness.SettingsMenu.ZoomInCommand.ExecuteAsync(null);

            Assert.Single(harness.Messages.Errors);
            Assert.Equal("Settings could not be saved", harness.Messages.Errors[0].Title);
            Assert.True(harness.Preferences.WordWrap);
            Assert.Equal(AppSettings.DefaultFontSize + 1, harness.Preferences.FontSize);
        });

    [Fact]
    public void TurningVimOff_LetsKeysType_InTheOpenEditor()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            await harness.SettingsMenu.ToggleVimCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            window.KeyTextInput("x");
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("xabc", Editor(window).Document.Text);
            Assert.False(window.FindControl<Border>("ModeChip")?.IsVisible);
            window.Close();
        });

    [Fact]
    public void WrapAndZoom_ReachTheOpenEditor()
        => fixture.RunAsync(async () =>
        {
            // Wrap off, so that the toggle turns it on.
            using EditorHarness harness = new(settings => settings.WordWrap = false);
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "abc"));
            MainWindow window = Show(harness);

            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            await harness.SettingsMenu.ZoomInCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(Editor(window).WordWrap);
            Assert.Equal(AppSettings.DefaultFontSize + 1, Editor(window).FontSize);
            window.Close();
        });

    [Fact]
    public void TheMenuBar_RunsFileEditViewToolsSettingsHelp()
        => fixture.RunAsync(async () =>
        {
            // Wrap off and a 5% step, which the marks below are checked against.
            using EditorHarness harness = new(settings =>
            {
                settings.WordWrap = false;
                settings.PaneResizeStep = 5;
            });
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "x"));
            MainWindow window = Show(harness);
            MenuItem[] menus = [.. window.FindControl<Menu>("MainMenu")!.Items.OfType<MenuItem>()];

            Assert.Equal(["_File", "_Edit", "_View", "_Tools", "_Settings", "_Help"], menus.Select(item => (string?)item.Header));

            MenuItem settings = menus[4];
            Assert.Equal(
                ["_Theme", "_Vim Mode", "_Word Wrap", "_Minimap", "Pane _Resize Step", "Default _Indentation", "Default _Line Endings", "_All Settings"],
                settings.Items.OfType<MenuItem>().Select(item => (string?)item.Header));
            Assert.True(Item(settings, "_Vim Mode").IsChecked);
            Assert.False(Item(settings, "_Word Wrap").IsChecked);
            Assert.True(Item(settings, "_Minimap").IsChecked);
            Assert.Equal(["2%", "5%", "10%"], Item(settings, "Pane _Resize Step").Items.OfType<MenuItem>().Select(item => (string?)item.Header));
            Assert.True(Item(Item(settings, "Pane _Resize Step"), "5%").IsChecked);
            Assert.True(Item(Item(settings, "_Theme"), "_System").IsChecked);
            Assert.Equal(["Spaces: 2", "Spaces: 4", "Tabs"], Item(settings, "Default _Indentation").Items.OfType<MenuItem>().Select(item => (string?)item.Header));
            Assert.Equal(["LF", "CRLF"], Item(settings, "Default _Line Endings").Items.OfType<MenuItem>().Select(item => (string?)item.Header));

            MenuItem view = menus[2];
            Assert.Contains("Zoom _In", view.Items.OfType<MenuItem>().Select(item => (string?)item.Header));
            Assert.Contains("Reset _Zoom", view.Items.OfType<MenuItem>().Select(item => (string?)item.Header));

            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(Item(settings, "_Word Wrap").IsChecked);

            Assert.Equal(
                ["_Key Bindings", "_About Enigma Text Editor"],
                menus[5].Items.OfType<MenuItem>().Select(item => (string?)item.Header));
            Assert.Equal(new KeyGesture(Key.F1), Item(menus[5], "_Key Bindings").InputGesture);
            window.Close();
        });

    [Fact]
    public void HelpAbout_AsksForTheAboutDialog()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Window.AboutCommand.ExecuteAsync(null);

            Assert.Equal(1, harness.About.Shown);
        });

    [Fact]
    public void TheAboutView_ShowsTheProductItsVersionAndTheCopyright()
        => fixture.Run(() =>
        {
            AboutViewModel about = new();
            Window window = new() { Content = new AboutView { DataContext = about } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            AboutView view = (AboutView)window.Content!;

            Assert.Equal(AppInfo.DisplayName, view.FindControl<TextBlock>("ProductName")?.Text);
            Assert.Equal(
                ["Version", AppInfo.Version],
                view.FindControl<StackPanel>("VersionLine")!.Children.OfType<TextBlock>().Select(block => block.Text));
            Assert.Equal(AppInfo.Copyright, view.FindControl<TextBlock>("CopyrightLine")?.Text);
            Assert.Equal(AppInfo.Copyright.Length > 0, view.FindControl<TextBlock>("CopyrightLine")?.IsVisible);
            Assert.Equal(AppInfo.BuildSha is not null, view.FindControl<StackPanel>("BuildLine")?.IsVisible);
            Assert.Equal(AppInfo.BuildSha, view.FindControl<SelectableTextBlock>("BuildSha")?.Text);
            Assert.Equal(AppInfo.BuildSha is not null, about.HasBuild);
            Assert.Equal(380, view.Bounds.Width, 0.5);
            window.Close();
        });

    private static MenuItem Item(MenuItem parent, string header)
        => parent.Items.OfType<MenuItem>().Single(item => (string?)item.Header == header);

    private static AvaloniaEdit.TextEditor Editor(MainWindow window)
        => window.GetVisualDescendants().OfType<EditorPaneView>().Single().EditorControl;

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window, Width = 1000, Height = 700 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<EditorPaneView>().Single().FocusEditor();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

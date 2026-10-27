using System;
using System.Collections.Generic;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The settings page's view-model: every value goes through the Settings menu, which applies and writes
/// it, and the page follows whatever changes the preferences.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class PreferencesTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void EveryPreference_AppliesAtOnce_AndIsWritten()
        => fixture.Run(() =>
        {
            // Wrap off and LF, so that turning them on and to CRLF are changes.
            using EditorHarness harness = new(settings =>
            {
                settings.WordWrap = false;
                settings.DefaultLineEnding = LineEndingStyle.Lf;
            });
            FakeSettingsWriter writer = new();
            SettingsMenuViewModel menu = new(harness.Settings, writer, harness.Theme, harness.Vim, harness.Preferences);
            PreferencesViewModel page = new(harness.Settings, menu);

            page.Theme = AppTheme.Dark;
            page.ShowMinimap = false;
            page.WordWrap = true;
            page.VimEnabled = false;
            page.PaneResizeStep = 7;
            page.IndentWithTabs = true;
            page.IndentSize = 3;
            page.DefaultLineEnding = LineEndingStyle.CrLf;
            page.EditorFont = "Some Mono";
            page.EditorFontSize = 20;

            Assert.Equal(10, writer.Saves);
            Assert.Equal([AppTheme.Dark], harness.Theme.Applied);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.True(harness.Preferences.WordWrap);
            Assert.False(harness.Vim.IsEnabled);
            Assert.Equal(7, harness.Preferences.PaneResizeStep);
            Assert.Equal(20, harness.Preferences.FontSize);
            Assert.Equal("Some Mono", harness.Preferences.FontFamily.FamilyNames.PrimaryFamilyName);

            AppSettings settings = harness.Settings.Value;
            Assert.Equal("Some Mono", settings.EditorFontFamily);
            Assert.True(settings.IndentWithTabs);
            Assert.Equal(3, settings.IndentSize);
            Assert.Equal(LineEndingStyle.CrLf, settings.DefaultLineEnding);
        });

    [Fact]
    public void TheSameValueAgain_IsNotWrittenAgain()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            FakeSettingsWriter writer = new();
            SettingsMenuViewModel menu = new(harness.Settings, writer, harness.Theme, harness.Vim, harness.Preferences);
            PreferencesViewModel page = new(harness.Settings, menu);

            page.Theme = AppTheme.System;
            page.PaneResizeStep = AppSettings.DefaultPaneResizeStep;
            page.ShowMinimap = true;

            Assert.Equal(0, writer.Saves);
            Assert.Empty(harness.Theme.Applied);
        });

    [Fact]
    public void AStepOrSizeTheMenuDoesNotOffer_IsSet_AndTheMenuChecksNoneOfItsOwn()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            PreferencesViewModel page = harness.PreferencesPage;

            page.PaneResizeStep = 7;
            page.IndentSize = 3;

            Assert.Equal(0.07, harness.Workspace.ResizeStep, 9);
            Assert.False(harness.SettingsMenu.IsResizeStep2);
            Assert.False(harness.SettingsMenu.IsResizeStep5);
            Assert.False(harness.SettingsMenu.IsResizeStep10);
            Assert.False(harness.SettingsMenu.IsDefaultTwoSpaces);
            Assert.False(harness.SettingsMenu.IsDefaultFourSpaces);
            Assert.False(harness.SettingsMenu.IsDefaultTabs);
        });

    [Theory]
    [InlineData(0, 400, AppSettings.MinimumPaneResizeStep, AppSettings.MaximumIndentSize)]
    [InlineData(500, -3, AppSettings.MaximumPaneResizeStep, AppSettings.MinimumIndentSize)]
    public void AValueOutsideTheSettingsRange_IsKeptWithinIt(int step, int size, int expectedStep, int expectedSize)
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            PreferencesViewModel page = harness.PreferencesPage;

            page.PaneResizeStep = step;
            page.IndentSize = size;

            Assert.Equal(expectedStep, page.PaneResizeStep);
            Assert.Equal(expectedSize, page.IndentSize);
        });

    [Fact]
    public void AChangeFromTheMenu_IsAnnouncedToTheDialog()
        => fixture.RunAsync(async () =>
        {
            // Wrap off and LF, so that the menu turns them on and to CRLF.
            using EditorHarness harness = new(settings =>
            {
                settings.WordWrap = false;
                settings.DefaultLineEnding = LineEndingStyle.Lf;
            });
            PreferencesViewModel page = harness.PreferencesPage;
            List<string?> announced = [];
            page.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

            await harness.SettingsMenu.ToggleWordWrapCommand.ExecuteAsync(null);
            await harness.SettingsMenu.DefaultCrLfCommand.ExecuteAsync(null);

            Assert.True(page.WordWrap);
            Assert.Equal(LineEndingStyle.CrLf, page.DefaultLineEnding);
            Assert.Contains(nameof(PreferencesViewModel.WordWrap), announced);
            Assert.Contains(nameof(PreferencesViewModel.DefaultLineEnding), announced);
        });

    [Fact]
    public void AControlWritingBackWhileTheValuesAreAnnounced_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            PreferencesViewModel page = harness.PreferencesPage;

            // A control that pushes its own, older value back as it is told the new one.
            page.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PreferencesViewModel.Theme))
                {
                    page.Theme = AppTheme.Dark;
                }
            };

            await harness.SettingsMenu.UseLightThemeCommand.ExecuteAsync(null);

            Assert.Equal(AppTheme.Light, harness.Settings.Value.Theme);
            Assert.Equal([AppTheme.Light], harness.Theme.Applied);
        });

    [Fact]
    public void Reset_PutsEveryPreferenceBack_AppliesThem_AndWritesThem()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(settings =>
            {
                settings.Theme = AppTheme.Dark;
                settings.VimEnabled = false;
                settings.WordWrap = false;
                settings.ShowMinimap = false;
                settings.PaneResizeStep = 20;
                settings.EditorFontSize = 30;
                settings.EditorFontFamily = "Some Mono";
                settings.IndentWithTabs = true;
                settings.IndentSize = 8;
                settings.DefaultLineEnding = LineEndings.ForPlatform(!OperatingSystem.IsWindows());
            });
            harness.Vim.OnSettingsChanged();
            List<string?> announced = [];
            harness.PreferencesPage.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

            await harness.PreferencesPage.ResetCommand.ExecuteAsync(null);

            AppSettings defaults = new();
            AppSettings written = SettingsLoader.Load(harness.Settings.FilePath).Config.Value;
            foreach (AppSettings settings in new[] { harness.Settings.Value, written })
            {
                Assert.Equal(defaults.Theme, settings.Theme);
                Assert.Equal(defaults.VimEnabled, settings.VimEnabled);
                Assert.Equal(defaults.WordWrap, settings.WordWrap);
                Assert.Equal(defaults.ShowMinimap, settings.ShowMinimap);
                Assert.Equal(defaults.PaneResizeStep, settings.PaneResizeStep);
                Assert.Equal(defaults.EditorFontSize, settings.EditorFontSize);
                Assert.Equal(defaults.EditorFontFamily, settings.EditorFontFamily);
                Assert.Equal(defaults.IndentWithTabs, settings.IndentWithTabs);
                Assert.Equal(defaults.IndentSize, settings.IndentSize);
                Assert.Equal(defaults.DefaultLineEnding, settings.DefaultLineEnding);
            }

            Assert.Equal([AppTheme.System], harness.Theme.Applied);
            Assert.True(harness.Vim.IsEnabled);
            Assert.True(harness.Preferences.WordWrap);
            Assert.True(harness.Preferences.ShowMinimap);
            Assert.Equal(AppSettings.DefaultPaneResizeStep, harness.Preferences.PaneResizeStep);
            Assert.Equal(AppSettings.DefaultFontSize, harness.Preferences.FontSize);
            Assert.Same(EditorPreferences.MonospaceFamily, harness.Preferences.FontFamily);
            Assert.True(harness.SettingsMenu.IsSystemTheme);
            Assert.True(harness.SettingsMenu.IsDefaultFourSpaces);
            Assert.Contains(nameof(PreferencesViewModel.Theme), announced);
            Assert.Empty(harness.Messages.Errors);
        });
}

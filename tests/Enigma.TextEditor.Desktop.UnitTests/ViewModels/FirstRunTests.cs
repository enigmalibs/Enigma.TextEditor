using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// A first start: how it is told, what the welcome dialog's choices start from, what they do once it
/// closes, and when the window shows it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FirstRunTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void NoSettingsFile_IsAFirstRun_AndOneThere_IsNot()
    {
        using TempFolder folder = new();
        string path = folder.PathOf(AppPaths.SettingsFileName);

        Assert.Same(FirstRunState.First, FirstRunState.Detect(path));

        File.WriteAllText(path, "not even JSON");

        // Read nothing: a broken file is still someone's file, and the loader recovers it.
        Assert.Same(FirstRunState.Returning, FirstRunState.Detect(path));
    }

    [Fact]
    public void TheChoices_StartFromTheSettings_AndChangeNothingBeforeTheDialogCloses()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new(settings =>
            {
                settings.Theme = AppTheme.Light;
                settings.VimEnabled = false;
                settings.ShowMinimap = false;
                settings.WordWrap = false;
            });
            FakeSettingsWriter writer = new();
            FirstRunViewModel welcome = Welcome(harness, writer);

            Assert.Equal(AppTheme.Light, welcome.Theme);
            Assert.False(welcome.VimEnabled);
            Assert.False(welcome.ShowMinimap);
            Assert.False(welcome.WordWrap);
            Assert.Equal([AppTheme.System, AppTheme.Light, AppTheme.Dark], welcome.Themes);
            Assert.Empty(harness.Theme.Applied);

            welcome.VimEnabled = true;
            welcome.ShowMinimap = true;
            welcome.WordWrap = true;

            Assert.False(harness.Settings.Value.VimEnabled);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.False(harness.Preferences.WordWrap);
            Assert.Equal(0, writer.Saves);
        });

    [Fact]
    public void ChoosingATheme_RepaintsTheWindowAtOnce_WithoutWritingIt()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            FakeSettingsWriter writer = new();
            FirstRunViewModel welcome = Welcome(harness, writer);

            welcome.Theme = AppTheme.Dark;
            welcome.Theme = AppTheme.Dark;

            Assert.Equal([AppTheme.Dark], harness.Theme.Applied);
            Assert.Equal(0, writer.Saves);
        });

    [Fact]
    public void Completing_AppliesTheFourChoices_AndWritesThemOnce()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            FakeSettingsWriter writer = new();
            SettingsMenuViewModel menu = new(harness.Settings, writer, harness.Theme, harness.Vim, harness.Preferences);
            FirstRunViewModel welcome = new(harness.Settings, harness.Theme, menu, NullLogger<FirstRunViewModel>.Instance)
            {
                Theme = AppTheme.Dark,
                VimEnabled = false,
                ShowMinimap = false,
                WordWrap = false,
            };

            await welcome.CompleteAsync();

            AppSettings settings = harness.Settings.Value;
            Assert.Equal(AppTheme.Dark, settings.Theme);
            Assert.False(settings.VimEnabled);
            Assert.False(settings.ShowMinimap);
            Assert.False(settings.WordWrap);
            Assert.False(harness.Vim.IsEnabled);
            Assert.False(harness.Preferences.ShowMinimap);
            Assert.False(harness.Preferences.WordWrap);
            Assert.Equal(AppTheme.Dark, harness.Theme.Applied[^1]);
            Assert.True(menu.IsDarkTheme);
            Assert.False(menu.IsVimEnabled);
            Assert.Equal(1, writer.Saves);
        });

    [Fact]
    public void Completing_WithTheDefaultsUntouched_StillWritesTheSettingsFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            Assert.False(File.Exists(harness.Settings.FilePath));

            await Welcome(harness).CompleteAsync();

            Assert.True(File.Exists(harness.Settings.FilePath), "Without the file, the welcome would come back at the next start.");
            Assert.Same(FirstRunState.Returning, FirstRunState.Detect(harness.Settings.FilePath));
        });

    [Fact]
    public void OnAFirstRun_TheWelcomeComesFirst_ThenTheFilesOnTheCommandLine()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(firstRun: FirstRunState.First);
            string path = harness.Folder.Write("a.txt", "text\n");
            bool openWhileShown = true;
            harness.FirstRunDialog.WhileShown = () => openWhileShown = harness.Workspace.ActiveDocument?.FilePath == path;

            await harness.Window.StartAsync(Startup(harness, path));

            Assert.Equal(1, harness.FirstRunDialog.Shown);
            Assert.False(openWhileShown, "The file was open before the welcome dialog.");
            Assert.Equal(path, harness.Workspace.ActiveDocument?.FilePath);
        });

    [Fact]
    public void OnAnyOtherStart_ThereIsNoWelcome()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "text\n");

            await harness.Window.StartAsync(Startup(harness, path));

            Assert.Equal(0, harness.FirstRunDialog.Shown);
            Assert.Equal(path, harness.Workspace.ActiveDocument?.FilePath);
        });

    private static FirstRunViewModel Welcome(EditorHarness harness, FakeSettingsWriter? writer = null)
    {
        SettingsMenuViewModel menu = writer is null
            ? harness.SettingsMenu
            : new SettingsMenuViewModel(harness.Settings, writer, harness.Theme, harness.Vim, harness.Preferences);

        return new FirstRunViewModel(harness.Settings, harness.Theme, menu, NullLogger<FirstRunViewModel>.Instance);
    }

    private static StartupFiles Startup(EditorHarness harness, string path)
        => StartupFiles.Parse([path], harness.Folder.Root, File.Exists, Directory.Exists);
}

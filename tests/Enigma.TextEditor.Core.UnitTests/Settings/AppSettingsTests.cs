using System.Threading.Tasks;
using Enigma.TextEditor.Core.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void TheDefaults_AreWhatAFreshInstallStartsWith()
    {
        AppSettings settings = new();

        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.True(settings.VimEnabled);
        Assert.False(settings.WordWrap);
        Assert.True(settings.ShowMinimap);
        Assert.Equal(5, settings.PaneResizeStep);
        Assert.Equal(AppSettings.DefaultFontSize, settings.EditorFontSize);
        Assert.Equal(string.Empty, settings.EditorFontFamily);
        Assert.False(settings.IndentWithTabs);
        Assert.Equal(4, settings.IndentSize);
        Assert.Equal(LineEndingStyle.Lf, settings.DefaultLineEnding);
    }

    [Fact]
    public void AMissingFile_LoadsTheDefaultsWithoutAFailure()
    {
        using TempSettingsFolder folder = new();

        ConfigLoadResult<AppSettings> result = SettingsLoader.Load(folder.SettingsFile);

        Assert.Null(result.Failure);
        Assert.True(result.Config.Value.VimEnabled);
    }

    [Fact]
    public async Task EverySetting_SurvivesAWriteAndARead()
    {
        using TempSettingsFolder folder = new();

        ConfigLoadResult<AppSettings> written = SettingsLoader.Load(folder.SettingsFile);
        written.Config.Value.Theme = AppTheme.Light;
        written.Config.Value.VimEnabled = false;
        written.Config.Value.WordWrap = true;
        written.Config.Value.ShowMinimap = false;
        written.Config.Value.PaneResizeStep = 4;
        written.Config.Value.EditorFontSize = 20;
        written.Config.Value.EditorFontFamily = "JetBrains Mono";
        written.Config.Value.IndentWithTabs = true;
        written.Config.Value.IndentSize = 2;
        written.Config.Value.DefaultLineEnding = LineEndingStyle.CrLf;
        await written.Config.WriteAsync(TestContext.Current.CancellationToken);

        AppSettings read = SettingsLoader.Load(folder.SettingsFile).Config.Value;

        Assert.Equal(AppTheme.Light, read.Theme);
        Assert.False(read.VimEnabled);
        Assert.True(read.WordWrap);
        Assert.False(read.ShowMinimap);
        Assert.Equal(4, read.PaneResizeStep);
        Assert.Equal(20, read.EditorFontSize);
        Assert.Equal("JetBrains Mono", read.EditorFontFamily);
        Assert.True(read.IndentWithTabs);
        Assert.Equal(2, read.IndentSize);
        Assert.Equal(LineEndingStyle.CrLf, read.DefaultLineEnding);
    }

    [Fact]
    public void ASettingsFileFromBeforeTheMinimap_ShowsIt()
    {
        using TempSettingsFolder folder = new();
        folder.WriteSettingsFile("{ \"WordWrap\": true }");

        AppSettings read = SettingsLoader.Load(folder.SettingsFile).Config.Value;

        Assert.True(read.ShowMinimap);
    }

    [Fact]
    public void ASettingsFileFromBeforeTheFontSetting_UsesTheBuiltInStack()
    {
        using TempSettingsFolder folder = new();
        folder.WriteSettingsFile("{ \"EditorFontSize\": 18 }");

        AppSettings read = SettingsLoader.Load(folder.SettingsFile).Config.Value;

        Assert.Equal(18, read.EditorFontSize);
        Assert.Equal(string.Empty, read.EditorFontFamily);
    }

    [Fact]
    public void EnumsAreWrittenByName()
    {
        using TempSettingsFolder folder = new();
        folder.WriteSettingsFile("{ \"Theme\": \"Dark\", \"DefaultLineEnding\": \"CrLf\" }");

        AppSettings read = SettingsLoader.Load(folder.SettingsFile).Config.Value;

        Assert.Equal(AppTheme.Dark, read.Theme);
        Assert.Equal(LineEndingStyle.CrLf, read.DefaultLineEnding);
    }
}

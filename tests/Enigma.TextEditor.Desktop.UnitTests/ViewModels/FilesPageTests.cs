using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The files page comes back by itself when the workspace opens, creates or rearranges a document while
/// the settings page is shown — and only then.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FilesPageTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ANewDocument_BringsTheFilesBack()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            harness.Shell.ShowSettings();

            harness.Workspace.NewCommand.Execute(null);

            Assert.True(harness.Shell.IsFilesPage);
        });

    [Fact]
    public void OpeningAFile_BringsTheFilesBack_ShowingIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "text\n");
            harness.Prompts.FilesToOpen.Enqueue([path]);
            harness.Shell.ShowSettings();

            await harness.Workspace.OpenCommand.ExecuteAsync(null);

            Assert.True(harness.Shell.IsFilesPage);
            Assert.Equal(path, harness.Workspace.ActiveDocument?.FilePath);
        });

    [Fact]
    public void OpeningTheFileAlreadyShown_StillBringsTheFilesBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "text\n");
            await harness.Workspace.OpenAsync(path);
            harness.Shell.ShowSettings();

            await harness.Workspace.OpenAsync(path);

            Assert.True(harness.Shell.IsFilesPage);
        });

    [Fact]
    public void ASplit_OrAnotherTab_BringsTheFilesBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b\n"));

            harness.Shell.ShowSettings();
            harness.Workspace.PreviousTabCommand.Execute(null);
            Assert.True(harness.Shell.IsFilesPage);

            harness.Shell.ShowSettings();
            harness.Workspace.Split(SplitAxis.Columns, newDocument: false);
            Assert.True(harness.Shell.IsFilesPage);
        });

    [Fact]
    public void ChangingSettings_OrSaving_KeepsTheSettingsPage()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            harness.Shell.ShowSettings();

            await harness.SettingsMenu.SetVimEnabledAsync(false);
            await harness.SettingsMenu.SetMinimapAsync(false);
            await harness.SettingsMenu.SetWordWrapAsync(true);
            await harness.SettingsMenu.SetThemeAsync(AppTheme.Dark);
            await harness.SettingsMenu.SetFontSizeAsync(20);
            await harness.SettingsMenu.ResetAsync();
            await harness.Workspace.SaveCommand.ExecuteAsync(null);

            Assert.True(harness.Shell.IsSettingsPage);
        });
}

using System.Collections.Generic;
using Enigma.Avalonia.Desktop.Controls.Navigation;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The rail's view-model: its two items, and which page is shown.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ShellTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheRail_HasTheFilesAtTheTop_TheSettingsAtTheBottom_AndStartsOnTheFiles()
        => fixture.Run(() =>
        {
            ShellViewModel shell = new();

            NavigationItem files = Assert.Single(shell.Items);
            NavigationItem settings = Assert.Single(shell.FooterItems);
            Assert.Equal(ShellViewModel.FilesHeader, files.Header);
            Assert.Equal(ShellViewModel.SettingsHeader, settings.Header);
            Assert.NotNull(files.IconData);
            Assert.NotNull(settings.IconData);
            Assert.Same(files, shell.SelectedItem);
            Assert.True(shell.IsFilesPage);
            Assert.False(shell.IsSettingsPage);
        });

    [Fact]
    public void ShowSettings_AndShowFiles_MoveTheSelection_AndAnnounceBothPages()
        => fixture.Run(() =>
        {
            ShellViewModel shell = new();
            List<string?> changed = [];
            shell.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            shell.ShowSettingsCommand.Execute(null);

            Assert.Same(shell.SettingsItem, shell.SelectedItem);
            Assert.True(shell.IsSettingsPage);
            Assert.False(shell.IsFilesPage);
            Assert.Equal([nameof(ShellViewModel.SelectedItem), nameof(ShellViewModel.IsFilesPage), nameof(ShellViewModel.IsSettingsPage)], changed);

            shell.ShowFilesCommand.Execute(null);

            Assert.Same(shell.FilesItem, shell.SelectedItem);
            Assert.True(shell.IsFilesPage);
        });

    [Fact]
    public void ShowingThePageAlreadyShown_AnnouncesNothing()
        => fixture.Run(() =>
        {
            ShellViewModel shell = new();
            int changes = 0;
            shell.PropertyChanged += (_, _) => changes++;

            shell.ShowFiles();

            Assert.Equal(0, changes);
        });

    [Fact]
    public void ANullSelection_OrAnItemNotOnTheRail_IsIgnored()
        => fixture.Run(() =>
        {
            ShellViewModel shell = new();
            shell.ShowSettings();

            shell.SelectedItem = null;
            shell.SelectedItem = new NavigationItem { Header = "Elsewhere" };

            Assert.Same(shell.SettingsItem, shell.SelectedItem);
            Assert.True(shell.IsSettingsPage);
        });
}

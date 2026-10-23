using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The editor's font family and size: what the preferences offer, and every pane and the hex rows
/// following a choice at once.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EditorFontTests(HeadlessAvaloniaFixture fixture)
{
    // A family no machine has: the editor falls back through the built-in stack behind it.
    private const string MissingFamily = "No Such Mono 7F3A";

    [Fact]
    public void AFreshInstall_DrawsInTheBuiltInMonospaceStack()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();

            Assert.Same(EditorPreferences.MonospaceFamily, harness.Preferences.FontFamily);
            Assert.Equal(PreferencesViewModel.DefaultFontLabel, harness.PreferencesPage.EditorFont);
        });

    [Fact]
    public void AChosenFamily_RedrawsEveryPane_KeepsTheStackBehindIt_AndIsWritten()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "text\n"));
            harness.Workspace.Split(SplitAxis.Columns);
            MainWindow window = Show(harness);
            EditorPaneView[] panes = [.. window.GetVisualDescendants().OfType<EditorPaneView>()];
            Assert.Equal(2, panes.Length);

            await harness.SettingsMenu.SetFontFamilyAsync(MissingFamily);
            Dispatcher.UIThread.RunJobs();

            FontFamily family = harness.Preferences.FontFamily;
            Assert.All(panes, pane => Assert.Same(family, pane.EditorControl.FontFamily));
            Assert.Equal(MissingFamily, family.FamilyNames.PrimaryFamilyName);
            Assert.True(family.FamilyNames.HasFallbacks);
            Assert.Contains("monospace", family.FamilyNames);
            Assert.Equal(MissingFamily, SettingsLoader.Load(harness.Settings.FilePath).Config.Value.EditorFontFamily);
            window.Close();
        });

    [Fact]
    public void TheHexRows_TakeTheEditorsFamilyAndSize()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenHexAsync(harness.Folder.Write("a.bin", new byte[64]));
            MainWindow window = Show(harness);

            await harness.SettingsMenu.SetFontFamilyAsync(MissingFamily);
            await harness.SettingsMenu.SetFontSizeAsync(22);
            Dispatcher.UIThread.RunJobs();

            TextBlock row = window.GetVisualDescendants().OfType<HexView>().Single()
                .GetVisualDescendants().OfType<TextBlock>().First(block => block.Classes.Contains("row"));
            Assert.Same(harness.Preferences.FontFamily, row.FontFamily);
            Assert.Equal(22, row.FontSize);
            window.Close();
        });

    [Fact]
    public void Default_ClearsTheSetting_AndBringsTheBuiltInStackBack()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new(settings => settings.EditorFontFamily = MissingFamily);
            PreferencesViewModel page = harness.PreferencesPage;
            Assert.Equal(MissingFamily, page.EditorFont);

            page.EditorFont = PreferencesViewModel.DefaultFontLabel;

            Assert.Equal(string.Empty, harness.Settings.Value.EditorFontFamily);
            Assert.Same(EditorPreferences.MonospaceFamily, harness.Preferences.FontFamily);
            Assert.Equal(PreferencesViewModel.DefaultFontLabel, page.EditorFont);
        });

    [Fact]
    public void TheFontList_StartsWithDefault_ListsMonospaceFacesOnly_AndKeepsASavedOneTheMachineLacks()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new(settings => settings.EditorFontFamily = MissingFamily);

            IReadOnlyList<string> fonts = harness.PreferencesPage.Fonts;

            Assert.Equal(PreferencesViewModel.DefaultFontLabel, fonts[0]);
            Assert.Equal(MissingFamily, fonts[^1]);
            Assert.Equal(fonts.Count, fonts.Distinct().Count());
            Assert.All(fonts.Skip(1).SkipLast(1), name => Assert.True(MonospaceFonts.IsMonospace(new FontFamily(name)), $"{name} is not monospace."));
        });

    [Fact]
    public void AProportionalFace_IsNotMonospace()
        => fixture.Run(() => Assert.False(MonospaceFonts.IsMonospace(FontFamily.Default)));

    [Fact]
    public void TheFontSizeField_AndTheZoomKeys_MoveTheSameSetting()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            PreferencesViewModel page = harness.PreferencesPage;
            List<string?> announced = [];
            page.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

            page.EditorFontSize = 20;
            Assert.Equal(20, harness.Preferences.FontSize);

            await harness.SettingsMenu.ZoomInCommand.ExecuteAsync(null);

            Assert.Equal(21, page.EditorFontSize);
            Assert.Equal(21, harness.Settings.Value.EditorFontSize);
            Assert.Contains(nameof(PreferencesViewModel.EditorFontSize), announced);

            page.EditorFontSize = 500;
            Assert.Equal(AppSettings.MaximumFontSize, page.EditorFontSize);
        });

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}

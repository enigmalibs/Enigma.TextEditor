using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// A buffer's language: detected from its file, chosen in the picker, shown in the status bar and
/// applied to the editor's grammar.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class LanguageTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheFilter_MatchesNamesIdsAndExtensions()
    {
        LanguageCatalog catalog = new();
        LanguagePickerViewModel picker = new(catalog.All, LanguageInfo.PlainText);

        picker.Filter = "pyth";
        Assert.Contains(picker.Items, language => language.DisplayName == "Python");
        Assert.DoesNotContain(picker.Items, language => language.DisplayName == "C#");

        picker.Filter = ".cs";
        Assert.Contains(picker.Items, language => language.DisplayName == "C#");

        picker.Filter = "zzzz-nothing";
        Assert.Empty(picker.Items);
        Assert.Null(picker.Selected);

        picker.Filter = string.Empty;
        Assert.Equal(catalog.All.Count, picker.Items.Count);
    }

    [Fact]
    public void ThePicker_StartsOnTheCurrentLanguage_AndCommitsTheHighlightedOne()
    {
        LanguageCatalog catalog = new();
        LanguageInfo csharp = catalog.Find("csharp")!;
        LanguagePickerViewModel picker = new(catalog.All, csharp);

        Assert.Same(csharp, picker.Selected);
        Assert.Null(picker.Committed);

        picker.Selected = LanguageInfo.PlainText;
        picker.Commit();

        Assert.Same(LanguageInfo.PlainText, picker.Committed);
    }

    [Fact]
    public void OpeningAFile_DetectsItsLanguage_AndSaveAsFollowsTheNewName()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("code.cs", "class A {}")))!;
            Assert.Equal("C#", tab.Document.Language.DisplayName);

            await harness.Workspace.SaveAsAsync(tab.Document, harness.Folder.PathOf("data.json"));

            Assert.Equal("JSON", tab.Document.Language.DisplayName);
        });

    [Fact]
    public void AChosenLanguage_SurvivesARename()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("code.cs", "x")))!;
            harness.LanguagePicker.Answers.Enqueue(LanguageInfo.PlainText);

            await harness.Tools.SetLanguageCommand.ExecuteAsync(null);
            await harness.Workspace.SaveAsAsync(tab.Document, harness.Folder.PathOf("other.py"));

            Assert.Same(LanguageInfo.PlainText, tab.Document.Language);
            Assert.True(tab.Document.IsLanguageChosen);
        });

    [Fact]
    public void CancellingThePicker_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("code.cs", "x")))!;

            await harness.Tools.SetLanguageCommand.ExecuteAsync(null);

            Assert.Equal("C#", Assert.Single(harness.LanguagePicker.OpenedWith).DisplayName);
            Assert.Equal("C#", tab.Document.Language.DisplayName);
            Assert.False(tab.Document.IsLanguageChosen);
        });

    [Fact]
    public void TheEditor_IsColouredByTheBuffersLanguage_AndTheStatusBarSaysWhich()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("code.cs", "class A {}")))!;
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            EditorPaneView pane = window.GetVisualDescendants().OfType<EditorPaneView>().Single();
            Button status = window.FindControl<Button>("LanguageStatus")!;

            Assert.Equal("source.cs", pane.Appearance?.ScopeName);
            Assert.Equal("C#", status.Content);

            tab.Document.ChooseLanguage(LanguageInfo.PlainText);
            Dispatcher.UIThread.RunJobs();

            Assert.Null(pane.Appearance?.ScopeName);
            Assert.Equal("Plain Text", status.Content);

            await harness.Workspace.OpenAsync(harness.Folder.Write("data.json", "{}"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("source.json", pane.Appearance?.ScopeName);
            Assert.Equal("JSON", status.Content);
            window.Close();
        });
}

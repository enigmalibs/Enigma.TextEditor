using System.IO;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

[Collection(HeadlessCollection.Name)]
public sealed class WorkspaceViewModelTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void OpeningAFileAlreadyOpen_ActivatesItsTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            string b = harness.Folder.Write("b.txt", "b");

            EditorTabViewModel? first = await harness.Workspace.OpenAsync(a);
            await harness.Workspace.OpenAsync(b);
            EditorTabViewModel? again = await harness.Workspace.OpenAsync(a);

            Assert.Same(first, again);
            Assert.Equal(2, harness.Workspace.ActivePane.Tabs.Count);
            Assert.Same(first, harness.Workspace.ActiveTab);
        });

    [Fact]
    public void OpenFromThePicker_OpensEveryChosenFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            harness.Prompts.FilesToOpen.Enqueue([harness.Folder.Write("a.txt", "a"), harness.Folder.Write("b.txt", "b")]);

            await harness.Workspace.OpenCommand.ExecuteAsync(null);

            Assert.Equal(2, harness.Workspace.ActivePane.Tabs.Count);
            Assert.Equal("b.txt", harness.Workspace.ActiveDocument?.Title);
        });

    [Fact]
    public void New_ShowsAnUntitledBuffer()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();

            harness.Workspace.NewCommand.Execute(null);
            harness.Workspace.NewCommand.Execute(null);

            Assert.Equal("Untitled-2", harness.Workspace.ActiveDocument?.Title);
            Assert.Equal(2, harness.Registry.Documents.Count);
        });

    [Fact]
    public void ClosingTheLastTabOfABuffer_DropsTheBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await harness.Workspace.CloseTabCommand.ExecuteAsync(null);

            Assert.Empty(harness.Workspace.ActivePane.Tabs);
            Assert.DoesNotContain(tab.Document, harness.Registry.Documents);
            Assert.False(harness.Workspace.CloseTabCommand.CanExecute(null));
        });

    [Fact]
    public void SaveAll_WritesEveryDirtyBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            string b = harness.Folder.Write("b.txt", "b");
            (await harness.Workspace.OpenAsync(a))!.Document.Document.Insert(1, "1");
            (await harness.Workspace.OpenAsync(b))!.Document.Document.Insert(1, "2");

            await harness.Workspace.SaveAllCommand.ExecuteAsync(null);

            Assert.Equal("a1", File.ReadAllText(a));
            Assert.Equal("b2", File.ReadAllText(b));
        });

    [Fact]
    public void TheWindowTitle_FollowsTheActiveBufferAndItsUnsavedChanges()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            Assert.Equal("Enigma Text Editor", harness.Window.Title);

            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("notes.txt", "x")))!;
            Assert.Equal("notes.txt — Enigma Text Editor", harness.Window.Title);

            tab.Document.Document.Insert(0, "y");
            Assert.Equal("notes.txt • — Enigma Text Editor", harness.Window.Title);
        });

    [Fact]
    public void TheStatusBar_ShowsTheActiveTabsCaret()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            Assert.False(harness.StatusBar.HasActiveTab);

            EditorTabViewModel tab = harness.Workspace.NewDocument();
            tab.CaretLine = 12;
            tab.CaretColumn = 5;

            Assert.True(harness.StatusBar.HasActiveTab);
            Assert.Equal("Ln 12, Col 5", harness.StatusBar.CaretText);
        });
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// <em>Delete</em> in the explorer, over real folders, with a fake trash and scripted confirmations: what leaves the
/// disk, what is asked first, and the tabs of what was deleted.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileExplorerDeleteTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Delete_NeedsAFileOrAFolderSelected()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await OpenProjectAsync(harness);

            Assert.False(harness.Explorer.DeleteCommand.CanExecute(null));

            harness.Explorer.SelectedNode = Row(harness, "readme.txt");
            Assert.True(harness.Explorer.DeleteCommand.CanExecute(null));
        });

    [Fact]
    public void AConfirmedDelete_MovesTheFileToTheTrash_AndItsRowGoes()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.Equal(["trash readme.txt"], harness.ExplorerPrompts.Confirmations);
            Assert.Equal([Path.Combine(project, "readme.txt")], harness.Trash.Trashed);
            Assert.Equal(["docs"], harness.Explorer.Roots.Select(row => row.Name));
            Assert.Null(harness.Explorer.SelectedNode);
        });

    [Fact]
    public void ADeclinedConfirmation_DeletesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.ExplorerPrompts.ConfirmsTrash = false;
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.True(File.Exists(Path.Combine(project, "readme.txt")));
            Assert.Empty(harness.Trash.Trashed);
            Assert.Equal(["trash readme.txt"], harness.ExplorerPrompts.Confirmations);
        });

    [Fact]
    public void WithNoTrash_ItAsksOnceForAPermanentDelete_AndDeletesForGood()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.Trash.IsAvailable = false;
            harness.Explorer.SelectedNode = Row(harness, "docs");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.Equal(["delete docs"], harness.ExplorerPrompts.Confirmations);
            Assert.False(Directory.Exists(Path.Combine(project, "docs")));
            Assert.Equal(["readme.txt"], harness.Explorer.Roots.Select(row => row.Name));
        });

    [Fact]
    public void ATrashThatDeclines_AsksAgain_AndADeclinedSecondQuestionKeepsTheFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.Trash.Declines = true;
            harness.ExplorerPrompts.ConfirmsPermanentDelete = false;
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.Equal(["trash readme.txt", "delete readme.txt"], harness.ExplorerPrompts.Confirmations);
            Assert.True(File.Exists(Path.Combine(project, "readme.txt")));
        });

    [Fact]
    public void ACleanTabOfTheDeletedFile_Closes_AndADirtyOneStays()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            EditorTabViewModel readme = (await harness.Workspace.OpenAsync(Path.Combine(project, "readme.txt")))!;
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.DoesNotContain(readme, harness.Workspace.ActivePane.Tabs);
            Assert.DoesNotContain(readme.Document, harness.Registry.Documents);

            harness.Folder.Write(Path.Combine("project", "notes.txt"), "notes\n");
            await harness.Explorer.RefreshAsync();
            EditorTabViewModel notes = (await harness.Workspace.OpenAsync(Path.Combine(project, "notes.txt")))!;
            notes.Document.Document.Insert(0, "unsaved ");
            harness.Explorer.SelectedNode = Row(harness, "notes.txt");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.Contains(notes, harness.Workspace.ActivePane.Tabs);
            Assert.True(notes.Document.IsDirty);
            Assert.False(File.Exists(Path.Combine(project, "notes.txt")));
        });

    [Fact]
    public void DeletingAFolder_ClosesTheCleanTabsUnderIt_AndAPaneLeftWithoutTabs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            await harness.Workspace.OpenAsync(Path.Combine(project, "readme.txt"));
            harness.Workspace.Split(SplitAxis.Columns, newDocument: false);
            EditorTabViewModel intro = (await harness.Workspace.OpenAsync(Path.Combine(project, "docs", "intro.txt")))!;
            Assert.Equal(2, harness.Workspace.Panes.Count);
            harness.Explorer.SelectedNode = Row(harness, "docs");

            await harness.Explorer.DeleteCommand.ExecuteAsync(null);

            Assert.DoesNotContain(intro.Document, harness.Registry.Documents);
            EditorPaneViewModel left = Assert.Single(harness.Workspace.Panes);
            Assert.Equal(["readme.txt"], left.Tabs.Select(tab => tab.Document.Title));
        });

    private static FileTreeNodeViewModel Row(EditorHarness harness, string name)
        => harness.Explorer.Roots.Single(row => row.Name == name);

    // project/{docs/intro.txt, readme.txt}, open in the explorer.
    private static async Task<string> OpenProjectAsync(EditorHarness harness)
    {
        harness.Folder.Write(Path.Combine("project", "docs", "intro.txt"), "intro\n");
        harness.Folder.Write(Path.Combine("project", "readme.txt"), "read me\n");
        string project = harness.Folder.PathOf("project");
        await harness.Explorer.OpenFolderAsync(project);

        return project;
    }
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// <em>Rename…</em> in the explorer, over real folders, the names answered by a fake: the tree, and the tabs of the
/// files renamed.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileExplorerRenameTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Rename_NeedsAFileOrAFolderSelected()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await OpenProjectAsync(harness);

            Assert.False(harness.Explorer.RenameCommand.CanExecute(null));

            harness.Explorer.SelectedNode = harness.Explorer.Roots[0];
            Assert.True(harness.Explorer.RenameCommand.CanExecute(null));

            FileTreeNodeViewModel docs = harness.Explorer.Roots[0];
            harness.Explorer.SelectedNode = docs.Children.Single(row => row.IsInformational);
            Assert.False(harness.Explorer.RenameCommand.CanExecute(null));
        });

    [Fact]
    public void RenamingAFile_ShowsTheNewName_Selected()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");
            harness.ExplorerPrompts.NewNames.Enqueue("about.txt");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            Assert.Equal([(Path.Combine(project, "readme.txt"), false)], harness.ExplorerPrompts.RenamesAsked);
            Assert.False(File.Exists(Path.Combine(project, "readme.txt")));
            Assert.Equal("read me\n", File.ReadAllText(Path.Combine(project, "about.txt")));
            Assert.Equal(["docs", "about.txt"], harness.Explorer.Roots.Select(row => row.Name));
            Assert.Equal("about.txt", harness.Explorer.SelectedNode?.Name);
        });

    [Fact]
    public void AnOpenTabOfTheFile_FollowsIt_KeepingItsChanges_AndSavesUnderTheNewName()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(Path.Combine(project, "readme.txt")))!;
            tab.Document.Document.Insert(0, "edited ");
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");
            harness.ExplorerPrompts.NewNames.Enqueue("about.txt");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            Assert.Equal(Path.Combine(project, "about.txt"), tab.Document.FilePath);
            Assert.Equal("about.txt", tab.Document.Title);
            Assert.True(tab.Document.IsDirty);

            Assert.True(await harness.Workspace.SaveAsync(tab.Document));
            Assert.Equal("edited read me\n", File.ReadAllText(Path.Combine(project, "about.txt")));
            Assert.False(File.Exists(Path.Combine(project, "readme.txt")));
        });

    [Fact]
    public void RenamingAnOpenFolder_MovesTheTabsUnderIt_AndKeepsItOpen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            EditorTabViewModel intro = (await harness.Workspace.OpenAsync(Path.Combine(project, "docs", "intro.txt")))!;
            EditorTabViewModel readme = (await harness.Workspace.OpenAsync(Path.Combine(project, "readme.txt")))!;
            FileTreeNodeViewModel docs = Row(harness, "docs");
            docs.IsExpanded = true;
            await docs.Loading!;
            harness.Explorer.SelectedNode = docs;
            harness.ExplorerPrompts.NewNames.Enqueue("guides");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            Assert.Equal(Path.Combine(project, "guides", "intro.txt"), intro.Document.FilePath);
            Assert.Equal(Path.Combine(project, "readme.txt"), readme.Document.FilePath);
            Assert.Equal([(docs.FullPath, true)], harness.ExplorerPrompts.RenamesAsked);
            FileTreeNodeViewModel guides = Row(harness, "guides");
            Assert.Same(guides, harness.Explorer.SelectedNode);
            Assert.True(guides.IsExpanded);
            Assert.Equal(["intro.txt"], guides.Children.Select(row => row.Name));
        });

    [Fact]
    public void ACaseOnlyRename_IsCarriedOut_AndTheTabFollows()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(Path.Combine(project, "readme.txt")))!;
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");
            harness.ExplorerPrompts.NewNames.Enqueue("README.txt");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            Assert.Equal("README.txt", Path.GetFileName(Directory.GetFiles(project).Single()));
            Assert.Equal("README.txt", tab.Document.Title);
            Assert.Equal("README.txt", harness.Explorer.SelectedNode?.Name);
        });

    [Fact]
    public void KeepingTheName_ChangesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            FileTreeNodeViewModel readme = Row(harness, "readme.txt");
            harness.Explorer.SelectedNode = readme;
            harness.ExplorerPrompts.NewNames.Enqueue("readme.txt");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            Assert.True(File.Exists(Path.Combine(project, "readme.txt")));
            Assert.Same(readme, Row(harness, "readme.txt"));
            Assert.Empty(harness.Messages.Errors);
        });

    [Fact]
    public void ANameTakenSinceItWasChecked_IsReported_AndBothFilesAreLeftAlone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.Folder.Write(Path.Combine("project", "about.txt"), "about\n");
            harness.Explorer.SelectedNode = Row(harness, "readme.txt");
            harness.ExplorerPrompts.NewNames.Enqueue("about.txt");

            await harness.Explorer.RenameCommand.ExecuteAsync(null);

            (string title, string _) = Assert.Single(harness.Messages.Errors);
            Assert.Equal("The file could not be renamed", title);
            Assert.Equal("about\n", File.ReadAllText(Path.Combine(project, "about.txt")));
            Assert.Equal("read me\n", File.ReadAllText(Path.Combine(project, "readme.txt")));
            Assert.Equal(["docs", "about.txt", "readme.txt"], harness.Explorer.Roots.Select(row => row.Name));
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

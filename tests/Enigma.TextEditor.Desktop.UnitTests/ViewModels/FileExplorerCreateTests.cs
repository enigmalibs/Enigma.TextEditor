using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// <em>New File…</em> and <em>New Folder…</em> in the explorer, over real folders, the names answered by a fake.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileExplorerCreateTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void WithNoFolderOpen_NothingCanBeCreated()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();

            Assert.False(harness.Explorer.NewFileCommand.CanExecute(null));
            Assert.False(harness.Explorer.NewFolderCommand.CanExecute(null));
            Assert.Null(harness.Explorer.TargetFolder);
        });

    [Fact]
    public void ANewFile_WithNothingSelected_GoesInTheFolder_IsSelected_AndOpens()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.ExplorerPrompts.NewFileNames.Enqueue("todo.txt");
            int opened = 0;
            harness.Explorer.FileOpened += (_, _) => opened++;

            await harness.Explorer.NewFileCommand.ExecuteAsync(null);

            string created = Path.Combine(project, "todo.txt");
            Assert.Equal([project], harness.ExplorerPrompts.AskedIn);
            Assert.True(File.Exists(created));
            Assert.Equal(created, harness.Explorer.SelectedNode?.FullPath);
            Assert.Contains(harness.Explorer.SelectedNode, harness.Explorer.Roots);
            Assert.Equal(created, harness.Workspace.ActiveDocument?.FilePath);
            Assert.Equal(1, opened);
        });

    [Fact]
    public void ANewFile_WithAFolderSelected_GoesInThatFolder_WhichExpands()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            FileTreeNodeViewModel docs = harness.Explorer.Roots.Single(row => row.Name == "docs");
            harness.Explorer.SelectedNode = docs;
            harness.ExplorerPrompts.NewFileNames.Enqueue("guide.txt");

            await harness.Explorer.NewFileCommand.ExecuteAsync(null);

            string created = Path.Combine(project, "docs", "guide.txt");
            Assert.True(File.Exists(created));
            Assert.True(docs.IsExpanded);
            Assert.True(docs.IsLoaded);
            Assert.Equal(["guide.txt", "intro.txt"], docs.Children.Select(row => row.Name));
            Assert.Equal(created, harness.Explorer.SelectedNode?.FullPath);
        });

    [Fact]
    public void ANewFile_WithAFileSelected_GoesBesideIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            FileTreeNodeViewModel docs = harness.Explorer.Roots.Single(row => row.Name == "docs");
            docs.IsExpanded = true;
            await docs.Loading!;
            harness.Explorer.SelectedNode = docs.Children.Single();
            harness.ExplorerPrompts.NewFileNames.Enqueue("outro.txt");

            await harness.Explorer.NewFileCommand.ExecuteAsync(null);

            Assert.Equal([Path.Combine(project, "docs")], harness.ExplorerPrompts.AskedIn);
            Assert.Equal(["intro.txt", "outro.txt"], docs.Children.Select(row => row.Name));
        });

    [Fact]
    public void ANewFolder_IsCreated_AndSelected_WithoutOpeningAnything()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.ExplorerPrompts.NewFolderNames.Enqueue("assets");

            await harness.Explorer.NewFolderCommand.ExecuteAsync(null);

            string created = Path.Combine(project, "assets");
            Assert.True(Directory.Exists(created));
            Assert.Equal(["assets", "docs", "readme.txt"], harness.Explorer.Roots.Select(row => row.Name));
            Assert.Equal(created, harness.Explorer.SelectedNode?.FullPath);
            Assert.True(harness.Explorer.SelectedNode?.IsFolder);
            Assert.Null(harness.Workspace.ActiveDocument);
        });

    [Fact]
    public void ACancelledName_CreatesNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);

            await harness.Explorer.NewFileCommand.ExecuteAsync(null);
            await harness.Explorer.NewFolderCommand.ExecuteAsync(null);

            Assert.Equal(2, Directory.GetFileSystemEntries(project).Length);
            Assert.Empty(harness.Messages.Errors);
        });

    [Fact]
    public void ANameTakenSinceItWasChecked_IsReported_AndWhatIsThereIsLeftAlone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = await OpenProjectAsync(harness);
            harness.ExplorerPrompts.NewFileNames.Enqueue("readme.txt");

            await harness.Explorer.NewFileCommand.ExecuteAsync(null);

            (string title, string message) = Assert.Single(harness.Messages.Errors);
            Assert.Equal("The file could not be created", title);
            Assert.Contains("readme.txt", message);
            Assert.Equal("read me\n", File.ReadAllText(Path.Combine(project, "readme.txt")));
            Assert.Null(harness.Workspace.ActiveDocument);
        });

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

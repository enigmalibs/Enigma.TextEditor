using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.Icons.Phosphor;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The explorer's model over real folders: opening, closing and listing a folder, refreshing it, and opening a file from
/// it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileExplorerTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheExplorer_StartsHidden_WithNoFolder()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();

            Assert.False(harness.Explorer.IsVisible);
            Assert.False(harness.Explorer.HasFolder);
            Assert.Equal(FileExplorerViewModel.DefaultHeader, harness.Explorer.Header);
            Assert.Empty(harness.Explorer.Roots);
            Assert.False(harness.Explorer.RefreshCommand.CanExecute(null));
            Assert.False(harness.Explorer.CloseFolderCommand.CanExecute(null));
        });

    [Fact]
    public void Toggle_ShowsThenHidesIt()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();

            harness.Explorer.ToggleCommand.Execute(null);
            Assert.True(harness.Explorer.IsVisible);

            harness.Explorer.ToggleCommand.Execute(null);
            Assert.False(harness.Explorer.IsVisible);
        });

    [Fact]
    public void OpeningAFolder_ListsItsTopLevel_FoldersFirst_AndShowsTheExplorer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string root = Tree(harness, "project");
            harness.Folder.Write(Path.Combine("project", "src", "deep.txt"), "x");

            await harness.Explorer.OpenFolderAsync(root);

            Assert.True(harness.Explorer.IsVisible);
            Assert.Equal(root, harness.Explorer.FolderPath);
            Assert.Equal("project", harness.Explorer.Header);
            Assert.Equal(root, harness.Explorer.HeaderToolTip);
            Assert.Equal(["docs", "src", ".editorconfig", "file2.txt", "file10.txt"], Names(harness.Explorer.Roots));
            Assert.True(harness.Explorer.RefreshCommand.CanExecute(null));
            Assert.True(harness.Explorer.CloseFolderCommand.CanExecute(null));

            FileTreeNodeViewModel src = harness.Explorer.Roots[1];
            Assert.True(src.IsFolder);
            Assert.False(src.IsLoaded);
            Assert.Equal(FileTreeNodeKind.Reading, Assert.Single(src.Children).Kind);
        });

    [Fact]
    public void ExpandingAFolder_ListsIt_Once()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string root = Tree(harness, "project");
            harness.Folder.Write(Path.Combine("project", "src", "b.txt"), "b");
            harness.Folder.Write(Path.Combine("project", "src", "a.txt"), "a");
            await harness.Explorer.OpenFolderAsync(root);
            FileTreeNodeViewModel src = harness.Explorer.Roots.Single(row => row.Name == "src");

            src.IsExpanded = true;
            Task loading = src.Loading!;
            await loading;
            src.IsExpanded = false;
            src.IsExpanded = true;

            Assert.Same(loading, src.Loading);
            Assert.True(src.IsLoaded);
            Assert.Equal(["a.txt", "b.txt"], Names(src.Children));
            Assert.Equal(PhosphorIcon.FolderOpen, src.IconKind);
        });

    [Fact]
    public void AFolderGoneBeforeItIsExpanded_ShowsWhy()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string root = Tree(harness, "project");
            await harness.Explorer.OpenFolderAsync(root);
            FileTreeNodeViewModel docs = harness.Explorer.Roots.Single(row => row.Name == "docs");
            Directory.Delete(docs.FullPath);

            docs.IsExpanded = true;
            await docs.Loading!;

            FileTreeNodeViewModel row = Assert.Single(docs.Children);
            Assert.Equal(FileTreeNodeKind.Unreadable, row.Kind);
            Assert.Equal("Not found", row.Name);
            Assert.True(row.IsInformational);
        });

    [Fact]
    public void ClosingTheFolder_EmptiesTheTree()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Explorer.OpenFolderAsync(Tree(harness, "project"));
            harness.Explorer.SelectedNode = harness.Explorer.Roots[0];

            harness.Explorer.CloseFolderCommand.Execute(null);

            Assert.False(harness.Explorer.HasFolder);
            Assert.Empty(harness.Explorer.Roots);
            Assert.Null(harness.Explorer.SelectedNode);
            Assert.Equal(FileExplorerViewModel.DefaultHeader, harness.Explorer.Header);
        });

    [Fact]
    public void AListingStillRunning_WhenTheFolderCloses_IsDropped()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string root = Tree(harness, "project");

            Task opening = harness.Explorer.OpenFolderAsync(root);
            harness.Explorer.CloseFolder();
            await opening;

            Assert.Empty(harness.Explorer.Roots);
            Assert.Null(harness.Explorer.FolderPath);
        });

    [Fact]
    public void OpeningAnotherFolder_ReplacesTheFirst()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Explorer.OpenFolderAsync(Tree(harness, "first"));
            string second = harness.Folder.PathOf("second");
            harness.Folder.Write(Path.Combine("second", "only.txt"), "x");

            await harness.Explorer.OpenFolderAsync(second);

            Assert.Equal(second, harness.Explorer.FolderPath);
            Assert.Equal(["only.txt"], Names(harness.Explorer.Roots));
        });

    [Fact]
    public void Refresh_ShowsWhatChangedOutside_KeepingRowsExpansionAndSelection()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string root = Tree(harness, "project");
            harness.Folder.Write(Path.Combine("project", "src", "a.txt"), "a");
            await harness.Explorer.OpenFolderAsync(root);
            FileTreeNodeViewModel src = harness.Explorer.Roots.Single(row => row.Name == "src");
            src.IsExpanded = true;
            await src.Loading!;
            FileTreeNodeViewModel kept = harness.Explorer.Roots.Single(row => row.Name == "file2.txt");
            harness.Explorer.SelectedNode = kept;

            File.Delete(harness.Folder.PathOf(Path.Combine("project", "file10.txt")));
            harness.Folder.Write(Path.Combine("project", "file3.txt"), "new");
            harness.Folder.Write(Path.Combine("project", "src", "b.txt"), "b");
            await harness.Explorer.RefreshCommand.ExecuteAsync(null);

            Assert.Equal(["docs", "src", ".editorconfig", "file2.txt", "file3.txt"], Names(harness.Explorer.Roots));
            Assert.Same(src, harness.Explorer.Roots[1]);
            Assert.True(src.IsExpanded);
            Assert.Equal(["a.txt", "b.txt"], Names(src.Children));
            Assert.Same(kept, harness.Explorer.Roots[3]);
            Assert.Same(kept, harness.Explorer.SelectedNode);
        });

    [Fact]
    public void OpeningAFileRow_OpensItInTheActivePane()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Explorer.OpenFolderAsync(Tree(harness, "project"));
            FileTreeNodeViewModel file = harness.Explorer.Roots.Single(row => row.Name == "file2.txt");

            await harness.Explorer.OpenCommand.ExecuteAsync(file);

            Assert.Equal(file.FullPath, harness.Workspace.ActiveDocument?.FilePath);
            Assert.Equal("two", harness.Workspace.ActiveDocument?.Document.Text);
        });

    [Fact]
    public void OpeningAFolderRow_ExpandsIt_ThenCollapsesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Explorer.OpenFolderAsync(Tree(harness, "project"));
            FileTreeNodeViewModel docs = harness.Explorer.Roots.Single(row => row.Name == "docs");

            await harness.Explorer.OpenCommand.ExecuteAsync(docs);
            Assert.True(docs.IsExpanded);

            await harness.Explorer.OpenCommand.ExecuteAsync(docs);
            Assert.False(docs.IsExpanded);
            Assert.Null(harness.Workspace.ActiveDocument);
        });

    // project/{docs/, src/, .editorconfig, file10.txt, file2.txt}
    private static string Tree(EditorHarness harness, string name)
    {
        Directory.CreateDirectory(harness.Folder.PathOf(Path.Combine(name, "docs")));
        Directory.CreateDirectory(harness.Folder.PathOf(Path.Combine(name, "src")));
        harness.Folder.Write(Path.Combine(name, ".editorconfig"), "root = true");
        harness.Folder.Write(Path.Combine(name, "file10.txt"), "ten");
        harness.Folder.Write(Path.Combine(name, "file2.txt"), "two");

        return harness.Folder.PathOf(name);
    }

    private static string[] Names(System.Collections.Generic.IEnumerable<FileTreeNodeViewModel> rows)
        => [.. rows.Select(row => row.Name)];
}

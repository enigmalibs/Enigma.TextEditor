using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The explorer from one run of the editor to the next: kept with the session, shown or hidden as it was, and with its
/// folder when that folder is still there.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ExplorerSessionTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void AFolderOpenAndShownAtTheClose_ComesBackListedAndShown_AfterTheTabsAndTheFilesNamedOnTheCommandLine()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string project = Project(before);
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            await before.Explorer.OpenFolderAsync(project);
            string b = before.Folder.Write("b.txt", "b");

            using EditorHarness after = await NextStartAsync(before, b);

            Assert.Equal(project, after.Explorer.FolderPath);
            Assert.True(after.Explorer.IsVisible);
            Assert.Equal(["docs", "readme.txt"], after.Explorer.Roots.Select(row => row.Name));
            Assert.Equal(["a.txt", "b.txt"], after.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
        });

    [Fact]
    public void AFolderOpenWithTheExplorerHidden_ComesBackOpen_WithTheExplorerHidden()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string project = Project(before);
            await before.Explorer.OpenFolderAsync(project);
            before.Explorer.IsVisible = false;

            using EditorHarness after = await NextStartAsync(before);

            Assert.Equal(project, after.Explorer.FolderPath);
            Assert.False(after.Explorer.IsVisible);
            Assert.Equal(["docs", "readme.txt"], after.Explorer.Roots.Select(row => row.Name));
        });

    [Fact]
    public void AFolderGoneSinceTheClose_LeavesTheExplorerWithNoFolder_AndTheTabsComeBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string project = Project(before);
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            await before.Explorer.OpenFolderAsync(project);
            Assert.True(before.Window.SaveSession());
            before.Session.Dispose();
            Directory.Delete(project, recursive: true);

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(new StartupFiles([], []));

            Assert.Null(after.Explorer.FolderPath);
            Assert.Empty(after.Explorer.Roots);
            Assert.True(after.Explorer.IsVisible);
            Assert.Equal(["a.txt"], after.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            Assert.Empty(after.Messages.Errors);
        });

    [Fact]
    public void NoFolderAtTheClose_NoFolderAtTheStart()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));

            using EditorHarness after = await NextStartAsync(before);

            Assert.Null(after.Explorer.FolderPath);
            Assert.False(after.Explorer.IsVisible);
        });

    [Fact]
    public void AnExplorerFolderThatIsNotAFullPath_IsNotOpened_AndTheSessionStillComesBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            Assert.True(before.Session.Save(before.Window.CaptureSession() with { ExplorerFolder = "project", ExplorerVisible = true }));
            before.Session.Dispose();

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(new StartupFiles([], []));

            Assert.Null(after.Explorer.FolderPath);
            Assert.Equal(["a.txt"], after.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            Assert.False(File.Exists(before.Folder.PathOf(AppPaths.QuarantinedSessionFileName)));
        });

    [Fact]
    public void AFolderTheUserOpenedBeforeTheRestore_IsTheOneThatStays()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string project = Project(harness);
            Directory.CreateDirectory(harness.Folder.PathOf("other"));
            await harness.Explorer.OpenFolderAsync(harness.Folder.PathOf("other"));

            await harness.Explorer.RestoreAsync(project, visible: true);

            Assert.Equal(harness.Folder.PathOf("other"), harness.Explorer.FolderPath);
        });

    // A folder with a subfolder and a file, for the explorer to show.
    private static string Project(EditorHarness harness)
    {
        Directory.CreateDirectory(harness.Folder.PathOf(Path.Combine("project", "docs")));
        harness.Folder.Write(Path.Combine("project", "readme.txt"), "read me\n");

        return harness.Folder.PathOf("project");
    }

    // Closes the way the window does with everything saved, lets go of the session, and starts the next editor on it.
    private static async Task<EditorHarness> NextStartAsync(EditorHarness before, params string[] files)
    {
        Assert.True(before.Window.KeepSessionOnClose());
        before.Session.Dispose();

        EditorHarness after = new(sessionFolder: before.Folder.Root);
        await after.Window.StartAsync(StartupFiles.Parse(files, before.Folder.Root, File.Exists, Directory.Exists));

        return after;
    }
}

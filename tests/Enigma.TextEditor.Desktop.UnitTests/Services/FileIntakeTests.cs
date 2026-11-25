using System.IO;
using System.Linq;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The ways files come into the editor area from outside, called on the intake itself with the editor area handed in:
/// the open dialogs, and what counts as a file worth a pane of its own.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class FileIntakeTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TheOpenDialog_StartsInTheActiveBuffersFolder_AndOpensEveryFileChosen()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string first = harness.Folder.Write(Path.Combine("docs", "a.txt"), "a");
            await harness.Workspace.OpenAsync(first);
            string second = harness.Folder.Write("b.txt", "b");
            string third = harness.Folder.Write("c.txt", "c");
            harness.Prompts.FilesToOpen.Enqueue([second, third]);

            await harness.Intake.OpenFromPromptAsync(harness.Workspace);

            Assert.Equal(Path.GetDirectoryName(first), harness.Prompts.LastOpenDirectory);
            Assert.Equal(["a.txt", "b.txt", "c.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("c.txt", harness.Workspace.ActiveDocument?.Title);
        });

    [Fact]
    public void TheHexOpenDialog_OpensEveryFileChosen_InTheHexViewer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            harness.Prompts.FilesToOpen.Enqueue([harness.Folder.Write("data.bin", new byte[] { 1, 2, 3 })]);

            await harness.Intake.OpenHexFromPromptAsync(harness.Workspace);

            EditorTabViewModel tab = Assert.Single(harness.Workspace.ActivePane.Tabs);
            Assert.True(tab.Document.IsHex);
        });

    [Fact]
    public void OnlyAFile_IsWorthAPaneOfItsOwn()
        => fixture.Run(() =>
        {
            using EditorHarness harness = new();
            string file = harness.Folder.Write("a.txt", "a");
            string folder = Directory.CreateDirectory(harness.Folder.PathOf("notes")).FullName;
            string gone = harness.Folder.PathOf("gone.txt");

            Assert.True(harness.Intake.AnyFile([folder, file]));
            Assert.False(harness.Intake.AnyFile([folder, gone]));
            Assert.False(harness.Intake.AnyFile([]));
        });
}

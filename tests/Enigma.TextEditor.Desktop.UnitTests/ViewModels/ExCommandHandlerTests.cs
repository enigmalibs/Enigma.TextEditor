using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The ex commands that leave the engine, carried out against a real workspace.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class ExCommandHandlerTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void Write_SavesTheActiveBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            tab.Document.Document.Insert(1, "b");

            await Run(harness, VimExCommand.Write);

            Assert.Equal("ab", File.ReadAllText(path));
            Assert.False(tab.Document.IsDirty);
        });

    [Fact]
    public void WriteWithAFile_SavesUnderThatNameNextToTheBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await Run(harness, VimExCommand.Write, "copy.txt");

            Assert.Equal("a", File.ReadAllText(harness.Folder.PathOf("copy.txt")));
            Assert.Equal("copy.txt", tab.Document.Title);
        });

    [Fact]
    public void Quit_ClosesTheTab_AndTheAppOnlyWithTheLastOne()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));

            await Run(harness, VimExCommand.Quit);
            Assert.Single(harness.Workspace.ActivePane.Tabs);
            Assert.Equal(0, harness.Shutdown.ExitRequests);

            await Run(harness, VimExCommand.Quit);
            Assert.Empty(harness.Workspace.ActivePane.Tabs);
            Assert.Equal(1, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void Quit_OnUnsavedChanges_AsksAndCancelKeepsEverything()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            tab.Document.Document.Insert(0, "x");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);

            await Run(harness, VimExCommand.Quit);

            Assert.Equal(["a.txt"], harness.Unsaved.AskedAbout);
            Assert.Single(harness.Workspace.ActivePane.Tabs);
            Assert.Equal(0, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void QuitDiscard_ClosesWithoutAsking()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            (await harness.Workspace.OpenAsync(path))!.Document.Document.Insert(0, "x");

            await Run(harness, VimExCommand.QuitDiscard);

            Assert.Empty(harness.Unsaved.AskedAbout);
            Assert.Equal("a", File.ReadAllText(path));
            Assert.Equal(1, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void WriteQuit_SavesThenCloses()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            (await harness.Workspace.OpenAsync(path))!.Document.Document.Insert(0, "x");

            await Run(harness, VimExCommand.WriteQuit);

            Assert.Equal("xa", File.ReadAllText(path));
            Assert.Empty(harness.Workspace.ActivePane.Tabs);
        });

    [Fact]
    public void WriteQuit_ThatCannotWrite_DoesNotClose()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            harness.Workspace.NewDocument().Document.Document.Text = "typed";

            // Untitled, and the save picker is cancelled.
            await Run(harness, VimExCommand.WriteQuit);

            Assert.Single(harness.Workspace.ActivePane.Tabs);
            Assert.Equal(0, harness.Shutdown.ExitRequests);
        });

    [Fact]
    public void Edit_OpensAFileRelativeToTheActiveBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            harness.Folder.Write("b.txt", "bee");

            await Run(harness, VimExCommand.Edit, "b.txt");

            Assert.Equal("bee", harness.Workspace.ActiveDocument?.Document.Text);
            Assert.Equal(2, harness.Workspace.ActivePane.Tabs.Count);
        });

    [Fact]
    public void EditWithoutAFile_AsksFromTheActiveBuffersFolder_AndOpensWhatWasPicked()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            await harness.Workspace.OpenAsync(a);
            harness.Prompts.FilesToOpen.Enqueue([harness.Folder.Write("b.txt", "bee"), harness.Folder.Write("c.txt", "sea")]);

            await Run(harness, VimExCommand.Edit);

            Assert.Equal(1, harness.Prompts.OpenQuestions);
            Assert.Equal(Path.GetDirectoryName(a), harness.Prompts.LastOpenDirectory);
            Assert.Equal(["a.txt", "b.txt", "c.txt"], harness.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
            Assert.Equal("sea", harness.Workspace.ActiveDocument?.Document.Text);
        });

    [Fact]
    public void EditWithoutAFile_Cancelled_OpensNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            await Run(harness, VimExCommand.Edit);

            Assert.Equal(1, harness.Prompts.OpenQuestions);
            Assert.Same(a, Assert.Single(harness.Workspace.ActivePane.Tabs));
        });

    [Fact]
    public void EditNew_AddsAnUntitledTab_AndKeepsTheOneThatWasShowing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            a.Document.Document.Insert(0, "x");

            await Run(harness, VimExCommand.EditNew);

            Assert.Equal(2, harness.Workspace.ActivePane.Tabs.Count);
            Assert.Same(a, harness.Workspace.ActivePane.Tabs[0]);
            Assert.Null(harness.Workspace.ActiveDocument?.FilePath);
            Assert.Equal(string.Empty, harness.Workspace.ActiveDocument?.Document.Text);
            Assert.Empty(harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void EditNewDiscard_ReplacesTheTab_WithoutAsking_AndItsChangesAreGone()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("first.txt", "1"));
            string path = harness.Folder.Write("a.txt", "a");
            DocumentViewModel a = (await harness.Workspace.OpenAsync(path))!.Document;
            await harness.Workspace.OpenAsync(harness.Folder.Write("last.txt", "3"));
            harness.Workspace.ActivePane.ActiveTab = harness.Workspace.ActivePane.Tabs[1];
            a.Document.Insert(0, "x");

            await Run(harness, VimExCommand.EditNewDiscard);

            EditorPaneViewModel pane = harness.Workspace.ActivePane;
            Assert.Equal(3, pane.Tabs.Count);
            Assert.Same(pane.Tabs[1], pane.ActiveTab);
            Assert.Null(pane.ActiveTab?.Document.FilePath);
            Assert.DoesNotContain(a, harness.Registry.Documents);
            Assert.Empty(harness.Unsaved.AskedAbout);
            Assert.Equal("a", File.ReadAllText(path));
        });

    [Fact]
    public void EditNewDiscard_OnABufferAnotherPaneShows_KeepsItOpenThere()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            DocumentViewModel a = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document;
            EditorPaneViewModel left = harness.Workspace.ActivePane;
            harness.Workspace.Split(SplitAxis.Columns);
            harness.Workspace.Show(a);
            a.Document.Insert(0, "x");

            await Run(harness, VimExCommand.EditNewDiscard);

            Assert.NotSame(a, harness.Workspace.ActiveDocument);
            Assert.Same(a, left.ActiveTab?.Document);
            Assert.Equal("xa", a.Document.Text);
            Assert.Contains(a, harness.Registry.Documents);
            Assert.Empty(harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void EditNewDiscard_InAnEmptyPane_OpensAnUntitledTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await Run(harness, VimExCommand.EditNewDiscard);

            Assert.Null(Assert.Single(harness.Workspace.ActivePane.Tabs).Document.FilePath);
        });

    [Fact]
    public void BufferNextAndPrevious_CycleThePanesTabs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel? a = await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a"));
            EditorTabViewModel? b = await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));

            await Run(harness, VimExCommand.BufferNext);
            Assert.Same(a, harness.Workspace.ActiveTab);

            await Run(harness, VimExCommand.BufferPrevious);
            Assert.Same(b, harness.Workspace.ActiveTab);
        });

    [Fact]
    public void WriteAll_SavesEveryDirtyBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            string b = harness.Folder.Write("b.txt", "b");
            (await harness.Workspace.OpenAsync(a))!.Document.Document.Insert(0, "1");
            (await harness.Workspace.OpenAsync(b))!.Document.Document.Insert(0, "2");

            await Run(harness, VimExCommand.WriteAll);

            Assert.Equal("1a", File.ReadAllText(a));
            Assert.Equal("2b", File.ReadAllText(b));
        });

    [Fact]
    public void QuitAll_AsksTheWindowToClose_ThroughItsQuestions()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");

            await Run(harness, VimExCommand.QuitAll);

            Assert.Equal(1, harness.Shutdown.ExitRequests);
            Assert.True(harness.Window.MustConfirmClose);
        });

    [Fact]
    public void QuitAllDiscard_ClosesWithoutAnyQuestion()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");

            await Run(harness, VimExCommand.QuitAllDiscard);

            Assert.True(harness.Shutdown.DiscardChangesOnExit);
            Assert.False(harness.Window.MustConfirmClose);
            Assert.True(await harness.Window.ConfirmCloseAsync());
            Assert.Empty(harness.Unsaved.AskedAbout);
        });

    private static Task Run(EditorHarness harness, VimExCommand command, string? argument = null)
        => harness.Workspace.ActivePane.ExecuteExAsync(new VimExRequest(command, argument));
}

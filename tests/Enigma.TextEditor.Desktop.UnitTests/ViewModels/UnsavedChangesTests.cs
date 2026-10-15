using System.IO;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// Nothing is lost without the user choosing <em>Don't save</em>: closing a tab, closing the window.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class UnsavedChangesTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ClosingACleanTab_AsksNothing()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;

            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);

            Assert.True(closed);
            Assert.Empty(harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void Save_WritesTheFileThenCloses()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            tab.Document.Document.Insert(1, "!");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Save);

            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);

            Assert.True(closed);
            Assert.Equal(["a.txt"], harness.Unsaved.AskedAbout);
            Assert.Equal("a!", File.ReadAllText(path));
            Assert.Empty(harness.Registry.Documents);
        });

    [Fact]
    public void Save_ThatDoesNotHappen_KeepsTheTab()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = harness.Workspace.NewDocument();
            tab.Document.Document.Text = "typed";
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Save);

            // The save picker is cancelled: there is nowhere to write it, so nothing may be closed.
            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);

            Assert.False(closed);
            Assert.Contains(tab, harness.Workspace.ActivePane.Tabs);
            Assert.Equal(1, harness.Prompts.SaveQuestions);
        });

    [Fact]
    public void DontSave_ClosesAndDropsTheChanges()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string path = harness.Folder.Write("a.txt", "a");
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(path))!;
            tab.Document.Document.Insert(1, "!");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);

            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);

            Assert.True(closed);
            Assert.Equal("a", File.ReadAllText(path));
            Assert.Empty(harness.Registry.Documents);
        });

    [Fact]
    public void Cancel_KeepsTheTabAndItsChanges()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!;
            tab.Document.Document.Insert(1, "!");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);

            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab);

            Assert.False(closed);
            Assert.Same(tab, harness.Workspace.ActiveTab);
            Assert.True(tab.Document.IsDirty);
        });

    [Fact]
    public void Discarding_SkipsTheQuestion()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            EditorTabViewModel tab = harness.Workspace.NewDocument();
            tab.Document.Document.Text = "typed";

            bool closed = await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab, discard: true);

            Assert.True(closed);
            Assert.Empty(harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void ClosingTheWindow_AsksAboutEveryDirtyBufferUntilOneIsCancelled()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");
            await harness.Workspace.OpenAsync(harness.Folder.Write("clean.txt", "c"));
            (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!.Document.Document.Insert(0, "2");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);

            bool mayClose = await harness.Window.ConfirmCloseAsync();

            Assert.False(mayClose);
            Assert.Equal(["a.txt", "b.txt"], harness.Unsaved.AskedAbout);
            Assert.Equal("b.txt", harness.Workspace.ActiveDocument?.Title);
        });

    [Fact]
    public void ClosingTheWindow_WithEverythingSavedOrDiscarded_IsAllowed()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            (await harness.Workspace.OpenAsync(a))!.Document.Document.Insert(0, "1");
            (await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b")))!.Document.Document.Insert(0, "2");
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Save);
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);

            Assert.True(await harness.Window.ConfirmCloseAsync());
            Assert.Equal("1a", File.ReadAllText(a));
        });

    [Fact]
    public void TheRealWindow_StaysOpenWhenTheQuestionIsCancelledAndClosesWhenItIsAnswered()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();
            bool closed = false;
            window.Closed += (_, _) => closed = true;

            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.False(closed);

            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.True(closed);
        });

    [Fact]
    public void StartupFiles_OpenInOrderWithTheLastActiveAndAMissingFileAsANewBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "a");
            string missing = harness.Folder.PathOf("new.txt");
            string folder = harness.Folder.Root;
            StartupFiles startup = StartupFiles.Parse([a, missing, folder], harness.Folder.Root, File.Exists, Directory.Exists);

            await harness.Workspace.OpenStartupFilesAsync(startup);

            Assert.Equal(["a.txt", "new.txt"], [.. System.Linq.Enumerable.Select(harness.Workspace.ActivePane.Tabs, tab => tab.Document.Title)]);
            Assert.Equal("new.txt", harness.Workspace.ActiveDocument?.Title);
            Assert.False(harness.Workspace.ActiveDocument?.IsDirty);
            Assert.Contains("is not a file", harness.StatusLine.Current, System.StringComparison.Ordinal);

            Assert.True(await harness.Workspace.SaveAsync(harness.Workspace.ActiveDocument));
            Assert.True(File.Exists(missing));
        });

    [Fact]
    public void StartingWithNoFiles_OpensAnUntitledBuffer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Workspace.OpenStartupFilesAsync(new StartupFiles([], []));

            Assert.Equal("Untitled-1", harness.Workspace.ActiveDocument?.Title);
            Assert.Null(harness.StatusLine.Current);
        });
}

using System.IO;
using System.Linq;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The session from one run of the editor to the next: kept as the window closes, brought back as the next one
/// starts — by the one instance that owns it.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SessionStartupTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ASessionKeptAtTheClose_ComesBackAtTheNextStart_BeforeTheFilesNamedOnTheCommandLine()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            await before.Workspace.OpenAsync(before.Folder.Write("b.txt", "b"));
            before.Workspace.Split(SplitAxis.Columns, newDocument: false);
            await before.Workspace.OpenAsync(before.Folder.Write("c.txt", "c"));
            string d = before.Folder.Write("d.txt", "d");

            Assert.True(before.Window.SaveSession());

            // The process ends, and its lock with it.
            before.Session.Dispose();

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(StartupFiles.Parse([d], before.Folder.Root, File.Exists, Directory.Exists));

            Assert.Equal("a.txt, *b.txt | c.txt, *d.txt", Describe(after.Workspace));
            Assert.Same(after.Workspace.Panes[1], after.Workspace.ActivePane);
        });

    [Fact]
    public void WithNoSession_TheStartIsAsBefore()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Window.StartAsync(new StartupFiles([], []));

            Assert.Equal("*Untitled-1", Describe(harness.Workspace));
        });

    [Fact]
    public void AnUnusableSessionFile_IsMovedAside_AndTheStartIsAsBefore()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            harness.Folder.Write(AppPaths.SessionFileName, "{ not a session");

            await harness.Window.StartAsync(new StartupFiles([], []));

            Assert.Equal("*Untitled-1", Describe(harness.Workspace));
            Assert.Equal("{ not a session", File.ReadAllText(harness.Folder.PathOf(AppPaths.QuarantinedSessionFileName)));
        });

    [Fact]
    public void AnotherInstance_NeitherRestoresNorKeepsTheSessionTheFirstOwns()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness first = new();
            await first.Workspace.OpenAsync(first.Folder.Write("a.txt", "a"));
            Assert.True(first.Window.SaveSession());
            string kept = File.ReadAllText(first.Folder.PathOf(AppPaths.SessionFileName));

            using EditorHarness second = new(sessionFolder: first.Folder.Root);
            await second.Window.StartAsync(new StartupFiles([], []));
            await second.Workspace.OpenAsync(first.Folder.Write("other.txt", "o"));

            Assert.False(second.Session.IsOwner);
            Assert.Equal("Untitled-1, *other.txt", Describe(second.Workspace));
            Assert.False(second.Window.SaveSession());
            Assert.Equal(kept, File.ReadAllText(first.Folder.PathOf(AppPaths.SessionFileName)));
        });

    [Fact]
    public void ClosingTheWindow_KeepsTheSession_AfterTheQuestionsAreAnswered()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");
            await harness.Workspace.OpenAsync(harness.Folder.Write("b.txt", "b"));
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();

            // Cancelled: the window stays, and nothing is kept.
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.False(File.Exists(harness.Folder.PathOf(AppPaths.SessionFileName)));

            // Answered: the window closes, and keeps its tabs.
            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            harness.Session.Dispose();

            using EditorHarness next = new(sessionFolder: harness.Folder.Root);
            await next.Window.StartAsync(new StartupFiles([], []));

            // The change thrown away is not kept: the file comes back as it is on disk.
            Assert.Equal("a.txt, *b.txt", Describe(next.Workspace));
            Assert.Equal("a", next.Workspace.ActivePane.Tabs[0].Document.Document.Text);
            Assert.False(next.Workspace.ActivePane.Tabs[0].Document.IsDirty);
        });

    // Each pane, in reading order: its tabs' titles, the showing one starred.
    private static string Describe(WorkspaceViewModel workspace)
        => string.Join(
            " | ",
            workspace.Panes.Select(pane => string.Join(
                ", ",
                pane.Tabs.Select(tab => ReferenceEquals(tab, pane.ActiveTab) ? $"*{tab.Document.Title}" : tab.Document.Title))));
}

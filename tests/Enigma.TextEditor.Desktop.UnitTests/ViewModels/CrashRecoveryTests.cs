using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The session kept every few seconds while the editor runs: what a crash leaves is what the next start brings back.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class CrashRecoveryTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void AfterATick_ACrash_LosesNoUnsavedChange()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Window.StartAsync(new StartupFiles([], []));
            string a = before.Folder.Write("a.txt", "one\n");
            (await before.Workspace.OpenAsync(a))!.Document.Document.Insert(0, "zero\n");
            before.Workspace.NewDocument().Document.Document.Text = "draft";

            Tick(before);
            Crash(before);

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(new StartupFiles([], []));

            DocumentViewModel file = after.Registry.Find(a)!;
            Assert.True(file.IsDirty);
            Assert.Equal("zero\none\n", file.Document.Text);
            // Beside the empty untitled tab the start had opened, which comes back too.
            DocumentViewModel draft = after.Registry.Documents.Single(document => document.IsUntitled && (document.Document.TextLength > 0));
            Assert.True(draft.IsDirty);
            Assert.Equal("draft", draft.Document.Text);
            Assert.Equal("one\n", File.ReadAllText(a));
        });

    [Fact]
    public void AChangeAfterTheLastTick_IsWhatACrashLoses()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Window.StartAsync(new StartupFiles([], []));
            DocumentViewModel draft = before.Workspace.ActiveDocument!;
            draft.Document.Text = "kept";
            Tick(before);
            draft.Document.Text = "kept, then lost";

            before.Clock.Advance(SessionAutosave.Interval / 2);
            Dispatcher.UIThread.RunJobs();
            Crash(before);

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(new StartupFiles([], []));

            Assert.Equal("kept", after.Workspace.ActiveDocument?.Document.Text);
        });

    [Fact]
    public void NoTickWrites_BeforeTheStartHasRestoredTheSession()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            Assert.True(before.Window.SaveSession());
            before.Session.Dispose();
            string kept = File.ReadAllText(before.Folder.PathOf(AppPaths.SessionFileName));

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            Tick(after);
            Tick(after);

            Assert.False(after.Autosave.IsRunning);
            Assert.Equal(kept, File.ReadAllText(before.Folder.PathOf(AppPaths.SessionFileName)));

            await after.Window.StartAsync(new StartupFiles([], []));

            Assert.True(after.Autosave.IsRunning);
            Assert.Equal(["a.txt"], after.Workspace.ActivePane.Tabs.Select(tab => tab.Document.Title));
        });

    [Fact]
    public void AnInstanceThatDoesNotOwnTheSession_NeverWritesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness first = new();
            await first.Window.StartAsync(new StartupFiles([], []));
            await first.Workspace.OpenAsync(first.Folder.Write("a.txt", "a"));
            Tick(first);
            string kept = File.ReadAllText(first.Folder.PathOf(AppPaths.SessionFileName));

            using EditorHarness second = new(sessionFolder: first.Folder.Root);
            await second.Window.StartAsync(new StartupFiles([], []));
            second.Workspace.ActiveDocument!.Document.Text = "second";
            Tick(second);

            Assert.False(second.Autosave.IsRunning);
            Assert.Equal(kept, File.ReadAllText(first.Folder.PathOf(AppPaths.SessionFileName)));
        });

    [Fact]
    public void AfterQuitAllDiscard_ATick_KeepsTheTabsButNoUnsavedText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Window.StartAsync(new StartupFiles([], []));
            string a = before.Folder.Write("a.txt", "one");
            (await before.Workspace.OpenAsync(a))!.Document.Document.Insert(0, "changed ");
            Tick(before);
            await before.Workspace.ExecuteExAsync(before.Workspace.ActivePane, new VimExRequest(VimExCommand.QuitAllDiscard, null));

            Tick(before);
            Crash(before);

            using EditorHarness after = new(sessionFolder: before.Folder.Root);
            await after.Window.StartAsync(new StartupFiles([], []));

            DocumentViewModel file = after.Registry.Find(a)!;
            Assert.False(file.IsDirty);
            Assert.Equal("one", file.Document.Text);
        });

    [Fact]
    public void OnceTheWindowHasClosed_NoTickReplacesTheSessionItKept()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            harness.Workspace.ActiveDocument!.Document.Text = "at the close";
            MainWindow window = new() { DataContext = harness.Window };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.Close();
            Dispatcher.UIThread.RunJobs();
            string sessionFile = harness.Folder.PathOf(AppPaths.SessionFileName);
            string kept = File.ReadAllText(sessionFile);
            string? keptText = Assert.Single(SessionFile.Read(sessionFile).State!.Documents).UnsavedText;
            harness.Workspace.ActiveDocument!.Document.Text = "after the close";
            Tick(harness);

            Assert.False(harness.Autosave.IsRunning);
            Assert.Equal("at the close", keptText);
            Assert.Equal(kept, File.ReadAllText(sessionFile));
            Assert.Equal("at the close", Assert.Single(SessionFile.Read(sessionFile).State!.Documents).UnsavedText);
        });

    // One interval goes by, and the tick it posts runs; the harness writes on the spot.
    private static void Tick(EditorHarness harness)
    {
        harness.Clock.Advance(SessionAutosave.Interval);
        Dispatcher.UIThread.RunJobs();
    }

    // The process ends without closing anything: only the operating system's release of the lock happens.
    private static void Crash(EditorHarness harness) => harness.Session.Dispose();
}

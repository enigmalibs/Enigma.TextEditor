using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
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
/// Closing the editor with unsaved changes: kept for the next start without a question, unless they cannot be —
/// and closing a tab still asks.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HotExitTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void UnsavedChanges_CloseWithoutAQuestion_AndComeBackAsTheyWere()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "one\ntwo\n");
            DocumentViewModel file = (await before.Workspace.OpenAsync(a))!.Document;
            file.Document.Insert(0, "zero\n");
            file.ChangeEncoding(TextEncodings.Utf16Le);
            file.ChangeLineEndings(LineEndingStyle.CrLf);
            file.Indentation = Indentation.Tabs(4);
            before.Workspace.NewDocument().Document.Document.Text = "draft";

            Assert.True(CloseWindow(before));
            Assert.Empty(before.Unsaved.AskedAbout);
            Assert.Equal("one\ntwo\n", File.ReadAllText(a));

            using EditorHarness after = await NextStartAsync(before);

            Assert.Equal("a.txt, *Untitled-1", Describe(after.Workspace));
            DocumentViewModel restored = after.Registry.Find(a)!;
            Assert.True(restored.IsDirty);
            Assert.Equal("zero\none\ntwo\n", restored.Document.Text);
            Assert.Equal(TextEncodings.Utf16Le, restored.Encoding);
            Assert.Equal(LineEndingStyle.CrLf, restored.Format.LineEndings);
            Assert.Equal(Indentation.Tabs(4), restored.Indentation);

            DocumentViewModel draft = after.Registry.Documents.Single(document => document.IsUntitled);
            Assert.Equal("draft", draft.Document.Text);
            Assert.True(draft.IsDirty);
            Assert.Contains("•", after.Window.Title, System.StringComparison.Ordinal);

            // Saved, it is written as it was set to be, and the dot goes.
            Assert.True(await after.Workspace.SaveAsync(restored));
            Assert.False(restored.IsDirty);
            Assert.Equal(
                [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("zero\r\none\r\ntwo\r\n")],
                File.ReadAllBytes(a));
        });

    [Fact]
    public void AFileGoneSinceTheClose_ComesBackWithItsUnsavedText_AndASaveWritesItAgain()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "a");
            (await before.Workspace.OpenAsync(a))!.Document.Document.Insert(1, "b");
            Assert.True(CloseWindow(before));
            File.Delete(a);

            using EditorHarness after = await NextStartAsync(before);

            DocumentViewModel restored = after.Registry.Find(a)!;
            Assert.Equal("ab", restored.Document.Text);
            Assert.True(await after.Workspace.SaveAsync(restored));
            Assert.Equal("ab", File.ReadAllText(a));
        });

    [Fact]
    public void QuitAllDiscard_ClosesWithoutAQuestion_TheTabsComeBackButNotTheChanges()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "one");
            (await before.Workspace.OpenAsync(a))!.Document.Document.Insert(0, "changed ");
            before.Workspace.NewDocument().Document.Document.Text = "draft";
            await before.Workspace.ExecuteExAsync(before.Workspace.ActivePane, new VimExRequest(VimExCommand.QuitAllDiscard, null));

            Assert.True(CloseWindow(before));
            Assert.Empty(before.Unsaved.AskedAbout);

            using EditorHarness after = await NextStartAsync(before);

            Assert.Equal("*a.txt", Describe(after.Workspace));
            Assert.Equal("one", after.Workspace.ActiveDocument?.Document.Text);
            Assert.False(after.Workspace.ActiveDocument?.IsDirty);
        });

    [Fact]
    public void WhenTheSessionCannotBeKept_ClosingAsksAsBefore()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            // A folder where the file should be: the session can be neither written nor moved over it.
            Directory.CreateDirectory(harness.Folder.PathOf(AppPaths.SessionFileName));
            (await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a")))!.Document.Document.Insert(0, "1");
            MainWindow window = Show(harness);
            bool closed = false;
            window.Closed += (_, _) => closed = true;

            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.False(closed);
            Assert.Equal(["a.txt"], harness.Unsaved.AskedAbout);

            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Discard);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            Assert.True(closed);
        });

    [Fact]
    public void ARestoredUnsavedBuffer_StaysUnsavedThroughUndo_AndClosingItsTabStillAsks()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "on disk");
            SessionState state = new(
                SessionState.CurrentVersion,
                new LayoutSnapshot(Pane: 0),
                0,
                [new SessionPane(0, [new SessionTab(0)], 0)],
                [new SessionDocument(a, "Untitled-1", UnsavedText: "kept")]);

            await harness.Workspace.RestoreSessionAsync(state);
            EditorTabViewModel tab = harness.Workspace.ActiveTab!;
            tab.Document.Document.Insert(0, "x");
            tab.Document.Document.UndoStack.Undo();

            Assert.Equal("kept", tab.Document.Document.Text);
            Assert.True(tab.Document.IsDirty);

            harness.Unsaved.Answers.Enqueue(UnsavedChangesChoice.Cancel);
            Assert.False(await harness.Workspace.CloseTabAsync(harness.Workspace.ActivePane, tab));
            Assert.Equal(["a.txt"], harness.Unsaved.AskedAbout);
        });

    [Fact]
    public void AReloadedRestoredBuffer_IsNoLongerUnsaved()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            string a = harness.Folder.Write("a.txt", "on disk");
            SessionState state = new(
                SessionState.CurrentVersion,
                new LayoutSnapshot(Pane: 0),
                0,
                [new SessionPane(0, [new SessionTab(0)], 0)],
                [new SessionDocument(a, "Untitled-1", UnsavedText: "kept")]);

            await harness.Workspace.RestoreSessionAsync(state);
            DocumentViewModel document = harness.Workspace.ActiveDocument!;

            Assert.True(await harness.Documents.ReopenWithEncodingAsync(document, TextEncodings.Utf8));

            Assert.Equal("on disk", document.Document.Text);
            Assert.False(document.IsDirty);
        });

    // Closes the harness's window as the user would, and lets the process go: the lock with it.
    private static bool CloseWindow(EditorHarness harness)
    {
        MainWindow window = Show(harness);
        bool closed = false;
        window.Closed += (_, _) => closed = true;

        window.Close();
        Dispatcher.UIThread.RunJobs();
        harness.Session.Dispose();

        return closed;
    }

    private static async Task<EditorHarness> NextStartAsync(EditorHarness before)
    {
        EditorHarness after = new(sessionFolder: before.Folder.Root);
        await after.Window.StartAsync(new StartupFiles([], []));

        return after;
    }

    private static MainWindow Show(EditorHarness harness)
    {
        MainWindow window = new() { DataContext = harness.Window };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    // Each pane, in reading order: its tabs' titles, the showing one starred.
    private static string Describe(WorkspaceViewModel workspace)
        => string.Join(
            " | ",
            workspace.Panes.Select(pane => string.Join(
                ", ",
                pane.Tabs.Select(tab => ReferenceEquals(tab, pane.ActiveTab) ? $"*{tab.Document.Title}" : tab.Document.Title))));
}

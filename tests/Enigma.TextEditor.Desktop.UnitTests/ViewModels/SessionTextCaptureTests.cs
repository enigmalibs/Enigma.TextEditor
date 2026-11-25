using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The autosave keeps each unsaved buffer's text in a file of its own, named after the buffer and the version of its
/// text: a tick that only moved a caret writes the session and no text; a keystroke writes that buffer's text alone.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SessionTextCaptureTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void AnUnchangedBuffer_IsCapturedUnderTheSameName_AndAChangedOneUnderANewOne()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            harness.Workspace.ActiveDocument!.Document.Text = "draft";

            string first = TextFileOf(harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true));
            string again = TextFileOf(harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true));
            harness.Workspace.ActiveDocument.Document.Insert(5, "!");
            string changed = TextFileOf(harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true));

            Assert.Equal(first, again);
            Assert.NotEqual(first, changed);
            Assert.Equal(
                harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true).State,
                harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true).State);
        });

    [Fact]
    public void ATickThatOnlyMovedACaret_RewritesTheSession_ButNoText()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            harness.Workspace.ActiveDocument!.Document.Text = "one\ntwo\n";
            Tick(harness);
            string session = File.ReadAllText(SessionFileOf(harness));
            Dictionary<string, DateTime> texts = TextFilesOf(harness);

            harness.Workspace.ActiveTab!.CaretOffset = 4;
            Tick(harness);

            Assert.NotEqual(session, File.ReadAllText(SessionFileOf(harness)));
            Assert.Equal(texts, TextFilesOf(harness));
            Assert.Single(texts);
        });

    [Fact]
    public void AKeystroke_RewritesThatBuffersText_Only()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            DocumentViewModel a = harness.Workspace.ActiveDocument!;
            a.Document.Text = "a";
            DocumentViewModel b = harness.Workspace.NewDocument().Document;
            b.Document.Text = "b";
            Tick(harness);
            Dictionary<string, DateTime> before = TextFilesOf(harness);

            a.Document.Insert(1, "a");
            Tick(harness);
            Dictionary<string, DateTime> after = TextFilesOf(harness);

            Assert.Equal(2, before.Count);
            Assert.Equal(2, after.Count);
            string kept = Assert.Single(before.Keys.Intersect(after.Keys));
            Assert.Equal(before[kept], after[kept]);
            SessionState read = SessionFile.Read(SessionFileOf(harness)).State!;
            Assert.Equal(["aa", "b"], read.Documents.Select(document => document.UnsavedText).Order(StringComparer.Ordinal));
        });

    [Fact]
    public void TheTextWritten_IsTheOneCaptured_EvenWhenTheBufferChangesBeforeTheWrite()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            harness.Workspace.ActiveDocument!.Document.Text = "as captured";
            SessionCapture capture = harness.WorkspaceSession.Capture(harness.Workspace, keepUnsaved: true);

            harness.Workspace.ActiveDocument.Document.Text = "typed meanwhile";
            Assert.True(harness.Session.Save(capture));

            Assert.Equal("as captured", Assert.Single(SessionFile.Read(SessionFileOf(harness)).State!.Documents).UnsavedText);
        });

    [Fact]
    public void ASessionKeptWithoutUnsavedChanges_LeavesNoTextBehind()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Window.StartAsync(new StartupFiles([], []));
            harness.Workspace.ActiveDocument!.Document.Text = "thrown away";
            Tick(harness);
            Assert.Single(TextFilesOf(harness));

            Assert.True(harness.Window.SaveSession());

            Assert.Empty(TextFilesOf(harness));
        });

    private static string TextFileOf(SessionCapture capture)
    {
        SessionText text = Assert.Single(capture.Texts);
        Assert.Equal(text.FileName, Assert.Single(capture.State.Documents).UnsavedTextFile);

        return text.FileName;
    }

    private static string SessionFileOf(EditorHarness harness) => harness.Folder.PathOf(AppPaths.SessionFileName);

    private static Dictionary<string, DateTime> TextFilesOf(EditorHarness harness)
    {
        string folder = SessionFile.UnsavedFolderFor(SessionFileOf(harness));

        return Directory.Exists(folder)
            ? Directory.GetFiles(folder).ToDictionary(file => Path.GetFileName(file), File.GetLastWriteTimeUtc, StringComparer.Ordinal)
            : [];
    }

    // One interval goes by, and the tick it posts runs; the harness writes on the spot.
    private static void Tick(EditorHarness harness)
    {
        harness.Clock.Advance(SessionAutosave.Interval);
        Dispatcher.UIThread.RunJobs();
    }
}

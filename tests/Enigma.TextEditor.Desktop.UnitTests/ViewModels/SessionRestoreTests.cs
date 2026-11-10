using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The editor area captured as a session and brought back in another: the splits, the tabs, where each was left —
/// and what happens to what cannot come back.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SessionRestoreTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void TabsAndSplits_ComeBackAsTheyWere_ThroughTheSessionFile()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "alpha\nbeta\n");
            string b = before.Folder.Write("b.txt", "bravo");
            EditorTabViewModel first = (await before.Workspace.OpenAsync(a))!;
            await before.Workspace.OpenAsync(b);
            first.CaretOffset = 6;
            first.VerticalScrollOffset = 12.5;
            first.HorizontalScrollOffset = 3;

            // A new untitled buffer in the new pane on the right, then the same file as on the left.
            before.Workspace.Split(SplitAxis.Columns);
            EditorTabViewModel second = (await before.Workspace.OpenAsync(a))!;
            second.CaretOffset = 2;
            before.Workspace.Layout.SetRatio((SplitNode)before.Workspace.Layout.Root, 0.3);

            string path = before.Folder.PathOf(AppPaths.SessionFileName);
            SessionFile.Write(path, before.Workspace.CaptureSession());
            SessionState state = SessionFile.Read(path).State!;

            using EditorHarness after = new();
            IReadOnlyList<string> skipped = await after.Workspace.RestoreSessionAsync(state);

            Assert.Empty(skipped);
            Assert.Equal(before.Workspace.Layout.ToSnapshot(), after.Workspace.Layout.ToSnapshot());
            Assert.Equal(Describe(before.Workspace), Describe(after.Workspace));
            Assert.Equal(before.Workspace.ActivePaneId, after.Workspace.ActivePaneId);
            Assert.True(after.Workspace.ActivePane.IsActive);

            // One buffer for the file, shown by a tab in each pane, each tab where it was left.
            EditorPaneViewModel left = after.Workspace.Panes[0];
            EditorPaneViewModel right = after.Workspace.Panes[1];
            Assert.Equal(3, after.Registry.Documents.Count);
            Assert.Same(left.Tabs[0].Document, right.Tabs[1].Document);
            Assert.Equal(2, after.Registry.ViewCount(left.Tabs[0].Document));
            Assert.Equal((6, 12.5, 3.0), (left.Tabs[0].CaretOffset, left.Tabs[0].VerticalScrollOffset, left.Tabs[0].HorizontalScrollOffset));
            Assert.Equal(2, right.Tabs[1].CaretOffset);
            Assert.Equal("alpha\nbeta\n", left.Tabs[0].Document.Document.Text);
            Assert.False(left.Tabs[0].Document.IsDirty);
        });

    [Fact]
    public void AHexView_AnEncodingChosen_AndALanguageChosen_ComeBack()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string text = before.Folder.Write("t.txt", "café");
            EditorTabViewModel textTab = (await before.Workspace.OpenAsync(text))!;
            Assert.True(await before.Documents.ReopenWithEncodingAsync(textTab.Document, TextEncodings.Windows1252));
            LanguageInfo language = before.Languages.All.First(candidate => !candidate.IsPlainText);
            textTab.Document.ChooseLanguage(language);
            textTab.Document.Indentation = Indentation.Tabs(8);

            string bin = before.Folder.Write("b.bin", new byte[64]);
            EditorTabViewModel hexTab = (await before.Workspace.OpenHexAsync(bin))!;
            hexTab.HexRow = 2;

            using EditorHarness after = new();
            await after.Workspace.RestoreSessionAsync(before.Workspace.CaptureSession());

            DocumentViewModel restored = after.Registry.Find(text)!;
            Assert.Equal(TextEncodings.Windows1252, restored.Encoding);
            Assert.Equal("cafÃ©", restored.Document.Text);
            Assert.True(restored.IsLanguageChosen);
            Assert.Equal(language, restored.Language);
            Assert.Equal(Indentation.Tabs(8), restored.Indentation);

            DocumentViewModel hex = after.Registry.FindHex(bin)!;
            Assert.True(hex.IsHex);
            Assert.Equal(2, after.Workspace.ActivePane.Tabs.Single(tab => tab.Document == hex).HexRow);
            Assert.Same(hex, after.Workspace.ActiveDocument);
        });

    [Fact]
    public void AChangeOfEncodingNotSaved_IsNotKept_TheFileComesBackInItsOwn()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string path = before.Folder.Write("p.txt", "plain");
            EditorTabViewModel tab = (await before.Workspace.OpenAsync(path))!;
            tab.Document.ChangeEncoding(TextEncodings.Utf16Le);
            tab.Document.ChangeLineEndings(LineEndingStyle.CrLf);
            Assert.True(tab.Document.IsDirty);

            using EditorHarness after = new();
            await after.Workspace.RestoreSessionAsync(before.Workspace.CaptureSession());

            DocumentViewModel restored = after.Registry.Find(path)!;
            Assert.Equal(TextEncodings.Utf8, restored.Encoding);
            Assert.Equal(LineEndingStyle.Lf, restored.Format.LineEndings);
            Assert.Equal("plain", restored.Document.Text);
            Assert.False(restored.IsDirty);
        });

    [Fact]
    public void AFileGoneSinceTheClose_IsSkipped_ItsEmptiedPaneCloses_AndTheStatusLineNamesIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "a");
            string b = before.Folder.Write("b.txt", "b");
            await before.Workspace.OpenAsync(a);
            before.Workspace.Split(SplitAxis.Rows, newDocument: false);
            await before.Workspace.OpenAsync(b);
            SessionState state = before.Workspace.CaptureSession();
            File.Delete(b);

            using EditorHarness after = new();
            IReadOnlyList<string> skipped = await after.Workspace.RestoreSessionAsync(state);

            Assert.Equal(["b.txt"], skipped);
            Assert.IsType<PaneNode>(after.Workspace.Layout.Root);
            Assert.Equal("a.txt", after.Workspace.ActiveDocument?.Title);
            Assert.Contains("b.txt", after.StatusLine.Current);
            Assert.Single(after.Registry.Documents);
        });

    [Fact]
    public void WhenNothingComesBack_OneEmptyPaneIsLeft()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            string a = before.Folder.Write("a.txt", "a");
            string b = before.Folder.Write("b.txt", "b");
            await before.Workspace.OpenAsync(a);
            before.Workspace.Split(SplitAxis.Columns, newDocument: false);
            await before.Workspace.OpenAsync(b);
            SessionState state = before.Workspace.CaptureSession();
            File.Delete(a);
            File.Delete(b);

            using EditorHarness after = new();
            await after.Workspace.RestoreSessionAsync(state);

            Assert.Single(after.Workspace.Panes);
            Assert.False(after.Workspace.HasTabs);
            Assert.Empty(after.Registry.Documents);
        });

    [Fact]
    public void AnUntitledBufferWithText_IsNotKept_AnEmptyOneComesBackUnderItsName()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            before.Workspace.NewDocument().Document.Document.Text = "draft";
            before.Workspace.NewDocument();

            SessionState state = before.Workspace.CaptureSession();

            Assert.Equal(["Untitled-2"], state.Documents.Select(document => document.UntitledName));

            using EditorHarness after = new();
            await after.Workspace.RestoreSessionAsync(state);

            DocumentViewModel restored = Assert.Single(after.Workspace.ActivePane.Tabs).Document;
            Assert.Equal("Untitled-2", restored.Title);
            Assert.True(restored.IsUntitled);
            Assert.Equal(string.Empty, restored.Document.Text);
        });

    [Fact]
    public void AnEditorAreaWithATab_IsLeftAsItIs()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness before = new();
            await before.Workspace.OpenAsync(before.Folder.Write("a.txt", "a"));
            SessionState state = before.Workspace.CaptureSession();

            using EditorHarness after = new();
            await after.Workspace.OpenAsync(after.Folder.Write("c.txt", "c"));

            Assert.Empty(await after.Workspace.RestoreSessionAsync(state));
            Assert.Equal("c.txt", Assert.Single(after.Workspace.ActivePane.Tabs).Document.Title);
            Assert.Single(after.Registry.Documents);
        });

    // Each pane, in reading order: its tabs' titles, the showing one starred.
    private static string Describe(WorkspaceViewModel workspace)
        => string.Join(
            " | ",
            workspace.Panes.Select(pane => string.Join(
                ", ",
                pane.Tabs.Select(tab => ReferenceEquals(tab, pane.ActiveTab) ? $"*{tab.Document.Title}" : tab.Document.Title))));
}

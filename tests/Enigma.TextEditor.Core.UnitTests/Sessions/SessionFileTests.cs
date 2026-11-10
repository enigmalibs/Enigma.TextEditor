using System;
using System.IO;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Sessions;

/// <summary>
/// The session file: written whole, read back exactly, and moved aside — never trusted — when it does not hold a
/// session a restore could use.
/// </summary>
public sealed class SessionFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Enigma.TextEditor.Tests", Guid.NewGuid().ToString("N"));

    private string Folder => Path.Combine(_root, "data");

    private string SessionPath => Path.Combine(Folder, AppPaths.SessionFileName);

    private string QuarantinedPath => Path.Combine(Folder, AppPaths.QuarantinedSessionFileName);

    private static string FullPath(string name) => Path.Combine(Path.GetTempPath(), name);

    [Fact]
    public void ASession_RoundTrips_AndTheFolderIsMadeForIt()
    {
        SessionState state = TwoPanes();

        SessionFile.Write(SessionPath, state);
        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.Failure);
        Assert.Equivalent(state, result.State, strict: true);
        Assert.Equal(state.Layout, result.State!.Layout);
    }

    [Fact]
    public void AWrite_ReplacesTheSessionBefore_AndLeavesNoTemporaryFileBehind()
    {
        SessionFile.Write(SessionPath, TwoPanes());
        SessionState single = OnePane(new SessionDocument(null, "Untitled-1"));

        SessionFile.Write(SessionPath, single);

        Assert.Equivalent(single, SessionFile.Read(SessionPath).State, strict: true);
        Assert.Equal([SessionPath], Directory.GetFiles(Folder));
    }

    [Fact]
    public void UnsavedText_IsKeptExactly()
    {
        const string text = "line \"one\"\n\ttab\\back\nü € 😀\n";
        SessionState state = OnePane(new SessionDocument(null, "Untitled-3", UnsavedText: text));

        SessionFile.Write(SessionPath, state);

        Assert.Equal(text, SessionFile.Read(SessionPath).State!.Documents[0].UnsavedText);
    }

    [Fact]
    public void NoFile_IsNoSession_AndNoFailure()
    {
        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.State);
        Assert.Null(result.Failure);
        Assert.Null(result.QuarantinedFilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"version\":2,\"layout\":{\"pane\":0},\"activePane\":0,\"panes\":[{\"id\":0,\"tabs\":[],\"activeTab\":-1}],\"documents\":[]}")]
    [InlineData("{\"version\":1,\"layout\":{\"axis\":\"Rows\"},\"activePane\":0,\"panes\":[],\"documents\":[]}")]
    [InlineData("{\"version\":1,\"layout\":{\"pane\":0},\"activePane\":0,\"panes\":[{\"id\":0,\"tabs\":null,\"activeTab\":-1}],\"documents\":[]}")]
    [InlineData("{\"version\":1,\"layout\":{\"pane\":0},\"activePane\":0,\"panes\":[{\"id\":0,\"tabs\":[],\"activeTab\":-1}],\"documents\":[{\"untitledName\":\"Untitled-1\",\"lineEndings\":\"Sideways\"}]}")]
    public void AFileThatIsNotASession_IsMovedAside(string json)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(SessionPath, json);

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.State);
        Assert.NotNull(result.Failure);
        Assert.Equal(QuarantinedPath, result.QuarantinedFilePath);
        Assert.False(File.Exists(SessionPath));
        Assert.Equal(json, File.ReadAllText(QuarantinedPath));
    }

    public static TheoryData<string, SessionState> InvalidSessions => new()
    {
        { "a pane the layout does not have", Valid() with { Panes = [.. Valid().Panes, new SessionPane(7, [], -1)] } },
        { "a leaf with no pane", Valid() with { Panes = [Valid().Panes[0]] } },
        { "a pane twice", Valid() with { Panes = [Valid().Panes[0], Valid().Panes[0]] } },
        { "an active pane not in the layout", Valid() with { ActivePane = 9 } },
        { "a tab showing a missing buffer", Valid() with { Panes = [Valid().Panes[0], new SessionPane(2, [new SessionTab(3)], 0)] } },
        { "an active tab out of range", Valid() with { Panes = [Valid().Panes[0], new SessionPane(2, [new SessionTab(0)], 1)] } },
        { "an empty pane showing a tab", Valid() with { Panes = [Valid().Panes[0], new SessionPane(2, [], 0)] } },
        { "a negative caret", Valid() with { Panes = [Valid().Panes[0], new SessionPane(2, [new SessionTab(0, CaretOffset: -1)], 0)] } },
        { "a negative scroll", Valid() with { Panes = [Valid().Panes[0], new SessionPane(2, [new SessionTab(0, VerticalScrollOffset: -5)], 0)] } },
        { "a blank untitled name", Valid() with { Documents = [Valid().Documents[0], new SessionDocument(null, " ")] } },
        { "a relative path", Valid() with { Documents = [Valid().Documents[0], new SessionDocument("notes.txt", "Untitled-2")] } },
        { "a hex view with no file", Valid() with { Documents = [Valid().Documents[0], new SessionDocument(null, "Untitled-2", IsHex: true)] } },
        { "a hex view with text", Valid() with { Documents = [Valid().Documents[0], new SessionDocument(FullPath("b.bin"), "Untitled-2", IsHex: true, UnsavedText: "x")] } },
        { "no indentation", Valid() with { Documents = [Valid().Documents[0], new SessionDocument(null, "Untitled-2", IndentSize: 0)] } },
        { "no code page", Valid() with { Documents = [Valid().Documents[0], new SessionDocument(null, "Untitled-2", CodePage: 0)] } },
        { "a negative pane id", Valid() with { Layout = new LayoutSnapshot(Pane: -1), ActivePane = -1, Panes = [new SessionPane(-1, [], -1)] } },
    };

    [Theory]
    [MemberData(nameof(InvalidSessions))]
    public void ASessionARestoreCouldNotUse_IsMovedAside(string why, SessionState state)
    {
        SessionFile.Write(SessionPath, state);

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.True(result.State is null, why);
        Assert.IsType<InvalidDataException>(result.Failure);
        Assert.True(File.Exists(QuarantinedPath), why);
        Assert.False(File.Exists(SessionPath), why);
    }

    [Fact]
    public void TheValidSessionTheInvalidOnesAreMadeFrom_Loads()
    {
        SessionFile.Write(SessionPath, Valid());

        Assert.NotNull(SessionFile.Read(SessionPath).State);
    }

    [Fact]
    public void AMovedAsideFile_ReplacesAnOlderOne()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(QuarantinedPath, "older");
        File.WriteAllText(SessionPath, "newer");

        SessionFile.Read(SessionPath);

        Assert.Equal("newer", File.ReadAllText(QuarantinedPath));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp folder is not worth failing a green test over.
        }
    }

    // Two panes side by side, the second shown; one file in both, an untitled buffer with text, a hex view.
    private static SessionState TwoPanes()
        => new(
            SessionState.CurrentVersion,
            new LayoutSnapshot(Axis: SplitAxis.Columns, Ratio: 0.3, First: new LayoutSnapshot(Pane: 1), Second: new LayoutSnapshot(Pane: 2)),
            2,
            [
                new SessionPane(1, [new SessionTab(0, 12, 40.5, 3), new SessionTab(2, HexRow: 7)], 1),
                new SessionPane(2, [new SessionTab(0, 3), new SessionTab(1)], 0),
            ],
            [
                new SessionDocument(FullPath("a.txt"), "Untitled-1", CodePage: TextEncodings.Utf16LeCodePage, ByteOrderMark: true, LineEndings: LineEndingStyle.CrLf, IndentWithTabs: true, IndentSize: 8, Language: "csharp", UnsavedText: "changed\n"),
                new SessionDocument(null, "Untitled-2", UnsavedText: "draft"),
                new SessionDocument(FullPath("b.bin"), "Untitled-3", IsHex: true),
            ]);

    private static SessionState OnePane(SessionDocument document)
        => new(SessionState.CurrentVersion, new LayoutSnapshot(Pane: 0), 0, [new SessionPane(0, [new SessionTab(0)], 0)], [document]);

    // Two panes stacked, a file shown in the first; the second empty.
    private static SessionState Valid()
        => new(
            SessionState.CurrentVersion,
            new LayoutSnapshot(Axis: SplitAxis.Rows, Ratio: 0.5, First: new LayoutSnapshot(Pane: 1), Second: new LayoutSnapshot(Pane: 2)),
            1,
            [new SessionPane(1, [new SessionTab(0)], 0), new SessionPane(2, [], -1)],
            [new SessionDocument(FullPath("a.txt"), "Untitled-1")]);
}

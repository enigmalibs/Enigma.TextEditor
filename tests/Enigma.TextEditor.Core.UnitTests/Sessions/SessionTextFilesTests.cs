using System;
using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Sessions;

/// <summary>
/// Format version 2: each buffer's unsaved text in a file of its own beside the session — written before the session
/// that names it, cleaned up after it, read back into the buffer, never named outside its folder — and version 1, with
/// the text inline, still read.
/// </summary>
public sealed class SessionTextFilesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Enigma.TextEditor.Tests", Guid.NewGuid().ToString("N"));

    private string SessionPath => Path.Combine(_root, AppPaths.SessionFileName);

    private string Unsaved => SessionFile.UnsavedFolderFor(SessionPath);

    [Fact]
    public void AnUnsavedText_IsWrittenBesideTheSession_AndReadBackIntoItsBuffer()
    {
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "first line\nsecond")]);

        Assert.Equal("first line\nsecond", File.ReadAllText(Path.Combine(Unsaved, "a-1.txt")));
        Assert.DoesNotContain("first line", File.ReadAllText(SessionPath), StringComparison.Ordinal);

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.Failure);
        Assert.Empty(result.MissingTexts);
        SessionDocument document = Assert.Single(result.State!.Documents);
        Assert.Equal("first line\nsecond", document.UnsavedText);
        Assert.Null(document.UnsavedTextFile);
        Assert.Equal(SessionState.CurrentVersion, result.State.Version);
    }

    [Fact]
    public void AVersion1Session_WithItsTextInline_IsStillRead()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(
            SessionPath,
            "{\"version\":1,\"layout\":{\"pane\":0},\"activePane\":0,\"panes\":[{\"id\":0,\"tabs\":[{\"document\":0}],\"activeTab\":0}],"
            + "\"documents\":[{\"untitledName\":\"Untitled-1\",\"unsavedText\":\"kept inline\"}]}");

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.Failure);
        Assert.Equal("kept inline", Assert.Single(result.State!.Documents).UnsavedText);
    }

    [Fact]
    public void TheTextsTheNewSessionNoLongerNames_AreDeletedAfterIt_AndTheOthersKept()
    {
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "a")]);

        SessionFile.Write(SessionPath, Untitled("b-1.txt"), [Text("b-1.txt", "b")]);

        Assert.Equal(["b-1.txt"], Directory.GetFiles(Unsaved).Select(Path.GetFileName));
        Assert.Equal("b", Assert.Single(SessionFile.Read(SessionPath).State!.Documents).UnsavedText);
    }

    [Fact]
    public void ATextAlreadyOnTheDisk_IsNotWrittenAgain()
    {
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "a")]);
        int writes = 0;

        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [new SessionText("a-1.txt", writer => { writes++; writer.Write("other"); })]);

        Assert.Equal(0, writes);
        Assert.Equal("a", File.ReadAllText(Path.Combine(Unsaved, "a-1.txt")));
    }

    [Fact]
    public void ATextWrittenButNotYetNamed_LeavesTheOldSessionWhole()
    {
        // A crash between the new text and the session naming it: the disk holds the old session and an extra file.
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "old")]);
        File.WriteAllText(Path.Combine(Unsaved, "a-2.txt"), "new");

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Equal("old", Assert.Single(result.State!.Documents).UnsavedText);

        // The next write cleans the stray up.
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), []);
        Assert.Equal(["a-1.txt"], Directory.GetFiles(Unsaved).Select(Path.GetFileName));
    }

    [Fact]
    public void ATextThatHasGone_IsNamed_AndItsBufferComesBackWithoutIt()
    {
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "a")]);
        File.Delete(Path.Combine(Unsaved, "a-1.txt"));

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.Failure);
        Assert.Equal(["a-1.txt"], result.MissingTexts);
        SessionDocument document = Assert.Single(result.State!.Documents);
        Assert.Null(document.UnsavedText);
        Assert.Null(document.UnsavedTextFile);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData(@"..\secret.txt")]
    [InlineData("/etc/passwd")]
    [InlineData(@"C:\Windows\win.ini")]
    [InlineData("a.json")]
    [InlineData("")]
    public void ASessionNamingATextOutsideItsFolder_IsSetAside_WithItsTexts(string name)
    {
        Directory.CreateDirectory(Unsaved);
        File.WriteAllText(Path.Combine(Unsaved, "kept-1.txt"), "kept");
        File.WriteAllText(
            SessionPath,
            "{\"version\":2,\"layout\":{\"pane\":0},\"activePane\":0,\"panes\":[{\"id\":0,\"tabs\":[{\"document\":0}],\"activeTab\":0}],"
            + "\"documents\":[{\"untitledName\":\"Untitled-1\",\"unsavedTextFile\":" + System.Text.Json.JsonSerializer.Serialize(name) + "}]}");

        SessionLoadResult result = SessionFile.Read(SessionPath);

        Assert.Null(result.State);
        Assert.IsType<InvalidDataException>(result.Failure);
        Assert.Equal("kept", File.ReadAllText(Path.Combine(_root, AppPaths.QuarantinedUnsavedFolderName, "kept-1.txt")));
        Assert.False(Directory.Exists(Unsaved));
    }

    [Fact]
    public void ATextWithANameNoSessionMayHave_IsRefused_AndNothingIsWritten()
    {
        Assert.Throws<ArgumentException>(() => SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("../a-1.txt", "a")]));

        Assert.False(File.Exists(SessionPath));
    }

    [Fact]
    public void ATextThatCannotBeEncoded_FailsTheWrite_AndLeavesThePreviousSession()
    {
        SessionFile.Write(SessionPath, Untitled("a-1.txt"), [Text("a-1.txt", "a")]);

        Assert.ThrowsAny<ArgumentException>(() => SessionFile.Write(SessionPath, Untitled("b-1.txt"), [Text("b-1.txt", "\ud800 lone")]));

        Assert.Equal("a", Assert.Single(SessionFile.Read(SessionPath).State!.Documents).UnsavedText);
        Assert.Equal(["a-1.txt"], Directory.GetFiles(Unsaved).Select(Path.GetFileName));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // Never created.
        }
    }

    private static SessionText Text(string name, string text) => new(name, writer => writer.Write(text));

    private static SessionState Untitled(string textFile)
        => new(
            SessionState.CurrentVersion,
            new LayoutSnapshot(Pane: 0),
            0,
            [new SessionPane(0, [new SessionTab(0)], 0)],
            [new SessionDocument(null, "Untitled-1", UnsavedTextFile: textFile)]);
}

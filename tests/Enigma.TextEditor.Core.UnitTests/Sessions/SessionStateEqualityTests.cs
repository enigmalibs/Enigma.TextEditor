using System.IO;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Sessions;

/// <summary>
/// Two sessions are equal when writing either would write the same file: what tells the autosave that nothing changed.
/// </summary>
public sealed class SessionStateEqualityTests
{
    [Fact]
    public void TwoCapturesOfTheSameEditor_AreEqual_AndHashAlike()
    {
        SessionState first = Capture();
        SessionState second = Capture();

        Assert.NotSame(first.Panes, second.Panes);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    public static TheoryData<string, SessionState> Changes => new()
    {
        { "a caret", Capture() with { Panes = [Capture().Panes[0], new SessionPane(2, [new SessionTab(0, 4)], 0)] } },
        { "a scroll", Capture() with { Panes = [Capture().Panes[0], new SessionPane(2, [new SessionTab(0, 3, 10)], 0)] } },
        { "the tab shown", Capture() with { Panes = [new SessionPane(1, [new SessionTab(0), new SessionTab(1)], 0), Capture().Panes[1]] } },
        { "the tab order", Capture() with { Panes = [new SessionPane(1, [new SessionTab(1), new SessionTab(0)], 1), Capture().Panes[1]] } },
        { "a tab closed", Capture() with { Panes = [new SessionPane(1, [new SessionTab(0)], 0), Capture().Panes[1]] } },
        { "the active pane", Capture() with { ActivePane = 1 } },
        { "a ratio", Capture() with { Layout = Capture().Layout with { Ratio = 0.4 } } },
        { "unsaved text", Capture() with { Documents = [Capture().Documents[0], new SessionDocument(null, "Untitled-1", UnsavedText: "drafts")] } },
        { "a buffer opened", Capture() with { Documents = [.. Capture().Documents, new SessionDocument(Full("c.txt"), "Untitled-2")] } },
        { "the explorer's folder", Capture() with { ExplorerFolder = Full("other") } },
        { "the explorer hidden", Capture() with { ExplorerVisible = false } },
    };

    [Theory]
    [MemberData(nameof(Changes))]
    public void ASessionThatDiffersInAnything_IsNotEqual(string what, SessionState changed)
        => Assert.False(Capture().Equals(changed), what);

    [Fact]
    public void ASession_IsNotEqualToNothing()
        => Assert.False(Capture().Equals(null));

    // Built anew on every call, lists included, as the editor captures.
    private static SessionState Capture()
        => new(
            SessionState.CurrentVersion,
            new LayoutSnapshot(Axis: SplitAxis.Columns, Ratio: 0.5, First: new LayoutSnapshot(Pane: 1), Second: new LayoutSnapshot(Pane: 2)),
            2,
            [
                new SessionPane(1, [new SessionTab(0), new SessionTab(1)], 1),
                new SessionPane(2, [new SessionTab(0, 3)], 0),
            ],
            [
                new SessionDocument(Full("a.txt"), "Untitled-3", UnsavedText: new string('x', 3)),
                new SessionDocument(null, "Untitled-1", UnsavedText: "draft"),
            ],
            Full("project"),
            ExplorerVisible: true);

    private static string Full(string name) => Path.Combine(Path.GetTempPath(), name);
}

using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// <see cref="VimEngine.Interrupt"/>: what the engine lets go of when something outside vim — the
/// editor's find panel — takes the keyboard (BUG-B026).
/// </summary>
public sealed class VimInterruptTests
{
    [Fact]
    public void AHalfTypedOperator_IsDropped()
    {
        VimRun run = VimScript.Run("a|bc def", "d");

        run.Engine.Interrupt();
        Feed(run, "w");

        // 'w' alone, a move: the 'd' typed before the panel opened is not waiting for it any more.
        Assert.Equal("abc |def", VimScript.Render(run.Buffer));
        Assert.False(run.Engine.HasPendingSequence);
    }

    [Fact]
    public void AHalfTypedCount_IsDropped()
    {
        VimRun run = VimScript.Run("|abcdef", "3");

        run.Engine.Interrupt();
        Feed(run, "x");

        Assert.Equal("|bcdef", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void AnOpenCommandLine_IsCancelled_WithoutRunning()
    {
        VimRun run = VimScript.Run("|abc", ":q");

        run.Engine.Interrupt();

        Assert.Null(run.Engine.CommandLine);
        Assert.Null(run.Buffer.CommandLine);
        Assert.Empty(run.ExRequests);
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Fact]
    public void AnOpenSearchLine_IsCancelled_AndItsPreviewGoes()
    {
        VimRun run = VimScript.Run("|abc abc", "/ab");

        run.Engine.Interrupt();

        Assert.Null(run.Engine.CommandLine);
        Assert.Null(run.Engine.IncSearch);
        Assert.Equal("|abc abc", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void AWindowPrefix_IsDropped()
    {
        VimRun run = VimScript.Run("|abc", "<C-w>");

        run.Engine.Interrupt();
        Feed(run, "v");

        // 'v' selects rather than splitting the window.
        Assert.Empty(run.WindowCommands);
        Assert.Equal(VimMode.Visual, run.Engine.Mode);
    }

    [Theory]
    [InlineData("vl")]
    [InlineData("V")]
    public void AVisualMode_IsLeft_AndItsSelectionRememberedForGv(string keys)
    {
        VimRun run = VimScript.Run("a|bcd", keys);

        run.Engine.Interrupt();

        Assert.Equal(VimMode.Normal, run.Engine.Mode);
        Assert.Null(run.Buffer.Selection);

        Feed(run, "gv");
        Assert.NotEqual(VimMode.Normal, run.Engine.Mode);
        Assert.NotNull(run.Buffer.Selection);
    }

    [Fact]
    public void InsertMode_IsKept_WithItsUndoGroup()
    {
        VimRun run = VimScript.Run("a|bc", "iX");

        run.Engine.Interrupt();

        Assert.Equal(VimMode.Insert, run.Engine.Mode);
        Assert.Equal(1, run.Buffer.OpenUndoGroups);

        // The session goes on: Esc then u takes back what it typed, before the interrupt and after.
        run.Buffer.Type('Y');
        Feed(run, "<Esc>u");
        Assert.Equal("abc", run.Buffer.Text);
    }

    [Fact]
    public void NormalModeWithNothingUnderWay_IsLeftAsItIs()
    {
        VimRun run = VimScript.Run("a|bc", "");

        run.Engine.Interrupt();

        Assert.Equal("a|bc", VimScript.Render(run.Buffer));
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    private static void Feed(VimRun run, string keys)
    {
        foreach (VimKey key in VimKeyNotation.Parse(keys))
        {
            run.Engine.HandleKey(key);
        }
    }
}

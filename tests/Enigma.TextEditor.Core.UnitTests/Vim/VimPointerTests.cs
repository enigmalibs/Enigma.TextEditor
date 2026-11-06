using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// What the engine makes of a pointer gesture the view reports once the editor has carried it out
/// (BUG-47C2): <see cref="VimEngine.PlaceCaret"/> for a click.
/// </summary>
public sealed class VimPointerTests
{
    [Fact]
    public void AClickPastTheEndOfALine_PutsTheBlockOnItsLastCharacter()
    {
        VimRun run = VimScript.Run("abc|\ndef", "");

        run.Engine.PlaceCaret();

        Assert.Equal("ab|c\ndef", VimScript.Render(run.Buffer));
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Fact]
    public void XAfterTheClick_DeletesTheCharacterTheBlockIsOn()
    {
        VimRun run = VimScript.Run("abc|\ndef", "");

        run.Engine.PlaceCaret();
        Feed(run, "x");

        Assert.Equal("a|b\ndef", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void AClickOnAnEmptyLine_StaysAtItsStart()
    {
        VimRun run = VimScript.Run("abc\n|\ndef", "");

        run.Engine.PlaceCaret();

        Assert.Equal("abc\n|\ndef", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void AClickInsideTheLine_LeavesTheCaretWhereItLanded()
    {
        VimRun run = VimScript.Run("a|bc\ndef", "");

        run.Engine.PlaceCaret();

        Assert.Equal("a|bc\ndef", VimScript.Render(run.Buffer));
    }

    [Theory]
    [InlineData("vl")]
    [InlineData("V")]
    public void AClickInAVisualMode_EndsIt_AndGvStillSelectsItAgain(string keys)
    {
        VimRun run = VimScript.Run("a|bcd\nefgh", keys);

        // What the editor does with the click before the engine hears of it.
        run.Buffer.ClearSelection();
        run.Buffer.CaretOffset = 7;
        run.Engine.PlaceCaret();

        Assert.Equal(VimMode.Normal, run.Engine.Mode);
        Assert.Null(run.Buffer.Selection);
        Assert.Equal("abcd\nef|gh", VimScript.Render(run.Buffer));

        Feed(run, "gv");
        Assert.NotEqual(VimMode.Normal, run.Engine.Mode);
    }

    [Fact]
    public void AClick_MakesTheClickedColumnTheOneJKeepsTo()
    {
        // '$' makes j stick to the ends of lines; a click somewhere else must end that.
        VimRun run = VimScript.Run("|abcdef\nabcdef\nabcdef", "$");

        run.Buffer.CaretOffset = 9;
        run.Engine.PlaceCaret();
        Feed(run, "j");

        Assert.Equal("abcdef\nabcdef\nab|cdef", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void InInsertMode_TheCaretStaysAfterTheLastCharacter()
    {
        VimRun run = VimScript.Run("a|bc\ndef", "A");

        run.Engine.PlaceCaret();

        Assert.Equal("abc|\ndef", VimScript.Render(run.Buffer));
        Assert.Equal(VimMode.Insert, run.Engine.Mode);
    }

    [Fact]
    public void WithTheCommandLineOpen_NothingChanges()
    {
        VimRun run = VimScript.Run("|abc\ndef", ":");

        run.Buffer.CaretOffset = 3;
        run.Engine.PlaceCaret();

        Assert.Equal(3, run.Buffer.CaretOffset);
        Assert.Equal(":", run.Engine.CommandLine);
    }

    private static void Feed(VimRun run, string keys)
    {
        foreach (VimKey key in VimKeyNotation.Parse(keys))
        {
            run.Engine.HandleKey(key);
        }
    }
}

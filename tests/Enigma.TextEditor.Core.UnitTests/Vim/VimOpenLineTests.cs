using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// <c>o</c> and <c>O</c> over an indented line: the indentation carried onto the new line, the way
/// the editor's own Enter carries it in insert mode.
/// </summary>
/// <remarks>
/// Ported from Enigma.MarkdownEditor, where these cases carried a markdown list marker instead. A text
/// editor has no list items, so a <c>- </c> line is just text and only its leading whitespace is
/// carried.
/// </remarks>
public sealed class VimOpenLineTests
{
    [Theory]
    [InlineData("    in|dented", "    indented\n    |")]
    [InlineData("\tta|bbed", "\ttabbed\n\t|")]
    [InlineData(" \t mi|xed", " \t mixed\n \t |")]
    [InlineData("  - nes|ted", "  - nested\n  |")]
    public void O_OpensBelowAtTheSameIndentation(string before, string after)
    {
        VimRun run = VimScript.Run(before, "o");

        Assert.Equal(after, run.Text);
        Assert.Equal(VimMode.Insert, run.Mode);
    }

    [Theory]
    [InlineData("    in|dented", "    |\n    indented")]
    [InlineData("\tta|bbed", "\t|\n\ttabbed")]
    public void CapitalO_OpensAboveAtTheSameIndentation(string before, string after)
    {
        VimRun run = VimScript.Run(before, "O");

        Assert.Equal(after, run.Text);
        Assert.Equal(VimMode.Insert, run.Mode);
    }

    [Theory]
    [InlineData("# a head|ing", "o", "# a heading\n|")]
    [InlineData("# a head|ing", "O", "|\n# a heading")]
    [InlineData("just a l|ine", "o", "just a line\n|")]
    [InlineData("- it|em", "o", "- item\n|")]
    [InlineData("1. it|em", "O", "|\n1. item")]
    [InlineData("|", "o", "\n|")]
    public void ALineWithNoIndentation_OpensPlain(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Fact]
    public void AWhitespaceOnlyLine_IsCarriedWhole()
        => Assert.Equal("  \n  |", VimScript.Run("  |", "o").Text);

    [Theory]
    [InlineData("  it|em", "oX", "  item\n  X|")]
    [InlineData("  it|em", "OX", "  X|\n  item")]
    [InlineData("  it|em", "oX<Esc>", "  item\n  |X")]
    public void WhatIsTypedNext_LandsAfterTheIndentation(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Fact]
    public void ACount_WritesTheIndentationOncePerLine()
    {
        // The indentation is re-derived from the line just written, so three copies come out exactly
        // as three separate 'o's would.
        Assert.Equal(
            "  item\n  x\n  x\n  |x",
            VimScript.Run("  it|em", "3ox<Esc>l").Text);
    }

    [Fact]
    public void ACountOverTabs_RepeatsTheTabs()
        => Assert.Equal("\titem\n\tx\n\t|x", VimScript.Run("\tit|em", "2ox<Esc>l").Text);

    [Fact]
    public void Dot_OpensAnotherIndentedLine()
        => Assert.Equal("  item\n  x\n  |x", VimScript.Run("  it|em", "ox<Esc>.l").Text);

    [Fact]
    public void OneUndo_TakesTheIndentationBackWithWhatWasTypedOnIt()
    {
        // The indentation is written inside the insert session's undo group, so a single 'u' cannot
        // leave an orphan line of whitespace behind.
        VimRun run = VimScript.Run("  it|em", "oX<Esc>u");

        Assert.Equal("  it|em", run.Text);
        Assert.Equal(1, run.Buffer.UndoGroupsOpened);
    }

    [Fact]
    public void OpeningAnIndentedLine_IsStillOneWriteToTheBuffer()
    {
        // The engine's rule: one command, one Replace, one undo step. The indentation rides in the
        // same write as the line break rather than following it.
        Assert.Equal(1, VimScript.Run("  it|em", "o").Buffer.Replacements);
    }

    [Fact]
    public void TheIndentationCarriesNoTerminatorOfItsOwn()
    {
        // A CRLF buffer keeps CRLF, and the indentation does not smuggle a line feed in behind it.
        FakeVimBuffer buffer = new("  item") { NewLine = "\r\n", CaretOffset = 3 };
        VimEngine engine = new(buffer);

        foreach (VimKey key in VimKeyNotation.Parse("o"))
        {
            engine.HandleKey(key);
        }

        Assert.Equal("  item\r\n  ", buffer.Text);
    }
}

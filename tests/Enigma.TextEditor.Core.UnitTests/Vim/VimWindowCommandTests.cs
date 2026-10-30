using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// <c>Ctrl+W</c> and the key after it: the window commands the engine asks the application for, and
/// the panes-wide state every engine shares.
/// </summary>
public sealed class VimWindowCommandTests
{
    [Theory]
    [InlineData("<C-w>h", VimWindowCommand.FocusLeft)]
    [InlineData("<C-w>j", VimWindowCommand.FocusDown)]
    [InlineData("<C-w>k", VimWindowCommand.FocusUp)]
    [InlineData("<C-w>l", VimWindowCommand.FocusRight)]
    [InlineData("<C-w><Left>", VimWindowCommand.FocusLeft)]
    [InlineData("<C-w><Down>", VimWindowCommand.FocusDown)]
    [InlineData("<C-w><Up>", VimWindowCommand.FocusUp)]
    [InlineData("<C-w><Right>", VimWindowCommand.FocusRight)]
    [InlineData("<C-w><C-j>", VimWindowCommand.FocusDown)]
    [InlineData("<C-w>w", VimWindowCommand.FocusNext)]
    [InlineData("<C-w><C-w>", VimWindowCommand.FocusNext)]
    [InlineData("<C-w>W", VimWindowCommand.FocusPrevious)]
    [InlineData("<C-w>s", VimWindowCommand.SplitHorizontal)]
    [InlineData("<C-w>S", VimWindowCommand.SplitHorizontal)]
    [InlineData("<C-w>v", VimWindowCommand.SplitVertical)]
    [InlineData("<C-w>c", VimWindowCommand.Close)]
    [InlineData("<C-w>q", VimWindowCommand.Quit)]
    [InlineData("<C-w>o", VimWindowCommand.Only)]
    [InlineData("<C-w>=", VimWindowCommand.Equalize)]
    [InlineData("<C-w>+", VimWindowCommand.Taller)]
    [InlineData("<C-w>-", VimWindowCommand.Shorter)]
    [InlineData("<C-w>>", VimWindowCommand.Wider)]
    [InlineData("<C-w><lt>", VimWindowCommand.Narrower)]
    public void CtrlW_ThenAKey_AsksForThatWindowCommand(string keys, VimWindowCommand expected)
    {
        VimRun run = VimScript.Run("a|bc", keys);

        Assert.Equal(expected, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc", run.Text);
        Assert.False(run.Engine.HasPendingSequence);
    }

    [Theory]
    [InlineData("gh", VimWindowCommand.FocusLeft)]
    [InlineData("gj", VimWindowCommand.FocusDown)]
    [InlineData("gk", VimWindowCommand.FocusUp)]
    [InlineData("gl", VimWindowCommand.FocusRight)]
    [InlineData("3gl", VimWindowCommand.FocusRight)]
    public void GThenADirection_AsksForThePaneThere_AndLeavesTheTextAlone(string keys, VimWindowCommand expected)
    {
        VimRun run = VimScript.Run("a|bc\ndef", keys);

        Assert.Equal(expected, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc\ndef", run.Text);
        Assert.False(run.Engine.HasPendingSequence);
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Theory]
    [InlineData("g=", VimWindowCommand.Equalize)]
    [InlineData("g+", VimWindowCommand.Taller)]
    [InlineData("g-", VimWindowCommand.Shorter)]
    [InlineData("g>", VimWindowCommand.Wider)]
    [InlineData("g<", VimWindowCommand.Narrower)]
    [InlineData("4g>", VimWindowCommand.Wider)]
    public void GThenASizeKey_AsksForWhatCtrlWAndThatKeyDo_AndLeavesTheTextAlone(string keys, VimWindowCommand expected)
    {
        VimRun run = VimScript.Run("a|bc\ndef", keys);

        Assert.Equal(expected, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc\ndef", run.Text);
        Assert.False(run.Engine.HasPendingSequence);
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Theory]
    [InlineData("<Space>rh", VimWindowCommand.MoveSplitterLeft)]
    [InlineData("<Space>rj", VimWindowCommand.MoveSplitterDown)]
    [InlineData("<Space>rk", VimWindowCommand.MoveSplitterUp)]
    [InlineData("<Space>rl", VimWindowCommand.MoveSplitterRight)]
    [InlineData("3<Space>rk", VimWindowCommand.MoveSplitterUp)]
    public void TheLeaderThenRAndADirection_AsksToMoveTheSplitter_AndLeavesTheTextAlone(string keys, VimWindowCommand expected)
    {
        VimRun run = VimScript.Run("a|bc\ndef", keys);

        Assert.Equal(expected, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc\ndef", run.Text);
        Assert.False(run.Engine.HasPendingSequence);
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Fact]
    public void TheSplitterKeys_WorkInVisualMode_AndAreNotWhatDotRepeats()
    {
        VimRun visual = VimScript.Run("a|bc", "v<Space>rl");
        Assert.Equal(VimWindowCommand.MoveSplitterRight, Assert.Single(visual.WindowCommands));
        Assert.Equal(VimMode.Visual, visual.Engine.Mode);

        VimRun run = VimScript.Run("a|bc", "x<Space>rk.");
        Assert.Equal("|a", run.Text);
        Assert.Equal(VimWindowCommand.MoveSplitterUp, Assert.Single(run.WindowCommands));
    }

    [Theory]
    [InlineData("<Space>mh", VimWindowCommand.MoveTabLeft)]
    [InlineData("<Space>mj", VimWindowCommand.MoveTabDown)]
    [InlineData("<Space>mk", VimWindowCommand.MoveTabUp)]
    [InlineData("<Space>ml", VimWindowCommand.MoveTabRight)]
    [InlineData("3<Space>ml", VimWindowCommand.MoveTabRight)]
    public void TheLeaderThenMAndADirection_AsksToMoveTheTab_AndLeavesTheTextAlone(string keys, VimWindowCommand expected)
    {
        VimRun run = VimScript.Run("a|bc\ndef", keys);

        Assert.Equal(expected, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc\ndef", run.Text);
        Assert.False(run.Engine.HasPendingSequence);
        Assert.Equal(VimMode.Normal, run.Engine.Mode);
    }

    [Fact]
    public void TheTabKeys_WorkInVisualMode_AndAreNotWhatDotRepeats()
    {
        VimRun visual = VimScript.Run("a|bc", "v<Space>mh");
        Assert.Equal(VimWindowCommand.MoveTabLeft, Assert.Single(visual.WindowCommands));

        VimRun run = VimScript.Run("a|bc", "x<Space>mj.");
        Assert.Equal("|a", run.Text);
        Assert.Equal(VimWindowCommand.MoveTabDown, Assert.Single(run.WindowCommands));
    }

    [Theory]
    [InlineData("gt", VimExCommand.BufferNext)]
    [InlineData("gT", VimExCommand.BufferPrevious)]
    [InlineData("3gt", VimExCommand.BufferNext)]
    public void GTAndGCapitalT_AskForTheNextAndPreviousTab_AndLeaveTheTextAlone(string keys, VimExCommand expected)
    {
        VimRun run = VimScript.Run("a|bc\ndef", keys);

        Assert.Equal(new VimExRequest(expected), Assert.Single(run.ExRequests));
        Assert.Equal("a|bc\ndef", run.Text);
        Assert.Empty(run.WindowCommands);
    }

    [Fact]
    public void GT_WorksInVisualMode_AndIsNotWhatDotRepeats()
    {
        Assert.Equal(VimExCommand.BufferNext, Assert.Single(VimScript.Run("a|bc", "vgt").ExCommands));

        VimRun run = VimScript.Run("a|bc", "xgt.");
        Assert.Equal("|a", run.Text);
        Assert.Equal(VimExCommand.BufferNext, Assert.Single(run.ExCommands));
    }

    [Fact]
    public void GThenADirection_WorksInVisualMode()
    {
        VimRun run = VimScript.Run("a|bc", "vgl");

        Assert.Equal(VimWindowCommand.FocusRight, Assert.Single(run.WindowCommands));
        Assert.Equal(VimMode.Visual, run.Engine.Mode);
    }

    [Fact]
    public void GThenADirection_IsNotWhatDotRepeats()
    {
        VimRun run = VimScript.Run("a|bc", "xgl.");

        Assert.Equal("|a", run.Text);
        Assert.Equal(VimWindowCommand.FocusRight, Assert.Single(run.WindowCommands));
    }

    [Fact]
    public void CtrlW_OnItsOwn_IsPending()
    {
        VimRun run = VimScript.Run("a|bc", "<C-w>");

        Assert.True(run.Engine.HasPendingSequence);
        Assert.Empty(run.WindowCommands);
    }

    [Theory]
    [InlineData("<C-w>x")]
    [InlineData("<C-w>z")]
    [InlineData("<C-w><Esc>")]
    public void AKeyThatNamesNoWindowCommand_CancelsAndIsNotACommandEither(string keys)
    {
        // 'x' after the prefix would delete a character if it fell through to the grammar.
        VimRun run = VimScript.Run("a|bc", keys + "l");

        Assert.Empty(run.WindowCommands);
        Assert.Equal("ab|c", run.Text);
    }

    [Fact]
    public void CtrlW_DropsAHalfTypedCommand()
    {
        VimRun run = VimScript.Run("a|bc", "d<C-w>v");

        Assert.Equal(VimWindowCommand.SplitVertical, Assert.Single(run.WindowCommands));
        Assert.Equal("a|bc", run.Text);
    }

    [Fact]
    public void CtrlW_WorksInVisualMode()
        => Assert.Equal(VimWindowCommand.FocusNext, Assert.Single(VimScript.Run("a|bc", "v<C-w>w").WindowCommands));

    [Fact]
    public void CtrlW_IsNotTakenInInsertMode()
    {
        VimRun run = VimScript.Run("a|bc", "i");

        Assert.False(run.Engine.HandleKey(VimKey.Typed('w', VimModifiers.Control)));
        Assert.False(run.Engine.HasPendingSequence);
    }

    [Theory]
    [InlineData(":sp<CR>", VimExCommand.Split, null)]
    [InlineData(":split notes.txt<CR>", VimExCommand.Split, "notes.txt")]
    [InlineData(":vs<CR>", VimExCommand.VerticalSplit, null)]
    [InlineData(":vsplit b.txt<CR>", VimExCommand.VerticalSplit, "b.txt")]
    [InlineData(":vsp<CR>", VimExCommand.VerticalSplit, null)]
    [InlineData(":spl b.txt<CR>", VimExCommand.Split, "b.txt")]
    [InlineData(":new<CR>", VimExCommand.New, null)]
    [InlineData(":vnew<CR>", VimExCommand.VerticalNew, null)]
    [InlineData(":on<CR>", VimExCommand.Only, null)]
    [InlineData(":only<CR>", VimExCommand.Only, null)]
    [InlineData(":clo<CR>", VimExCommand.Close, null)]
    [InlineData(":close<CR>", VimExCommand.Close, null)]
    public void TheWindowExCommands_AreAskedFor(string keys, VimExCommand command, string? argument)
        => Assert.Equal(new VimExRequest(command, argument), Assert.Single(VimScript.Run("|abc", keys).ExRequests));

    [Fact]
    public void AYankInOneEngine_IsPutByAnother()
    {
        VimSharedState shared = new();
        FakeVimBuffer first = VimScript.NewBuffer("|one\ntwo");
        FakeVimBuffer second = VimScript.NewBuffer("|three");
        VimEngine left = new(first, shared: shared);
        VimEngine right = new(second, shared: shared);

        Type(left, "yy");
        Type(right, "p");

        Assert.Equal("three\none", second.Text);
        Assert.Same(shared.Registers, right.Registers);
    }

    [Fact]
    public void ASearchInOneEngine_IsRepeatedByNInAnother()
    {
        VimSharedState shared = new();
        FakeVimBuffer first = VimScript.NewBuffer("|a needle");
        FakeVimBuffer second = VimScript.NewBuffer("|hay needle hay");
        VimEngine left = new(first, shared: shared);
        VimEngine right = new(second, shared: shared);

        Type(left, "/needle<CR>");
        Type(right, "n");

        Assert.Equal(4, second.CaretOffset);
    }

    [Fact]
    public void AFindInOneEngine_IsRepeatedBySemicolonInAnother()
    {
        VimSharedState shared = new();
        FakeVimBuffer first = VimScript.NewBuffer("|a,b");
        FakeVimBuffer second = VimScript.NewBuffer("|xx,y");
        VimEngine left = new(first, shared: shared);
        VimEngine right = new(second, shared: shared);

        Type(left, "f,");
        Type(right, ";");

        Assert.Equal(2, second.CaretOffset);
    }

    [Fact]
    public void EnginesBuiltWithoutSharedState_DoNotShare()
    {
        FakeVimBuffer first = VimScript.NewBuffer("|one");
        FakeVimBuffer second = VimScript.NewBuffer("|two");
        VimEngine left = new(first);
        VimEngine right = new(second);

        Type(left, "yy");

        Assert.Null(right.Register);
    }

    private static void Type(VimEngine engine, string keys)
    {
        foreach (VimKey key in VimKeyNotation.Parse(keys))
        {
            engine.HandleKey(key);
        }
    }
}

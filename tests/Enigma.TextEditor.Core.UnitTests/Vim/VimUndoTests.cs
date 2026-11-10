using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// <c>u</c> and <c>Ctrl+R</c>, and what one step costs.
/// </summary>
/// <remarks>
/// <para>
/// The engine keeps no history of its own — it asks the buffer, which is the editor's own undo stack,
/// so that <c>u</c> and <c>Ctrl+Z</c> take back the same step. What it does decide is how much goes
/// into a step, and it decides it in one place: every command that changes the buffer does it in
/// exactly one write, and the only thing that opens a group is an insert session — including the one
/// a change opens on its own behalf.
/// </para>
/// <para>
/// That last part is the acceptance criterion. <c>cc</c>, the sentence typed after it and the
/// <c>Esc</c> that ends it are one <c>u</c>, not three.
/// </para>
/// </remarks>
public sealed class VimUndoTests
{
    [Theory]
    [InlineData("a|bc", "xu", "a|bc")]
    [InlineData("a|bc\ndef", "ddu", "a|bc\ndef")]
    [InlineData("|foo bar", "dwu", "|foo bar")]
    [InlineData("a|bc", "rXu", "a|bc")]
    [InlineData("|abc", "3~u", "|abc")]
    [InlineData("a|bc", "iXY<Esc>u", "a|bc")]
    [InlineData("a|bc", "oXY<Esc>u", "a|bc")]
    public void U_TakesBackTheLastCommandWhole(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Theory]
    [InlineData("abc\nd|ef\nghi", "ccXYZ<Esc>u", "abc\nd|ef\nghi")]
    [InlineData("|foo bar", "cwqux<Esc>u", "|foo bar")]
    [InlineData("ab|cdef", "CXY<Esc>u", "ab|cdef")]
    public void AChangeAndWhatWasTypedInItsPlace_ComeBackInExactlyOneStep(
        string before,
        string keys,
        string after)
    {
        VimRun run = VimScript.Run(before, keys);

        Assert.Equal(after, run.Text);

        // Nothing left on the stack: the one step that was there has just been taken.
        Assert.Equal(0, run.Buffer.UndoSteps);
    }

    [Fact]
    public void ACommandThatChangesNothing_CostsNoStep()
        => Assert.Equal(0, VimScript.Run("a|bc\ndef", "yyGgg$0w").Buffer.UndoSteps);

    [Theory]
    [InlineData("|abcde", "xx", 2)]
    [InlineData("|abcde", "3x", 1)]
    [InlineData("|abcde", "xiQ<Esc>x", 3)]
    [InlineData("a|bc\ndef", "yy5p", 1)]
    public void EachCommandThatChangesSomething_IsOneStep(string before, string keys, int steps)
        => Assert.Equal(steps, VimScript.Run(before, keys).Buffer.UndoSteps);

    [Theory]
    [InlineData("|abcde", "xxu", "|bcde")]
    [InlineData("|abcde", "xx2u", "|abcde")]
    [InlineData("|abcde", "xx9u", "|abcde")]
    public void ACountOnU_TakesBackThatManySteps(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Theory]
    [InlineData("a|bc", "xu<C-r>", "a|c")]
    [InlineData("|abcde", "xxuu<C-r><C-r>", "|cde")]
    [InlineData("abc\nd|ef\nghi", "ccXYZ<Esc>u<C-r>", "abc\nXY|Z\nghi")]
    public void CtrlR_PutsBackWhatUTook(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Fact]
    public void UWithNothingToTakeBack_DoesNothing()
    {
        VimRun run = VimScript.Run("a|bc", "u");

        Assert.Equal("a|bc", run.Text);
        Assert.Equal(0, run.Buffer.UndoSteps);
    }

    [Fact]
    public void CtrlRWithNothingToPutBack_DoesNothing()
        => Assert.Equal("a|bc", VimScript.Run("a|bc", "<C-r>").Text);

    [Fact]
    public void CtrlR_TheUndoKeys_TheSixScrollKeysAndTheWindowPrefix_AreTheOnlyHeldKeysTheEngineClaims()
    {
        // Every other Control combination belongs to the window: the editor has to be free to see
        // Ctrl+S, or vim would have taken the application's own shortcuts away. Ctrl+W is the window
        // prefix here (FEATURE-04BC), as it is in vim — the panes are what it commands.
        VimRun run = VimScript.Run("a|bc", "x");

        Assert.True(run.Engine.HandleKey(VimKeyControl('r')));
        Assert.True(run.Engine.HandleKey(VimKeyControl('d')));
        Assert.True(run.Engine.HandleKey(VimKeyControl('u')));
        Assert.True(run.Engine.HandleKey(VimKeyControl('z')));
        Assert.False(run.Engine.HandleKey(VimKeyControl('s')));
        Assert.False(run.Engine.HandleKey(VimKeyControl('k')));
        Assert.True(run.Engine.HandleKey(VimKeyControl('w')));
    }

    [Fact]
    public void CtrlR_IsNotClaimedInInsertMode()
    {
        VimRun run = VimScript.Run("a|bc", "i");

        Assert.False(run.Engine.HandleKey(VimKeyControl('r')));
    }

    // BUG-375B: an insert session holds an undo group open until Esc, and the editor's undo refuses
    // to run inside one. The fake refuses too, so every case below would throw if the engine let the
    // undo reach the buffer with the group still open.
    [Theory]
    [InlineData("a|bc", "iXY<C-z>", "a|bc")]
    [InlineData("|abc", "xiQ<C-z>", "|bc")]
    [InlineData("|abc", "xiQ<C-z><C-z>", "|abc")]
    [InlineData("|abc", "i<C-z>", "|abc")]
    [InlineData("abc\nd|ef\nghi", "ccXYZ<C-z>", "abc\nd|ef\nghi")]
    public void CtrlZInInsertMode_TakesBackWhatWasTyped_AndStaysInInsertMode(string before, string keys, string after)
    {
        VimRun run = VimScript.Run(before, keys);

        Assert.Equal(after, run.Text);
        Assert.Equal(VimMode.Insert, run.Mode);

        // A new group is open for what comes next, and only that one.
        Assert.Equal(1, run.Buffer.OpenUndoGroups);
    }

    [Fact]
    public void AfterCtrlZInInsertMode_EscAndUTakeBackOnlyWhatWasTypedAfterIt()
    {
        VimRun run = VimScript.Run("|abc", "xiQ<C-z>R<Esc>");

        Assert.Equal("|Rbc", run.Text);
        Assert.Equal(VimMode.Normal, run.Mode);
        Assert.Equal(0, run.Buffer.OpenUndoGroups);

        Assert.Equal("|bc", VimScript.Run("|abc", "xiQ<C-z>R<Esc>u").Text);
    }

    [Theory]
    [InlineData("|abcde", "xui<C-y>", "|bcde")]
    [InlineData("|abcde", "xui<C-S-z>", "|bcde")]
    [InlineData("|abcde", "xui<C-Z>", "|bcde")]
    [InlineData("a|bc", "iXY<C-z><C-y>", "aXY|bc")]
    public void TheRedoKeysInInsertMode_PutBackWhatWasTaken_AndStayInInsertMode(string before, string keys, string after)
    {
        VimRun run = VimScript.Run(before, keys);

        Assert.Equal(after, run.Text);
        Assert.Equal(VimMode.Insert, run.Mode);
        Assert.Equal(1, run.Buffer.OpenUndoGroups);
    }

    [Theory]
    [InlineData("a|bc", "x", "<C-z>", "u")]
    [InlineData("ab|c", "x", "<C-z>", "u")]
    [InlineData("abc\nd|ef", "dd", "<C-z>", "u")]
    [InlineData("|abcde", "xx", "<C-z><C-z>", "uu")]
    [InlineData("|abcde", "xu", "<C-S-z>", "<C-r>")]
    [InlineData("|abcde", "xu", "<C-Z>", "<C-r>")]
    [InlineData("|abcde", "xu", "<C-y>", "<C-r>")]
    [InlineData("|abcde", "xu", "<C-Y>", "<C-r>")]
    [InlineData("|abcde", "xxuu", "<C-y><C-y>", "<C-r><C-r>")]
    public void TheUndoKeysInNormalMode_DoExactlyWhatUAndCtrlRDo(string before, string setup, string keys, string vimKeys)
    {
        VimRun held = VimScript.Run(before, setup + keys);
        VimRun vim = VimScript.Run(before, setup + vimKeys);

        Assert.Equal(vim.Text, held.Text);
        Assert.Equal(VimMode.Normal, held.Mode);
    }

    [Fact]
    public void CtrlZInVisualMode_LeavesTheSelection_ThenUndoes()
    {
        VimRun run = VimScript.Run("|abc def", "xvl<C-z>");

        Assert.Equal("|abc def", run.Text);
        Assert.Equal(VimMode.Normal, run.Mode);
        Assert.Null(run.Buffer.Selection);
    }

    [Fact]
    public void CtrlYInNormalMode_IsTheRedo()
    {
        // The Edit menu names Ctrl+Y for its Redo (BUG-C6A9): with vim on it puts back what 'u' took,
        // as Ctrl+R does, rather than scrolling the view up a line.
        VimRun run = VimScript.Run("|abcde", "xu<C-y>");

        Assert.Equal("|bcde", run.Text);
        Assert.Equal(VimMode.Normal, run.Mode);
    }

    [Fact]
    public void CtrlYWithNothingToRedo_ChangesNothing()
        => Assert.Equal("|bcde", VimScript.Run("|abcde", "x<C-y>").Text);

    [Fact]
    public void CtrlYInVisualMode_LeavesTheSelection_ThenRedoes()
    {
        VimRun run = VimScript.Run("|abc def", "xuvl<C-y>");

        Assert.Equal(VimScript.Run("|abc def", "xu<C-y>").Text, run.Text);
        Assert.Equal("bc def", run.Buffer.Text);
        Assert.Equal(VimMode.Normal, run.Mode);
        Assert.Null(run.Buffer.Selection);
    }

    [Theory]
    [InlineData("|world", "ihello<C-z>X<Esc>.", "|XXworld")]
    [InlineData("|w", "3iab<C-z>c<Esc>", "cc|cw")]
    public void AfterCtrlZInInsertMode_TheCountAndDotRepeatOnlyWhatWasTypedAfterIt(string before, string keys, string after)
        => Assert.Equal(after, VimScript.Run(before, keys).Text);

    [Fact]
    public void ARecordingWithCtrlZInInsertMode_ReplaysToTheSameText()
    {
        VimRun run = VimScript.Run("|abc\ndef", "qaiXY<C-z>Z<Esc>qj0@a");

        Assert.Equal("Zabc\n|Zdef", run.Text);
        Assert.Equal("iXY<C-z>Z<Esc>", run.RegisterNamed('a')?.Text);
    }

    [Theory]
    [InlineData("<C-y>")]
    [InlineData("<C-Y>")]
    public void ARecording_WritesCtrlYAsTheRedoItIs_AndReplaysIt(string redo)
    {
        VimRun run = VimScript.Run("|abcde", $"xuqa{redo}qu@a");

        Assert.Equal("|bcde", run.Text);
        Assert.Equal("<C-Z>", run.RegisterNamed('a')?.Text);
    }

    [Fact]
    public void ARecording_WritesCtrlShiftZAsTheRedoItIs()
    {
        VimRun run = VimScript.Run("|abc", "xuqa<C-S-z>q");

        Assert.Equal("|bc", run.Text);
        Assert.Equal("<C-Z>", run.RegisterNamed('a')?.Text);
    }

    [Fact]
    public void UndoFromTheMenu_InInsertMode_CutsTheSessionInTwo_AndStaysInInsertMode()
    {
        VimRun run = VimScript.Run("a|bc", "iXY");

        Assert.True(run.Engine.Undo());
        Assert.Equal("a|bc", VimScript.Render(run.Buffer));
        Assert.Equal(VimMode.Insert, run.Engine.Mode);
        Assert.Equal(1, run.Buffer.OpenUndoGroups);

        Assert.True(run.Engine.Redo());
        Assert.Equal("aXY|bc", VimScript.Render(run.Buffer));
        Assert.Equal(VimMode.Insert, run.Engine.Mode);
    }

    [Fact]
    public void UndoFromTheMenu_InNormalMode_IsU()
    {
        VimRun run = VimScript.Run("a|bc", "x");

        Assert.True(run.Engine.Undo());
        Assert.Equal("a|bc", VimScript.Render(run.Buffer));

        Assert.True(run.Engine.Redo());
        Assert.Equal("a|c", VimScript.Render(run.Buffer));
    }

    [Fact]
    public void UndoFromTheMenu_WithTheCommandLineOpen_IsLeftToTheEditor()
    {
        VimRun run = VimScript.Run("a|bc", "x:");

        Assert.False(run.Engine.Undo());
        Assert.False(run.Engine.Redo());
        Assert.Equal("a|c", VimScript.Render(run.Buffer));
        Assert.Equal(":", run.Engine.CommandLine);
    }

    [Fact]
    public void UndoFromTheMenu_DuringARecording_IsRecordedAsTheKey()
    {
        VimRun run = VimScript.Run("|abc", "qaiX");

        run.Engine.Undo();
        run.Engine.HandleKey(VimKey.Named(VimSpecialKey.Escape));
        run.Engine.HandleKey(VimKey.Typed('q'));

        Assert.Equal("iX<C-z><Esc>", run.RegisterNamed('a')?.Text);
    }

    private static VimKey VimKeyControl(char character)
        => VimKey.Typed(character, VimModifiers.Control);
}

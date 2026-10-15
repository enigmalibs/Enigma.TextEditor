using System.Collections.Generic;
using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// The search highlight: <c>incsearch</c> while a pattern is typed, <c>hlsearch</c> after Enter, and
/// <c>:nohlsearch</c> to take it away until the next search.
/// </summary>
public sealed class VimSearchHighlightTests
{
    [Fact]
    public void WhileTyping_ThePatternSoFarIsPreviewedWithTheMatchEnterWouldLandOn()
    {
        Session session = new("|one foo two foo");

        session.Type("/f");
        Assert.Equal(4, session.Engine.IncSearch?.MatchOffset);
        Assert.Equal(1, session.Engine.IncSearch?.MatchLength);

        session.Type("oo");
        Assert.Equal(4, session.Engine.IncSearch?.MatchOffset);
        Assert.Equal(3, session.Engine.IncSearch?.MatchLength);
        Assert.Matches(session.Engine.IncSearch!.Pattern, "foo");

        // The caret only moves on Enter.
        Assert.Equal(0, session.Buffer.CaretOffset);
    }

    [Fact]
    public void ABackwardSearch_PreviewsTheMatchBehindTheCaret()
    {
        Session session = new("foo one foo two |x");

        session.Type("?foo");

        Assert.Equal(8, session.Engine.IncSearch?.MatchOffset);
    }

    [Fact]
    public void Enter_EndsThePreviewAndHighlightsThePattern()
    {
        Session session = new("|one foo two foo");

        session.Type("/foo<CR>");

        Assert.Null(session.Engine.IncSearch);
        Assert.NotNull(session.Shared.VisibleHighlight);
        Assert.Matches(session.Shared.VisibleHighlight!, "foo");
        Assert.Equal(4, session.Buffer.CaretOffset);
        Assert.True(session.HighlightChanges > 0);
    }

    [Fact]
    public void Escape_EndsThePreviewAndLeavesThePreviousHighlightAlone()
    {
        Session session = new("|one foo bar");
        session.Type("/foo<CR>");
        int before = session.HighlightChanges;

        session.Type("/bar<Esc>");

        Assert.Null(session.Engine.IncSearch);
        Assert.Matches(session.Shared.VisibleHighlight!, "foo");
        Assert.DoesNotMatch(session.Shared.VisibleHighlight!, "bar");
        Assert.Equal(before, session.HighlightChanges);
    }

    [Theory]
    [InlineData(":noh<CR>")]
    [InlineData(":nohl<CR>")]
    [InlineData(":nohlsearch<CR>")]
    public void NoHlSearch_HidesTheHighlightButKeepsThePattern(string keys)
    {
        Session session = new("|a foo b foo");
        session.Type("/foo<CR>");

        session.Type(keys);

        Assert.Null(session.Shared.VisibleHighlight);
        Assert.NotNull(session.Shared.HighlightPattern);
        Assert.Null(session.Buffer.Status);

        // n still works, and brings the highlight back.
        session.Type("n");
        Assert.Equal(8, session.Buffer.CaretOffset);
        Assert.NotNull(session.Shared.VisibleHighlight);
    }

    [Fact]
    public void ARangeOnNoh_IsRefused()
    {
        Session session = new("|a");

        session.Type(":1noh<CR>");

        Assert.Equal("No range allowed", session.Buffer.Status);
    }

    [Fact]
    public void Star_HighlightsTheWord()
    {
        Session session = new("|word other word");

        session.Type("*");

        Assert.Matches(session.Shared.VisibleHighlight!, "word");
        Assert.DoesNotMatch(session.Shared.VisibleHighlight!, "wordy");
    }

    [Fact]
    public void ASubstitute_HighlightsItsPattern()
    {
        Session session = new("|a foo");

        session.Type(":s/foo/bar/<CR>");

        Assert.Matches(session.Shared.VisibleHighlight!, "foo");
    }

    [Fact]
    public void SetNoHlSearch_HidesTheHighlight_AndSetHlSearchBringsItBack()
    {
        Session session = new("|a foo");
        session.Type("/foo<CR>");

        session.Type(":set nohls<CR>");
        Assert.Null(session.Shared.VisibleHighlight);
        Assert.False(session.Shared.HlSearch);

        session.Type(":set hlsearch<CR>");
        Assert.NotNull(session.Shared.VisibleHighlight);
    }

    [Fact]
    public void SetNoIncSearch_ShowsNoPreview()
    {
        Session session = new("|a foo");

        session.Type(":set nois<CR>/fo");

        Assert.Null(session.Engine.IncSearch);
        Assert.False(session.Shared.IncSearch);
    }

    [Fact]
    public void APatternThatDoesNotCompileYet_PreviewsNothingAndSaysNothing()
    {
        Session session = new("|a foo(x)");

        session.Type("/foo(");

        Assert.Null(session.Engine.IncSearch);
        Assert.Null(session.Buffer.Status);
        Assert.Equal("/foo(", session.Engine.CommandLine);
    }

    [Fact]
    public void BackspacingToNothing_EndsThePreview()
    {
        Session session = new("|a foo");

        session.Type("/f");
        Assert.NotNull(session.Engine.IncSearch);

        session.Type("<BS>");
        Assert.Null(session.Engine.IncSearch);
    }

    [Fact]
    public void TheHighlight_IsSharedByEveryEngine()
    {
        VimSharedState shared = new();
        VimEngine left = new(VimScript.NewBuffer("|foo"), shared: shared);
        VimEngine right = new(VimScript.NewBuffer("|bar"), shared: shared);

        foreach (VimKey key in VimKeyNotation.Parse("/foo<CR>"))
        {
            left.HandleKey(key);
        }

        Assert.Matches(shared.VisibleHighlight!, "foo");

        foreach (VimKey key in VimKeyNotation.Parse(":noh<CR>"))
        {
            right.HandleKey(key);
        }

        Assert.Null(shared.VisibleHighlight);
    }

    [Fact]
    public void ASearchThatScrollsOffScreen_BringsTheMatchIntoViewAndEscapeTakesItBack()
    {
        string text = "|start\n" + string.Join('\n', Repeat("line", 50)) + "\ntarget\n";
        Session session = new(text);
        session.Buffer.ViewportLines = 10;
        session.Buffer.FirstVisibleLine = 1;

        session.Type("/target");
        Assert.True(session.Buffer.FirstVisibleLine > 1);

        session.Type("<Esc>");
        Assert.Equal(1, session.Buffer.FirstVisibleLine);
    }

    private static IEnumerable<string> Repeat(string text, int count)
    {
        for (int index = 0; index < count; index++)
        {
            yield return text;
        }
    }

    /// <summary>
    /// One engine over one buffer, typed into step by step, counting the highlight's notifications.
    /// </summary>
    private sealed class Session
    {
        public Session(string textWithCaret)
        {
            Buffer = VimScript.NewBuffer(textWithCaret);
            Engine = new VimEngine(Buffer, shared: Shared);
            Shared.HighlightChanged += (_, _) => HighlightChanges++;
        }

        public FakeVimBuffer Buffer { get; }

        public VimSharedState Shared { get; } = new();

        public VimEngine Engine { get; }

        public int HighlightChanges { get; private set; }

        public void Type(string keys)
        {
            foreach (VimKey key in VimKeyNotation.Parse(keys))
            {
                Engine.HandleKey(key);
            }
        }
    }
}

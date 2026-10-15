using System;
using System.Linq;
using Enigma.TextEditor.Core.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Documents;

public sealed class IndentationDetectorTests
{
    private static readonly Indentation Fallback = Indentation.Spaces(4);

    [Fact]
    public void TwoSpaceIndentation_IsDetected()
    {
        const string text = "a:\n  b:\n    c: 1\n  d: 2\ne:\n  f: 3\n";

        Assert.Equal(Indentation.Spaces(2), IndentationDetector.Detect(text, Fallback));
    }

    [Fact]
    public void FourSpaceIndentation_IsDetected()
    {
        const string text = "class A\n{\n    void B()\n    {\n        C();\n    }\n}\n";

        Assert.Equal(Indentation.Spaces(4), IndentationDetector.Detect(text, Indentation.Spaces(2)));
    }

    [Fact]
    public void TabIndentation_IsDetectedWithTheFallbackTabSize()
    {
        const string text = "func a() {\n\tb()\n\tif c {\n\t\td()\n\t}\n}\n";

        Assert.Equal(Indentation.Tabs(4), IndentationDetector.Detect(text, Fallback));
    }

    [Fact]
    public void AStepOfEight_CountsAsTwoLevelsOfFour()
        => Assert.Equal(Indentation.Spaces(4), IndentationDetector.Detect("a\n        b\nc\n        d\n", Indentation.Spaces(2)));

    [Fact]
    public void BlockCommentAlignment_DoesNotMakeAFileOneSpaceIndented()
    {
        const string text = "/**\n * Doc.\n * More.\n */\nint a;\n    int b;\n";

        Assert.Equal(Indentation.Spaces(4), IndentationDetector.Detect(text, Indentation.Spaces(2)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no indentation at all\nanywhere\n")]
    [InlineData("\n\n   \n")]
    public void ATextThatDoesNotSay_GetsTheFallback(string text)
        => Assert.Equal(Indentation.Spaces(3), IndentationDetector.Detect(text, Indentation.Spaces(3)));

    [Fact]
    public void SpaceIndentationWithNoClearStep_KeepsSpacesAtTheFallbackSize()
        => Assert.Equal(Indentation.Spaces(4), IndentationDetector.Detect("a\n   b\n", Indentation.Tabs(4)));

    [Fact]
    public void MoreTabLinesThanSpaceLines_MeansTabs()
        => Assert.True(IndentationDetector.Detect("a\n\tb\n\tc\n  d\n", Fallback).UseTabs);

    [Fact]
    public void OnlyTheFirstLinesAreRead()
    {
        string spaces = string.Concat(Enumerable.Repeat("a\n  b\n", IndentationDetector.MaximumLines / 2));
        string tabs = string.Concat(Enumerable.Repeat("\tc\n", 3 * IndentationDetector.MaximumLines));

        Assert.Equal(Indentation.Spaces(2), IndentationDetector.Detect(spaces + tabs, Fallback));
    }

    [Theory]
    [InlineData(false, 4, "Spaces: 4")]
    [InlineData(false, 2, "Spaces: 2")]
    [InlineData(true, 4, "Tab Size: 4")]
    public void TheLabel_IsWhatTheStatusBarShows(bool useTabs, int size, string label)
        => Assert.Equal(label, new Indentation(useTabs, size).Label);

    [Fact]
    public void ASizeBelowOne_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Indentation.Spaces(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Indentation.Tabs(-1));
    }
}

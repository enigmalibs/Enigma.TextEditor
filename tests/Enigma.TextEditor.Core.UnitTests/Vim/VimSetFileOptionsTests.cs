using Enigma.TextEditor.Core.Vim;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Vim;

/// <summary>
/// <c>:set</c> of the file's options — <c>fileformat</c>, <c>fileencoding</c>, <c>bomb</c>,
/// <c>filetype</c>: known to the engine, not kept by it, and handed to the application in one request.
/// </summary>
public sealed class VimSetFileOptionsTests
{
    [Theory]
    [InlineData(":set ff=dos<CR>", "ff=dos")]
    [InlineData(":set fileformat=unix<CR>", "fileformat=unix")]
    [InlineData(":set ff?<CR>", "ff?")]
    [InlineData(":set ff<CR>", "ff")]
    [InlineData(":set fenc=latin1<CR>", "fenc=latin1")]
    [InlineData(":set fileencoding=utf-8<CR>", "fileencoding=utf-8")]
    [InlineData(":set bomb<CR>", "bomb")]
    [InlineData(":set nobomb<CR>", "nobomb")]
    [InlineData(":set bomb!<CR>", "bomb!")]
    [InlineData(":set bomb?<CR>", "bomb?")]
    [InlineData(":set ft=python<CR>", "ft=python")]
    [InlineData(":set filetype=<CR>", "filetype=")]
    [InlineData(":se ft?<CR>", "ft?")]
    public void AFileOption_IsHandedOn_AsTyped(string keys, string argument)
    {
        VimRun run = VimScript.Run("|a", keys);

        Assert.Equal([new VimExRequest(VimExCommand.SetFileOptions, argument)], run.ExRequests);
        Assert.Null(run.Status);
    }

    [Fact]
    public void SeveralFileOptions_AreHandedOnTogether_InOrder()
        => Assert.Equal(
            [new VimExRequest(VimExCommand.SetFileOptions, "ff? fenc=latin1 ft=python")],
            VimScript.Run("|a", ":set ff? fenc=latin1 ft=python<CR>").ExRequests);

    [Fact]
    public void AmongTheEnginesOwn_OnlyTheFileOptionsAreHandedOn()
    {
        VimRun run = VimScript.Run("|a", ":set ic ff=dos sw=2<CR>");

        Assert.Equal([new VimExRequest(VimExCommand.SetFileOptions, "ff=dos")], run.ExRequests);
        Assert.True(run.Engine.Options.IgnoreCase);
        Assert.Equal(2, run.Engine.Options.ShiftWidth);
    }

    [Fact]
    public void ARefusalAfterAFileOption_StillHandsItOn()
    {
        VimRun run = VimScript.Run("|a", ":set ff=dos foo ft=python<CR>");

        Assert.Equal([new VimExRequest(VimExCommand.SetFileOptions, "ff=dos")], run.ExRequests);
        Assert.Equal("Unknown option: foo", run.Status);
    }

    [Theory]
    [InlineData(":set<CR>")]
    [InlineData(":set ic<CR>")]
    public void ASetWithoutFileOptions_AsksTheApplicationNothing(string keys)
        => Assert.Empty(VimScript.Run("|a", keys).ExRequests);

    [Fact]
    public void ANoInFrontOfAValuedFileOption_IsUnknown()
    {
        VimRun run = VimScript.Run("|a", ":set noff<CR>");

        Assert.Equal("Unknown option: noff", run.Status);
        Assert.Empty(run.ExRequests);
    }
}

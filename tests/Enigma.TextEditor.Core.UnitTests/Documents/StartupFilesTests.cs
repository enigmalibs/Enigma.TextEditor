using System.Collections.Generic;
using System.IO;
using Enigma.TextEditor.Core.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Documents;

public sealed class StartupFilesTests
{
    private static readonly string Work = Path.Combine(Path.GetTempPath(), "work");

    [Fact]
    public void Files_AreResolvedAgainstTheWorkingDirectoryInOrder()
    {
        StartupFiles startup = Parse(["a.txt", Path.Combine("sub", "b.txt")], existing: [Path.Combine(Work, "a.txt")]);

        Assert.Equal(
            [new StartupFile(Path.Combine(Work, "a.txt"), true), new StartupFile(Path.Combine(Work, "sub", "b.txt"), false)],
            startup.Files);
        Assert.Empty(startup.Skipped);
    }

    [Fact]
    public void AnAbsolutePath_IsKeptAsItIs()
    {
        string absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "c.txt");

        Assert.Equal(absolute, Assert.Single(Parse([absolute]).Files).Path);
    }

    [Fact]
    public void AFolder_IsSkipped()
    {
        StartupFiles startup = Parse(["folder", "a.txt"], folders: [Path.Combine(Work, "folder")]);

        Assert.Equal("folder", Assert.Single(startup.Skipped));
        Assert.Equal(Path.Combine(Work, "a.txt"), Assert.Single(startup.Files).Path);
    }

    [Fact]
    public void TheSameFileTwice_OpensOnce()
        => Assert.Single(Parse(["a.txt", Path.Combine(".", "a.txt")]).Files);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankArgument_IsIgnored(string blank)
    {
        StartupFiles startup = Parse([blank]);

        Assert.Empty(startup.Files);
        Assert.Empty(startup.Skipped);
    }

    [Fact]
    public void AMalformedPath_IsSkipped()
    {
        StartupFiles startup = Parse(["bad\0name.txt"]);

        Assert.Empty(startup.Files);
        Assert.Single(startup.Skipped);
    }

    private static StartupFiles Parse(
        IReadOnlyList<string> args,
        IReadOnlyCollection<string>? existing = null,
        IReadOnlyCollection<string>? folders = null)
    {
        HashSet<string> files = [.. existing ?? []];
        HashSet<string> directories = [.. folders ?? []];

        return StartupFiles.Parse(args, Work, files.Contains, directories.Contains);
    }
}

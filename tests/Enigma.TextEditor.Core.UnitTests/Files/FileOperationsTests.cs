using System;
using System.IO;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Files;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Files;

/// <summary>
/// Tests for the operations the explorer's context menu carries out, over real folders. What matters here
/// is not only that the right thing happens, but that the wrong thing is refused with a sentence
/// rather than by overwriting something the user still wanted.
/// </summary>
public sealed class FileOperationsTests
{
    [Fact]
    public async Task ANewFile_IsCreatedAndIsEmpty()
    {
        using TempTreeFolder folder = new();

        string created = await FileOperations.CreateFileAsync(
            folder.Root,
            "notes.md",
            TestContext.Current.CancellationToken);

        Assert.Equal(Path.Combine(folder.Root, "notes.md"), created);
        Assert.True(File.Exists(created));
        Assert.Equal(0, new FileInfo(created).Length);
    }

    [Fact]
    public async Task ANewFolder_IsCreated()
    {
        using TempTreeFolder folder = new();

        string created = await FileOperations.CreateDirectoryAsync(
            folder.Root,
            "drafts",
            TestContext.Current.CancellationToken);

        Assert.Equal(Path.Combine(folder.Root, "drafts"), created);
        Assert.True(Directory.Exists(created));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notes/2024.md")]
    [InlineData("CON.md")]
    [InlineData("notes.md ")]
    public async Task ANameTheRulesRefuse_NeverReachesTheFileSystem(string name)
    {
        using TempTreeFolder folder = new();

        await Assert.ThrowsAsync<FileOperationException>(
            () => FileOperations.CreateFileAsync(folder.Root, name, TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFileSystemEntries(folder.Root));
    }

    [Fact]
    public async Task ANameAFileAlreadyHas_LeavesThatFileAlone()
    {
        using TempTreeFolder folder = new();
        folder.AddFile("notes.md", "mine");

        await Assert.ThrowsAsync<FileOperationException>(
            () => FileOperations.CreateFileAsync(folder.Root, "notes.md", TestContext.Current.CancellationToken));
        Assert.Equal("mine", File.ReadAllText(folder.Combine("notes.md")));
    }

    [Fact]
    public async Task ANameAFolderAlreadyHas_LeavesThatFolderAlone()
    {
        using TempTreeFolder folder = new();
        folder.AddFile(Path.Combine("drafts", "notes.md"), "mine");

        await Assert.ThrowsAsync<FileOperationException>(
            () => FileOperations.CreateDirectoryAsync(folder.Root, "drafts", TestContext.Current.CancellationToken));
        Assert.True(File.Exists(folder.Combine(Path.Combine("drafts", "notes.md"))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ANamelessFolderToCreateIn_IsRejected(string? directory)
        => await Assert.ThrowsAnyAsync<ArgumentException>(
            () => FileOperations.CreateFileAsync(directory!, "notes.md", TestContext.Current.CancellationToken));
}

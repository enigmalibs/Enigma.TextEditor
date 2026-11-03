using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Files;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Files;

/// <summary>
/// Tests for the one place the explorer reads the file system, over real directories.
/// </summary>
public sealed class DirectoryListerTests
{
    [Fact]
    public async Task ADirectory_IsListedFoldersFirst_InNaturalOrder()
    {
        using TempTreeFolder folder = new();
        folder.AddFile("zebra.txt");
        folder.AddFile("file10.txt");
        folder.AddFile("File2.txt");
        folder.AddDirectory("notes");
        folder.AddDirectory("Assets");

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Root, TestContext.Current.CancellationToken);

        Assert.Equal(DirectoryListingStatus.Ok, listing.Status);
        Assert.Equal(["Assets", "notes", "File2.txt", "file10.txt", "zebra.txt"], listing.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task DotEntries_AndEntriesMarkedHidden_AreListed()
    {
        using TempTreeFolder folder = new();
        folder.AddDirectory(".git");
        folder.AddFile(".editorconfig");
        TempTreeFolder.Hide(folder.AddFile("secret.txt"));

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Root, TestContext.Current.CancellationToken);

        Assert.Equal([".git", ".editorconfig", "secret.txt"], listing.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task AnEmptyDirectory_IsReadRatherThanFailed()
    {
        using TempTreeFolder folder = new();

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Root, TestContext.Current.CancellationToken);

        Assert.Equal(DirectoryListingStatus.Ok, listing.Status);
        Assert.True(listing.IsEmpty);
    }

    [Fact]
    public async Task ADirectoryThatIsNotThere_IsReportedAsMissing()
    {
        using TempTreeFolder folder = new();

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Combine("gone"), TestContext.Current.CancellationToken);

        Assert.Equal(DirectoryListingStatus.Missing, listing.Status);
        Assert.Empty(listing.Entries);
        Assert.False(listing.IsEmpty);
    }

    [Fact]
    public async Task EachEntry_CarriesItsFullPathAndWhetherItIsADirectory()
    {
        using TempTreeFolder folder = new();
        string notes = folder.AddDirectory("notes");
        string readme = folder.AddFile("readme.txt");

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Root, TestContext.Current.CancellationToken);

        Assert.Collection(
            listing.Entries,
            entry =>
            {
                Assert.True(entry.IsDirectory);
                Assert.Equal(notes, entry.FullPath);
            },
            entry =>
            {
                Assert.False(entry.IsDirectory);
                Assert.Equal(readme, entry.FullPath);
            });
    }

    [Fact]
    public async Task OnlyOneLevel_IsRead()
    {
        using TempTreeFolder folder = new();
        folder.AddDirectory("notes");
        folder.AddFile(Path.Combine("notes", "deep.txt"));

        DirectoryListing listing = await DirectoryLister.ListAsync(folder.Root, TestContext.Current.CancellationToken);

        Assert.Equal(["notes"], listing.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public async Task ACancelledListing_Throws()
    {
        using TempTreeFolder folder = new();
        folder.AddFile("a.txt");
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DirectoryLister.ListAsync(folder.Root, cancelled.Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ANamelessDirectory_IsRejected(string? directory)
        => await Assert.ThrowsAnyAsync<ArgumentException>(() => DirectoryLister.ListAsync(directory!, TestContext.Current.CancellationToken));

    [Fact]
    public void AFailedListing_NeedsAReason()
        => Assert.Throws<ArgumentOutOfRangeException>(() => DirectoryListing.Failed(DirectoryListingStatus.Ok));
}

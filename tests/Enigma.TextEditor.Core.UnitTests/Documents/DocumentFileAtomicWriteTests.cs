using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Documents;

/// <summary>
/// A save never leaves the user's file empty or cut short: the bytes go to a temporary file beside it, which replaces it
/// only once they are all on the disk — and what the replacement could lose (a link, an attribute, permissions) is kept.
/// </summary>
public sealed class DocumentFileAtomicWriteTests
{
    private static readonly byte[] Original = Encoding.UTF8.GetBytes("the original content, longer than the new\n");
    private static readonly byte[] Replacement = Encoding.UTF8.GetBytes("new\n");

    [Fact]
    public async Task AWriteThatFailsHalfWay_LeavesTheFileByteForByte_AndNoTemporaryFile()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("notes.txt", Original);

        IOException full = await Assert.ThrowsAsync<IOException>(() => DocumentFile.WriteBytesAsync(
            path,
            Replacement,
            static async (stream, data, token) =>
            {
                await stream.WriteAsync(data[..2], token);
                throw new IOException("There is not enough space on the disk.");
            },
            TestContext.Current.CancellationToken));

        Assert.Equal("There is not enough space on the disk.", full.Message);
        Assert.Equal(Original, folder.ReadBytes("notes.txt"));
        Assert.Equal([path], Directory.GetFiles(folder.Root));
    }

    [Fact]
    public async Task AWrite_ReplacesTheBytes_AndLeavesNoTemporaryFile()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("notes.txt", Original);

        await DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken);

        Assert.Equal(Replacement, folder.ReadBytes("notes.txt"));
        Assert.Equal([path], Directory.GetFiles(folder.Root));
    }

    [Fact]
    public async Task ANewFile_IsCreated()
    {
        using TempDocumentFolder folder = new();
        string path = folder.Combine("new.txt");

        await DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken);

        Assert.Equal(Replacement, folder.ReadBytes("new.txt"));
        Assert.Equal([path], Directory.GetFiles(folder.Root));
    }

    [Fact]
    public async Task AReadOnlyFile_IsRefused_AsBefore_AndLeftAsItWas()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("locked.txt", Original);
        File.SetAttributes(path, FileAttributes.ReadOnly);

        try
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken));

            Assert.Equal(Original, folder.ReadBytes("locked.txt"));
            Assert.Equal([path], Directory.GetFiles(folder.Root));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task AFileHeldOpenForWritingByAnotherProgram_IsStillWritten()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("shared.txt", Original);

        // Shared for reading and writing but not for deleting: on Windows the file can be written, not replaced, and
        // the write falls back to writing in place.
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            await DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken);
        }

        Assert.Equal(Replacement, folder.ReadBytes("shared.txt"));
        Assert.Equal([path], Directory.GetFiles(folder.Root));
    }

    [Fact]
    public async Task AHiddenFile_StaysHidden_AndKeepsItsCreationTime()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Hidden is an attribute on Windows only.");

        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("hidden.txt", Original);
        DateTime created = new(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetCreationTimeUtc(path, created);
        File.SetAttributes(path, FileAttributes.Hidden);

        await DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken);

        Assert.Equal(Replacement, folder.ReadBytes("hidden.txt"));
        Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
        Assert.Equal(created, File.GetCreationTimeUtc(path));
    }

    [Fact]
    public async Task AFilesPermissions_AreKept()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix permissions do not exist on Windows.");
        }
        else
        {
            using TempDocumentFolder folder = new();
            string path = folder.WriteBytes("script.sh", Original);
            UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead;
            File.SetUnixFileMode(path, mode);

            await DocumentFile.WriteBytesAsync(path, Replacement, TestContext.Current.CancellationToken);

            Assert.Equal(Replacement, folder.ReadBytes("script.sh"));
            Assert.Equal(mode, File.GetUnixFileMode(path));
        }
    }

    [Fact]
    public async Task ASymbolicLink_StaysALink_AndTheFileItPointsAtIsWritten()
    {
        using TempDocumentFolder folder = new();
        string target = folder.WriteBytes("target.txt", Original);
        string link = folder.Combine("link.txt");

        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Assert.Skip($"This account cannot create symbolic links: {exception.Message}");
        }

        await DocumentFile.WriteBytesAsync(link, Replacement, TestContext.Current.CancellationToken);

        Assert.NotNull(new FileInfo(link).LinkTarget);
        Assert.Equal(Replacement, folder.ReadBytes("target.txt"));
        Assert.Equal(2, Directory.GetFiles(folder.Root).Length);
    }

    [Fact]
    public async Task ACancelledWrite_LeavesTheFileAsItWas()
    {
        using TempDocumentFolder folder = new();
        string path = folder.WriteBytes("notes.txt", Original);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DocumentFile.WriteBytesAsync(path, Replacement, cancelled.Token));

        Assert.Equal(Original, folder.ReadBytes("notes.txt"));
        Assert.Equal([path], Directory.GetFiles(folder.Root));
    }
}

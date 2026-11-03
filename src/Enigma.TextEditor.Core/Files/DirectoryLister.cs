using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Files;

/// <summary>
/// Reads one directory's entries and puts them in tree order: folders first, then files, each by
/// <see cref="NaturalNameComparer"/>.
/// </summary>
/// <remarks>
/// <para>
/// One level at a time and never recursively: the explorer expands lazily, so a folder with a hundred thousand files
/// below it costs one listing, not a walk.
/// </para>
/// <para>
/// Nothing is hidden. A text editor edits dot files and files the platform marks hidden as much as any other, and
/// there is no filter to configure yet.
/// </para>
/// <para>
/// Ported from Enigma.MarkdownEditor, without its filter.
/// </para>
/// </remarks>
public static class DirectoryLister
{
    /// <summary>
    /// Lists a directory off the calling thread.
    /// </summary>
    /// <param name="directory">The directory to list.</param>
    /// <param name="cancellationToken">Abandons the listing.</param>
    /// <returns>The directory's entries, or why there are none.</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    /// <remarks>
    /// Offloaded with <see cref="Task.Run(Func{DirectoryListing}, CancellationToken)"/> rather than awaiting a
    /// genuinely asynchronous API, because there is none: directory enumeration is synchronous all the way down. A
    /// folder on a sleeping network share blocks for seconds, and this keeps that off the UI thread.
    /// </remarks>
    public static Task<DirectoryListing> ListAsync(string directory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        return Task.Run(() => List(directory, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Lists a directory on the calling thread.
    /// </summary>
    /// <param name="directory">The directory to list.</param>
    /// <param name="cancellationToken">Abandons the listing.</param>
    /// <returns>The directory's entries, or why there are none.</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    public static DirectoryListing List(string directory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        List<FileSystemEntry> entries = [];

        try
        {
            DirectoryInfo root = new(directory);

            // Asked before enumerating so that "not there" is reported as itself rather than as the
            // DirectoryNotFoundException the enumerator would raise on its first move.
            if (!root.Exists)
            {
                return DirectoryListing.Failed(DirectoryListingStatus.Missing);
            }

            EnumerationOptions options = new()
            {
                RecurseSubdirectories = false,

                // An entry this user cannot stat is skipped rather than aborting the whole listing: one unreadable
                // subfolder must not empty its parent.
                IgnoreInaccessible = true,

                // Hidden and system entries are listed: the default would skip them.
                AttributesToSkip = 0,
            };

            foreach (FileSystemInfo info in root.EnumerateFileSystemInfos("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (AttributesOf(info) is not { } attributes)
                {
                    continue;
                }

                entries.Add(new FileSystemEntry(info.FullName, info.Name, (attributes & FileAttributes.Directory) != 0));
            }
        }
        catch (UnauthorizedAccessException)
        {
            return DirectoryListing.Failed(DirectoryListingStatus.AccessDenied);
        }
        catch (DirectoryNotFoundException)
        {
            // The directory went away between the Exists check and the enumeration.
            return DirectoryListing.Failed(DirectoryListingStatus.Missing);
        }
        catch (IOException)
        {
            return DirectoryListing.Failed(DirectoryListingStatus.Unavailable);
        }

        entries.Sort(FileSystemEntryComparer.Instance);

        return DirectoryListing.Ok(entries);
    }

    // A broken symlink or a reparse point the OS refuses to follow throws from the property rather than from the
    // enumerator. Skipped, rather than drawn as an entry that leads nowhere.
    private static FileAttributes? AttributesOf(FileSystemInfo info)
    {
        try
        {
            return info.Attributes;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

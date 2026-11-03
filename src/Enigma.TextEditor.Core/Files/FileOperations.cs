using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Files;

/// <summary>
/// The file system half of the explorer's context menu.
/// </summary>
/// <remarks>
/// <para>
/// Every method is offloaded with <see cref="Task.Run{TResult}(Func{TResult}, CancellationToken)"/> for the reason
/// <see cref="DirectoryLister"/> is: there is no genuinely asynchronous file system API underneath, and a folder on a
/// sleeping network share takes seconds to answer.
/// </para>
/// <para>
/// Names are checked with <see cref="FileNameValidation"/> before anything is touched, and every failure the file
/// system reports is turned into a <see cref="FileOperationException"/> carrying a sentence rather than a stack trace.
/// The check can never be enough on its own — between it and the operation the folder can change — so each operation
/// is also written to fail rather than to overwrite something silently.
/// </para>
/// <para>
/// Ported from Enigma.MarkdownEditor.
/// </para>
/// </remarks>
public static class FileOperations
{
    /// <summary>
    /// Creates an empty file in a folder.
    /// </summary>
    /// <param name="directory">The folder to create it in, as a full path.</param>
    /// <param name="name">The file's name, as the user typed it.</param>
    /// <param name="cancellationToken">Abandons the operation before it starts.</param>
    /// <returns>The full path of the new file.</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileOperationException">The name breaks a rule, is already taken, or the file could not be written.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    /// <remarks>
    /// <paramref name="name"/> is deliberately not guarded like an argument: it comes from a text box, so an empty one
    /// is something to tell the user about, not a caller's bug.
    /// </remarks>
    public static Task<string> CreateFileAsync(string directory, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        return Task.Run(() => CreateFile(directory, name), cancellationToken);
    }

    /// <summary>
    /// Creates a folder in a folder.
    /// </summary>
    /// <param name="directory">The folder to create it in, as a full path.</param>
    /// <param name="name">The new folder's name, as the user typed it.</param>
    /// <param name="cancellationToken">Abandons the operation before it starts.</param>
    /// <returns>The full path of the new folder.</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileOperationException">The name breaks a rule, is already taken, or the folder could not be created.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    public static Task<string> CreateDirectoryAsync(string directory, string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        return Task.Run(() => CreateDirectory(directory, name), cancellationToken);
    }

    /// <summary>
    /// Renames a file or a folder in place.
    /// </summary>
    /// <param name="path">The full path of the entry to rename.</param>
    /// <param name="newName">The name it should have, as the user typed it.</param>
    /// <param name="cancellationToken">Abandons the operation before it starts.</param>
    /// <returns>The full path the entry now has.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileOperationException">The entry is gone, the name breaks a rule or is already taken, or the rename failed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was signalled.</exception>
    /// <remarks>
    /// Renaming an entry to the name it already has is a no-op rather than an error: a user who opens the rename box
    /// and presses Enter has asked for nothing. Changing only the case of a name is a rename, on every platform.
    /// </remarks>
    public static Task<string> RenameAsync(string path, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Task.Run(() => Rename(path, newName), cancellationToken);
    }

    private static string CreateFile(string directory, string name)
    {
        if (FileNameValidation.ValidateIn(name, directory, isDirectory: false) is { } problem)
        {
            throw new FileOperationException(problem);
        }

        string path = Path.Combine(directory, name);

        try
        {
            // FileMode.CreateNew is what makes this a create rather than a truncate: should the name have been taken
            // since the check above, the file that is there is left untouched.
            File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None).Dispose();
        }
        catch (IOException exception) when (File.Exists(path) || Directory.Exists(path))
        {
            throw new FileOperationException($"There is already something called '{name}' here.", exception);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileOperationException($"'{name}' could not be created here.", exception);
        }

        return path;
    }

    private static string CreateDirectory(string directory, string name)
    {
        if (FileNameValidation.ValidateIn(name, directory, isDirectory: true) is { } problem)
        {
            throw new FileOperationException(problem);
        }

        string path = Path.Combine(directory, name);

        // Directory.CreateDirectory accepts a folder that already exists and returns it, so the race the file branch
        // gets for free from FileMode.CreateNew has to be asked for here. Silently adopting somebody else's folder is
        // worse than the rare wrong answer.
        if (Directory.Exists(path) || File.Exists(path))
        {
            throw new FileOperationException($"There is already something called '{name}' here.");
        }

        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileOperationException($"'{name}' could not be created here.", exception);
        }

        return path;
    }

    private static string Rename(string path, string newName)
    {
        string full = Normalize(path);
        bool isDirectory = Directory.Exists(full);
        string currentName = FilePaths.NameOf(full);

        if (!isDirectory && !File.Exists(full))
        {
            throw new FileOperationException($"'{currentName}' is no longer there.");
        }

        string parent = Path.GetDirectoryName(full)
            ?? throw new FileOperationException($"'{currentName}' is not somewhere anything can be renamed.");

        if (string.Equals(newName, currentName, StringComparison.Ordinal))
        {
            return full;
        }

        if (FileNameValidation.ValidateIn(newName, parent, isDirectory, currentName) is { } problem)
        {
            throw new FileOperationException(problem);
        }

        string destination = Path.Combine(parent, newName);

        // Changing only the case of a name is a real rename that a case-insensitive file system reads as a move onto
        // the entry itself, and refuses. Going out to a name nothing else can have and back in is how "notes.txt"
        // becomes "Notes.txt" on Windows; elsewhere the two names differ already and the move is the ordinary one.
        bool caseOnly = FilePaths.Comparer.Equals(currentName, newName);
        string staging = caseOnly ? StagingPath(parent) : string.Empty;

        try
        {
            if (caseOnly)
            {
                MoveEntry(full, staging, isDirectory);
                MoveEntry(staging, destination, isDirectory);
            }
            else
            {
                MoveEntry(full, destination, isDirectory);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (caseOnly)
            {
                Restore(staging, full, isDirectory);
            }

            throw new FileOperationException($"'{currentName}' could not be renamed to '{newName}'.", exception);
        }

        return destination;
    }

    private static void MoveEntry(string source, string destination, bool isDirectory)
    {
        if (isDirectory)
        {
            Directory.Move(source, destination);
        }
        else
        {
            File.Move(source, destination);
        }
    }

    // A name nothing else can be holding, for the middle of a case-only rename.
    private static string StagingPath(string parent)
    {
        string path;

        do
        {
            path = Path.Combine(parent, $".rename-{Guid.NewGuid():N}");
        }
        while (File.Exists(path) || Directory.Exists(path));

        return path;
    }

    // Only reached when the second half of a two-step rename failed: the entry is sitting under a name the user never
    // chose and cannot find, so putting it back is worth one attempt.
    private static void Restore(string staging, string original, bool isDirectory)
    {
        try
        {
            if (isDirectory ? Directory.Exists(staging) : File.Exists(staging))
            {
                MoveEntry(staging, original, isDirectory);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Nothing further to try. The failure the caller is told about is the rename's own.
        }
    }

    private static string Normalize(string path)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (exception is ArgumentException or PathTooLongException or NotSupportedException)
        {
            throw new FileOperationException($"'{path}' is not a usable path.", exception);
        }
    }
}

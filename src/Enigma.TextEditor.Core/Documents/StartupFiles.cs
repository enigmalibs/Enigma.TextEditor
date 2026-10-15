using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// One file named on the command line.
/// </summary>
/// <param name="Path">Its full path.</param>
/// <param name="Exists">Whether it exists. A missing file is opened as a new, empty buffer bound to that path.</param>
public readonly record struct StartupFile(string Path, bool Exists);

/// <summary>
/// The files named on the command line, read the way the editor opens them.
/// </summary>
/// <param name="Files">The files to open, in order, each once.</param>
/// <param name="Skipped">The arguments that are not files — folders, malformed paths — as typed.</param>
public sealed record StartupFiles(IReadOnlyList<StartupFile> Files, IReadOnlyList<string> Skipped)
{
    /// <summary>
    /// Reads the command-line arguments.
    /// </summary>
    /// <param name="args">The arguments, every one a path.</param>
    /// <param name="workingDirectory">What a relative path is relative to.</param>
    /// <param name="fileExists">Whether a file exists.</param>
    /// <param name="directoryExists">Whether a folder exists.</param>
    /// <returns>The files, and what was skipped.</returns>
    /// <remarks>
    /// A path that does not exist is still a file to open — <c>vim new.txt</c> starts a buffer that the
    /// first save creates — and the same file named twice opens once.
    /// </remarks>
    public static StartupFiles Parse(
        IReadOnlyList<string> args,
        string workingDirectory,
        Func<string, bool> fileExists,
        Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentNullException.ThrowIfNull(fileExists);
        ArgumentNullException.ThrowIfNull(directoryExists);

        List<StartupFile> files = [];
        List<string> skipped = [];
        HashSet<string> seen = new(OperatingSystem.IsLinux() ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

        foreach (string argument in args)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                continue;
            }

            string? fullPath = TryResolve(argument, workingDirectory);

            if ((fullPath is null) || directoryExists(fullPath))
            {
                skipped.Add(argument);
                continue;
            }

            if (seen.Add(fullPath))
            {
                files.Add(new StartupFile(fullPath, fileExists(fullPath)));
            }
        }

        return new StartupFiles(files, skipped);
    }

    private static string? TryResolve(string argument, string workingDirectory)
    {
        try
        {
            return System.IO.Path.GetFullPath(argument, workingDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            return null;
        }
    }
}

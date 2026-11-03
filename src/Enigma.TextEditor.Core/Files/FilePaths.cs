using System;
using System.IO;

namespace Enigma.TextEditor.Core.Files;

/// <summary>
/// How the explorer compares and names paths.
/// </summary>
public static class FilePaths
{
    /// <summary>
    /// Gets the comparer that says whether two paths name the same entry: ignoring case on Windows, whose file
    /// systems do, and exactly elsewhere.
    /// </summary>
    public static StringComparer Comparer { get; } =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Gets the name of the entry a path names: its last part, or the path itself for a drive root, which has none.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The name.</returns>
    public static string NameOf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) is { Length: > 0 } name ? name : path;
    }
}

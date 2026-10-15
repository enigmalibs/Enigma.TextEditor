using System;
using System.IO;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// A file is too large to be edited as text.
/// </summary>
/// <remarks>
/// An <see cref="IOException"/>, so a caller reporting I/O failures reports this one too; its own type,
/// so a caller that can offer another way to look at the file — the hex viewer — can tell it apart.
/// </remarks>
public sealed class FileTooLargeException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileTooLargeException"/> class.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <param name="length">Its size, in bytes.</param>
    /// <param name="maximum">The largest size the editor opens as text.</param>
    public FileTooLargeException(string filePath, long length, long maximum)
        : base($"'{Path.GetFileName(filePath)}' is {length / (1024 * 1024)} MB. The editor opens text files up to {maximum / (1024 * 1024)} MB.")
    {
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        Length = length;
        Maximum = maximum;
    }

    /// <summary>
    /// Gets the file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets its size, in bytes.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets the largest size the editor opens as text.
    /// </summary>
    public long Maximum { get; }
}

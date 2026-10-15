namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// What a quick look at a file says about it, before deciding how to open it.
/// </summary>
/// <param name="Length">Its size, in bytes.</param>
/// <param name="LooksBinary">Whether its first bytes look binary (see <see cref="ContentSniffer.IsBinary"/>).</param>
public readonly record struct FileProbe(long Length, bool LooksBinary)
{
    /// <summary>
    /// Gets a value indicating whether the file is too large to be opened as text.
    /// </summary>
    public bool IsTooLargeForText => Length > DocumentFile.MaximumBytes;
}

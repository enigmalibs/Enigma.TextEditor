namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// What is at a path, as <see cref="IDocumentService.ProbeAsync"/> found it.
/// </summary>
public enum PathKind
{
    /// <summary>
    /// Nothing — or nothing this account may see.
    /// </summary>
    Missing,

    /// <summary>
    /// A file.
    /// </summary>
    File,

    /// <summary>
    /// A folder.
    /// </summary>
    Folder,
}

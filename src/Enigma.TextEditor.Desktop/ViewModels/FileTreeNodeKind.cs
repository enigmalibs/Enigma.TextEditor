namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What one row of the explorer's tree stands for.
/// </summary>
public enum FileTreeNodeKind
{
    /// <summary>
    /// A folder, listed the first time it is expanded.
    /// </summary>
    Folder,

    /// <summary>
    /// A file, which opens in the active pane.
    /// </summary>
    File,

    /// <summary>
    /// The row a folder holds until it has been listed, which gives it an expander.
    /// </summary>
    Reading,

    /// <summary>
    /// The row a folder holds when it could not be listed: gone, not allowed, or unreadable.
    /// </summary>
    Unreadable,
}

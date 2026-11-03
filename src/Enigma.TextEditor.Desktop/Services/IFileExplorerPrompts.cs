using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks the user what the explorer's file operations have to ask.
/// </summary>
public interface IFileExplorerPrompts
{
    /// <summary>
    /// Asks for the name of a new file, checked as it is typed against the rules and the folder.
    /// </summary>
    /// <param name="directory">The folder the file will be created in.</param>
    /// <returns>The name, trimmed, or <see langword="null"/> when the user cancelled.</returns>
    Task<string?> AskNewFileNameAsync(string directory);

    /// <summary>
    /// Asks for the name of a new folder, checked as it is typed against the rules and the folder.
    /// </summary>
    /// <param name="directory">The folder the new one will be created in.</param>
    /// <returns>The name, trimmed, or <see langword="null"/> when the user cancelled.</returns>
    Task<string?> AskNewFolderNameAsync(string directory);

    /// <summary>
    /// Asks for the new name of a file or folder, starting from its current one, checked as it is typed.
    /// </summary>
    /// <param name="path">The entry to rename.</param>
    /// <param name="isFolder">Whether it is a folder.</param>
    /// <returns>The name, trimmed — its own name when it was left as it was — or <see langword="null"/> when the user cancelled.</returns>
    Task<string?> AskNewNameAsync(string path, bool isFolder);

    /// <summary>
    /// Asks whether to move a file or folder to the trash, from where it can be restored.
    /// </summary>
    /// <param name="name">The entry's name.</param>
    /// <param name="isFolder">Whether it is a folder, which goes with everything in it.</param>
    /// <returns>Whether the user confirmed; anything but the confirming button is <see langword="false"/>.</returns>
    Task<bool> ConfirmMoveToTrashAsync(string name, bool isFolder);

    /// <summary>
    /// Asks whether to delete a file or folder for good, when it cannot go to the trash.
    /// </summary>
    /// <param name="name">The entry's name.</param>
    /// <param name="isFolder">Whether it is a folder, which goes with everything in it.</param>
    /// <returns>Whether the user confirmed; anything but the confirming button is <see langword="false"/>.</returns>
    Task<bool> ConfirmPermanentDeleteAsync(string name, bool isFolder);
}

using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// How a file the editor would rather not open as text is to be opened.
/// </summary>
public enum BinaryFileChoice
{
    /// <summary>
    /// Open nothing.
    /// </summary>
    Cancel,

    /// <summary>
    /// Open it in the read-only hex viewer.
    /// </summary>
    Hex,

    /// <summary>
    /// Open it as text anyway.
    /// </summary>
    Text,
}

/// <summary>
/// Asks how to open a file that looks binary, or is too large to edit as text.
/// </summary>
public interface IBinaryFilePrompt
{
    /// <summary>
    /// Asks about a file whose first bytes look binary.
    /// </summary>
    /// <param name="fileName">The file's name.</param>
    /// <returns>The answer; dismissing the question is <see cref="BinaryFileChoice.Cancel"/>.</returns>
    Task<BinaryFileChoice> AskBinaryAsync(string fileName);

    /// <summary>
    /// Asks about a file too large to edit as text, offering the hex viewer instead.
    /// </summary>
    /// <param name="fileName">The file's name.</param>
    /// <param name="size">Its size, as the status bar would write it.</param>
    /// <returns>Whether to open it in the hex viewer.</returns>
    Task<bool> AskTooLargeAsync(string fileName, string size);
}

using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks whether to save a buffer's changes before it is closed.
/// </summary>
public interface IUnsavedChangesPrompt
{
    /// <summary>
    /// Asks about one buffer.
    /// </summary>
    /// <param name="documentTitle">The buffer's name, as its tab shows it.</param>
    /// <returns>
    /// The answer. Dismissing the question — Escape, a click outside it — is
    /// <see cref="UnsavedChangesChoice.Cancel"/>: nothing is lost by not answering.
    /// </returns>
    Task<UnsavedChangesChoice> AskAsync(string documentTitle);
}

using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks the user to confirm something that cannot be undone.
/// </summary>
public interface IConfirmationPrompt
{
    /// <summary>
    /// Asks.
    /// </summary>
    /// <param name="title">The question, in a few words.</param>
    /// <param name="message">What will happen.</param>
    /// <param name="confirmText">The confirming button's text — the action, not "OK".</param>
    /// <returns>
    /// Whether the user confirmed. Cancelling or dismissing the question is <see langword="false"/>.
    /// </returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmText);
}

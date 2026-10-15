using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Tells the user something went wrong, and waits until they have read it.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Shows an error.
    /// </summary>
    /// <param name="title">What failed, in a few words.</param>
    /// <param name="message">Why, and what the user can do about it.</param>
    /// <returns>A task that completes when the message has been dismissed.</returns>
    Task ShowErrorAsync(string title, string message);
}

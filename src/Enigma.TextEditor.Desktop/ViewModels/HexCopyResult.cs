namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What copying from the hex view came to: the text for the clipboard, or a message for the status line saying why
/// there is none.
/// </summary>
/// <param name="Text">The text to put on the clipboard; <see langword="null"/> when nothing is to be copied.</param>
/// <param name="Message">Why nothing was copied, for the status line; <see langword="null"/> when nothing is to be said.</param>
public sealed record HexCopyResult(string? Text, string? Message)
{
    /// <summary>
    /// Nothing to copy, and nothing to say: no selection, or not a hex tab.
    /// </summary>
    public static HexCopyResult Nothing { get; } = new(null, null);

    /// <summary>
    /// The text to copy.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The result.</returns>
    public static HexCopyResult Copied(string text) => new(text, null);

    /// <summary>
    /// Nothing copied, and why.
    /// </summary>
    /// <param name="message">Why, for the status line.</param>
    /// <returns>The result.</returns>
    public static HexCopyResult Refused(string message) => new(null, message);
}

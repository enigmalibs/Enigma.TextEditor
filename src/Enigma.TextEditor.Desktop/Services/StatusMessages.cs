using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The one-line message the status bar shows beside the mode: something worth saying that does not
/// deserve a dialog.
/// </summary>
/// <remarks>
/// A message stays until the next one replaces it or it is cleared, as Vim's message line does.
/// </remarks>
public sealed class StatusMessages : ObservableObject
{
    /// <summary>
    /// Gets the message on show, or <see langword="null"/>.
    /// </summary>
    public string? Current { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Shows a message, replacing the one on show.
    /// </summary>
    /// <param name="message">The message, or <see langword="null"/> to clear.</param>
    public void Show(string? message) => Current = message;
}

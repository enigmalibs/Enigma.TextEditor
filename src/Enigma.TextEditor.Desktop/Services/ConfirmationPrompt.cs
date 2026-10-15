using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The confirmation question, in the window's content dialog.
/// </summary>
public sealed class ConfirmationPrompt : IConfirmationPrompt
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmationPrompt"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public ConfirmationPrompt(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = message;
            dialog.PrimaryButtonText = confirmText;
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Close;
        });

        // Only the confirming button confirms: Escape and a click outside resolve as None.
        return result == DialogResult.Primary;
    }
}

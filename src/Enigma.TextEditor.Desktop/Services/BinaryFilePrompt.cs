using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The binary-file and too-large questions, in the window's content dialog.
/// </summary>
public sealed class BinaryFilePrompt : IBinaryFilePrompt
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryFilePrompt"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public BinaryFilePrompt(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task<BinaryFileChoice> AskBinaryAsync(string fileName)
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Open a binary file?";
            dialog.Content = $"{fileName} looks like a binary file. The hex viewer shows its bytes as they are; as text it would read as garbage, and saving it could damage it.";
            dialog.PrimaryButtonText = "Open in hex viewer";
            dialog.SecondaryButtonText = "Open as text";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        return result switch
        {
            DialogResult.Primary => BinaryFileChoice.Hex,
            DialogResult.Secondary => BinaryFileChoice.Text,
            _ => BinaryFileChoice.Cancel,
        };
    }

    /// <inheritdoc />
    public async Task<bool> AskTooLargeAsync(string fileName, string size)
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Too large to edit";
            dialog.Content = $"{fileName} is {size}, too large to edit as text. The read-only hex viewer can show it whatever its size.";
            dialog.PrimaryButtonText = "Open in hex viewer";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        return result == DialogResult.Primary;
    }
}

using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The unsaved-changes question, in the window's content dialog.
/// </summary>
public sealed class UnsavedChangesPrompt : IUnsavedChangesPrompt
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsavedChangesPrompt"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public UnsavedChangesPrompt(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task<UnsavedChangesChoice> AskAsync(string documentTitle)
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Save changes?";
            dialog.Content = $"Do you want to save the changes you made to {documentTitle}?";
            dialog.PrimaryButtonText = "Save";
            dialog.SecondaryButtonText = "Don't save";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        return result switch
        {
            DialogResult.Primary => UnsavedChangesChoice.Save,
            DialogResult.Secondary => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel,
        };
    }
}

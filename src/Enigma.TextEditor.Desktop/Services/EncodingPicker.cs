using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The encoding picker, in the window's content dialog.
/// </summary>
public sealed class EncodingPicker : IEncodingPicker
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncodingPicker"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public EncodingPicker(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task<TextEncoding?> PickAsync(string title, TextEncoding current)
    {
        EncodingPickerViewModel picker = new(TextEncodings.All, current);
        EncodingPickerView view = new() { DataContext = picker };

        // Enter or a double click picks without the button: the dialog is closed from here, which
        // resolves it as dismissed, and the pick is read back from the view-model.
        view.Picked += (_, _) => _dialogs.HideAsync().Forget();

        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = view;
            dialog.PrimaryButtonText = "Select";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        return result == DialogResult.Primary ? picker.Selected : picker.Committed;
    }
}

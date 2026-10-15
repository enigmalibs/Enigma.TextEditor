using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The language picker, in the window's content dialog.
/// </summary>
public sealed class LanguagePicker : ILanguagePicker
{
    private readonly IContentDialogService _dialogs;
    private readonly LanguageCatalog _catalog;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguagePicker"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    /// <param name="catalog">The languages offered.</param>
    public LanguagePicker(IContentDialogService dialogs, LanguageCatalog catalog)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <inheritdoc />
    public async Task<LanguageInfo?> PickAsync(LanguageInfo current)
    {
        LanguagePickerViewModel picker = new(_catalog.All, current);
        LanguagePickerView view = new() { DataContext = picker };

        // Enter or a double click in the list picks without the button: the dialog is closed from
        // here, which resolves it as dismissed, and the pick is read back from the view-model.
        view.Picked += (_, _) => _ = _dialogs.HideAsync();

        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Set language";
            dialog.Content = view;
            dialog.PrimaryButtonText = "Select";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Primary;
        });

        return result == DialogResult.Primary ? picker.Selected : picker.Committed;
    }
}

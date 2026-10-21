using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The About dialog, in the window's content dialog.
/// </summary>
public sealed class AboutDialogService : IAboutDialogService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutDialogService"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public AboutDialogService(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        AboutView view = new() { DataContext = new AboutViewModel() };

        // The service resets the host before configuring it, so only what this dialog has is set.
        await _dialogs.ShowAsync(dialog =>
        {
            // No title, as in Enigma.GitClient: the view already shows the application's icon and name
            // above everything else, and a title would only say "About" over them.
            dialog.Title = null;
            dialog.Content = view;

            // One button, and it only dismisses: the dialog asks nothing.
            dialog.CloseButtonText = "Close";
            dialog.DefaultButton = DefaultButton.Close;
        });
    }
}

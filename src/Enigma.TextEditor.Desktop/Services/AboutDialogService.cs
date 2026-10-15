using System;
using System.Threading.Tasks;
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

        // One button, and it only dismisses: the dialog asks nothing.
        await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "About";
            dialog.Content = view;
            dialog.CloseButtonText = "Close";
        });
    }
}

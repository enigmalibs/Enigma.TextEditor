using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The key-binding help, in the window's content dialog; <c>q</c> in the help closes it.
/// </summary>
public sealed class HelpDialogService : IHelpDialogService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpDialogService"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public HelpDialogService(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        HelpView view = new() { DataContext = new HelpViewModel(() => _ = _dialogs.HideAsync()) };

        await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Key bindings";
            dialog.Content = view;
            dialog.CloseButtonText = "Close";
        });

        // Typing carries on where it was: the editor, after ':help'.
        view.ReturnFocus();
    }
}

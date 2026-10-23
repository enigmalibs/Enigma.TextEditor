using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The Preferences dialog, in the window's content dialog.
/// </summary>
public sealed class PreferencesDialogService : IPreferencesDialogService
{
    private readonly IContentDialogService _dialogs;
    private readonly PreferencesViewModel _preferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesDialogService"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    /// <param name="preferences">What the dialog shows and changes.</param>
    public PreferencesDialogService(IContentDialogService dialogs, PreferencesViewModel preferences)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        PreferencesView view = new() { DataContext = _preferences };

        // One button, and it only dismisses: every change has already been applied and written.
        await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = "Preferences";
            dialog.Content = view;
            dialog.CloseButtonText = "Close";
            dialog.DefaultButton = DefaultButton.Close;
        });
    }
}

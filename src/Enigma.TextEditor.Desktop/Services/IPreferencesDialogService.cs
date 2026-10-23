using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Shows the Preferences dialog.
/// </summary>
public interface IPreferencesDialogService
{
    /// <summary>
    /// Shows the preferences, and waits until they are closed.
    /// </summary>
    /// <returns>A task that completes when the dialog has closed.</returns>
    Task ShowAsync();
}

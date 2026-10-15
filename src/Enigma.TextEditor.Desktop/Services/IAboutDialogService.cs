using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Shows the About dialog.
/// </summary>
public interface IAboutDialogService
{
    /// <summary>
    /// Shows it, and waits until it is closed.
    /// </summary>
    /// <returns>A task that completes when the dialog has closed.</returns>
    Task ShowAsync();
}

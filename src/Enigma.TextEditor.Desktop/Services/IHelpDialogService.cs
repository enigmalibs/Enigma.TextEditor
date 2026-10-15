using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Shows the key-binding help.
/// </summary>
public interface IHelpDialogService
{
    /// <summary>
    /// Shows the help, and waits until it is closed.
    /// </summary>
    /// <returns>A task that completes when the help has closed.</returns>
    Task ShowAsync();
}

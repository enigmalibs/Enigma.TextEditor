using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Shows the welcome dialog of a first start.
/// </summary>
public interface IFirstRunDialogService
{
    /// <summary>
    /// Shows the dialog, waits for it to close, then applies and writes what it shows.
    /// </summary>
    /// <returns>A task that completes once the choices are written, or their failure reported.</returns>
    /// <remarks>
    /// Every way out of the dialog — <em>Get started</em>, <em>Skip</em>, Esc — applies the same choices:
    /// the settings file has to exist afterwards, or the dialog would come back at the next start.
    /// </remarks>
    Task ShowAsync();
}

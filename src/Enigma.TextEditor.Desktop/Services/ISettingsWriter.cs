using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Writes the settings to their file after a change.
/// </summary>
public interface ISettingsWriter
{
    /// <summary>
    /// Writes the settings as they are now.
    /// </summary>
    /// <returns>Whether they were written.</returns>
    /// <remarks>
    /// A failure is reported to the user once, not on every change: a settings folder that cannot be
    /// written to will not become writable on the next toggle, and the change still applies for the
    /// session.
    /// </remarks>
    Task<bool> SaveAsync();
}

using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Applies the theme setting to the running application.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Repaints the application in the theme asked for.
    /// </summary>
    /// <param name="theme">The theme setting.</param>
    void Apply(AppTheme theme);
}

using Enigma.TextEditor.Core.Configuration;

namespace Enigma.TextEditor.Core.Settings;

/// <summary>
/// Loads <see cref="AppSettings"/>, recovering from a file that cannot be read.
/// </summary>
public static class SettingsLoader
{
    /// <summary>
    /// Loads the settings from a file, moving it aside and starting from the defaults when it is
    /// unreadable.
    /// </summary>
    /// <param name="settingsFilePath">The settings file. It does not have to exist.</param>
    /// <returns>The loaded settings, and what went wrong if anything did.</returns>
    public static ConfigLoadResult<AppSettings> Load(string settingsFilePath)
        => ConfigRecovery.Load(
            settingsFilePath,
            new AppSettingsValidator(),
            AppPaths.QuarantinedSettingsFileName);
}

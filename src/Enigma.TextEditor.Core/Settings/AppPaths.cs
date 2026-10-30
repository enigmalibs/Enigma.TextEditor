using System.IO;
using Enigma.Configuration;

namespace Enigma.TextEditor.Core.Settings;

/// <summary>
/// Where the application keeps its files under the per-user application-data root.
/// </summary>
public static class AppPaths
{
    /// <summary>
    /// The settings file's name.
    /// </summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>
    /// The name an unreadable settings file is moved aside to.
    /// </summary>
    public const string QuarantinedSettingsFileName = "settings.invalid.json";

    /// <summary>
    /// The folder the log files are written to.
    /// </summary>
    public const string LogsFolderName = "logs";

    /// <summary>
    /// The current log file's name; older days are archived beside it.
    /// </summary>
    public const string LogFileName = "app.log";

    /// <summary>
    /// The name of the file pointing at the instance used last, which a launch with files hands them to.
    /// </summary>
    public const string ActiveInstanceFileName = "instance.json";

    /// <summary>
    /// Gets the full path of the settings file.
    /// </summary>
    public static string SettingsFile
        => ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, SettingsFileName);

    /// <summary>
    /// Gets the full path of the current log file.
    /// </summary>
    public static string LogFile
        => ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, Path.Combine(LogsFolderName, LogFileName));

    /// <summary>
    /// Gets the full path of the file pointing at the instance used last.
    /// </summary>
    public static string ActiveInstanceFile
        => ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, ActiveInstanceFileName);
}

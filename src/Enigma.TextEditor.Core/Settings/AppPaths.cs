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
    /// The name of the file holding what the editor showed when it last closed.
    /// </summary>
    public const string SessionFileName = "session.json";

    /// <summary>
    /// The name an unusable session file is moved aside to.
    /// </summary>
    public const string QuarantinedSessionFileName = "session.invalid.json";

    /// <summary>
    /// The name of the file the instance that owns the session holds open.
    /// </summary>
    public const string SessionLockFileName = "session.lock";

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

    /// <summary>
    /// Gets the full path of the session file.
    /// </summary>
    public static string SessionFile
        => ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, SessionFileName);

    /// <summary>
    /// Gets the full path of the session's lock file.
    /// </summary>
    public static string SessionLockFile
        => ConfigPaths.InApplicationData(AppInfo.AppDataFolderName, SessionLockFileName);
}

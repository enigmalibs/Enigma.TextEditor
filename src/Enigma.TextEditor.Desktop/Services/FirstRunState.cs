using System;
using System.IO;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Whether this start is the very first one, kept for the window to read once it is on screen.
/// </summary>
/// <remarks>
/// <para>
/// The signal is the absence of the settings file, and it can only be read at one moment: before
/// <see cref="SettingsLoader.Load(string)"/> runs. An unreadable file is recovered by moving it aside,
/// which leaves the path absent — so the same question asked after the load would take a broken file for
/// a first run and greet someone who has used the editor for months.
/// </para>
/// <para>
/// A type rather than a bare <see cref="bool"/> because it is a container registration: an injected
/// <c>bool</c> says nothing about which flag it is.
/// </para>
/// </remarks>
/// <param name="IsFirstRun">
/// <see langword="true"/> when the settings file was not there, which is what makes this a first run.
/// </param>
public sealed record FirstRunState(bool IsFirstRun)
{
    /// <summary>
    /// Gets the "this is not a first run" state: the normal case, and the container's fallback.
    /// </summary>
    public static FirstRunState Returning { get; } = new(false);

    /// <summary>
    /// Gets the "nothing has been chosen yet" state.
    /// </summary>
    public static FirstRunState First { get; } = new(true);

    /// <summary>
    /// Decides whether this start is a first run, from the settings file alone.
    /// </summary>
    /// <param name="settingsFilePath">The settings file. Not read, only looked for.</param>
    /// <returns><see cref="First"/> when the file is not there; otherwise <see cref="Returning"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="settingsFilePath"/> is empty or white-space.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="settingsFilePath"/> is <see langword="null"/>.</exception>
    public static FirstRunState Detect(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);

        return File.Exists(settingsFilePath) ? Returning : First;
    }
}

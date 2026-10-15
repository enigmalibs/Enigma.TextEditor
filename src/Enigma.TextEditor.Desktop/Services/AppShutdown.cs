using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Closes the main window of the classic desktop lifetime.
/// </summary>
public sealed class AppShutdown : IAppShutdown
{
    /// <inheritdoc />
    public bool DiscardChangesOnExit { get; private set; }

    /// <inheritdoc />
    public void ExitDiscardingChanges()
    {
        DiscardChangesOnExit = true;
        RequestExit();
    }

    /// <inheritdoc />
    public void RequestExit()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        // Closing the window rather than shutting the lifetime down, so that the window's Closing
        // handler — where unsaved work is asked about — sees the exit like any other close.
        if (desktop.MainWindow is { } window)
        {
            window.Close();
        }
        else
        {
            desktop.Shutdown();
        }
    }
}

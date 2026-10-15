// Using directives stay at file scope, above the namespace declaration, throughout this project:
// inside a namespace beginning with "Enigma.", the simple name "Avalonia" resolves against the
// referenced Enigma.Avalonia.* assemblies instead of Avalonia itself.
using System;
using Avalonia;

namespace Enigma.TextEditor.Desktop;

/// <summary>
/// The application's entry point.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts the application. The arguments are the files to open; see <see cref="App"/>.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures Avalonia for the desktop. Also used by the designer.
    /// </summary>
    /// <returns>The configured builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            // Avalonia 12 replacement for the removed Avalonia.Diagnostics / this.AttachDevTools().
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}

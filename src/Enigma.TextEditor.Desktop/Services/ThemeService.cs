using System;
using Avalonia;
using Avalonia.Styling;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Maps the theme setting onto <see cref="Application.RequestedThemeVariant"/>.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly Application _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="application">The application whose variant is set.</param>
    public ThemeService(Application application)
        => _application = application ?? throw new ArgumentNullException(nameof(application));

    /// <summary>
    /// The variant a theme setting stands for. <see cref="AppTheme.System"/> is
    /// <see cref="ThemeVariant.Default"/>, which follows the operating system while the app runs.
    /// </summary>
    /// <param name="theme">The theme setting.</param>
    /// <returns>The variant.</returns>
    public static ThemeVariant ToVariant(AppTheme theme) => theme switch
    {
        AppTheme.Dark => ThemeVariant.Dark,
        AppTheme.Light => ThemeVariant.Light,
        _ => ThemeVariant.Default,
    };

    /// <inheritdoc />
    public void Apply(AppTheme theme) => _application.RequestedThemeVariant = ToVariant(theme);
}

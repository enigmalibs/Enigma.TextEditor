using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The welcome dialog of a first start: the four things worth choosing before the editor is first used —
/// the theme, Vim mode, the minimap and wrapping long lines.
/// </summary>
/// <remarks>
/// <para>
/// The choices start from the settings — the defaults, on a first run — and are held here until the
/// dialog closes. Only the theme is applied as it is chosen, so the dialog repaints in it; nothing is
/// written while the dialog is open.
/// </para>
/// <para>
/// <see cref="CompleteAsync"/> then applies all four through <see cref="SettingsMenuViewModel"/>, the one
/// place settings are applied and written, and writes them once. It runs on every way out of the dialog:
/// writing the settings file is what keeps the dialog from coming back at the next start.
/// </para>
/// </remarks>
public sealed class FirstRunViewModel : ObservableObject
{
    private readonly IThemeService _themes;
    private readonly SettingsMenuViewModel _menu;
    private readonly ILogger<FirstRunViewModel> _logger;

    // Not the property's own field: the choice starts from the settings without repainting anything.
    private AppTheme _theme;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstRunViewModel"/> class.
    /// </summary>
    /// <param name="settings">The settings the choices start from.</param>
    /// <param name="theme">Repaints the window in the theme being chosen.</param>
    /// <param name="menu">Applies and writes the choices once the dialog has closed.</param>
    /// <param name="logger">Records what the first run chose.</param>
    public FirstRunViewModel(
        IConfig<AppSettings> settings,
        IThemeService theme,
        SettingsMenuViewModel menu,
        ILogger<FirstRunViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _themes = theme ?? throw new ArgumentNullException(nameof(theme));
        _menu = menu ?? throw new ArgumentNullException(nameof(menu));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        AppSettings value = settings.Value;
        _theme = value.Theme;
        VimEnabled = value.VimEnabled;
        ShowMinimap = value.ShowMinimap;
        WordWrap = value.WordWrap;
    }

    /// <summary>
    /// Gets the themes to choose between, in the settings page's order.
    /// </summary>
    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    /// <summary>
    /// Gets or sets the theme. The window is repainted in it as it is chosen.
    /// </summary>
    public AppTheme Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                _themes.Apply(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether Vim drives the editors.
    /// </summary>
    public bool VimEnabled { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether the text has a minimap beside it, rather than a vertical
    /// scrollbar.
    /// </summary>
    public bool ShowMinimap { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap.
    /// </summary>
    public bool WordWrap { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Applies the four choices and writes the settings file.
    /// </summary>
    /// <returns>A task that completes once the settings are written, or their failure reported.</returns>
    public async Task CompleteAsync()
    {
        await _menu.ApplyFirstRunAsync(Theme, VimEnabled, ShowMinimap, WordWrap);

        _logger.LogInformation(
            "First run finished: theme {Theme}, Vim mode {VimEnabled}, minimap {ShowMinimap}, word wrap {WordWrap}.",
            Theme,
            VimEnabled,
            ShowMinimap,
            WordWrap);
    }
}

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The Settings menu, and the zoom commands of the View menu: every preference is a toggle or a
/// short choice, applied at once and written at once.
/// </summary>
/// <remarks>
/// Each item says what it is and shows its current value as a check or a radio mark. The settings
/// page shows the same settings with their full ranges and changes them through the
/// <c>Set…Async</c> methods here, so the menu and the dialog apply and write a change the same way.
/// The defaults only reach new buffers and files whose own indentation does not say.
/// </remarks>
public sealed class SettingsMenuViewModel : ObservableObject
{
    private readonly IConfig<AppSettings> _settings;
    private readonly ISettingsWriter _writer;
    private readonly IThemeService _theme;
    private readonly IVimSession _vim;
    private readonly EditorPreferences _preferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsMenuViewModel"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="writer">Writes them after each change.</param>
    /// <param name="theme">Applies the theme.</param>
    /// <param name="vim">Turns Vim on and off in every pane.</param>
    /// <param name="preferences">The editors' font size and wrapping.</param>
    public SettingsMenuViewModel(
        IConfig<AppSettings> settings,
        ISettingsWriter writer,
        IThemeService theme,
        IVimSession vim,
        EditorPreferences preferences)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        _vim = vim ?? throw new ArgumentNullException(nameof(vim));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));

        UseSystemThemeCommand = Command(() => SetThemeAsync(AppTheme.System));
        UseLightThemeCommand = Command(() => SetThemeAsync(AppTheme.Light));
        UseDarkThemeCommand = Command(() => SetThemeAsync(AppTheme.Dark));
        ToggleVimCommand = Command(() => SetVimEnabledAsync(!Value.VimEnabled));
        ToggleWordWrapCommand = Command(() => SetWordWrapAsync(!Value.WordWrap));
        ToggleMinimapCommand = Command(() => SetMinimapAsync(!Value.ShowMinimap));
        ResizeStep2Command = Command(() => SetPaneResizeStepAsync(2));
        ResizeStep5Command = Command(() => SetPaneResizeStepAsync(5));
        ResizeStep10Command = Command(() => SetPaneResizeStepAsync(10));
        DefaultTwoSpacesCommand = Command(() => SetDefaultIndentationAsync(false, 2));
        DefaultFourSpacesCommand = Command(() => SetDefaultIndentationAsync(false, 4));
        DefaultTabsCommand = Command(() => SetDefaultIndentationAsync(true, 4));
        DefaultLfCommand = Command(() => SetDefaultLineEndingAsync(LineEndingStyle.Lf));
        DefaultCrLfCommand = Command(() => SetDefaultLineEndingAsync(LineEndingStyle.CrLf));
        ZoomInCommand = Command(() => SetFontSizeAsync(Value.EditorFontSize + 1));
        ZoomOutCommand = Command(() => SetFontSizeAsync(Value.EditorFontSize - 1));
        ResetZoomCommand = Command(() => SetFontSizeAsync(AppSettings.DefaultFontSize));
    }

    /// <summary>
    /// Raised once a preference has been applied, before it is written, whoever changed it: this menu,
    /// the zoom keys or the settings page.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets <em>Settings → Theme → System</em>.
    /// </summary>
    public AsyncRelayCommand UseSystemThemeCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Theme → Light</em>.
    /// </summary>
    public AsyncRelayCommand UseLightThemeCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Theme → Dark</em>.
    /// </summary>
    public AsyncRelayCommand UseDarkThemeCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Vim Mode</em>.
    /// </summary>
    public AsyncRelayCommand ToggleVimCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Word Wrap</em>.
    /// </summary>
    public AsyncRelayCommand ToggleWordWrapCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Minimap</em>.
    /// </summary>
    public AsyncRelayCommand ToggleMinimapCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Pane Resize Step → 2%</em>.
    /// </summary>
    public AsyncRelayCommand ResizeStep2Command { get; }

    /// <summary>
    /// Gets <em>Settings → Pane Resize Step → 5%</em>.
    /// </summary>
    public AsyncRelayCommand ResizeStep5Command { get; }

    /// <summary>
    /// Gets <em>Settings → Pane Resize Step → 10%</em>.
    /// </summary>
    public AsyncRelayCommand ResizeStep10Command { get; }

    /// <summary>
    /// Gets <em>Settings → Default Indentation → Spaces: 2</em>.
    /// </summary>
    public AsyncRelayCommand DefaultTwoSpacesCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Default Indentation → Spaces: 4</em>.
    /// </summary>
    public AsyncRelayCommand DefaultFourSpacesCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Default Indentation → Tabs</em>.
    /// </summary>
    public AsyncRelayCommand DefaultTabsCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Default Line Endings → LF</em>.
    /// </summary>
    public AsyncRelayCommand DefaultLfCommand { get; }

    /// <summary>
    /// Gets <em>Settings → Default Line Endings → CRLF</em>.
    /// </summary>
    public AsyncRelayCommand DefaultCrLfCommand { get; }

    /// <summary>
    /// Gets <em>View → Zoom In</em>.
    /// </summary>
    public AsyncRelayCommand ZoomInCommand { get; }

    /// <summary>
    /// Gets <em>View → Zoom Out</em>.
    /// </summary>
    public AsyncRelayCommand ZoomOutCommand { get; }

    /// <summary>
    /// Gets <em>View → Reset Zoom</em>.
    /// </summary>
    public AsyncRelayCommand ResetZoomCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the theme follows the system.
    /// </summary>
    public bool IsSystemTheme => Value.Theme == AppTheme.System;

    /// <summary>
    /// Gets a value indicating whether the theme is always light.
    /// </summary>
    public bool IsLightTheme => Value.Theme == AppTheme.Light;

    /// <summary>
    /// Gets a value indicating whether the theme is always dark.
    /// </summary>
    public bool IsDarkTheme => Value.Theme == AppTheme.Dark;

    /// <summary>
    /// Gets a value indicating whether Vim drives the editors.
    /// </summary>
    public bool IsVimEnabled => Value.VimEnabled;

    /// <summary>
    /// Gets a value indicating whether long lines wrap.
    /// </summary>
    public bool IsWordWrap => Value.WordWrap;

    /// <summary>
    /// Gets a value indicating whether the text has a minimap beside it.
    /// </summary>
    public bool IsMinimap => Value.ShowMinimap;

    /// <summary>
    /// Gets a value indicating whether a resize key moves a splitter by 2%.
    /// </summary>
    public bool IsResizeStep2 => Value.PaneResizeStep == 2;

    /// <summary>
    /// Gets a value indicating whether a resize key moves a splitter by 5%.
    /// </summary>
    public bool IsResizeStep5 => Value.PaneResizeStep == 5;

    /// <summary>
    /// Gets a value indicating whether a resize key moves a splitter by 10%.
    /// </summary>
    public bool IsResizeStep10 => Value.PaneResizeStep == 10;

    /// <summary>
    /// Gets a value indicating whether new files are indented with two spaces.
    /// </summary>
    public bool IsDefaultTwoSpaces => !Value.IndentWithTabs && (Value.IndentSize == 2);

    /// <summary>
    /// Gets a value indicating whether new files are indented with four spaces.
    /// </summary>
    public bool IsDefaultFourSpaces => !Value.IndentWithTabs && (Value.IndentSize == 4);

    /// <summary>
    /// Gets a value indicating whether new files are indented with tabs.
    /// </summary>
    public bool IsDefaultTabs => Value.IndentWithTabs;

    /// <summary>
    /// Gets a value indicating whether new files are written with LF.
    /// </summary>
    public bool IsDefaultLf => Value.DefaultLineEnding == LineEndingStyle.Lf;

    /// <summary>
    /// Gets a value indicating whether new files are written with CRLF.
    /// </summary>
    public bool IsDefaultCrLf => Value.DefaultLineEnding == LineEndingStyle.CrLf;

    private AppSettings Value => _settings.Value;

    /// <summary>
    /// Applies a theme and writes it.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetThemeAsync(AppTheme theme)
    {
        Value.Theme = theme;
        _theme.Apply(theme);
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));

        await AppliedAsync();
    }

    /// <summary>
    /// Turns Vim on or off in every pane and writes it.
    /// </summary>
    /// <param name="enabled">Whether Vim drives the editors.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetVimEnabledAsync(bool enabled)
    {
        Value.VimEnabled = enabled;

        // The session re-reads the setting and announces it; every pane attaches or detaches its engine.
        _vim.OnSettingsChanged();
        OnPropertyChanged(nameof(IsVimEnabled));

        await AppliedAsync();
    }

    /// <summary>
    /// Turns word wrap on or off in every pane and writes it.
    /// </summary>
    /// <param name="wrap">Whether long lines wrap.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetWordWrapAsync(bool wrap)
    {
        Value.WordWrap = wrap;
        _preferences.WordWrap = wrap;
        OnPropertyChanged(nameof(IsWordWrap));

        await AppliedAsync();
    }

    /// <summary>
    /// Shows the minimap, or the vertical scrollbar instead, in every pane and writes it.
    /// </summary>
    /// <param name="show">Whether the text has a minimap beside it.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetMinimapAsync(bool show)
    {
        Value.ShowMinimap = show;
        _preferences.ShowMinimap = show;
        OnPropertyChanged(nameof(IsMinimap));

        await AppliedAsync();
    }

    /// <summary>
    /// Sets how far a resize key moves a splitter and writes it.
    /// </summary>
    /// <param name="percent">The step, in percent of the split; kept within the setting's range.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    /// <remarks>
    /// Any step but the menu's three can be set from the settings page or written in settings.json;
    /// the menu then shows none of its three checked.
    /// </remarks>
    public async Task SetPaneResizeStepAsync(int percent)
    {
        int clamped = Math.Clamp(percent, AppSettings.MinimumPaneResizeStep, AppSettings.MaximumPaneResizeStep);

        Value.PaneResizeStep = clamped;
        _preferences.PaneResizeStep = clamped;
        OnPropertyChanged(nameof(IsResizeStep2));
        OnPropertyChanged(nameof(IsResizeStep5));
        OnPropertyChanged(nameof(IsResizeStep10));

        await AppliedAsync();
    }

    /// <summary>
    /// Sets the indentation new files start with and writes it.
    /// </summary>
    /// <param name="tabs">Whether a level is a tab rather than spaces.</param>
    /// <param name="size">How many columns a level is; kept within the setting's range.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetDefaultIndentationAsync(bool tabs, int size)
    {
        Value.IndentWithTabs = tabs;
        Value.IndentSize = Math.Clamp(size, AppSettings.MinimumIndentSize, AppSettings.MaximumIndentSize);
        OnPropertyChanged(nameof(IsDefaultTwoSpaces));
        OnPropertyChanged(nameof(IsDefaultFourSpaces));
        OnPropertyChanged(nameof(IsDefaultTabs));

        await AppliedAsync();
    }

    /// <summary>
    /// Sets the line terminator new files are written with and writes it.
    /// </summary>
    /// <param name="lineEnding">The line terminator.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetDefaultLineEndingAsync(LineEndingStyle lineEnding)
    {
        Value.DefaultLineEnding = lineEnding;
        OnPropertyChanged(nameof(IsDefaultLf));
        OnPropertyChanged(nameof(IsDefaultCrLf));

        await AppliedAsync();
    }

    /// <summary>
    /// Sets the editors' font size and writes it.
    /// </summary>
    /// <param name="size">The size; kept within the zoom's limits.</param>
    /// <returns>A task that completes once the settings are written, or at once if nothing changed.</returns>
    public async Task SetFontSizeAsync(int size)
    {
        int clamped = Math.Clamp(size, AppSettings.MinimumFontSize, AppSettings.MaximumFontSize);

        if (clamped == Value.EditorFontSize)
        {
            return;
        }

        Value.EditorFontSize = clamped;
        _preferences.FontSize = clamped;

        await AppliedAsync();
    }

    /// <summary>
    /// Sets the family the editors' text is drawn in and writes it.
    /// </summary>
    /// <param name="family">The family's name, or an empty one for the built-in monospace stack.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task SetFontFamilyAsync(string family)
    {
        ArgumentNullException.ThrowIfNull(family);

        Value.EditorFontFamily = family.Trim();
        _preferences.FontFamily = EditorPreferences.FamilyFor(Value.EditorFontFamily);

        await AppliedAsync();
    }

    /// <summary>
    /// Puts every preference back to its default, applies them all, and writes them once.
    /// </summary>
    /// <returns>A task that completes once the settings are written.</returns>
    public async Task ResetAsync()
    {
        AppSettings defaults = new();

        Value.Theme = defaults.Theme;
        Value.VimEnabled = defaults.VimEnabled;
        Value.WordWrap = defaults.WordWrap;
        Value.ShowMinimap = defaults.ShowMinimap;
        Value.PaneResizeStep = defaults.PaneResizeStep;
        Value.EditorFontSize = defaults.EditorFontSize;
        Value.EditorFontFamily = defaults.EditorFontFamily;
        Value.IndentWithTabs = defaults.IndentWithTabs;
        Value.IndentSize = defaults.IndentSize;
        Value.DefaultLineEnding = defaults.DefaultLineEnding;

        _theme.Apply(Value.Theme);
        _vim.OnSettingsChanged();
        _preferences.WordWrap = Value.WordWrap;
        _preferences.ShowMinimap = Value.ShowMinimap;
        _preferences.PaneResizeStep = Value.PaneResizeStep;
        _preferences.FontSize = Value.EditorFontSize;
        _preferences.FontFamily = EditorPreferences.FamilyFor(Value.EditorFontFamily);

        // Every check and radio mark of the menu at once.
        OnPropertyChanged(string.Empty);

        await AppliedAsync();
    }

    /// <summary>
    /// Applies what the welcome dialog of a first start chose, and writes the settings once.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="vimEnabled">Whether Vim drives the editors.</param>
    /// <param name="showMinimap">Whether the text has a minimap beside it.</param>
    /// <param name="wordWrap">Whether long lines wrap.</param>
    /// <returns>A task that completes once the settings are written.</returns>
    /// <remarks>
    /// Written even when nothing differs from the defaults: on a first run, the file's absence is what
    /// would bring the dialog back.
    /// </remarks>
    public async Task ApplyFirstRunAsync(AppTheme theme, bool vimEnabled, bool showMinimap, bool wordWrap)
    {
        Value.Theme = theme;
        Value.VimEnabled = vimEnabled;
        Value.ShowMinimap = showMinimap;
        Value.WordWrap = wordWrap;

        _theme.Apply(theme);
        _vim.OnSettingsChanged();
        _preferences.ShowMinimap = showMinimap;
        _preferences.WordWrap = wordWrap;

        // Every check and radio mark of the menu at once.
        OnPropertyChanged(string.Empty);

        await AppliedAsync();
    }

    // A held Ctrl+= must not be dropped, nor the menu item grey out, while the previous step is
    // still being written: the writer serializes the writes instead.
    private static AsyncRelayCommand Command(Func<Task> execute)
        => new(execute, AsyncRelayCommandOptions.AllowConcurrentExecutions);

    // Every change ends here once it is applied: whoever shows the preferences is told, then they are
    // written.
    private async Task AppliedAsync()
    {
        Changed?.Invoke(this, EventArgs.Empty);

        await _writer.SaveAsync();
    }
}

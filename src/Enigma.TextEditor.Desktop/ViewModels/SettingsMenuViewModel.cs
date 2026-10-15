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
/// No preferences dialog: each item says what it is and shows its current value as a check or a
/// radio mark. The defaults only reach new buffers and files whose own indentation does not say.
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
        ToggleVimCommand = Command(OnToggleVimAsync);
        ToggleWordWrapCommand = Command(OnToggleWordWrapAsync);
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

    // A held Ctrl+= must not be dropped, nor the menu item grey out, while the previous step is
    // still being written: the writer serializes the writes instead.
    private static AsyncRelayCommand Command(Func<Task> execute)
        => new(execute, AsyncRelayCommandOptions.AllowConcurrentExecutions);

    private async Task SetThemeAsync(AppTheme theme)
    {
        Value.Theme = theme;
        _theme.Apply(theme);
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));

        await _writer.SaveAsync();
    }

    private async Task OnToggleVimAsync()
    {
        Value.VimEnabled = !Value.VimEnabled;

        // The session re-reads the setting and announces it; every pane attaches or detaches its engine.
        _vim.OnSettingsChanged();
        OnPropertyChanged(nameof(IsVimEnabled));

        await _writer.SaveAsync();
    }

    private async Task OnToggleWordWrapAsync()
    {
        Value.WordWrap = !Value.WordWrap;
        _preferences.WordWrap = Value.WordWrap;
        OnPropertyChanged(nameof(IsWordWrap));

        await _writer.SaveAsync();
    }

    private async Task SetDefaultIndentationAsync(bool tabs, int size)
    {
        Value.IndentWithTabs = tabs;
        Value.IndentSize = size;
        OnPropertyChanged(nameof(IsDefaultTwoSpaces));
        OnPropertyChanged(nameof(IsDefaultFourSpaces));
        OnPropertyChanged(nameof(IsDefaultTabs));

        await _writer.SaveAsync();
    }

    private async Task SetDefaultLineEndingAsync(LineEndingStyle lineEnding)
    {
        Value.DefaultLineEnding = lineEnding;
        OnPropertyChanged(nameof(IsDefaultLf));
        OnPropertyChanged(nameof(IsDefaultCrLf));

        await _writer.SaveAsync();
    }

    private async Task SetFontSizeAsync(int size)
    {
        int clamped = Math.Clamp(size, AppSettings.MinimumFontSize, AppSettings.MaximumFontSize);

        if (clamped == Value.EditorFontSize)
        {
            return;
        }

        Value.EditorFontSize = clamped;
        _preferences.FontSize = clamped;

        await _writer.SaveAsync();
    }
}

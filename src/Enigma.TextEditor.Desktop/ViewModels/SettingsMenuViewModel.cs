using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The Settings menu, the zoom commands of the View menu and the status bar's font size item: every
/// preference is a toggle or a short choice, applied at once and written at once.
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
    private readonly ObservableCollection<FontSizeChoice> _fontSizeChoices = [];

    // The menu's steps, in percent: the settings page and settings.json can set any other.
    private static readonly int[] PaneResizeSteps = [2, 5, 10];

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

        ThemeChoices =
        [
            new MenuChoice("_System", "theme", () => Value.Theme == AppTheme.System, Command(() => SetThemeAsync(AppTheme.System))),
            new MenuChoice("_Light", "theme", () => Value.Theme == AppTheme.Light, Command(() => SetThemeAsync(AppTheme.Light))),
            new MenuChoice("_Dark", "theme", () => Value.Theme == AppTheme.Dark, Command(() => SetThemeAsync(AppTheme.Dark))),
        ];
        ToggleVimCommand = Command(() => SetVimEnabledAsync(!Value.VimEnabled));
        ToggleWordWrapCommand = Command(() => SetWordWrapAsync(!Value.WordWrap));
        ToggleMinimapCommand = Command(() => SetMinimapAsync(!Value.ShowMinimap));
        PaneResizeStepChoices =
        [
            .. PaneResizeSteps.Select(step => new MenuChoice(
                string.Create(CultureInfo.InvariantCulture, $"{step}%"),
                "paneResizeStep",
                () => Value.PaneResizeStep == step,
                Command(() => SetPaneResizeStepAsync(step)))),
        ];
        DefaultIndentationChoices =
        [
            new MenuChoice("Spaces: 2", "defaultIndentation", () => !Value.IndentWithTabs && (Value.IndentSize == 2), Command(() => SetDefaultIndentationAsync(false, 2))),
            new MenuChoice("Spaces: 4", "defaultIndentation", () => !Value.IndentWithTabs && (Value.IndentSize == 4), Command(() => SetDefaultIndentationAsync(false, 4))),
            new MenuChoice("Tabs", "defaultIndentation", () => Value.IndentWithTabs, Command(() => SetDefaultIndentationAsync(true, 4))),
        ];
        DefaultLineEndingChoices =
        [
            new MenuChoice("LF", "defaultLineEndings", () => Value.DefaultLineEnding == LineEndingStyle.Lf, Command(() => SetDefaultLineEndingAsync(LineEndingStyle.Lf))),
            new MenuChoice("CRLF", "defaultLineEndings", () => Value.DefaultLineEnding == LineEndingStyle.CrLf, Command(() => SetDefaultLineEndingAsync(LineEndingStyle.CrLf))),
        ];
        ZoomInCommand = Command(() => SetFontSizeAsync(Value.EditorFontSize + 1));
        ZoomOutCommand = Command(() => SetFontSizeAsync(Value.EditorFontSize - 1));
        ResetZoomCommand = Command(() => SetFontSizeAsync(AppSettings.DefaultFontSize));
        SetFontSizeCommand = new AsyncRelayCommand<int>(SetFontSizeAsync, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        FontSizeChoices = new ReadOnlyObservableCollection<FontSizeChoice>(_fontSizeChoices);
        RefreshFontSizeChoices();
    }

    /// <summary>
    /// Raised once a preference has been applied, before it is written, whoever changed it: this menu,
    /// the zoom keys or the settings page.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets <em>Settings → Theme</em>: System, Light, Dark.
    /// </summary>
    public IReadOnlyList<MenuChoice> ThemeChoices { get; }

    /// <summary>
    /// Gets <em>Settings → Pane Resize Step</em>: how far a resize key moves a splitter, in percent.
    /// </summary>
    /// <remarks>
    /// Any step but these can be set from the settings page or written in settings.json; the menu then shows none of
    /// them checked.
    /// </remarks>
    public IReadOnlyList<MenuChoice> PaneResizeStepChoices { get; }

    /// <summary>
    /// Gets <em>Settings → Default Indentation</em>: two spaces, four spaces, tabs.
    /// </summary>
    public IReadOnlyList<MenuChoice> DefaultIndentationChoices { get; }

    /// <summary>
    /// Gets <em>Settings → Default Line Endings</em>: LF, CRLF.
    /// </summary>
    public IReadOnlyList<MenuChoice> DefaultLineEndingChoices { get; }

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
    /// Gets the status bar's font size list: sets the editors' font size to the size it is given.
    /// </summary>
    public AsyncRelayCommand<int> SetFontSizeCommand { get; }

    /// <summary>
    /// Gets the status bar's font size item: <c>Font Size: 14</c>.
    /// </summary>
    public string FontSizeText => string.Create(CultureInfo.InvariantCulture, $"Font Size: {Value.EditorFontSize}");

    /// <summary>
    /// Gets the sizes the status bar's font size item offers, the current one checked.
    /// </summary>
    /// <remarks>
    /// One collection for the life of the menu, brought in step in place whenever the size changes: a flyout's items
    /// follow the collection it first showed, and would never see a new one.
    /// </remarks>
    public ReadOnlyObservableCollection<FontSizeChoice> FontSizeChoices { get; }

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
        MenuChoice.Refresh(ThemeChoices);

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
    public async Task SetPaneResizeStepAsync(int percent)
    {
        int clamped = Math.Clamp(percent, AppSettings.MinimumPaneResizeStep, AppSettings.MaximumPaneResizeStep);

        Value.PaneResizeStep = clamped;
        _preferences.PaneResizeStep = clamped;
        MenuChoice.Refresh(PaneResizeStepChoices);

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
        MenuChoice.Refresh(DefaultIndentationChoices);

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
        MenuChoice.Refresh(DefaultLineEndingChoices);

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
        OnPropertyChanged(nameof(FontSizeText));
        RefreshFontSizeChoices();

        await AppliedAsync();
    }

    /// <summary>
    /// Zooms the editors' text by a number of sizes: what Ctrl+wheel and the touchpad's pinch do.
    /// </summary>
    /// <param name="steps">How many sizes: positive larger, negative smaller; the result stays within the zoom's limits.</param>
    /// <returns>A task that completes once the settings are written, or at once if nothing changed.</returns>
    public Task ZoomByAsync(int steps)
        => steps == 0 ? Task.CompletedTask : SetFontSizeAsync(Value.EditorFontSize + steps);

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
        RefreshFontSizeChoices();

        // Every check and radio mark of the menu at once.
        OnPropertyChanged(string.Empty);
        RefreshChoices();

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
        RefreshChoices();

        await AppliedAsync();
    }

    private void RefreshChoices()
    {
        MenuChoice.Refresh(ThemeChoices);
        MenuChoice.Refresh(PaneResizeStepChoices);
        MenuChoice.Refresh(DefaultIndentationChoices);
        MenuChoice.Refresh(DefaultLineEndingChoices);
    }

    // A held Ctrl+= must not be dropped, nor the menu item grey out, while the previous step is
    // still being written: the writer serializes the writes instead.
    private static AsyncRelayCommand Command(Func<Task> execute)
        => new(execute, AsyncRelayCommandOptions.AllowConcurrentExecutions);

    // Entry by entry, so the list on screen changes only where the sizes or the check do.
    private void RefreshFontSizeChoices()
    {
        int current = Value.EditorFontSize;
        FontSizeChoice[] wanted =
        [
            .. FontSizes.ListFor(current).Select(size => new FontSizeChoice(
                size,
                size == current,
                size == AppSettings.DefaultFontSize,
                SetFontSizeCommand)),
        ];

        for (int index = 0; index < wanted.Length; index++)
        {
            if (index == _fontSizeChoices.Count)
            {
                _fontSizeChoices.Add(wanted[index]);
            }
            else if (_fontSizeChoices[index] != wanted[index])
            {
                _fontSizeChoices[index] = wanted[index];
            }
        }

        while (_fontSizeChoices.Count > wanted.Length)
        {
            _fontSizeChoices.RemoveAt(_fontSizeChoices.Count - 1);
        }
    }

    // Every change ends here once it is applied: whoever shows the preferences is told, then they are
    // written.
    private async Task AppliedAsync()
    {
        Changed?.Invoke(this, EventArgs.Empty);

        await _writer.SaveAsync();
    }
}

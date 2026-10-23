using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The Preferences dialog: every setting, grouped by what it acts on, with the full range each one
/// allows — the editor's font among them.
/// </summary>
/// <remarks>
/// <para>
/// It changes nothing itself. Every change goes through <see cref="SettingsMenuViewModel"/>, which
/// applies it and writes it, so the menu and the dialog are two views of the same preferences.
/// </para>
/// <para>
/// The values are read from the settings each time, and all of them are announced again whenever any
/// preference changes — from the menu, the zoom keys or the dialog itself — so an open dialog never
/// shows a stale value. The setters do not await the write: a binding has nowhere to return it to,
/// the change is applied before the write begins, and the writer reports its own failures.
/// </para>
/// </remarks>
public sealed class PreferencesViewModel : ObservableObject
{
    /// <summary>
    /// What the font list calls "no choice": the built-in monospace stack.
    /// </summary>
    public const string DefaultFontLabel = "Default";

    private readonly IConfig<AppSettings> _settings;
    private readonly SettingsMenuViewModel _menu;

    // Set while every value is announced again, so a control that writes back what it was just told
    // does not start a change of its own.
    private bool _announcing;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesViewModel"/> class.
    /// </summary>
    /// <param name="settings">The settings, which the values are read from.</param>
    /// <param name="menu">Applies and writes every change.</param>
    public PreferencesViewModel(IConfig<AppSettings> settings, SettingsMenuViewModel menu)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _menu = menu ?? throw new ArgumentNullException(nameof(menu));

        ResetCommand = new AsyncRelayCommand(_menu.ResetAsync);

        // Both are container singletons: the subscription lives as long as the menu does.
        _menu.Changed += OnPreferencesChanged;
    }

    /// <summary>
    /// Gets the families the editor's text can be drawn in: <see cref="DefaultFontLabel"/>, then this
    /// machine's monospace faces, then the saved family if the machine does not have it.
    /// </summary>
    /// <remarks>
    /// Built on first read rather than in the constructor: listing the installed fonts needs a font manager,
    /// and the container is resolved where there is no Avalonia platform behind it. A saved family stays
    /// listed so the dropdown can show it, rather than show nothing.
    /// </remarks>
    public IReadOnlyList<string> Fonts => field ??= BuildFontList(Value.EditorFontFamily);

    /// <summary>
    /// Gets or sets the family the editor's text is drawn in, as the font list writes it.
    /// </summary>
    public string EditorFont
    {
        get => string.IsNullOrWhiteSpace(Value.EditorFontFamily) ? DefaultFontLabel : Value.EditorFontFamily;
        set
        {
            // Null when the dropdown loses its selection, which is not a choice.
            if (value is null)
            {
                return;
            }

            string family = string.Equals(value, DefaultFontLabel, StringComparison.Ordinal) ? string.Empty : value;

            if (!string.Equals(family, Value.EditorFontFamily, StringComparison.Ordinal) && !_announcing)
            {
                _ = _menu.SetFontFamilyAsync(family);
            }
        }
    }

    /// <summary>
    /// Gets or sets the editor's font size: the one the zoom keys change.
    /// </summary>
    public int EditorFontSize
    {
        get => Value.EditorFontSize;
        set
        {
            if ((value != Value.EditorFontSize) && !_announcing)
            {
                _ = _menu.SetFontSizeAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets the themes to choose between.
    /// </summary>
    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    /// <summary>
    /// Gets the line terminators a new file can be written with.
    /// </summary>
    public IReadOnlyList<LineEndingStyle> LineEndings { get; } = [LineEndingStyle.Lf, LineEndingStyle.CrLf];

    /// <summary>
    /// Gets the command that puts every preference back to its default.
    /// </summary>
    public AsyncRelayCommand ResetCommand { get; }

    /// <summary>
    /// Gets or sets the theme the window is painted in.
    /// </summary>
    public AppTheme Theme
    {
        get => Value.Theme;
        set
        {
            if ((value != Value.Theme) && !_announcing)
            {
                _ = _menu.SetThemeAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the text has a minimap beside it, rather than a vertical
    /// scrollbar.
    /// </summary>
    public bool ShowMinimap
    {
        get => Value.ShowMinimap;
        set
        {
            if ((value != Value.ShowMinimap) && !_announcing)
            {
                _ = _menu.SetMinimapAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap.
    /// </summary>
    public bool WordWrap
    {
        get => Value.WordWrap;
        set
        {
            if ((value != Value.WordWrap) && !_announcing)
            {
                _ = _menu.SetWordWrapAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether Vim drives the editors.
    /// </summary>
    public bool VimEnabled
    {
        get => Value.VimEnabled;
        set
        {
            if ((value != Value.VimEnabled) && !_announcing)
            {
                _ = _menu.SetVimEnabledAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets how far a resize key moves a splitter, in percent of the split.
    /// </summary>
    public int PaneResizeStep
    {
        get => Value.PaneResizeStep;
        set
        {
            if ((value != Value.PaneResizeStep) && !_announcing)
            {
                _ = _menu.SetPaneResizeStepAsync(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a new file is indented with tabs rather than spaces.
    /// </summary>
    public bool IndentWithTabs
    {
        get => Value.IndentWithTabs;
        set
        {
            if ((value != Value.IndentWithTabs) && !_announcing)
            {
                _ = _menu.SetDefaultIndentationAsync(value, Value.IndentSize);
            }
        }
    }

    /// <summary>
    /// Gets or sets how many columns one level of a new file's indentation is.
    /// </summary>
    public int IndentSize
    {
        get => Value.IndentSize;
        set
        {
            if ((value != Value.IndentSize) && !_announcing)
            {
                _ = _menu.SetDefaultIndentationAsync(Value.IndentWithTabs, value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the line terminator a new file is written with.
    /// </summary>
    public LineEndingStyle DefaultLineEnding
    {
        get => Value.DefaultLineEnding;
        set
        {
            if ((value != Value.DefaultLineEnding) && !_announcing)
            {
                _ = _menu.SetDefaultLineEndingAsync(value);
            }
        }
    }

    private AppSettings Value => _settings.Value;

    private static IReadOnlyList<string> BuildFontList(string saved)
    {
        List<string> fonts = [DefaultFontLabel, .. MonospaceFonts.InstalledNames()];

        if (!string.IsNullOrWhiteSpace(saved) && !fonts.Contains(saved, StringComparer.Ordinal))
        {
            fonts.Add(saved);
        }

        return fonts;
    }

    private void OnPreferencesChanged(object? sender, EventArgs e)
    {
        _announcing = true;

        try
        {
            OnPropertyChanged(nameof(EditorFont));
            OnPropertyChanged(nameof(EditorFontSize));
            OnPropertyChanged(nameof(Theme));
            OnPropertyChanged(nameof(ShowMinimap));
            OnPropertyChanged(nameof(WordWrap));
            OnPropertyChanged(nameof(VimEnabled));
            OnPropertyChanged(nameof(PaneResizeStep));
            OnPropertyChanged(nameof(IndentWithTabs));
            OnPropertyChanged(nameof(IndentSize));
            OnPropertyChanged(nameof(DefaultLineEnding));
        }
        finally
        {
            _announcing = false;
        }
    }
}

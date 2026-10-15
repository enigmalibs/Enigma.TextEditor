using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Configuration;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// How every editor looks: its font, its size and whether long lines wrap.
/// </summary>
/// <remarks>
/// One instance for the whole window, so that every pane follows a change at once.
/// </remarks>
public sealed class EditorPreferences : ObservableObject
{
    /// <summary>
    /// The editor's font: the first monospace family the machine has, on any of the three platforms.
    /// </summary>
    public static readonly FontFamily MonospaceFamily =
        new("Cascadia Mono, Consolas, JetBrains Mono, DejaVu Sans Mono, Menlo, Courier New, monospace");

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorPreferences"/> class from the settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public EditorPreferences(IConfig<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        FontSize = settings.Value.EditorFontSize;
        WordWrap = settings.Value.WordWrap;
    }

    /// <summary>
    /// Gets the editor's font.
    /// </summary>
    public FontFamily FontFamily => MonospaceFamily;

    /// <summary>
    /// Gets or sets the editor's font size.
    /// </summary>
    public double FontSize { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap.
    /// </summary>
    public bool WordWrap { get; set => SetProperty(ref field, value); }
}

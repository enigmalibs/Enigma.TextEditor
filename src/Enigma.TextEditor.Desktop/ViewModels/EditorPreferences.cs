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
    /// The built-in monospace stack: the first of these the machine has, on any of the three platforms.
    /// </summary>
    public const string MonospaceStack = "Cascadia Mono, Consolas, JetBrains Mono, DejaVu Sans Mono, Menlo, Courier New, monospace";

    /// <summary>
    /// The editor's font when no family is chosen.
    /// </summary>
    public static readonly FontFamily MonospaceFamily = new(MonospaceStack);

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorPreferences"/> class from the settings.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public EditorPreferences(IConfig<AppSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        FontFamily = FamilyFor(settings.Value.EditorFontFamily);
        FontSize = settings.Value.EditorFontSize;
        WordWrap = settings.Value.WordWrap;
        ShowMinimap = settings.Value.ShowMinimap;
        PaneResizeStep = settings.Value.PaneResizeStep;
    }

    /// <summary>
    /// Gets or sets the editor's font.
    /// </summary>
    public FontFamily FontFamily { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets the editor's font size.
    /// </summary>
    public double FontSize { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap.
    /// </summary>
    public bool WordWrap { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether the text has a minimap beside it, rather than a vertical
    /// scrollbar.
    /// </summary>
    public bool ShowMinimap { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets how far a resize key moves a splitter, in percent of the split.
    /// </summary>
    public int PaneResizeStep { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// The font for a chosen family.
    /// </summary>
    /// <param name="family">The family's name, or an empty one for the built-in stack.</param>
    /// <returns>
    /// The family, with the built-in stack behind it: a name the machine does not have falls back to a
    /// monospace face rather than to the interface's own.
    /// </returns>
    public static FontFamily FamilyFor(string? family)
        => string.IsNullOrWhiteSpace(family) ? MonospaceFamily : new FontFamily($"{family.Trim()}, {MonospaceStack}");
}

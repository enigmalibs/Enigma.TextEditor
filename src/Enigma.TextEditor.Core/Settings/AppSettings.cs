using System.ComponentModel.DataAnnotations;
using Enigma.TextEditor.Core.Documents;

namespace Enigma.TextEditor.Core.Settings;

/// <summary>
/// The user's preferences, persisted as <c>settings.json</c> in the application-data folder.
/// </summary>
/// <remarks>
/// Every property has a default, so a missing file and a file written by an older version both load:
/// a key that is absent keeps the value set here. Enums are persisted by name.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>
    /// The smallest editor font size the zoom commands go down to.
    /// </summary>
    public const int MinimumFontSize = 6;

    /// <summary>
    /// The largest editor font size the zoom commands go up to.
    /// </summary>
    public const int MaximumFontSize = 72;

    /// <summary>
    /// The editor font size a fresh install starts with, and what resetting the zoom goes back to.
    /// </summary>
    public const int DefaultFontSize = 14;

    /// <summary>
    /// The least a resize key may move a splitter by, in percent of the split.
    /// </summary>
    public const int MinimumPaneResizeStep = 1;

    /// <summary>
    /// The most a resize key may move a splitter by, in percent of the split.
    /// </summary>
    public const int MaximumPaneResizeStep = 50;

    /// <summary>
    /// How far a resize key moves a splitter on a fresh install, in percent of the split.
    /// </summary>
    public const int DefaultPaneResizeStep = 5;

    /// <summary>
    /// The fewest columns one level of indentation may be.
    /// </summary>
    public const int MinimumIndentSize = 1;

    /// <summary>
    /// The most columns one level of indentation may be.
    /// </summary>
    public const int MaximumIndentSize = 16;

    /// <summary>
    /// Gets or sets the theme variant the window is painted in.
    /// </summary>
    [EnumDataType(typeof(AppTheme))]
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>
    /// Gets or sets a value indicating whether the editor is driven by the vim engine.
    /// </summary>
    public bool VimEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether long lines wrap at the edge of the editor.
    /// </summary>
    public bool WordWrap { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the editor shows a minimap beside the text rather than
    /// a vertical scrollbar.
    /// </summary>
    public bool ShowMinimap { get; set; } = true;

    /// <summary>
    /// Gets or sets how far the resize keys (<c>Ctrl+W &gt;</c>, <c>g&gt;</c> and the others) move a
    /// splitter, in percent of the split: at 4, one step takes a 50/50 split to 54/46.
    /// </summary>
    [Range(MinimumPaneResizeStep, MaximumPaneResizeStep)]
    public int PaneResizeStep { get; set; } = DefaultPaneResizeStep;

    /// <summary>
    /// Gets or sets the editor's font size, in device-independent pixels.
    /// </summary>
    [Range(MinimumFontSize, MaximumFontSize)]
    public int EditorFontSize { get; set; } = DefaultFontSize;

    /// <summary>
    /// Gets or sets the family the editor's text is drawn in, or an empty string for the built-in
    /// monospace stack.
    /// </summary>
    /// <remarks>
    /// A family name, as the font list shows it. One the machine does not have falls back through the
    /// built-in stack, so a settings file carried to another machine still draws in a monospace face.
    /// </remarks>
    public string EditorFontFamily { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a new file — and a file whose own indentation cannot be
    /// told — is indented with tabs rather than spaces.
    /// </summary>
    public bool IndentWithTabs { get; set; }

    /// <summary>
    /// Gets or sets how many columns one level of indentation is, for those same files.
    /// </summary>
    [Range(MinimumIndentSize, MaximumIndentSize)]
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets the line terminator a new file is written with.
    /// </summary>
    [EnumDataType(typeof(LineEndingStyle))]
    public LineEndingStyle DefaultLineEnding { get; set; } = LineEndingStyle.Lf;
}

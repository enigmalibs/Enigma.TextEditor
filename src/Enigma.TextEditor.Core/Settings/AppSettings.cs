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
    /// Gets or sets the editor's font size, in device-independent pixels.
    /// </summary>
    [Range(MinimumFontSize, MaximumFontSize)]
    public int EditorFontSize { get; set; } = DefaultFontSize;

    /// <summary>
    /// Gets or sets a value indicating whether a new file — and a file whose own indentation cannot be
    /// told — is indented with tabs rather than spaces.
    /// </summary>
    public bool IndentWithTabs { get; set; }

    /// <summary>
    /// Gets or sets how many columns one level of indentation is, for those same files.
    /// </summary>
    [Range(1, 16)]
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets the line terminator a new file is written with.
    /// </summary>
    [EnumDataType(typeof(LineEndingStyle))]
    public LineEndingStyle DefaultLineEnding { get; set; } = LineEndingStyle.Lf;
}

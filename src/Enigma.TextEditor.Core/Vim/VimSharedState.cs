using System;
using System.Text.RegularExpressions;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What every Vim engine of the application shares: the registers, the last search and the last
/// find.
/// </summary>
/// <remarks>
/// <para>
/// In Vim these belong to the editor, not to a window: a line yanked in one split is put in another,
/// and <c>n</c> in any window repeats the last <c>/</c> typed in any of them. Each pane here has an
/// engine of its own — the engine carries per-window state such as the visual selection — so what
/// has to be common lives in one instance handed to all of them.
/// </para>
/// <para>
/// An engine built without one gets a private instance, which is what a single buffer under test
/// wants.
/// </para>
/// </remarks>
public sealed class VimSharedState
{
    /// <summary>
    /// Raised when what the search highlight shows has changed: a new pattern, <c>:noh</c>, or
    /// <c>hlsearch</c> switched.
    /// </summary>
    public event EventHandler? HighlightChanged;

    /// <summary>
    /// Gets the registers.
    /// </summary>
    public VimRegisters Registers { get; } = new();

    /// <summary>
    /// Gets or sets <c>hlsearch</c>: whether every match of the last search pattern stays highlighted.
    /// On by default here, which Vim's own default is not — the highlight is the point of the feature.
    /// </summary>
    /// <remarks>
    /// Global, as Vim's is, which is why it lives here rather than on <see cref="VimOptions"/>: a
    /// <c>:set nohls</c> typed in one pane takes the highlight off every pane.
    /// </remarks>
    public bool HlSearch
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                HighlightChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    } = true;

    /// <summary>
    /// Gets or sets <c>incsearch</c>: whether the matches of a pattern are shown while it is typed. On
    /// by default.
    /// </summary>
    public bool IncSearch { get; set; } = true;

    /// <summary>
    /// Gets the last search pattern, compiled the way it was searched with, or <see langword="null"/>
    /// before the first search.
    /// </summary>
    public Regex? HighlightPattern { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <c>:nohlsearch</c> has hidden the highlight until the next search.
    /// </summary>
    public bool IsHighlightSuppressed { get; private set; }

    /// <summary>
    /// Gets the pattern whose matches are to be highlighted now, or <see langword="null"/> when nothing
    /// is.
    /// </summary>
    public Regex? VisibleHighlight => HlSearch && !IsHighlightSuppressed ? HighlightPattern : null;

    /// <summary>
    /// Gets or sets what <c>/</c>, <c>?</c>, <c>*</c>, <c>#</c> or <c>:s</c> last looked for, and which
    /// way: what <c>n</c> and <c>N</c> repeat.
    /// </summary>
    internal VimLastSearch? LastSearch { get; set; }

    /// <summary>
    /// Gets or sets which of <c>f</c>, <c>F</c>, <c>t</c>, <c>T</c> ran last, for <c>;</c> and <c>,</c>.
    /// </summary>
    internal VimMotion LastFind { get; set; } = VimMotion.None;

    /// <summary>
    /// Gets or sets the character the last find looked for.
    /// </summary>
    internal char LastFindCharacter { get; set; }

    /// <summary>
    /// Highlights a search pattern, lifting a <c>:noh</c>: every search does, <c>n</c> and <c>N</c>
    /// included.
    /// </summary>
    /// <param name="pattern">The pattern, compiled.</param>
    internal void Highlight(Regex pattern)
    {
        HighlightPattern = pattern;
        IsHighlightSuppressed = false;
        HighlightChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// <c>:nohlsearch</c>: hides the highlight without forgetting the pattern.
    /// </summary>
    internal void SuppressHighlight()
    {
        if (IsHighlightSuppressed)
        {
            return;
        }

        IsHighlightSuppressed = true;
        HighlightChanged?.Invoke(this, EventArgs.Empty);
    }
}

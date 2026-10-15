using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Enigma.TextEditor.Core.Vim;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Draws Vim's search highlight behind the text: every match of the last search (<c>hlsearch</c>),
/// or, while a <c>/</c> or <c>?</c> is being typed, every match of the pattern so far with the one
/// Enter would land on picked out (<c>incsearch</c>).
/// </summary>
/// <remarks>
/// <para>
/// Only the lines on screen are searched, on every draw: the cost is the screen's, not the file's,
/// and there is no list of matches to keep in step with the edits. A pattern that spans a line break
/// is found as long as both ends are on screen.
/// </para>
/// <para>
/// The colours are the Enigma accent, see-through for every match and solid-edged for the current
/// one, resolved for the variant on screen so they follow a theme switch.
/// </para>
/// </remarks>
public sealed class SearchHighlightRenderer : IBackgroundRenderer
{
    private const string AccentKey = "EnigmaAccentBrush";

    // Gold, for a palette that has no accent to read.
    private static readonly Color FallbackAccent = Color.FromRgb(0xE8, 0xA3, 0x3D);

    private readonly Control _host;
    private readonly Func<VimSharedState?> _shared;
    private readonly Func<VimIncSearch?> _incSearch;
    private readonly Func<bool> _isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchHighlightRenderer"/> class.
    /// </summary>
    /// <param name="host">The control whose theme the colours are resolved against.</param>
    /// <param name="shared">The state every engine shares, where the last search's highlight is.</param>
    /// <param name="incSearch">This editor's engine's preview, while a search is typed in it.</param>
    /// <param name="isEnabled">Whether Vim is on for this editor; with it off, nothing is drawn.</param>
    public SearchHighlightRenderer(Control host, Func<VimSharedState?> shared, Func<VimIncSearch?> incSearch, Func<bool> isEnabled)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _shared = shared ?? throw new ArgumentNullException(nameof(shared));
        _incSearch = incSearch ?? throw new ArgumentNullException(nameof(incSearch));
        _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The selection layer: above the current-line fill, so a match on the caret's line still shows,
    /// and under the text.
    /// </remarks>
    public KnownLayer Layer => KnownLayer.Selection;

    /// <summary>
    /// Gets the matches the last draw found, as document segments — what a test can read without
    /// looking at pixels.
    /// </summary>
    public IReadOnlyList<(int Offset, int Length)> LastMatches { get; private set; } = [];

    /// <inheritdoc />
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(drawingContext);

        LastMatches = [];

        if (!_isEnabled() || (textView.Document is not { } document))
        {
            return;
        }

        VimIncSearch? preview = _incSearch();
        Regex? pattern = preview?.Pattern ?? _shared()?.VisibleHighlight;

        if (pattern is null)
        {
            return;
        }

        textView.EnsureVisualLines();
        ReadOnlyCollection<VisualLine> lines = textView.VisualLines;

        if (lines.Count == 0)
        {
            return;
        }

        int start = lines[0].FirstDocumentLine.Offset;
        int end = lines[^1].LastDocumentLine.EndOffset;
        string visible = document.GetText(start, end - start);

        List<(int Offset, int Length)> found = [];

        try
        {
            foreach (Match match in pattern.Matches(visible))
            {
                if (match.Length > 0)
                {
                    found.Add((start + match.Index, match.Length));
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // A pattern too slow for the screen draws nothing rather than freezing it.
            return;
        }

        LastMatches = found;

        Color accent = Accent();
        IBrush matchBrush = new SolidColorBrush(accent, 0.35);
        IBrush currentBrush = new SolidColorBrush(accent, 0.75);
        IPen currentPen = new Pen(new SolidColorBrush(accent), 1);

        foreach ((int offset, int length) in found)
        {
            bool current = (preview is not null) && (offset == preview.MatchOffset);
            Draw(textView, drawingContext, offset, length, current ? currentBrush : matchBrush, current ? currentPen : null);
        }
    }

    private static void Draw(TextView textView, DrawingContext context, int offset, int length, IBrush brush, IPen? pen)
    {
        BackgroundGeometryBuilder builder = new() { AlignToWholePixels = true, CornerRadius = 2 };
        builder.AddSegment(textView, new TextSegment { StartOffset = offset, Length = length });

        if (builder.CreateGeometry() is { } geometry)
        {
            context.DrawGeometry(brush, pen, geometry);
        }
    }

    private Color Accent()
        => _host.TryFindResource(AccentKey, _host.ActualThemeVariant, out object? value) && (value is ISolidColorBrush brush)
            ? brush.Color
            : FallbackAccent;
}

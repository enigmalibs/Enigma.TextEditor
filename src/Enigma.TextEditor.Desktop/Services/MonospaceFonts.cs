using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The monospace font families this machine has: the faces the editor's text may be drawn in.
/// </summary>
/// <remarks>
/// <para>
/// Enigma.GitClient's diff-font probe. A face is monospace when a narrow letter and a wide one measure
/// the same: Vim's column motions, block selections and the hex rows all assume every character is as
/// wide as the next.
/// </para>
/// <para>
/// Measuring needs a font manager, so nothing here runs before there is an Avalonia platform to ask;
/// callers build their list on first use rather than in a constructor.
/// </para>
/// </remarks>
public static class MonospaceFonts
{
    private const double ProbeSize = 16;

    /// <summary>
    /// Tells whether a family is monospace.
    /// </summary>
    /// <param name="family">The family.</param>
    /// <returns>
    /// Whether its <c>i</c> and its <c>W</c> are the same width; <see langword="false"/> for a family the
    /// font manager lists but cannot draw.
    /// </returns>
    public static bool IsMonospace(FontFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);

        if (!TryMeasure("i", family, out double narrow) || !TryMeasure("W", family, out double wide))
        {
            return false;
        }

        return (narrow > 0) && (Math.Abs(narrow - wide) < 0.01);
    }

    /// <summary>
    /// Lists the monospace families installed on this machine, by name.
    /// </summary>
    /// <returns>Their names in alphabetical order; empty on a machine with no monospace face at all.</returns>
    public static IReadOnlyList<string> InstalledNames()
        => [.. FontManager.Current.SystemFonts
            .Where(IsMonospace)
            .Select(family => family.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.CurrentCulture)];

    // The system collection lists families it cannot always turn into glyphs — a name the platform
    // enumerates but whose file is missing or unreadable — and asking one of those for a typeface throws.
    // A font list is not worth taking the preferences down for.
    private static bool TryMeasure(string text, FontFamily family, out double width)
    {
        try
        {
            width = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(family),
                ProbeSize,
                Brushes.Black).Width;

            return true;
        }
        catch (InvalidOperationException)
        {
            width = 0;
            return false;
        }
    }
}

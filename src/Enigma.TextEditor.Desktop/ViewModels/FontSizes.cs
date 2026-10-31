using System.Collections.Generic;
using System.Linq;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The font sizes the status bar offers.
/// </summary>
/// <remarks>
/// Every everyday size one step apart, then the usual larger steps: one short list rather than every size the setting
/// allows, which the settings page and the zoom keys still reach.
/// </remarks>
internal static class FontSizes
{
    /// <summary>
    /// Gets the sizes offered whatever the current one, smallest first.
    /// </summary>
    public static IReadOnlyList<int> Presets { get; } = [8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 24, 28, 32, 36];

    /// <summary>
    /// The sizes to offer when the text is at a given size.
    /// </summary>
    /// <param name="current">The size the text is at now.</param>
    /// <returns>
    /// The presets, smallest first, with the current size among them in its place when it is not one of them but is
    /// within the setting's range: after a zoom to 17, the list still shows where the text is.
    /// </returns>
    public static IReadOnlyList<int> ListFor(int current)
        => (current is < AppSettings.MinimumFontSize or > AppSettings.MaximumFontSize) || Presets.Contains(current)
            ? Presets
            : [.. Presets.Append(current).Order()];
}

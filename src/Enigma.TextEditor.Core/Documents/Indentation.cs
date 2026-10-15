using System;
using System.Globalization;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// How a document is indented: with tabs or spaces, and how wide one level is.
/// </summary>
/// <param name="UseTabs">Whether a level of indentation is a tab rather than spaces.</param>
/// <param name="Size">How many columns one level — or one tab — is.</param>
public readonly record struct Indentation(bool UseTabs, int Size)
{
    /// <summary>
    /// The indentation of a document nothing is known about: four spaces.
    /// </summary>
    public static readonly Indentation Default = Spaces(4);

    /// <summary>
    /// Indentation with spaces.
    /// </summary>
    /// <param name="size">How many spaces one level is.</param>
    /// <returns>The indentation.</returns>
    public static Indentation Spaces(int size) => new(false, Check(size));

    /// <summary>
    /// Indentation with tabs.
    /// </summary>
    /// <param name="size">How many columns a tab is shown as.</param>
    /// <returns>The indentation.</returns>
    public static Indentation Tabs(int size) => new(true, Check(size));

    /// <summary>
    /// Gets what the status bar says: <c>Spaces: 4</c> or <c>Tab Size: 4</c>.
    /// </summary>
    public string Label => UseTabs
        ? string.Create(CultureInfo.InvariantCulture, $"Tab Size: {Size}")
        : string.Create(CultureInfo.InvariantCulture, $"Spaces: {Size}");

    private static int Check(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        return size;
    }
}

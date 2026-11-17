using System.Collections.Generic;
using System.Linq;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// Compares the lists a session holds by what is in them: a record compares its lists as references, and two captures of
/// the same editor never share one.
/// </summary>
internal static class SessionLists
{
    /// <summary>
    /// Whether two lists hold equal items in the same order.
    /// </summary>
    /// <typeparam name="T">The items.</typeparam>
    /// <param name="first">One list; <see langword="null"/> only in a file that says so.</param>
    /// <param name="second">The other.</param>
    /// <returns>Whether they are the same list, both missing, or equal item by item.</returns>
    public static bool SameItems<T>(IReadOnlyList<T>? first, IReadOnlyList<T>? second)
        => ReferenceEquals(first, second) || ((first is not null) && (second is not null) && first.SequenceEqual(second));
}

using System;
using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// One pane of a saved session: its tabs, in strip order, and the one that was showing.
/// </summary>
/// <param name="Id">The pane's id in the session's layout.</param>
/// <param name="Tabs">The tabs, left to right.</param>
/// <param name="ActiveTab">The index of the tab that was showing; -1 for a pane with no tab.</param>
public sealed record SessionPane(int Id, IReadOnlyList<SessionTab> Tabs, int ActiveTab)
{
    /// <summary>
    /// Whether another pane is the same pane with the same tabs, in the same order, showing the same one.
    /// </summary>
    /// <param name="other">The other pane.</param>
    /// <returns>Whether they hold the same; the tabs compared item by item rather than as a reference.</returns>
    public bool Equals(SessionPane? other)
        => (other is not null) && (Id == other.Id) && (ActiveTab == other.ActiveTab) && SessionLists.SameItems(Tabs, other.Tabs);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, ActiveTab, Tabs?.Count);
}

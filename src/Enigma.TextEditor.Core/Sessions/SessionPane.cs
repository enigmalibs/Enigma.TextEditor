using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// One pane of a saved session: its tabs, in strip order, and the one that was showing.
/// </summary>
/// <param name="Id">The pane's id in the session's layout.</param>
/// <param name="Tabs">The tabs, left to right.</param>
/// <param name="ActiveTab">The index of the tab that was showing; -1 for a pane with no tab.</param>
public sealed record SessionPane(int Id, IReadOnlyList<SessionTab> Tabs, int ActiveTab);

using System.Collections.Generic;
using Enigma.TextEditor.Core.Layout;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One pane of a restored session, as the workspace takes it back: its layout leaf, its tabs in their order, and the one
/// showing.
/// </summary>
/// <param name="Id">The leaf of the restored layout the pane stands at.</param>
/// <param name="Tabs">The tabs, each already where it was left; none when no buffer of the pane came back.</param>
/// <param name="Showing">The tab showing, one of <paramref name="Tabs"/>; <see langword="null"/> with none.</param>
internal sealed record RestoredPane(PaneId Id, IReadOnlyList<EditorTabViewModel> Tabs, EditorTabViewModel? Showing);

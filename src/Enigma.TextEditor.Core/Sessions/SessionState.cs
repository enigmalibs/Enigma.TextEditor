using System;
using System.Collections.Generic;
using Enigma.TextEditor.Core.Layout;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// What the editor was showing when it closed, as plain data: how the window was split, what each pane
/// held, the buffers behind the tabs, and the explorer's folder.
/// </summary>
/// <remarks>
/// <para>
/// Buffers are listed once and tabs point at them by index, because one buffer can be shown by tabs in
/// several panes — Vim's buffers — and has to come back as one buffer, not as two copies that drift apart.
/// </para>
/// <para>
/// Read back by <see cref="SessionFile.Read(string)"/>, which checks every index and id before handing a
/// state over, so a restore can trust what it is given.
/// </para>
/// <para>
/// The explorer's two members came after the format's first version, and are optional: a file written before them
/// reads as no folder and the explorer hidden, and an editor written before them ignores them.
/// </para>
/// </remarks>
/// <param name="Version">The file format, <see cref="CurrentVersion"/> for what this editor writes.</param>
/// <param name="Layout">How the window was divided, with each leaf's pane id.</param>
/// <param name="ActivePane">The id of the pane that had the keyboard.</param>
/// <param name="Panes">Each pane of the layout, with its tabs.</param>
/// <param name="Documents">The buffers the tabs show, each once.</param>
/// <param name="ExplorerFolder">
/// The full path of the folder the explorer showed, or <see langword="null"/> with none open. Not checked on reading:
/// a folder that is gone, or a path that is not a full one, is simply not reopened — never a reason to set aside a
/// session that may hold unsaved text.
/// </param>
/// <param name="ExplorerVisible">Whether the explorer was shown beside the panes.</param>
public sealed record SessionState(
    int Version,
    LayoutSnapshot Layout,
    int ActivePane,
    IReadOnlyList<SessionPane> Panes,
    IReadOnlyList<SessionDocument> Documents,
    string? ExplorerFolder = null,
    bool ExplorerVisible = false)
{
    /// <summary>
    /// The format this editor writes, and the only one it reads.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Whether another session holds the same: the same layout, panes, buffers and explorer, the lists compared item by
    /// item rather than as references.
    /// </summary>
    /// <param name="other">The other session.</param>
    /// <returns>Whether writing either would write the same file.</returns>
    /// <remarks>
    /// What tells the autosave a session has changed since it was last written. Unsaved text is compared as strings:
    /// the same string — a buffer that has not changed hands out the one it holds — compares at once.
    /// </remarks>
    public bool Equals(SessionState? other)
        => (other is not null)
           && (Version == other.Version)
           && (ActivePane == other.ActivePane)
           && (ExplorerVisible == other.ExplorerVisible)
           && string.Equals(ExplorerFolder, other.ExplorerFolder, StringComparison.Ordinal)
           && Equals(Layout, other.Layout)
           && SessionLists.SameItems(Panes, other.Panes)
           && SessionLists.SameItems(Documents, other.Documents);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Version, ActivePane, ExplorerVisible, ExplorerFolder, Layout, Panes?.Count, Documents?.Count);
}

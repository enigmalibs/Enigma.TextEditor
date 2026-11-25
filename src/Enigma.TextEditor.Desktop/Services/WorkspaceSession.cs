using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Turns an editor area into a saved session, and a saved session back into an editor area: the layout, each pane's
/// tabs and the one showing, where each was left, the pane with the keyboard, and the buffers behind the tabs.
/// </summary>
/// <remarks>
/// <para>
/// The editor area is handed to each call: this class holds none, and the workspace knows nothing of sessions. It
/// reads what the workspace shows through its public members, and hands the restored panes back to it whole, so the
/// pane bookkeeping — which tab keeps which buffer open, the active pane — stays the workspace's own.
/// </para>
/// <para>
/// One for the application.
/// </para>
/// </remarks>
public sealed class WorkspaceSession
{
    private readonly IDocumentService _documents;
    private readonly DocumentRegistry _registry;
    private readonly StatusMessages _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceSession"/> class.
    /// </summary>
    /// <param name="documents">Reads the buffers of a session back.</param>
    /// <param name="registry">The open buffers.</param>
    /// <param name="status">Where the buffers that could not be brought back are named.</param>
    public WorkspaceSession(IDocumentService documents, DocumentRegistry registry, StatusMessages status)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Captures what an editor area shows, for the next start to restore: the layout, each pane's tabs and the one
    /// showing, the pane with the keyboard, and the buffers behind the tabs.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="keepUnsaved">
    /// Whether unsaved changes are kept: the text of every buffer with changes its file does not have, and of every
    /// untitled buffer with text. Without it, they are left out — the close is throwing them away.
    /// </param>
    /// <returns>The session, and the unsaved texts it names.</returns>
    /// <remarks>
    /// <para>
    /// A buffer with a file — text or hex — is stored by its file, with its text when that is kept: in a file of its
    /// own, named after the buffer and the version of its text, so that a buffer that has not changed since the last
    /// capture is the same file and nothing of it is copied again. An untitled buffer
    /// is stored when it is empty, or when its text is kept: without it, there is nothing of it to bring back. A buffer
    /// not stored takes its tabs with it, and a pane whose showing tab went shows its first tab left.
    /// </para>
    /// <para>
    /// Positions are stored as the tabs last recorded them, and clamped to what a session may hold, so the file written
    /// is one the next start accepts.
    /// </para>
    /// </remarks>
    public SessionCapture Capture(WorkspaceViewModel workspace, bool keepUnsaved = false)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        List<SessionDocument> documents = [];
        List<SessionText> texts = [];
        Dictionary<DocumentViewModel, int> indexes = [];

        foreach (DocumentViewModel document in _registry.Documents)
        {
            if (keepUnsaved || !document.IsUntitled || (document.Document.TextLength == 0))
            {
                (SessionDocument entry, SessionText? text) = document.ToSession(keepUnsaved);
                indexes[document] = documents.Count;
                documents.Add(entry);

                if (text is not null)
                {
                    texts.Add(text);
                }
            }
        }

        List<SessionPane> panes = [];

        foreach (PaneId id in workspace.Layout.Panes)
        {
            EditorPaneViewModel pane = workspace.PaneFor(id);
            List<SessionTab> tabs = [];
            int showing = -1;

            foreach (EditorTabViewModel tab in pane.Tabs)
            {
                if (!indexes.TryGetValue(tab.Document, out int index))
                {
                    continue;
                }

                if (ReferenceEquals(tab, pane.ActiveTab))
                {
                    showing = tabs.Count;
                }

                tabs.Add(new SessionTab(
                    index,
                    Math.Max(tab.CaretOffset, 0),
                    Offset(tab.VerticalScrollOffset),
                    Offset(tab.HorizontalScrollOffset),
                    Math.Max(tab.HexRow, -1)));
            }

            panes.Add(new SessionPane(id.Value, tabs, (showing < 0) && (tabs.Count > 0) ? 0 : showing));
        }

        return new SessionCapture(
            new SessionState(SessionState.CurrentVersion, workspace.Layout.ToSnapshot(), workspace.ActivePaneId.Value, panes, documents),
            texts);

        static double Offset(double value) => double.IsFinite(value) ? Math.Max(value, 0) : 0;
    }

    /// <summary>
    /// Brings a saved session back into an editor area: the layout and its ratios, every pane's tabs in their order with
    /// the one that was showing, where each was left, and the pane that had the keyboard.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="state">The session, as <see cref="SessionFile.Read(string)"/> checked it.</param>
    /// <returns>The names of the buffers that could not be brought back; none when every one was.</returns>
    /// <remarks>
    /// <para>
    /// Only onto an editor area with no tab, which is what a start has: a session replaces the panes, and must not
    /// replace anything the user opened. One that has a tab by the time the buffers are read is left as it is.
    /// </para>
    /// <para>
    /// A buffer whose file is gone or unreadable is skipped, and named once on the status line rather than asked about:
    /// a start is not the moment for a stack of dialogs. A pane left with no tab closes, unless it is the last.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<string>> RestoreAsync(WorkspaceViewModel workspace, SessionState state)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(state);

        if (workspace.HasTabs)
        {
            return [];
        }

        // Only the buffers a tab shows, each once, read before anything on screen changes.
        SortedSet<int> shown = [.. state.Panes.SelectMany(pane => pane.Tabs).Select(tab => tab.Document)];
        DocumentViewModel?[] documents = new DocumentViewModel?[state.Documents.Count];
        List<string> skipped = [];

        foreach (int index in shown)
        {
            SessionDocument saved = state.Documents[index];
            documents[index] = await _documents.RestoreAsync(saved);

            if (documents[index] is null)
            {
                skipped.Add(saved.FilePath is { } path ? Path.GetFileName(path) : saved.UntitledName);
            }
        }

        if (workspace.HasTabs)
        {
            // Something opened while the files were read: what the user opened wins, and the restored buffers, shown by
            // no tab, are let go.
            foreach (DocumentViewModel document in documents.OfType<DocumentViewModel>().Distinct())
            {
                if (_registry.ViewCount(document) == 0)
                {
                    _registry.Release(document);
                }
            }

            return [];
        }

        workspace.ReplacePanes(state.Layout, [.. state.Panes.Select(saved => Restore(saved, documents))], new PaneId(state.ActivePane));

        if (skipped.Count > 0)
        {
            _status.Show($"Not restored from the previous session: {string.Join(", ", skipped)}.");
        }

        return skipped;
    }

    // A pane's tabs as they come back: each where it was left, the ones whose buffer could not be read left out, a
    // buffer the pane already shows not shown twice.
    private static RestoredPane Restore(SessionPane saved, DocumentViewModel?[] documents)
    {
        List<EditorTabViewModel> tabs = [];
        EditorTabViewModel? showing = null;

        for (int index = 0; index < saved.Tabs.Count; index++)
        {
            SessionTab tab = saved.Tabs[index];

            if ((documents[tab.Document] is not { } document) || tabs.Any(existing => ReferenceEquals(existing.Document, document)))
            {
                continue;
            }

            EditorTabViewModel restored = new(document)
            {
                CaretOffset = Math.Min(tab.CaretOffset, document.Document.TextLength),
                VerticalScrollOffset = tab.VerticalScrollOffset,
                HorizontalScrollOffset = tab.HorizontalScrollOffset,
                HexRow = tab.HexRow,
            };

            tabs.Add(restored);

            // The tab that was showing, or — when it could not come back — the nearest one before it.
            if ((index <= saved.ActiveTab) || (showing is null))
            {
                showing = restored;
            }
        }

        return new RestoredPane(new PaneId(saved.Id), tabs, showing);
    }
}

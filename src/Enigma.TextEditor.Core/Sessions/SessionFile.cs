using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// Reads and writes the session file: what the editor showed when it closed, unsaved text included.
/// </summary>
/// <remarks>
/// <para>
/// The file can hold the only copy of text the user never saved, so it is written beside itself and moved over the
/// old one: a write that fails half-way — a full disk, a killed process — leaves the previous session whole rather
/// than half of a new one.
/// </para>
/// <para>
/// For the same reason a file that cannot be used is moved aside to
/// <see cref="AppPaths.QuarantinedSessionFileName"/> rather than deleted or left for the next close to overwrite:
/// whatever text it holds can still be dug out by hand. The settings file is recovered the same way.
/// </para>
/// </remarks>
public static class SessionFile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Writes a session, replacing whichever was there.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="state">The session.</param>
    /// <exception cref="IOException">The file could not be written; the previous one is left as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The file or its folder is not writable.</exception>
    public static void Write(string path, SessionState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(state);

        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        // The process in the name: two instances may write at once, and must not write into each other's file.
        string temporary = $"{path}.{Environment.ProcessId}.tmp";
        bool moved = false;

        try
        {
            using (FileStream stream = new(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, state, Json);

                // On the disk before the move, or a power cut just after it could leave the new name on an empty file.
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, path, overwrite: true);
            moved = true;
        }
        finally
        {
            if (!moved)
            {
                TryDelete(temporary);
            }
        }
    }

    /// <summary>
    /// Reads a session, checking everything a restore will rely on.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>
    /// The session; or none, without a failure, when there is no file; or none with the failure when the file could
    /// not be read or does not hold a session this editor can restore — moved aside in the second case.
    /// </returns>
    public static SessionLoadResult Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return SessionLoadResult.None;
        }

        string json;

        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // Not the content's fault, and possibly passing: the file stays where it is.
            return SessionLoadResult.Failed(failure, null);
        }

        try
        {
            SessionState state = JsonSerializer.Deserialize<SessionState>(json, Json)
                ?? throw new InvalidDataException("The session file is empty.");
            Validate(state);

            return SessionLoadResult.Loaded(state);
        }
        catch (Exception failure) when (failure is JsonException or InvalidDataException or NotSupportedException)
        {
            return SessionLoadResult.Failed(failure, TryQuarantine(path));
        }
    }

    /// <summary>
    /// Checks a session against everything a restore will index into, so that the restore never has to.
    /// </summary>
    /// <exception cref="InvalidDataException">Something does not hold.</exception>
    private static void Validate(SessionState state)
    {
        if (state.Version != SessionState.CurrentVersion)
        {
            throw new InvalidDataException($"The session file is version {state.Version}; this editor reads version {SessionState.CurrentVersion}.");
        }

        // A file can say null for anything, whatever the types claim.
        if ((state.Layout is null) || (state.Panes is null) || (state.Documents is null))
        {
            throw new InvalidDataException("The session file has no layout, panes or documents.");
        }

        HashSet<int> leaves = [.. Leaves(state.Layout)];
        HashSet<int> panes = [];

        foreach (SessionPane pane in state.Panes)
        {
            if ((pane?.Tabs is null) || !panes.Add(pane.Id))
            {
                throw new InvalidDataException("A pane of the session is missing its tabs or appears twice.");
            }

            // A pane with tabs shows one of them; an empty pane shows none.
            bool showsATab = pane.Tabs.Count == 0
                ? pane.ActiveTab == -1
                : (pane.ActiveTab >= 0) && (pane.ActiveTab < pane.Tabs.Count);

            if (!showsATab)
            {
                throw new InvalidDataException($"The pane {pane.Id} shows a tab it does not have.");
            }

            foreach (SessionTab tab in pane.Tabs)
            {
                ValidateTab(tab, state.Documents.Count);
            }
        }

        if (!panes.SetEquals(leaves))
        {
            throw new InvalidDataException("The session's panes are not the panes of its layout.");
        }

        if (!panes.Contains(state.ActivePane))
        {
            throw new InvalidDataException($"The active pane {state.ActivePane} is not in the session's layout.");
        }

        foreach (SessionDocument document in state.Documents)
        {
            ValidateDocument(document);
        }
    }

    private static IEnumerable<int> Leaves(LayoutSnapshot snapshot)
    {
        // Built for the checks it makes — every node a pane or a whole split, no id twice — and thrown away.
        try
        {
            _ = LayoutTree.FromSnapshot(snapshot);
        }
        catch (ArgumentException failure)
        {
            throw new InvalidDataException($"The session's layout is not a layout: {failure.Message}", failure);
        }

        List<int> ids = [];
        Collect(snapshot, ids);

        return ids;

        static void Collect(LayoutSnapshot node, List<int> ids)
        {
            if (node.Pane is { } id)
            {
                // Below the top: the tree numbers the next pane it creates one past the highest it holds.
                if ((id < 0) || (id == int.MaxValue))
                {
                    throw new InvalidDataException($"The pane id {id} is out of range.");
                }

                ids.Add(id);

                return;
            }

            if ((node.Axis is not { } axis) || !Enum.IsDefined(axis) || !double.IsFinite(node.Ratio ?? 0.5))
            {
                throw new InvalidDataException("A split of the session's layout has no valid axis or ratio.");
            }

            Collect(node.First!, ids);
            Collect(node.Second!, ids);
        }
    }

    private static void ValidateTab(SessionTab tab, int documents)
    {
        if (tab is null)
        {
            throw new InvalidDataException("A tab of the session is empty.");
        }

        if ((tab.Document < 0) || (tab.Document >= documents))
        {
            throw new InvalidDataException($"A tab shows the document {tab.Document}, which the session does not have.");
        }

        if ((tab.CaretOffset < 0) || (tab.HexRow < -1)
            || !IsOffset(tab.VerticalScrollOffset) || !IsOffset(tab.HorizontalScrollOffset))
        {
            throw new InvalidDataException("A tab of the session has a caret or a scroll position out of range.");
        }

        static bool IsOffset(double value) => double.IsFinite(value) && (value >= 0);
    }

    private static void ValidateDocument(SessionDocument document)
    {
        if ((document is null) || string.IsNullOrWhiteSpace(document.UntitledName))
        {
            throw new InvalidDataException("A document of the session has no name.");
        }

        if ((document.FilePath is { } path) && !Path.IsPathFullyQualified(path))
        {
            throw new InvalidDataException($"The document path '{path}' is not a full path.");
        }

        if (document.IsHex && ((document.FilePath is null) || (document.UnsavedText is not null)))
        {
            throw new InvalidDataException("A hex document of the session has no file, or text of its own.");
        }

        if ((document.CodePage <= 0) || (document.IndentSize < 1) || !Enum.IsDefined(document.LineEndings))
        {
            throw new InvalidDataException("A document of the session has an encoding, indentation or line ending out of range.");
        }
    }

    private static string? TryQuarantine(string path)
    {
        string quarantined = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, AppPaths.QuarantinedSessionFileName);

        try
        {
            File.Move(path, quarantined, overwrite: true);

            return quarantined;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Left in place: the next close replaces it, and the editor starts without it either way.
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A stray temporary file is harmless; the next write replaces it.
        }
    }
}

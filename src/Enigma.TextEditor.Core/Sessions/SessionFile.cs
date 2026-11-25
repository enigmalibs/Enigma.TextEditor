using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
/// Each buffer's unsaved text is a file of its own, in the <see cref="AppPaths.UnsavedFolderName"/> folder beside the
/// session (format version 2): written before the session that names it, each the same way, and the files the new
/// session no longer names deleted only after it — so whichever session the disk holds, the texts it names are there
/// and whole. A text whose name is already on the disk is not written again: a name stands for one text.
/// </para>
/// <para>
/// For the same reason a file that cannot be used is moved aside to
/// <see cref="AppPaths.QuarantinedSessionFileName"/> rather than deleted or left for the next close to overwrite:
/// whatever text it holds can still be dug out by hand — and its unsaved-text folder with it, to
/// <see cref="AppPaths.QuarantinedUnsavedFolderName"/>. The settings file is recovered the same way.
/// </para>
/// </remarks>
public static class SessionFile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    // UTF-8 without a mark, refusing what it cannot encode — a lone surrogate — rather than writing a '?' in its place:
    // the write then fails, as the session's own JSON does, and closing asks about the changes instead.
    private static readonly UTF8Encoding TextEncoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    // What an unsaved-text file may be called: a plain name, so that a session can never point outside its folder.
    private static readonly Regex TextFileName = new(@"^[A-Za-z0-9_-]{1,96}\.txt$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>
    /// The folder that holds the unsaved texts of a session file.
    /// </summary>
    /// <param name="path">The session file.</param>
    /// <returns>The folder beside it.</returns>
    public static string UnsavedFolderFor(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, AppPaths.UnsavedFolderName);
    }

    /// <summary>
    /// Whether a name may be an unsaved-text file's: letters, digits, <c>-</c> and <c>_</c>, then <c>.txt</c>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>Whether it is one.</returns>
    public static bool IsTextFileName(string? name) => (name is not null) && TextFileName.IsMatch(name);

    /// <summary>
    /// Writes a session, replacing whichever was there, with no unsaved text of its own to write.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="state">The session.</param>
    /// <exception cref="IOException">The file could not be written; the previous one is left as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The file or its folder is not writable.</exception>
    public static void Write(string path, SessionState state) => Write(path, state, []);

    /// <summary>
    /// Writes a session, replacing whichever was there: the unsaved texts first, then the session that names them, then
    /// away with the texts no longer named.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="state">The session.</param>
    /// <param name="texts">The unsaved texts its buffers name; one already on the disk is not written again.</param>
    /// <exception cref="IOException">The file could not be written; the previous one is left as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The file or its folder is not writable.</exception>
    /// <exception cref="ArgumentException">A text's name is not one <see cref="IsTextFileName"/> accepts, or it cannot be encoded.</exception>
    public static void Write(string path, SessionState state, IReadOnlyList<SessionText> texts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(texts);

        if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        string unsaved = UnsavedFolderFor(path);

        foreach (SessionText text in texts)
        {
            WriteText(unsaved, text);
        }

        WriteAtomically(path, stream => JsonSerializer.Serialize(stream, state, Json));

        // Only now: the session on the disk until a moment ago may have named them.
        DeleteTextsNotNamed(unsaved, state);
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

        SessionState state;

        try
        {
            state = JsonSerializer.Deserialize<SessionState>(json, Json)
                ?? throw new InvalidDataException("The session file is empty.");
            Validate(state);
        }
        catch (Exception failure) when (failure is JsonException or InvalidDataException or NotSupportedException)
        {
            return SessionLoadResult.Failed(failure, TryQuarantine(path));
        }

        return ReadTexts(path, state);
    }

    // The texts the buffers name, read into each buffer; a file that has gone leaves its buffer without, and is named.
    private static SessionLoadResult ReadTexts(string path, SessionState state)
    {
        if (!state.Documents.Any(document => document.UnsavedTextFile is not null))
        {
            return SessionLoadResult.Loaded(state);
        }

        string unsaved = UnsavedFolderFor(path);
        List<string> missing = [];
        List<SessionDocument> documents = [];

        foreach (SessionDocument document in state.Documents)
        {
            if (document.UnsavedTextFile is not { } name)
            {
                documents.Add(document);

                continue;
            }

            try
            {
                documents.Add(document with { UnsavedText = File.ReadAllText(Path.Combine(unsaved, name), TextEncoding), UnsavedTextFile = null });
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or DecoderFallbackException)
            {
                missing.Add(name);
                documents.Add(document with { UnsavedTextFile = null });
            }
        }

        return SessionLoadResult.Loaded(state with { Documents = documents }, missing);
    }

    // Beside its final name, flushed to the disk, then moved over it.
    private static void WriteText(string folder, SessionText text)
    {
        if (!IsTextFileName(text.FileName))
        {
            throw new ArgumentException($"'{text.FileName}' is not a name an unsaved text may have.", nameof(text));
        }

        string target = Path.Combine(folder, text.FileName);

        if (File.Exists(target))
        {
            return;
        }

        Directory.CreateDirectory(folder);
        WriteAtomically(
            target,
            stream =>
            {
                using StreamWriter writer = new(stream, TextEncoding, bufferSize: 64 * 1024, leaveOpen: true);
                text.WriteTo(writer);
            });
    }

    private static void WriteAtomically(string path, Action<Stream> write)
    {
        // The process in the name: two instances may write at once, and must not write into each other's file.
        string temporary = $"{path}.{Environment.ProcessId}.tmp";
        bool moved = false;

        try
        {
            using (FileStream stream = new(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                write(stream);

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

    // The files of the folder the session no longer names, and any temporary one a killed write left: only the instance
    // that owns the session writes here.
    private static void DeleteTextsNotNamed(string folder, SessionState state)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        HashSet<string> named = new(
            state.Documents.Select(document => document.UnsavedTextFile).OfType<string>(),
            StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (string file in Directory.EnumerateFiles(folder))
            {
                if (!named.Contains(Path.GetFileName(file)))
                {
                    TryDelete(file);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Left for the next write: a stray file is harmless, the session written names what it needs.
        }
    }

    /// <summary>
    /// Checks a session against everything a restore will index into, so that the restore never has to.
    /// </summary>
    /// <exception cref="InvalidDataException">Something does not hold.</exception>
    private static void Validate(SessionState state)
    {
        if ((state.Version < SessionState.OldestVersion) || (state.Version > SessionState.CurrentVersion))
        {
            throw new InvalidDataException(
                $"The session file is version {state.Version}; this editor reads versions {SessionState.OldestVersion} to {SessionState.CurrentVersion}.");
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

        if (document.IsHex && ((document.FilePath is null) || (document.UnsavedText is not null) || (document.UnsavedTextFile is not null)))
        {
            throw new InvalidDataException("A hex document of the session has no file, or text of its own.");
        }

        if (document.UnsavedTextFile is { } name)
        {
            // Never a path: a session must not reach a file outside its own folder.
            if (!IsTextFileName(name) || (document.UnsavedText is not null))
            {
                throw new InvalidDataException($"A document of the session names '{name}' for its text, which is not a text file of the session.");
            }
        }

        if ((document.CodePage <= 0) || (document.IndentSize < 1) || !Enum.IsDefined(document.LineEndings))
        {
            throw new InvalidDataException("A document of the session has an encoding, indentation or line ending out of range.");
        }
    }

    private static string? TryQuarantine(string path)
    {
        string quarantined = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, AppPaths.QuarantinedSessionFileName);

        // The texts with it: the next write would otherwise delete every one the set-aside file names.
        TryQuarantineTexts(path);

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

    private static void TryQuarantineTexts(string path)
    {
        string unsaved = UnsavedFolderFor(path);

        if (!Directory.Exists(unsaved))
        {
            return;
        }

        string quarantined = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, AppPaths.QuarantinedUnsavedFolderName);

        try
        {
            if (Directory.Exists(quarantined))
            {
                Directory.Delete(quarantined, recursive: true);
            }

            Directory.Move(unsaved, quarantined);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Left in place: the session set aside still names them, and the next write may delete them; nothing better
            // can be done from here.
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

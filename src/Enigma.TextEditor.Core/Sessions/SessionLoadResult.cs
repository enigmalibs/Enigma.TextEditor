using System;
using System.Collections.Generic;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// What <see cref="SessionFile.Read(string)"/> came back with: the session, or nothing — and, when there
/// was a file but it could not be used, why and where it went.
/// </summary>
public sealed class SessionLoadResult
{
    private SessionLoadResult(SessionState? state, Exception? failure, string? quarantinedFilePath, IReadOnlyList<string>? missingTexts = null)
    {
        State = state;
        Failure = failure;
        QuarantinedFilePath = quarantinedFilePath;
        MissingTexts = missingTexts ?? [];
    }

    /// <summary>
    /// Gets the session, or <see langword="null"/> when there is none to restore.
    /// </summary>
    public SessionState? State { get; }

    /// <summary>
    /// Gets why the file could not be used, or <see langword="null"/> when it was read or was not there.
    /// </summary>
    public Exception? Failure { get; }

    /// <summary>
    /// Gets where an unusable file was moved to, or <see langword="null"/> when nothing was moved.
    /// </summary>
    public string? QuarantinedFilePath { get; }

    internal static SessionLoadResult None { get; } = new(null, null, null);

    /// <summary>
    /// Gets the unsaved-text files the session names but that could not be read: those buffers come back without their
    /// text — a file read again from the disk, an untitled buffer empty. The session itself is still used.
    /// </summary>
    public IReadOnlyList<string> MissingTexts { get; }

    internal static SessionLoadResult Loaded(SessionState state, IReadOnlyList<string>? missingTexts = null)
        => new(state, null, null, missingTexts);

    internal static SessionLoadResult Failed(Exception failure, string? quarantinedFilePath)
        => new(null, failure, quarantinedFilePath);
}

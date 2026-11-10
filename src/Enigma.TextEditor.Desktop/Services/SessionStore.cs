using System;
using System.IO;
using Enigma.TextEditor.Core.Sessions;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Keeps the session in its file, for the instance that holds the session's lock.
/// </summary>
/// <remarks>
/// The lock is taken on first use — the start's <see cref="Load"/> — and held for the life of the application:
/// disposing the store, as the container does when the application ends, lets the next instance take it.
/// </remarks>
public sealed class SessionStore : ISessionStore, IDisposable
{
    private readonly string _sessionFile;
    private readonly string _lockFile;
    private readonly ILogger<SessionStore> _logger;
    private SessionLock? _lock;
    private bool _claimed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionStore"/> class.
    /// </summary>
    /// <param name="sessionFile">The session file.</param>
    /// <param name="lockFile">The file the owner holds open.</param>
    /// <param name="logger">Where failures are logged.</param>
    public SessionStore(string sessionFile, string lockFile, ILogger<SessionStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(lockFile);

        _sessionFile = sessionFile;
        _lockFile = lockFile;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsOwner
    {
        get
        {
            Claim();

            return _lock is not null;
        }
    }

    /// <inheritdoc />
    public SessionState? Load()
    {
        if (!IsOwner)
        {
            return null;
        }

        SessionLoadResult result = SessionFile.Read(_sessionFile);

        if (result.Failure is not null)
        {
            _logger.LogWarning(
                result.Failure,
                "The previous session could not be restored from {SessionFile}; starting without it. Moved aside to: {QuarantinedFile}.",
                _sessionFile,
                result.QuarantinedFilePath ?? "(not moved)");
        }

        return result.State;
    }

    /// <inheritdoc />
    public bool Save(SessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!IsOwner)
        {
            return false;
        }

        try
        {
            SessionFile.Write(_sessionFile, state);

            return true;
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // ArgumentException too: the serializer refuses text it cannot write as UTF-8 — a lone surrogate — and
            // a window that is closing must not be stopped by that.
            _logger.LogError(failure, "The session could not be written to {SessionFile}.", _sessionFile);

            return false;
        }
    }

    /// <summary>
    /// Lets go of the session, for the next instance to start to own.
    /// </summary>
    public void Dispose()
    {
        _lock?.Dispose();
        _lock = null;
    }

    private void Claim()
    {
        if (_claimed)
        {
            return;
        }

        _claimed = true;
        _lock = SessionLock.TryAcquire(_lockFile);

        if (_lock is null)
        {
            _logger.LogInformation("Another instance owns the session at {SessionFile}; this one neither restores nor saves it.", _sessionFile);
        }
    }
}

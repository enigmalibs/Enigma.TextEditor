using System;
using System.IO;
using System.Threading;
using Enigma.TextEditor.Core.Sessions;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Keeps the session in its file, for the instance that holds the session's lock.
/// </summary>
/// <remarks>
/// <para>
/// The lock is taken on first use — the start's <see cref="Load"/> — and held for the life of the application:
/// disposing the store, as the container does when the application ends, lets the next instance take it.
/// </para>
/// <para>
/// Safe to use from any thread: the autosave writes from the thread pool while the window may write from its own.
/// The session is written every few seconds, so a folder that refuses it would fill the log: the first failure of a run
/// is logged as an error, the ones after it at Debug, and the next success says the session is written again.
/// </para>
/// </remarks>
public sealed class SessionStore : ISessionStore, IDisposable
{
    private readonly string _sessionFile;
    private readonly string _lockFile;
    private readonly ILogger<SessionStore> _logger;
    private readonly Lock _gate = new();
    private SessionLock? _lock;
    private bool _claimed;
    private bool _failing;

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

        if (result.MissingTexts.Count > 0)
        {
            _logger.LogWarning(
                "The previous session names unsaved text that could not be read, and those buffers come back without it: {MissingTexts}.",
                string.Join(", ", result.MissingTexts));
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

        lock (_gate)
        {
            try
            {
                SessionFile.Write(_sessionFile, state);
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // ArgumentException too: the serializer refuses text it cannot write as UTF-8 — a lone surrogate — and
                // a window that is closing must not be stopped by that.
                if (_failing)
                {
                    _logger.LogDebug(failure, "The session still could not be written to {SessionFile}.", _sessionFile);
                }
                else
                {
                    _failing = true;
                    _logger.LogError(
                        failure,
                        "The session could not be written to {SessionFile}. Until it is written again, further failures are logged at Debug.",
                        _sessionFile);
                }

                return false;
            }

            if (_failing)
            {
                _failing = false;
                _logger.LogInformation("The session is written to {SessionFile} again.", _sessionFile);
            }

            return true;
        }
    }

    /// <summary>
    /// Lets go of the session, for the next instance to start to own.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _lock?.Dispose();
            _lock = null;
        }
    }

    private void Claim()
    {
        lock (_gate)
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
}

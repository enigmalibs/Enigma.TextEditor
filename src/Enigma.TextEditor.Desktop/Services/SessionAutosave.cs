using System;
using System.Threading;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.Extensions;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Keeps the session file up to date while the editor runs, so that a crash loses no more than the last few seconds of
/// it.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Interval"/> the session is captured — on the UI thread, where the buffers live — and compared with
/// the last one written. Only a session that changed is written, and the write, flush to the disk included, runs on the
/// thread pool: typing never waits for the disk. One write runs at a time; a tick that finds one running leaves the
/// change to the next tick.
/// </para>
/// <para>
/// The close writes as well, through <see cref="SaveNow"/>, and has the last word. Every capture is numbered, every
/// write goes through one gate, and a write whose capture is older than the last one written is dropped: a background
/// write still waiting when the close writes never lands after it, and one already writing finishes first.
/// </para>
/// <para>
/// Only the instance that owns the session writes it (<see cref="ISessionStore.IsOwner"/>); for any other,
/// <see cref="Start"/> starts nothing.
/// </para>
/// </remarks>
public sealed class SessionAutosave : IDisposable
{
    private readonly ISessionStore _store;
    private readonly TimeProvider _time;
    private readonly ILogger<SessionAutosave> _logger;
    private readonly Func<Func<bool>, Task<bool>> _background;
    private readonly Lock _gate = new();

    // UI thread.
    private Func<SessionState>? _capture;
    private SynchronizationContext? _uiThread;
    private ITimer? _timer;
    private SessionState? _saved;
    private Task<bool>? _writing;
    private long _issued;

    // Under the gate.
    private long _landed;
    private bool _stopped;

    // Set by the timer, cleared by the tick it posted: a UI thread busy for longer than the interval gets one tick, not
    // a queue of them.
    private int _tickPosted;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAutosave"/> class.
    /// </summary>
    /// <param name="store">Where the session is written.</param>
    /// <param name="time">The clock the interval is counted on.</param>
    /// <param name="logger">Where the start of the autosave is noted.</param>
    public SessionAutosave(ISessionStore store, TimeProvider time, ILogger<SessionAutosave> logger)
        : this(store, time, logger, work => Task.Run(work))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAutosave"/> class, with the way a write is sent off the UI
    /// thread — for a test to decide when it runs.
    /// </summary>
    /// <param name="store">Where the session is written.</param>
    /// <param name="time">The clock the interval is counted on.</param>
    /// <param name="logger">Where the start of the autosave is noted.</param>
    /// <param name="background">Runs a write away from the UI thread, and completes with its result.</param>
    internal SessionAutosave(ISessionStore store, TimeProvider time, ILogger<SessionAutosave> logger, Func<Func<bool>, Task<bool>> background)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _background = background ?? throw new ArgumentNullException(nameof(background));
    }

    /// <summary>
    /// Gets how often the session is checked, and written when it changed: at most this much is lost to a crash.
    /// </summary>
    public static TimeSpan Interval { get; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets a value indicating whether the session is being kept: started, by the instance that owns it, and not stopped.
    /// </summary>
    public bool IsRunning => _timer is not null;

    /// <summary>
    /// Starts keeping the session, every <see cref="Interval"/>. On the UI thread, once the session the editor last
    /// closed with has been restored: the first write replaces it.
    /// </summary>
    /// <param name="capture">Captures the session as the window shows it; called on the UI thread.</param>
    /// <exception cref="InvalidOperationException">Not called on a thread with a synchronization context.</exception>
    /// <remarks>Does nothing when this instance does not own the session, when already started, or once stopped.</remarks>
    public void Start(Func<SessionState> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);

        if ((_timer is not null) || IsStopped() || !_store.IsOwner)
        {
            return;
        }

        _uiThread = SynchronizationContext.Current
            ?? throw new InvalidOperationException("The autosave is started on the UI thread, where the session is captured.");
        _capture = capture;
        _timer = _time.CreateTimer(_ => OnTimer(), null, Interval, Interval);
        _logger.LogInformation("The session is kept every {Interval} while the editor runs.", Interval);
    }

    /// <summary>
    /// Captures the session and, when it changed since it was last written, writes it away from the UI thread.
    /// </summary>
    /// <returns>
    /// A task that completes once the write is done; at once when nothing changed, when a write is still running, or
    /// when the autosave is not running.
    /// </returns>
    /// <remarks>
    /// A write that failed — the store logged it — leaves the session as unwritten, and the next tick tries again.
    /// </remarks>
    public async Task SaveIfChangedAsync()
    {
        if ((_capture is null) || (_writing is not null) || IsStopped())
        {
            return;
        }

        SessionState state = _capture();

        if (state.Equals(_saved))
        {
            return;
        }

        long number = ++_issued;
        Task<bool> writing = _background(() => Write(state, number));
        _writing = writing;

        try
        {
            if (await writing)
            {
                _saved = state;
            }
        }
        finally
        {
            _writing = null;
        }
    }

    /// <summary>
    /// Writes a session now, on this thread, ahead of every capture taken before it — the close's write.
    /// </summary>
    /// <param name="state">The session.</param>
    /// <returns>Whether it was written; not when the write failed — logged — or this instance does not own the session.</returns>
    /// <remarks>
    /// A background write already writing is waited for, and replaced; one not started yet is dropped. Written even
    /// once the autosave has stopped, or was never started.
    /// </remarks>
    public bool SaveNow(SessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        long number = ++_issued;
        bool written;

        lock (_gate)
        {
            _landed = number;
            written = _store.Save(state);
        }

        if (written)
        {
            _saved = state;
        }

        return written;
    }

    /// <summary>
    /// Stops keeping the session: no tick runs after this, and a background write not started yet is dropped. A write
    /// already writing is waited for.
    /// </summary>
    public void Stop()
    {
        lock (_gate)
        {
            _stopped = true;
        }

        _timer?.Dispose();
        _timer = null;
        _capture = null;
    }

    /// <summary>
    /// Stops keeping the session.
    /// </summary>
    public void Dispose() => Stop();

    private bool IsStopped()
    {
        lock (_gate)
        {
            return _stopped;
        }
    }

    // Thread pool. A capture older than the last one written — the close's, or a stop — is not written over it.
    private bool Write(SessionState state, long number)
    {
        lock (_gate)
        {
            if (_stopped || (number <= _landed))
            {
                return false;
            }

            _landed = number;

            return _store.Save(state);
        }
    }

    // The timer's thread: the capture belongs to the UI thread, so the tick is posted there.
    private void OnTimer()
    {
        if ((_uiThread is not { } uiThread) || (Interlocked.Exchange(ref _tickPosted, 1) == 1))
        {
            return;
        }

        uiThread.Post(
            _ =>
            {
                Volatile.Write(ref _tickPosted, 0);
                SaveIfChangedAsync().Forget();
            },
            null);
    }
}

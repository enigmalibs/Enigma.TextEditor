using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A clock that only moves when a test moves it, so "not again for a minute" is tested in no time.
/// </summary>
/// <remarks>
/// Its timers fire as <see cref="Advance"/> moves the clock past them, on the thread that moves it, in the order they
/// fall due — a periodic one as many times as its period fits.
/// </remarks>
public sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = [];
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc />
    /// <remarks>One tick per tick of <see cref="GetUtcNow"/>, so elapsed times follow this clock too.</remarks>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp() => _now.UtcTicks;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);

        ManualTimer timer = new(this, callback, state);
        timer.Change(dueTime, period);
        _timers.Add(timer);

        return timer;
    }

    /// <summary>
    /// Moves the clock on, firing the timers it passes.
    /// </summary>
    /// <param name="by">How far.</param>
    public void Advance(TimeSpan by)
    {
        DateTimeOffset target = _now + by;

        while (_timers.Where(timer => timer.Due <= target).MinBy(timer => timer.Due) is { Due: { } due } next)
        {
            _now = due;
            next.Fire();
        }

        _now = target;
    }

    private sealed class ManualTimer(ManualTimeProvider clock, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan _period;

        public DateTimeOffset? Due { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Due = dueTime == Timeout.InfiniteTimeSpan ? null : clock._now + dueTime;
            _period = period;

            return true;
        }

        public void Fire()
        {
            Due = (_period > TimeSpan.Zero) && (_period != Timeout.InfiniteTimeSpan) ? Due + _period : null;
            callback(state);
        }

        public void Dispose()
        {
            Due = null;
            clock._timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();

            return ValueTask.CompletedTask;
        }
    }
}

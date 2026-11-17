using System.Collections.Concurrent;
using System.Threading;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A session store that keeps what it is given in memory, fails when told to, and can hold a write until released —
/// for the autosave's ordering to be watched.
/// </summary>
internal sealed class FakeSessionStore : ISessionStore
{
    public bool IsOwner { get; set; } = true;

    /// <summary>
    /// Gets every session written, oldest first.
    /// </summary>
    public ConcurrentQueue<SessionState> Saved { get; } = new();

    /// <summary>
    /// Gets or sets how many of the next writes fail.
    /// </summary>
    public int FailuresLeft { get; set; }

    /// <summary>
    /// Gets or sets an event a write waits for before it writes; none, it writes at once.
    /// </summary>
    public ManualResetEventSlim? Hold { get; set; }

    /// <summary>
    /// Gets the event set as a write starts, before it waits for <see cref="Hold"/>.
    /// </summary>
    public ManualResetEventSlim Entered { get; } = new();

    public SessionState? Load() => null;

    public bool Save(SessionState state)
    {
        if (!IsOwner)
        {
            return false;
        }

        Entered.Set();
        Hold?.Wait(System.TimeSpan.FromSeconds(10));

        if (FailuresLeft > 0)
        {
            FailuresLeft--;

            return false;
        }

        Saved.Enqueue(state);

        return true;
    }
}

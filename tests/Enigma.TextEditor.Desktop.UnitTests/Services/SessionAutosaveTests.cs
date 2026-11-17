using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The session kept while the editor runs: written when it changed, on a clock, away from the UI thread — and never
/// over the close's write.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class SessionAutosaveTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void ATickWithAChangedSession_WritesIt_AndATickWithNothingChanged_WritesNothing()
        => fixture.RunAsync(async () =>
        {
            FakeSessionStore store = new();
            int caret = 0;
            using SessionAutosave autosave = Inline(store);
            autosave.Start(() => State(caret));

            await autosave.SaveIfChangedAsync();
            await autosave.SaveIfChangedAsync();
            caret = 3;
            await autosave.SaveIfChangedAsync();

            Assert.Equal([0, 3], store.Saved.Select(CaretOf));
        });

    [Fact]
    public void AFailedWrite_IsRetriedAtTheNextTick_WithoutAChange()
        => fixture.RunAsync(async () =>
        {
            FakeSessionStore store = new() { FailuresLeft = 1 };
            using SessionAutosave autosave = Inline(store);
            autosave.Start(() => State(5));

            await autosave.SaveIfChangedAsync();
            Assert.Empty(store.Saved);

            await autosave.SaveIfChangedAsync();
            Assert.Equal([5], store.Saved.Select(CaretOf));
        });

    [Fact]
    public void TheClock_RunsATickOnTheUiThread_OnceEveryInterval()
        => fixture.Run(() =>
        {
            FakeSessionStore store = new();
            ManualTimeProvider clock = new();
            int caret = 0;
            using SessionAutosave autosave = Inline(store, clock);
            autosave.Start(() => State(caret));

            clock.Advance(SessionAutosave.Interval - TimeSpan.FromMilliseconds(1));
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(store.Saved);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0], store.Saved.Select(CaretOf));

            caret = 1;
            clock.Advance(SessionAutosave.Interval);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1], store.Saved.Select(CaretOf));
        });

    [Fact]
    public void ATimerFiringSeveralTimesBeforeTheUiThreadIsFree_PostsOneTick()
        => fixture.Run(() =>
        {
            FakeSessionStore store = new();
            ManualTimeProvider clock = new();
            int captures = 0;
            using SessionAutosave autosave = Inline(store, clock);
            autosave.Start(() => State(captures++));

            clock.Advance(SessionAutosave.Interval * 3);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(1, captures);
        });

    [Fact]
    public void AnInstanceThatDoesNotOwnTheSession_StartsNothing()
        => fixture.RunAsync(async () =>
        {
            FakeSessionStore store = new() { IsOwner = false };
            ManualTimeProvider clock = new();
            int captures = 0;
            using SessionAutosave autosave = Inline(store, clock);

            autosave.Start(() => State(captures++));
            clock.Advance(SessionAutosave.Interval * 2);
            Dispatcher.UIThread.RunJobs();
            await autosave.SaveIfChangedAsync();

            Assert.False(autosave.IsRunning);
            Assert.Equal(0, captures);
            Assert.Empty(store.Saved);
        });

    [Fact]
    public void OnceStopped_NoTickWrites_NotEvenOnePostedBeforeTheStop()
        => fixture.Run(() =>
        {
            FakeSessionStore store = new();
            ManualTimeProvider clock = new();
            using SessionAutosave autosave = Inline(store, clock);
            autosave.Start(() => State(1));

            // Posted to the UI thread, not run yet.
            clock.Advance(SessionAutosave.Interval);
            autosave.Stop();
            Dispatcher.UIThread.RunJobs();
            clock.Advance(SessionAutosave.Interval * 3);
            Dispatcher.UIThread.RunJobs();

            Assert.False(autosave.IsRunning);
            Assert.Empty(store.Saved);
        });

    [Fact]
    public void TheClosesWrite_DropsABackgroundWriteNotStartedYet()
        => fixture.RunAsync(async () =>
        {
            FakeSessionStore store = new();
            QueuedWrites writes = new();
            using SessionAutosave autosave = new(store, new ManualTimeProvider(), NullLogger<SessionAutosave>.Instance, writes.Run);
            autosave.Start(() => State(1));

            Task tick = autosave.SaveIfChangedAsync();
            Assert.True(autosave.SaveNow(State(2)));
            writes.RunNext();
            await tick;

            Assert.Equal([2], store.Saved.Select(CaretOf));
        });

    [Fact]
    public void TheClosesWrite_WaitsForABackgroundWriteAlreadyWriting_AndHasTheLastWord()
        => fixture.RunAsync(async () =>
        {
            using ManualResetEventSlim hold = new();
            FakeSessionStore store = new() { Hold = hold };
            using SessionAutosave autosave = new(store, new ManualTimeProvider(), NullLogger<SessionAutosave>.Instance);
            autosave.Start(() => State(1));

            Task tick = autosave.SaveIfChangedAsync();
            Assert.True(store.Entered.Wait(TimeSpan.FromSeconds(10)));

            // Released from elsewhere: the close's write blocks this thread until the background one is done.
            Task release = Task.Run(async () =>
            {
                await Task.Delay(100);
                hold.Set();
            });
            Assert.True(autosave.SaveNow(State(2)));
            await tick;
            await release;

            Assert.Equal([1, 2], store.Saved.Select(CaretOf));
        });

    [Fact]
    public void TheClosesWrite_IsWritten_EvenOnceStopped()
        => fixture.Run(() =>
        {
            FakeSessionStore store = new();
            using SessionAutosave autosave = Inline(store);
            autosave.Start(() => State(1));
            autosave.Stop();

            Assert.True(autosave.SaveNow(State(2)));
            Assert.Equal([2], store.Saved.Select(CaretOf));
        });

    // Writes on the spot, so a tick is over by the time it returns.
    private static SessionAutosave Inline(FakeSessionStore store, ManualTimeProvider? clock = null)
        => new(store, clock ?? new ManualTimeProvider(), NullLogger<SessionAutosave>.Instance, work => Task.FromResult(work()));

    // One pane, one file, the caret where asked — a new instance every time, equal for equal carets.
    private static SessionState State(int caret)
        => new(
            SessionState.CurrentVersion,
            new LayoutSnapshot(Pane: 0),
            0,
            [new SessionPane(0, [new SessionTab(0, caret)], 0)],
            [new SessionDocument(@"C:\a.txt", "Untitled-1")]);

    private static int CaretOf(SessionState state) => state.Panes[0].Tabs[0].CaretOffset;

    // Background writes the test runs when it says.
    private sealed class QueuedWrites
    {
        private readonly Queue<(Func<bool> Work, TaskCompletionSource<bool> Done)> _queue = new();

        public Task<bool> Run(Func<bool> work)
        {
            TaskCompletionSource<bool> done = new();
            _queue.Enqueue((work, done));

            return done.Task;
        }

        public void RunNext()
        {
            (Func<bool> work, TaskCompletionSource<bool> done) = _queue.Dequeue();
            done.SetResult(work());
        }
    }
}

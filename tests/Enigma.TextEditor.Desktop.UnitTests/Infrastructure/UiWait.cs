using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Waits on the headless UI thread for what a test is about to look at, rather than for a guessed number of dispatcher
/// rounds.
/// </summary>
/// <remarks>
/// <para>
/// Run from inside <see cref="HeadlessAvaloniaFixture.RunAsync"/>: each round runs the dispatcher's jobs and yields, so
/// the fixture's own loop gets to run the continuations posted meanwhile, and work on the thread pool gets to finish.
/// </para>
/// <para>
/// A fixed count of rounds passes on an idle machine and fails on a loaded one. Waiting on the condition the test then
/// asserts takes as long as it takes, up to <see cref="Deadline"/>, and fails with what it was waiting for.
/// </para>
/// </remarks>
internal static class UiWait
{
    /// <summary>
    /// How long a wait lasts before the test fails: far past anything the UI does, short of hanging the run.
    /// </summary>
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    // How many rounds in a row the dispatcher must find nothing to do before it counts as settled.
    private const int QuietRounds = 3;

    /// <summary>
    /// Pumps the dispatcher until a condition holds.
    /// </summary>
    /// <param name="condition">What the test waits for; checked on the UI thread, after each round.</param>
    /// <param name="because">What the condition means, for the failure message.</param>
    /// <returns>A task that completes once the condition holds.</returns>
    /// <exception cref="Xunit.Sdk.FailException">The condition did not hold within <see cref="Deadline"/>.</exception>
    public static async Task UntilAsync(Func<bool> condition, string? because = null)
    {
        ArgumentNullException.ThrowIfNull(condition);

        Stopwatch clock = Stopwatch.StartNew();
        Dispatcher.UIThread.RunJobs();

        while (!condition())
        {
            if (clock.Elapsed > Deadline)
            {
                Assert.Fail(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Timed out after {Deadline.TotalSeconds:0} s waiting until {because ?? "the condition held"}."));
            }

            await Task.Yield();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>
    /// Reads a value that is only to be had asynchronously — the clipboard's text — until it is what the test expects,
    /// pumping the dispatcher between reads.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="read">Reads the value; called on the UI thread.</param>
    /// <param name="expected">Whether a value read is the one the test waits for.</param>
    /// <returns>
    /// The last value read: the expected one, or — after <see cref="Deadline"/> — whatever was there, for the test's own
    /// assertion to compare and report.
    /// </returns>
    public static async Task<T> ValueAsync<T>(Func<Task<T>> read, Func<T, bool> expected)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(expected);

        Stopwatch clock = Stopwatch.StartNew();
        Dispatcher.UIThread.RunJobs();
        T value = await read();

        while (!expected(value) && (clock.Elapsed <= Deadline))
        {
            await Task.Yield();
            Dispatcher.UIThread.RunJobs();
            value = await read();
        }

        return value;
    }

    /// <summary>
    /// Pumps the dispatcher until it has had nothing to do for a few rounds in a row: for a test that asserts that
    /// something did <em>not</em> happen, where there is no condition to wait on.
    /// </summary>
    /// <returns>A task that completes once the dispatcher is quiet.</returns>
    /// <exception cref="Xunit.Sdk.FailException">The dispatcher was still busy after <see cref="Deadline"/>.</exception>
    public static async Task SettleAsync()
    {
        Stopwatch clock = Stopwatch.StartNew();
        int quiet = 0;

        while (quiet < QuietRounds)
        {
            if (clock.Elapsed > Deadline)
            {
                Assert.Fail(string.Create(
                    CultureInfo.InvariantCulture,
                    $"The dispatcher was still busy after {Deadline.TotalSeconds:0} s."));
            }

            Dispatcher.UIThread.RunJobs();
            await Task.Yield();
            quiet = Dispatcher.UIThread.HasJobsWithPriority(DispatcherPriority.SystemIdle) ? 0 : quiet + 1;
        }

        Dispatcher.UIThread.RunJobs();
    }
}

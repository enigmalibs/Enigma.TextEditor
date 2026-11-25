using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The way into the safety net: what is reported before the reporter exists is kept and handed over,
/// nothing reaches a reporter that was taken away, and a posted callback that throws is reported, not
/// rethrown. The process events are neither hooked nor raised here: an exception that reaches
/// <see cref="AppDomain.UnhandledException"/> ends the test host too, and the test host's own unobserved
/// tasks would land among the reports under test.
/// </summary>
/// <remarks>
/// In the headless collection: the hooks' state is the process's, and the dispatcher hook — unhooked again
/// after each test — must never be live while another UI test runs.
/// </remarks>
[Collection(HeadlessCollection.Name)]
public sealed class UnhandledExceptionHooksTests : IDisposable
{
    private readonly HeadlessAvaloniaFixture _fixture;

    public UnhandledExceptionHooksTests(HeadlessAvaloniaFixture fixture)
    {
        _fixture = fixture;
        UnhandledExceptionHooks.ResetForTests();
    }

    public void Dispose() => UnhandledExceptionHooks.ResetForTests();

    [Fact]
    public void ACallbackThatThrowsOnTheUiThread_IsReported_AndTheDispatcherCarriesOn()
        => _fixture.Run(() =>
        {
            RecordingErrorReporter reporter = new();
            UnhandledExceptionHooks.InstallDispatcher();
            UnhandledExceptionHooks.Attach(reporter);
            InvalidOperationException error = new("a command failed");
            bool after = false;

            Dispatcher.UIThread.Post(() => throw error);
            Dispatcher.UIThread.Post(() => after = true);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(UnhandledErrorSource.Dispatcher, reporter.SourceOf(error));
            Assert.True(after, "The dispatcher stopped at the failing callback.");
        });

    [Fact]
    public void AReportBeforeTheReporter_IsHandedOverOnAttach_WithItsSource()
    {
        InvalidOperationException early = new("before the container");
        UnhandledExceptionHooks.Report(early, UnhandledErrorSource.Startup);
        RecordingErrorReporter reporter = new();

        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(UnhandledErrorSource.Startup, reporter.SourceOf(early));
    }

    [Fact]
    public void EarlyReports_AreKeptUpToTheLimit_InOrder()
    {
        List<Exception> early = [.. Enumerable.Range(0, UnhandledExceptionHooks.EarlyLimit + 5).Select(index => new InvalidOperationException($"early {index}"))];

        foreach (Exception exception in early)
        {
            UnhandledExceptionHooks.Report(exception, UnhandledErrorSource.UnobservedTask);
        }

        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);

        Assert.Equal(early.Take(UnhandledExceptionHooks.EarlyLimit), reporter.Reports.Select(report => report.Exception));
    }

    [Fact]
    public void AReportAfterAttach_GoesStraightToTheReporter_AndIsNotReplayedLater()
    {
        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        InvalidOperationException error = new("after the container");

        UnhandledExceptionHooks.Report(error, UnhandledErrorSource.Dispatcher);

        Assert.Equal(UnhandledErrorSource.Dispatcher, reporter.SourceOf(error));

        RecordingErrorReporter next = new();
        UnhandledExceptionHooks.Attach(next);
        Assert.Null(next.SourceOf(error));
    }

    [Fact]
    public void AfterDetach_NothingReachesTheReporter_AndReportsWaitForTheNextOne()
    {
        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        UnhandledExceptionHooks.Detach();
        InvalidOperationException late = new("while closing");

        UnhandledExceptionHooks.Report(late, UnhandledErrorSource.FireAndForget);

        Assert.Null(reporter.SourceOf(late));

        RecordingErrorReporter next = new();
        UnhandledExceptionHooks.Attach(next);
        Assert.Equal(UnhandledErrorSource.FireAndForget, next.SourceOf(late));
    }

    [Fact]
    public void AnEarlyReport_IsWrittenInFullToTheEarlyErrorsFile_AndStillHandedOver()
    {
        using TempFolder folder = new();
        string file = folder.PathOf(Path.Combine("logs", "early-errors.log"));
        UnhandledExceptionHooks.KeepEarlyErrorsIn(file);
        InvalidOperationException early = Thrown(new InvalidOperationException("the settings could not be read"));

        UnhandledExceptionHooks.Report(early, UnhandledErrorSource.Startup);

        string written = File.ReadAllText(file);
        Assert.Contains("(Startup)", written);
        Assert.Contains("System.InvalidOperationException: the settings could not be read", written);
        Assert.Contains(nameof(Thrown), written);

        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        Assert.Equal(UnhandledErrorSource.Startup, reporter.SourceOf(early));
    }

    [Fact]
    public void TwoEarlyReports_AreBothKept_InOrder()
    {
        using TempFolder folder = new();
        string file = folder.PathOf("early-errors.log");
        UnhandledExceptionHooks.KeepEarlyErrorsIn(file);

        UnhandledExceptionHooks.Report(new InvalidOperationException("first"), UnhandledErrorSource.Startup);
        UnhandledExceptionHooks.Report(new InvalidOperationException("second"), UnhandledErrorSource.UnobservedTask);

        string written = File.ReadAllText(file);
        Assert.True(written.IndexOf("first", StringComparison.Ordinal) < written.IndexOf("second", StringComparison.Ordinal));
    }

    [Fact]
    public void OnceTheReporterIsAttached_NothingIsWrittenToTheFile()
    {
        using TempFolder folder = new();
        string file = folder.PathOf("early-errors.log");
        UnhandledExceptionHooks.KeepEarlyErrorsIn(file);
        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);

        UnhandledExceptionHooks.Report(new InvalidOperationException("later"), UnhandledErrorSource.Dispatcher);

        Assert.False(File.Exists(file));
        Assert.Single(reporter.Reports);
    }

    [Fact]
    public void AFileThatCannotBeWritten_DoesNotThrow_AndTheReportIsStillKept()
    {
        using TempFolder folder = new();

        // A file where the folder would have to be: the folder cannot be created.
        string blocker = folder.Write("logs", "not a folder");
        UnhandledExceptionHooks.KeepEarlyErrorsIn(Path.Combine(blocker, "early-errors.log"));
        InvalidOperationException early = new("nowhere to write");

        UnhandledExceptionHooks.Report(early, UnhandledErrorSource.Startup);

        RecordingErrorReporter reporter = new();
        UnhandledExceptionHooks.Attach(reporter);
        Assert.Equal(UnhandledErrorSource.Startup, reporter.SourceOf(early));
    }

    [Fact]
    public void AFilePastItsLimit_IsStartedAgain()
    {
        using TempFolder folder = new();
        string file = folder.Write("early-errors.log", new string('x', (int)UnhandledExceptionHooks.EarlyFileLimit + 1));
        UnhandledExceptionHooks.KeepEarlyErrorsIn(file);

        UnhandledExceptionHooks.Report(new InvalidOperationException("fresh"), UnhandledErrorSource.Startup);

        string written = File.ReadAllText(file);
        Assert.DoesNotContain("xxx", written);
        Assert.Contains("fresh", written);
    }

    // Thrown and caught, so that it has a stack to write.
    private static T Thrown<T>(T exception)
        where T : Exception
    {
        try
        {
            throw exception;
        }
        catch (T caught)
        {
            return caught;
        }
    }
}

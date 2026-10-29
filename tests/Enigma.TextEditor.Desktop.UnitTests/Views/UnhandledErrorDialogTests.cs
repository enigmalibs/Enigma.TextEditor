using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Views;

/// <summary>
/// The safety net's dialog in the real window: Enigma.GitClient's, one at a time, never over a question.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class UnhandledErrorDialogTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void AReportedError_OpensTheDialog_WithTheMessageAndOneCloseButton()
        => fixture.RunAsync(async () =>
        {
            using Session session = new();

            session.Reporter.Report(new InvalidOperationException("the cache is gone"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();

            ContentDialog host = session.Window.HostDialog;
            Assert.True(host.IsOpen);
            Assert.Equal(UnhandledErrorReporter.Title, host.Title);
            Assert.Null(host.PrimaryButtonText);
            Assert.Equal("Close", host.CloseButtonText);
            Assert.Equal(DefaultButton.Close, host.DefaultButton);
            Assert.NotNull(host.IconData);
            UnhandledErrorViewModel error = ViewModelOf(host);
            Assert.Equal("the cache is gone", error.Message);
            Assert.False(error.HasMore);
            Assert.Contains("System.InvalidOperationException: the cache is gone", error.Details, StringComparison.Ordinal);
        });

    [Fact]
    public void ASecondErrorWhileItIsOpen_IsCountedIntoIt()
        => fixture.RunAsync(async () =>
        {
            using Session session = new();

            session.Reporter.Report(new InvalidOperationException("first"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();
            session.Reporter.Report(new InvalidOperationException("second"), UnhandledErrorSource.FireAndForget);
            await DrainAsync();

            UnhandledErrorViewModel error = ViewModelOf(session.Window.HostDialog);
            Assert.Equal("first", error.Message);
            Assert.True(error.HasMore);
            Assert.Equal("1 more error occurred.", error.MoreText);
        });

    [Fact]
    public void AnErrorDuringAQuestion_WaitsForTheAnswer_ThenShows()
        => fixture.RunAsync(async () =>
        {
            using Session session = new();
            Task<DialogResult> question = session.Dialogs.ShowMessageAsync("Save changes?", "Unsaved.");
            await DrainAsync();

            session.Reporter.Report(new InvalidOperationException("meanwhile"), UnhandledErrorSource.Dispatcher);
            await DrainAsync();

            Assert.Equal("Save changes?", session.Window.HostDialog.Title);

            await session.Dialogs.HideAsync();
            await DrainAsync();

            Assert.True(question.IsCompleted);
            Assert.True(session.Window.HostDialog.IsOpen);
            Assert.Equal(UnhandledErrorReporter.Title, session.Window.HostDialog.Title);
        });

    [Fact]
    public void AnErrorBeforeTheHost_IsShownOnceTheHostIsRegistered()
        => fixture.RunAsync(async () =>
        {
            using Session session = new(registerHost: false);

            session.Reporter.Report(new InvalidOperationException("early"), UnhandledErrorSource.Startup);
            await DrainAsync();
            Assert.False(session.Window.HostDialog.IsOpen);

            session.Reporter.RegisterHost(session.Window.HostDialog);
            await DrainAsync();

            Assert.True(session.Window.HostDialog.IsOpen);
            Assert.Equal("early", ViewModelOf(session.Window.HostDialog).Message);
        });

    [Fact]
    public void ACancellationSomeoneAskedFor_IsNotShown()
        => fixture.RunAsync(async () =>
        {
            using Session session = new();
            using CancellationTokenSource source = new();
            source.Cancel();

            session.Reporter.Report(new OperationCanceledException(source.Token), UnhandledErrorSource.FireAndForget);
            await DrainAsync();

            Assert.False(session.Window.HostDialog.IsOpen);
        });

    [Fact]
    public void AnErrorThatEndsTheProcess_IsLoggedAndFlushed_NotShown()
        => fixture.RunAsync(async () =>
        {
            using Session session = new();

            session.Reporter.Report(new InvalidOperationException("fatal"), UnhandledErrorSource.AppDomain);
            await DrainAsync();

            Assert.Equal(1, session.LoggerHandle.Flushes);
            Assert.False(session.Window.HostDialog.IsOpen);
        });

    [Fact]
    public void CopyDetails_PutsTheDetailsOnTheClipboard()
        => fixture.RunAsync(async () =>
        {
            string? copied = null;
            UnhandledErrorBatch batch = new(new InvalidOperationException("to report"));
            UnhandledErrorViewModel error = new(batch, text =>
            {
                copied = text;
                return Task.CompletedTask;
            });

            await error.CopyDetailsCommand.ExecuteAsync(null);

            Assert.Equal(batch.Details, copied);
        });

    private static UnhandledErrorViewModel ViewModelOf(ContentDialog host)
        => Assert.IsType<UnhandledErrorViewModel>(Assert.IsType<UnhandledErrorView>(host.Content).DataContext);

    private static async Task DrainAsync()
    {
        for (int round = 0; round < 5; round++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }
    }

    /// <summary>
    /// The real window, its dialog service and a reporter over them.
    /// </summary>
    private sealed class Session : IDisposable
    {
        private readonly EditorHarness _harness = new();

        public Session(bool registerHost = true)
        {
            Window = new MainWindow { DataContext = _harness.Window };
            Window.Show();
            Dispatcher.UIThread.RunJobs();
            Dialogs.RegisterHost(Window.HostDialog);
            Reporter = new UnhandledErrorReporter(Dialogs, LoggerHandle, new ManualTimeProvider(), NullLogger<UnhandledErrorReporter>.Instance);

            if (registerHost)
            {
                Reporter.RegisterHost(Window.HostDialog);
            }
        }

        public MainWindow Window { get; }

        public ContentDialogService Dialogs { get; } = new();

        public FakeLoggerHandle LoggerHandle { get; } = new();

        public UnhandledErrorReporter Reporter { get; }

        public void Dispose()
        {
            Window.Close();
            _harness.Dispose();
        }
    }
}

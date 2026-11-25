using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The window's start and close, owned by one sequence the window hands them to: what it says while it starts, and
/// that the window's members are that sequence's.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class EditorLifecycleTests(HeadlessAvaloniaFixture fixture)
{
    [Fact]
    public void WhileTheStartRuns_ItIsStarting_AndTheWindowSaysSoToo()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new(firstRun: FirstRunState.First);
            bool lifecycleStarting = false;
            bool windowStarting = false;
            harness.FirstRunDialog.WhileShown = () =>
            {
                lifecycleStarting = harness.Lifecycle.IsStarting;
                windowStarting = harness.Window.IsStarting;
            };

            await harness.Window.StartAsync(new StartupFiles([], []));

            Assert.True(lifecycleStarting);
            Assert.True(windowStarting);
            Assert.False(harness.Lifecycle.IsStarting);
            Assert.False(harness.Window.IsStarting);
        });

    [Fact]
    public void TheStart_KeepsTheSessionFromThenOn_AndTheWindowClosingStopsIt()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Lifecycle.StartAsync(new StartupFiles([], []));
            Assert.True(harness.Autosave.IsRunning);

            harness.Window.WindowClosed();
            Assert.False(harness.Autosave.IsRunning);
        });

    [Fact]
    public void TheCapture_IsTheWindows_EditorAreaAndExplorer()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();
            await harness.Workspace.OpenAsync(harness.Folder.Write("a.txt", "a\n"));
            harness.Explorer.IsVisible = true;

            Assert.Equal(harness.Lifecycle.CaptureSession(), harness.Window.CaptureSession());
            Assert.True(harness.Lifecycle.CaptureSession().ExplorerVisible);
            Assert.Single(harness.Lifecycle.CaptureSession().Documents);
        });
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Instances;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Instances;

/// <summary>
/// A server and a client on a real pipe, each test on a name of its own: what arrives, what comes back, and
/// that nothing a client does wrong stops the server.
/// </summary>
public sealed class InstanceServerTests
{
    [Fact]
    public async Task TheFiles_ArriveAtTheHandler_AndTheClientIsToldTheyWereTaken()
    {
        string pipe = NewPipeName();
        ConcurrentQueue<IReadOnlyList<string>> received = new();
        await using InstanceServer server = InstanceServer.Start(pipe, files => Take(received, files));

        bool forwarded = await InstanceClient.TryForwardAsync(pipe, ["/a.txt", "/b c.txt"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(forwarded);
        Assert.Equal(["/a.txt", "/b c.txt"], Assert.Single(received));
    }

    [Fact]
    public async Task TwoRequestsInARow_BothArrive()
    {
        string pipe = NewPipeName();
        ConcurrentQueue<IReadOnlyList<string>> received = new();
        await using InstanceServer server = InstanceServer.Start(pipe, files => Take(received, files));

        Assert.True(await InstanceClient.TryForwardAsync(pipe, ["/1.txt"], cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(await InstanceClient.TryForwardAsync(pipe, ["/2.txt"], cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal([["/1.txt"], ["/2.txt"]], received);
    }

    [Fact]
    public async Task WithNobodyListening_TheClientGivesUpWithinItsTimeout()
    {
        Stopwatch clock = Stopwatch.StartNew();

        bool forwarded = await InstanceClient.TryForwardAsync(
            NewPipeName(),
            ["/a.txt"],
            connectTimeout: TimeSpan.FromMilliseconds(300),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(forwarded);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3), $"Gave up after {clock.Elapsed}.");
    }

    [Fact]
    public async Task AHandlerThatRefuses_IsAFalse()
    {
        string pipe = NewPipeName();
        await using InstanceServer server = InstanceServer.Start(pipe, _ => false);

        Assert.False(await InstanceClient.TryForwardAsync(pipe, ["/a.txt"], cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AHandlerThatThrows_IsAFalse_IsReported_AndTheServerCarriesOn()
    {
        string pipe = NewPipeName();
        ConcurrentQueue<Exception> errors = new();
        int calls = 0;
        await using InstanceServer server = InstanceServer.Start(
            pipe,
            _ => ++calls == 1 ? throw new InvalidOperationException("boom") : true,
            errors.Enqueue);

        Assert.False(await InstanceClient.TryForwardAsync(pipe, ["/a.txt"], cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(await InstanceClient.TryForwardAsync(pipe, ["/b.txt"], cancellationToken: TestContext.Current.CancellationToken));

        Assert.IsType<InvalidOperationException>(Assert.Single(errors));
    }

    [Fact]
    public async Task AClientSpeakingAnotherLanguage_IsRefused_AndTheServerCarriesOn()
    {
        string pipe = NewPipeName();
        ConcurrentQueue<IReadOnlyList<string>> received = new();
        await using InstanceServer server = InstanceServer.Start(pipe, files => Take(received, files));

        await using (NamedPipeClientStream stranger = Connect(pipe))
        {
            await stranger.ConnectAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await stranger.WriteAsync(Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\n\r\n"), TestContext.Current.CancellationToken);
            await stranger.FlushAsync(TestContext.Current.CancellationToken);

            Assert.False(await InstanceProtocol.ReadReplyAsync(stranger, TestContext.Current.CancellationToken));
        }

        Assert.True(await InstanceClient.TryForwardAsync(pipe, ["/a.txt"], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(["/a.txt"], Assert.Single(received));
    }

    [Fact]
    public async Task AClientThatSaysNothing_IsDroppedAfterTheTimeout_AndTheServerCarriesOn()
    {
        string pipe = NewPipeName();
        ConcurrentQueue<Exception> errors = new();
        await using InstanceServer server = InstanceServer.Start(pipe, _ => true, errors.Enqueue, TimeSpan.FromMilliseconds(300));

        await using (NamedPipeClientStream silent = Connect(pipe))
        {
            await silent.ConnectAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

            Assert.True(await InstanceClient.TryForwardAsync(
                pipe,
                ["/a.txt"],
                connectTimeout: TimeSpan.FromSeconds(3),
                cancellationToken: TestContext.Current.CancellationToken));
        }

        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task OnceDisposed_TheServerTakesNoMore()
    {
        string pipe = NewPipeName();
        InstanceServer server = InstanceServer.Start(pipe, _ => true);
        Assert.True(await InstanceClient.TryForwardAsync(pipe, ["/a.txt"], cancellationToken: TestContext.Current.CancellationToken));

        await server.DisposeAsync();
        await server.DisposeAsync();

        Assert.False(await InstanceClient.TryForwardAsync(
            pipe,
            ["/a.txt"],
            connectTimeout: TimeSpan.FromMilliseconds(300),
            cancellationToken: TestContext.Current.CancellationToken));
    }

    private static bool Take(ConcurrentQueue<IReadOnlyList<string>> received, IReadOnlyList<string> files)
    {
        received.Enqueue(files);

        return true;
    }

    private static NamedPipeClientStream Connect(string pipe)
        => new(".", pipe, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    // Short: on Linux the pipe is a socket file whose whole path must fit in about a hundred characters.
    private static string NewPipeName() => $"ET.Tests.{Guid.NewGuid():N}";
}

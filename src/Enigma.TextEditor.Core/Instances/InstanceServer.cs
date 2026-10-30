using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Instances;

/// <summary>
/// The pipe a running instance listens on for files that a later launch hands over.
/// </summary>
/// <remarks>
/// <para>
/// One connection at a time, on a background task: the request is read, given to the handler, and answered,
/// and the server waits for the next. A connection that says nothing, or nothing this build reads, is answered
/// with a refusal or dropped after <see cref="DefaultRequestTimeout"/>; either way the server carries on. The
/// handler runs on that task, not on the UI thread.
/// </para>
/// <para>
/// The pipe is the current user's only: another account on the same machine cannot reach it.
/// </para>
/// </remarks>
public sealed class InstanceServer : IAsyncDisposable
{
    /// <summary>
    /// How long a connection has to make its request and take the answer.
    /// </summary>
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(5);

    private const PipeOptions Options = PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly string _pipeName;
    private readonly Func<IReadOnlyList<string>, bool> _onRequest;
    private readonly Action<Exception> _onError;
    private readonly TimeSpan _requestTimeout;
    private readonly CancellationTokenSource _stop = new();
    private Task _loop = Task.CompletedTask;

    private InstanceServer(string pipeName, Func<IReadOnlyList<string>, bool> onRequest, Action<Exception> onError, TimeSpan requestTimeout)
    {
        _pipeName = pipeName;
        _onRequest = onRequest;
        _onError = onError;
        _requestTimeout = requestTimeout;
    }

    /// <summary>
    /// Starts listening.
    /// </summary>
    /// <param name="pipeName">The pipe's name.</param>
    /// <param name="onRequest">Takes the files of a request, as full paths; returns whether it took them.</param>
    /// <param name="onError">Told about a connection that failed, or a handler that threw; the server carries on.</param>
    /// <param name="requestTimeout">How long a connection has; <see cref="DefaultRequestTimeout"/> when omitted.</param>
    /// <returns>The running server; disposing it stops it.</returns>
    /// <exception cref="IOException">The pipe could not be created, its name being taken.</exception>
    /// <remarks>The first pipe is created before this returns, so a name that cannot be served is known at once.</remarks>
    public static InstanceServer Start(
        string pipeName,
        Func<IReadOnlyList<string>, bool> onRequest,
        Action<Exception>? onError = null,
        TimeSpan? requestTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(onRequest);

        InstanceServer server = new(pipeName, onRequest, onError ?? (_ => { }), requestTimeout ?? DefaultRequestTimeout);
        NamedPipeServerStream first = server.CreatePipe();
        server._loop = Task.Run(() => server.ListenAsync(first, server._stop.Token));

        return server;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_stop.IsCancellationRequested)
        {
            return;
        }

        await _stop.CancelAsync().ConfigureAwait(false);

        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Stopping is what was asked for.
        }

        _stop.Dispose();
    }

    // Two instances of the pipe: the one being served, and the next one, already listening.
    private NamedPipeServerStream CreatePipe()
        => new(_pipeName, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, Options);

    private async Task ListenAsync(NamedPipeServerStream first, CancellationToken stopping)
    {
        NamedPipeServerStream? listening = first;

        try
        {
            while (!stopping.IsCancellationRequested)
            {
                NamedPipeServerStream? connected = null;

                try
                {
                    listening ??= CreatePipe();
                    await listening.WaitForConnectionAsync(stopping).ConfigureAwait(false);
                    (connected, listening) = (listening, null);

                    // The next pipe listens while this one is served: a launch arriving meanwhile waits its turn
                    // rather than finding no one there — on Linux, closing the last pipe would drop it.
                    listening = CreatePipe();
                    await ServeAsync(connected, stopping).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stopping.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException or UnauthorizedAccessException)
                {
                    // One connection went wrong: the next one is served all the same.
                    _onError(exception);

                    if (connected is null)
                    {
                        // The listening pipe itself failed: it is made again, after a pause, so that a name that
                        // cannot be served is not retried in a tight loop.
                        await DisposeQuietlyAsync(listening).ConfigureAwait(false);
                        listening = null;
                        await Task.Delay(RetryDelay, stopping).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await DisposeQuietlyAsync(connected).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await DisposeQuietlyAsync(listening).ConfigureAwait(false);
        }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken stopping)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        deadline.CancelAfter(_requestTimeout);

        IReadOnlyList<string>? files = await InstanceProtocol.ReadRequestAsync(pipe, deadline.Token).ConfigureAwait(false);
        bool accepted = (files is not null) && Handle(files);

        await InstanceProtocol.WriteReplyAsync(pipe, accepted, deadline.Token).ConfigureAwait(false);

        // Held open until the other end has read the answer and let go: closed any sooner, the answer could be
        // lost with the pipe. The other end closes; anything else it sends is not read.
        _ = await pipe.ReadAsync(new byte[1], deadline.Token).ConfigureAwait(false);
    }

    private bool Handle(IReadOnlyList<string> files)
    {
        try
        {
            return _onRequest(files);
        }
        catch (Exception exception)
        {
            // Whatever the handler does wrong, the launch is told no and opens the files itself.
            _onError(exception);

            return false;
        }
    }

    private static async ValueTask DisposeQuietlyAsync(NamedPipeServerStream? pipe)
    {
        if (pipe is null)
        {
            return;
        }

        try
        {
            await pipe.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
            // The other end left first; there is nothing left to flush.
        }
    }
}

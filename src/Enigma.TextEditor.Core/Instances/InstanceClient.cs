using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Instances;

/// <summary>
/// Hands files to a running instance's <see cref="InstanceServer"/>.
/// </summary>
public static class InstanceClient
{
    /// <summary>
    /// How long a launch waits to reach the running instance.
    /// </summary>
    public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long a launch waits for the running instance to answer, once reached.
    /// </summary>
    public static readonly TimeSpan DefaultReplyTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Asks the instance serving a pipe to open files.
    /// </summary>
    /// <param name="pipeName">The pipe.</param>
    /// <param name="files">The files, as full paths.</param>
    /// <param name="connectTimeout">How long to wait to reach it; <see cref="DefaultConnectTimeout"/> when omitted.</param>
    /// <param name="replyTimeout">How long to wait for its answer; <see cref="DefaultReplyTimeout"/> when omitted.</param>
    /// <param name="cancellationToken">Gives up.</param>
    /// <returns>
    /// Whether it took them. Every failure — nobody there, no answer in time, a refusal, a broken pipe, a request too
    /// long to send — is <see langword="false"/>, never an exception: the launch then opens the files itself.
    /// </returns>
    public static async Task<bool> TryForwardAsync(
        string pipeName,
        IReadOnlyList<string> files,
        TimeSpan? connectTimeout = null,
        TimeSpan? replyTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentNullException.ThrowIfNull(files);

        try
        {
            await using NamedPipeClientStream pipe = new(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            await pipe.ConnectAsync(connectTimeout ?? DefaultConnectTimeout, cancellationToken).ConfigureAwait(false);

            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(replyTimeout ?? DefaultReplyTimeout);

            await InstanceProtocol.WriteRequestAsync(pipe, files, deadline.Token).ConfigureAwait(false);

            return await InstanceProtocol.ReadReplyAsync(pipe, deadline.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or OperationCanceledException
                                              or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }
}

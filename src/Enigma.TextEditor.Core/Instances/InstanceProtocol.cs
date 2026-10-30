using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Core.Instances;

/// <summary>
/// What a launch says to a running instance, and the answer: one request naming files to open, one reply saying
/// whether they were taken.
/// </summary>
/// <remarks>
/// <para>
/// The request is a four-byte little-endian length, then that many bytes of UTF-8 JSON —
/// <c>{"version":1,"files":["…"]}</c> — so that any character a path may hold, a line break included, arrives as
/// it left. The reply is one byte: 1 for taken, 0 for refused.
/// </para>
/// <para>
/// A request of another version, longer than <see cref="MaxRequestBytes"/>, cut short or not this shape is
/// refused rather than thrown: the other end may be anything that found the pipe.
/// </para>
/// </remarks>
public static class InstanceProtocol
{
    /// <summary>
    /// The version of the request this build writes and reads.
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// The longest request read: thousands of long paths, and no more.
    /// </summary>
    public const int MaxRequestBytes = 1024 * 1024;

    private const byte Accepted = 1;
    private const byte Refused = 0;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Writes a request to open files.
    /// </summary>
    /// <param name="stream">The connection.</param>
    /// <param name="files">The files, as full paths.</param>
    /// <param name="cancellationToken">Stops the write.</param>
    /// <returns>A task that completes once the request is written.</returns>
    /// <exception cref="ArgumentException">The request would be longer than <see cref="MaxRequestBytes"/>.</exception>
    public static async Task WriteRequestAsync(Stream stream, IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(files);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(new Request(Version, files), Json);

        if (body.Length > MaxRequestBytes)
        {
            throw new ArgumentException($"The request is {body.Length} bytes long; at most {MaxRequestBytes} are read.", nameof(files));
        }

        byte[] length = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(length, body.Length);

        await stream.WriteAsync(length, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a request to open files.
    /// </summary>
    /// <param name="stream">The connection.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>The files, or <see langword="null"/> for a request this build does not take.</returns>
    /// <exception cref="OperationCanceledException">The read was cancelled.</exception>
    /// <exception cref="IOException">The connection failed.</exception>
    public static async Task<IReadOnlyList<string>?> ReadRequestAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            byte[] length = new byte[sizeof(int)];
            await stream.ReadExactlyAsync(length, cancellationToken).ConfigureAwait(false);
            int size = BinaryPrimitives.ReadInt32LittleEndian(length);

            if (size is <= 0 or > MaxRequestBytes)
            {
                return null;
            }

            byte[] body = new byte[size];
            await stream.ReadExactlyAsync(body, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<Request>(body, Json) is { Version: Version, Files: { } files } && files.All(file => file is not null)
                ? files
                : null;
        }
        catch (Exception exception) when (exception is EndOfStreamException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the reply to a request.
    /// </summary>
    /// <param name="stream">The connection.</param>
    /// <param name="accepted">Whether the files were taken.</param>
    /// <param name="cancellationToken">Stops the write.</param>
    /// <returns>A task that completes once the reply is written.</returns>
    public static async Task WriteReplyAsync(Stream stream, bool accepted, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await stream.WriteAsync(new[] { accepted ? Accepted : Refused }, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the reply to a request.
    /// </summary>
    /// <param name="stream">The connection.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>Whether the files were taken; <see langword="false"/> when the connection closed without a reply.</returns>
    public static async Task<bool> ReadReplyAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] reply = new byte[1];

        return (await stream.ReadAsync(reply, cancellationToken).ConfigureAwait(false) == 1) && (reply[0] == Accepted);
    }

    private sealed record Request(int Version, IReadOnlyList<string>? Files);
}

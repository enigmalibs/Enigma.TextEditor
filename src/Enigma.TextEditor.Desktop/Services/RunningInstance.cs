using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Instances;
using Microsoft.Extensions.Logging;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The receiving side of opening files in the running instance: this instance's pipe, and its name in the pointer
/// file whenever its window is the one in use.
/// </summary>
/// <remarks>
/// Built by the composition root once the window exists, and disposed as the application exits — process-edge
/// plumbing, as the unhandled-exception hooks are. When the pipe cannot be served, this instance only misses out on
/// files: a launch that cannot reach it opens them in an instance of its own.
/// </remarks>
public sealed class RunningInstance : IAsyncDisposable
{
    private readonly string _pointerFile;
    private readonly InstanceEndpoint _endpoint;
    private readonly ILogger<RunningInstance> _logger;
    private readonly Lock _writing = new();
    private InstanceServer? _server;

    private RunningInstance(string pointerFile, InstanceEndpoint endpoint, ILogger<RunningInstance> logger)
    {
        _pointerFile = pointerFile;
        _endpoint = endpoint;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the pipe is served: whether a launch can hand files to this instance.
    /// </summary>
    public bool IsListening => _server is not null;

    /// <summary>
    /// Starts serving this instance's pipe, and names it in the pointer file as the instance in use.
    /// </summary>
    /// <param name="pointerFile">The file naming the instance used last.</param>
    /// <param name="endpoint">This instance's pipe and process.</param>
    /// <param name="onFiles">
    /// Takes the files a launch handed over, as full paths, on a background thread; returns whether it took them.
    /// </param>
    /// <param name="logger">Where requests and failures are logged.</param>
    /// <returns>The running instance, listening if its pipe could be served.</returns>
    public static RunningInstance Start(
        string pointerFile,
        InstanceEndpoint endpoint,
        Func<IReadOnlyList<string>, bool> onFiles,
        ILogger<RunningInstance> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pointerFile);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(onFiles);
        ArgumentNullException.ThrowIfNull(logger);

        RunningInstance instance = new(pointerFile, endpoint, logger);

        try
        {
            instance._server = InstanceServer.Start(
                endpoint.PipeName,
                files =>
                {
                    logger.LogInformation("Another launch handed over {FileCount} file(s) to open.", files.Count);

                    return onFiles(files);
                },
                failure => logger.LogWarning(failure, "A launch handing files over failed to reach this instance."));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            logger.LogWarning(exception, "Files opened from the system cannot reach this instance: its pipe {PipeName} could not be served.", endpoint.PipeName);

            return instance;
        }

        instance.WritePointer();

        return instance;
    }

    /// <summary>
    /// Names this instance in the pointer file, off the calling thread: its window is the one in use now.
    /// </summary>
    /// <returns>A task that completes once it is written, or has failed and been logged.</returns>
    public Task MarkActiveAsync() => IsListening ? Task.Run(WritePointer) : Task.CompletedTask;

    /// <inheritdoc />
    /// <remarks>Stops serving, and takes this instance's name out of the pointer file, when it is still there.</remarks>
    public async ValueTask DisposeAsync()
    {
        if (_server is not { } server)
        {
            return;
        }

        _server = null;
        await server.DisposeAsync().ConfigureAwait(false);

        lock (_writing)
        {
            ActiveInstanceFile.Clear(_pointerFile, _endpoint);
        }
    }

    // One write at a time: two activations close together would otherwise share the temporary file.
    private void WritePointer()
    {
        lock (_writing)
        {
            if (_server is null)
            {
                return;
            }

            try
            {
                ActiveInstanceFile.Write(_pointerFile, _endpoint);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Could not name this instance in {PointerFile}; files opened from the system may go to another.", _pointerFile);
            }
        }
    }
}

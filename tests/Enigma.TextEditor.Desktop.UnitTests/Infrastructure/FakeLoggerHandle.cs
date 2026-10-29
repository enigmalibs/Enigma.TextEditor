using Enigma.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// An <see cref="IFlushableLoggerHandle"/> that logs nowhere and counts its flushes.
/// </summary>
public sealed class FakeLoggerHandle : IFlushableLoggerHandle
{
    /// <summary>
    /// Gets how many times the log was flushed.
    /// </summary>
    public int Flushes { get; private set; }

    /// <inheritdoc />
    public ILogger Logger => NullLogger.Instance;

    /// <inheritdoc />
    public ILoggerFactory Factory => NullLoggerFactory.Instance;

    /// <inheritdoc />
    public ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;

    /// <inheritdoc />
    public void Flush() => Flushes++;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

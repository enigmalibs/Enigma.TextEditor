using System;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// Bytes already in memory, as a byte source.
/// </summary>
public sealed class MemoryByteSource : IByteSource
{
    private readonly ReadOnlyMemory<byte> _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryByteSource"/> class.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    public MemoryByteSource(ReadOnlyMemory<byte> bytes) => _bytes = bytes;

    /// <inheritdoc />
    public long Length => _bytes.Length;

    /// <inheritdoc />
    public int Read(long offset, Span<byte> buffer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        if (offset >= _bytes.Length)
        {
            return 0;
        }

        ReadOnlySpan<byte> available = _bytes.Span[(int)offset..];
        int count = Math.Min(available.Length, buffer.Length);
        available[..count].CopyTo(buffer);

        return count;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}

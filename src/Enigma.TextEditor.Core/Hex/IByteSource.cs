using System;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// Bytes read on demand, a few rows at a time — what the hex viewer shows.
/// </summary>
public interface IByteSource : IDisposable
{
    /// <summary>
    /// Gets how many bytes there are.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Reads bytes from an offset into a buffer.
    /// </summary>
    /// <param name="offset">Where to start, from the beginning.</param>
    /// <param name="buffer">Where to put them; at most its length is read.</param>
    /// <returns>How many bytes were read: fewer than asked at the end, none past it.</returns>
    int Read(long offset, Span<byte> buffer);
}

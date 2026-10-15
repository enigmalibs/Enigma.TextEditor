using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// A file's bytes, read where they are asked for and never loaded whole.
/// </summary>
/// <remarks>
/// <para>
/// The handle is opened for reading only and shares writing and deleting, so viewing a file never stops
/// another program from changing it. The length is the one the file had when it was opened: a file
/// that grows afterwards shows what it held then, and one that shrinks reads short at the end.
/// </para>
/// <para>
/// <see cref="RandomAccess"/> reads at an offset without a file position, so a read is one system call
/// and there is no stream to keep in step with the rows on screen.
/// </para>
/// </remarks>
public sealed class FileByteSource : IByteSource
{
    private readonly SafeFileHandle _handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileByteSource"/> class.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <exception cref="IOException">The file could not be opened.</exception>
    /// <exception cref="UnauthorizedAccessException">The file may not be read.</exception>
    public FileByteSource(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _handle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Length = RandomAccess.GetLength(_handle);
    }

    /// <inheritdoc />
    public long Length { get; }

    /// <inheritdoc />
    public int Read(long offset, Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_handle.IsClosed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        if (offset >= Length)
        {
            return 0;
        }

        int wanted = (int)Math.Min(buffer.Length, Length - offset);
        int read = 0;

        // A read may return short of what was asked even before the end; ask again for the rest.
        while (read < wanted)
        {
            int chunk = RandomAccess.Read(_handle, buffer[read..wanted], offset + read);

            if (chunk == 0)
            {
                break;
            }

            read += chunk;
        }

        return read;
    }

    /// <inheritdoc />
    public void Dispose() => _handle.Dispose();
}

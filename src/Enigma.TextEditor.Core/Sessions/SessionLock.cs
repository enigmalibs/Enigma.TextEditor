using System;
using System.IO;

namespace Enigma.TextEditor.Core.Sessions;

/// <summary>
/// The claim one running instance has on the session: only the instance holding it restores the session and
/// writes it back.
/// </summary>
/// <remarks>
/// <para>
/// Several instances can run at once — a launch without files starts another — and they share one session file.
/// Without an owner, each would restore the same tabs, and the last one to close would overwrite the unsaved text
/// the others had kept. The first instance takes the lock and keeps it for its whole life; the others work as if
/// there were no session at all.
/// </para>
/// <para>
/// The lock is the file held open with no sharing, not the file's existence: the operating system lets go of it
/// when the process ends, however it ends, so a crash never leaves the session claimed by an instance that is gone.
/// The file itself stays, empty, between runs.
/// </para>
/// </remarks>
public sealed class SessionLock : IDisposable
{
    private readonly FileStream _handle;

    private SessionLock(FileStream handle) => _handle = handle;

    /// <summary>
    /// Takes the lock, unless another process holds it.
    /// </summary>
    /// <param name="path">The lock file; created, with its folder, when missing.</param>
    /// <returns>
    /// The lock, held until disposed; or <see langword="null"/> when another process holds it — or when the file
    /// cannot be made at all, in which case no session could be written either.
    /// </returns>
    public static SessionLock? TryAcquire(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (Path.GetDirectoryName(path) is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
            }

            return new SessionLock(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Lets go of the lock, for the next instance to start to take.
    /// </summary>
    public void Dispose() => _handle.Dispose();
}

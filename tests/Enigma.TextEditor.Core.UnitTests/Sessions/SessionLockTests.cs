using System;
using System.IO;
using Enigma.TextEditor.Core.Sessions;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Sessions;

/// <summary>
/// The session's lock: one holder at a time, free again as soon as the holder lets go.
/// </summary>
public sealed class SessionLockTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Enigma.TextEditor.Tests", Guid.NewGuid().ToString("N"));

    private string LockPath => Path.Combine(_root, "data", "session.lock");

    [Fact]
    public void TheFirstClaim_GetsTheLock_AndTheFolderIsMadeForIt()
    {
        using SessionLock? held = SessionLock.TryAcquire(LockPath);

        Assert.NotNull(held);
        Assert.True(File.Exists(LockPath));
    }

    [Fact]
    public void ASecondClaim_IsRefusedWhileTheFirstHoldsIt()
    {
        using SessionLock? first = SessionLock.TryAcquire(LockPath);

        Assert.NotNull(first);
        Assert.Null(SessionLock.TryAcquire(LockPath));
    }

    [Fact]
    public void ALockLetGo_CanBeTakenAgain()
    {
        SessionLock? first = SessionLock.TryAcquire(LockPath);
        Assert.NotNull(first);
        first.Dispose();

        using SessionLock? second = SessionLock.TryAcquire(LockPath);

        Assert.NotNull(second);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp folder is not worth failing a green test over.
        }
    }
}

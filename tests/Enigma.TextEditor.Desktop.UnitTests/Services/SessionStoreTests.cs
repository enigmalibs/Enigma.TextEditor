using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The store the session is written through: written every few seconds, so a folder that refuses it must not fill the
/// log.
/// </summary>
public sealed class SessionStoreTests
{
    [Fact]
    public void ARunOfFailedWrites_IsLoggedOnceAsAnError_AndTheNextWriteSaysItIsWrittenAgain()
    {
        using TempFolder folder = new();
        RecordingLogger<SessionStore> logger = new();
        using SessionStore store = new(folder.PathOf(AppPaths.SessionFileName), folder.PathOf(AppPaths.SessionLockFileName), logger);

        // A folder where the file should be: the session can be neither written nor moved over it.
        Directory.CreateDirectory(folder.PathOf(AppPaths.SessionFileName));

        Assert.False(store.Save(Empty()));
        Assert.False(store.Save(Empty()));
        Assert.False(store.Save(Empty()));

        Directory.Delete(folder.PathOf(AppPaths.SessionFileName));
        Assert.True(store.Save(Empty()));
        Assert.True(store.Save(Empty()));

        Assert.Equal(
            [LogLevel.Error, LogLevel.Debug, LogLevel.Debug, LogLevel.Information],
            logger.Entries.Select(entry => entry.Level));
    }

    private static SessionState Empty()
        => new(SessionState.CurrentVersion, new LayoutSnapshot(Pane: 0), 0, [new SessionPane(0, [], -1)], []);
}

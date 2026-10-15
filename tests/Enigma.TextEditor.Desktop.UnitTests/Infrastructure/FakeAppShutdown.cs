using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records exit requests instead of closing anything.
/// </summary>
internal sealed class FakeAppShutdown : IAppShutdown
{
    public int ExitRequests { get; private set; }

    public bool DiscardChangesOnExit { get; private set; }

    public void RequestExit() => ExitRequests++;

    public void ExitDiscardingChanges()
    {
        DiscardChangesOnExit = true;
        ExitRequests++;
    }
}

using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Counts how often the About dialog was asked for.
/// </summary>
internal sealed class FakeAboutDialogService : IAboutDialogService
{
    public int Shown { get; private set; }

    public Task ShowAsync()
    {
        Shown++;
        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Counts the writes asked for, and finishes each one at once.
/// </summary>
internal sealed class FakeSettingsWriter : ISettingsWriter
{
    public int Saves { get; private set; }

    public Task<bool> SaveAsync()
    {
        Saves++;
        return Task.FromResult(true);
    }
}

using System;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Counts how often the welcome dialog was shown, and runs what a test wants done while it is up.
/// </summary>
internal sealed class FakeFirstRunDialogService : IFirstRunDialogService
{
    public int Shown { get; private set; }

    public Action? WhileShown { get; set; }

    public Task ShowAsync()
    {
        Shown++;
        WhileShown?.Invoke();

        return Task.CompletedTask;
    }
}

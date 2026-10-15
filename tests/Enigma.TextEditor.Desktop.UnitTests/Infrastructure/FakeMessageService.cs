using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Records the errors that would have been shown.
/// </summary>
internal sealed class FakeMessageService : IMessageService
{
    public List<(string Title, string Message)> Errors { get; } = [];

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return Task.CompletedTask;
    }
}

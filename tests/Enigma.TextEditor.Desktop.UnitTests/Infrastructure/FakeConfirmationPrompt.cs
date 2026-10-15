using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers confirmations from a script; an unscripted question is declined, the answer that loses
/// nothing.
/// </summary>
internal sealed class FakeConfirmationPrompt : IConfirmationPrompt
{
    public Queue<bool> Answers { get; } = new();

    public List<string> Asked { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        Asked.Add(title);

        return Task.FromResult(Answers.Count > 0 && Answers.Dequeue());
    }
}

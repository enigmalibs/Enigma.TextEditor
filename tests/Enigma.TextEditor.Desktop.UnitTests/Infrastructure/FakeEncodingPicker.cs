using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the encoding picker from a script; an unscripted pick is a cancel.
/// </summary>
internal sealed class FakeEncodingPicker : IEncodingPicker
{
    public Queue<TextEncoding?> Answers { get; } = new();

    public List<(string Title, TextEncoding Current)> Asked { get; } = [];

    public Task<TextEncoding?> PickAsync(string title, TextEncoding current)
    {
        Asked.Add((title, current));

        return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
    }
}

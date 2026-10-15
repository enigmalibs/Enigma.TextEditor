using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the binary-file and too-large questions from a script; an unscripted question is a cancel.
/// </summary>
internal sealed class FakeBinaryFilePrompt : IBinaryFilePrompt
{
    public Queue<BinaryFileChoice> BinaryAnswers { get; } = new();

    public Queue<bool> TooLargeAnswers { get; } = new();

    public List<string> AskedAbout { get; } = [];

    public Task<BinaryFileChoice> AskBinaryAsync(string fileName)
    {
        AskedAbout.Add($"binary:{fileName}");

        return Task.FromResult(BinaryAnswers.Count > 0 ? BinaryAnswers.Dequeue() : BinaryFileChoice.Cancel);
    }

    public Task<bool> AskTooLargeAsync(string fileName, string size)
    {
        AskedAbout.Add($"large:{fileName}:{size}");

        return Task.FromResult(TooLargeAnswers.Count > 0 && TooLargeAnswers.Dequeue());
    }
}

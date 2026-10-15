using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the unsaved-changes question from a script, recording who it was asked about.
/// </summary>
/// <remarks>
/// An unscripted question is answered <see cref="UnsavedChangesChoice.Cancel"/>, the answer that loses
/// nothing — so a test that forgot to expect a question fails on what was kept rather than on data loss.
/// </remarks>
internal sealed class FakeUnsavedChangesPrompt : IUnsavedChangesPrompt
{
    public Queue<UnsavedChangesChoice> Answers { get; } = new();

    public List<string> AskedAbout { get; } = [];

    public Task<UnsavedChangesChoice> AskAsync(string documentTitle)
    {
        AskedAbout.Add(documentTitle);

        return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : UnsavedChangesChoice.Cancel);
    }
}

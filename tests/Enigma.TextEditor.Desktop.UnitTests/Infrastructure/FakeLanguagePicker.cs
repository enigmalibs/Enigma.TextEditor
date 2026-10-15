using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the language picker from a script; an unscripted pick is a cancel.
/// </summary>
internal sealed class FakeLanguagePicker : ILanguagePicker
{
    public Queue<LanguageInfo?> Answers { get; } = new();

    public List<LanguageInfo> OpenedWith { get; } = [];

    public Task<LanguageInfo?> PickAsync(LanguageInfo current)
    {
        OpenedWith.Add(current);

        return Task.FromResult(Answers.Count > 0 ? Answers.Dequeue() : null);
    }
}

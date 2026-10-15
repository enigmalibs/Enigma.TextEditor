using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the open and save pickers with scripted paths.
/// </summary>
internal sealed class FakeFilePrompts : IFilePrompts
{
    public Queue<IReadOnlyList<string>> FilesToOpen { get; } = new();

    public Queue<string?> SavePaths { get; } = new();

    public int SaveQuestions { get; private set; }

    public string? LastSuggestedName { get; private set; }

    public Task<IReadOnlyList<string>> AskFilesToOpenAsync(string? startDirectory)
        => Task.FromResult(FilesToOpen.Count > 0 ? FilesToOpen.Dequeue() : (IReadOnlyList<string>)[]);

    public Task<string?> AskWhereToSaveAsync(string suggestedName, string? startDirectory)
    {
        SaveQuestions++;
        LastSuggestedName = suggestedName;

        return Task.FromResult(SavePaths.Count > 0 ? SavePaths.Dequeue() : null);
    }
}

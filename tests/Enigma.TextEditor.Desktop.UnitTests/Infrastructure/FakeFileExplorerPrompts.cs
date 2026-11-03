using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// Answers the explorer's questions with scripted names; an empty queue answers as a cancel.
/// </summary>
internal sealed class FakeFileExplorerPrompts : IFileExplorerPrompts
{
    public Queue<string?> NewFileNames { get; } = new();

    public Queue<string?> NewFolderNames { get; } = new();

    public List<string> AskedIn { get; } = [];

    public Task<string?> AskNewFileNameAsync(string directory)
    {
        AskedIn.Add(directory);

        return Task.FromResult(NewFileNames.Count > 0 ? NewFileNames.Dequeue() : null);
    }

    public Task<string?> AskNewFolderNameAsync(string directory)
    {
        AskedIn.Add(directory);

        return Task.FromResult(NewFolderNames.Count > 0 ? NewFolderNames.Dequeue() : null);
    }

    public Queue<string?> NewNames { get; } = new();

    public List<(string Path, bool IsFolder)> RenamesAsked { get; } = [];

    public Task<string?> AskNewNameAsync(string path, bool isFolder)
    {
        RenamesAsked.Add((path, isFolder));

        return Task.FromResult(NewNames.Count > 0 ? NewNames.Dequeue() : null);
    }
}

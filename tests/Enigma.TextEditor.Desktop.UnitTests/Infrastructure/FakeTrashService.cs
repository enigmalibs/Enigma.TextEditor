using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Files;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A trash that takes an entry off the disk as the real one would — without filling the developer's recycle bin — or
/// declines it, leaving it where it is.
/// </summary>
internal sealed class FakeTrashService : ITrashService
{
    public bool IsAvailable { get; set; } = true;

    public bool Declines { get; set; }

    /// <summary>
    /// Gets or sets what the next move to the trash throws, as a trash that cannot reach the entry would; none, it works.
    /// </summary>
    public System.Exception? Throws { get; set; }

    public List<string> Trashed { get; } = [];

    public Task<bool> MoveToTrashAsync(string path, CancellationToken cancellationToken = default)
    {
        if (Throws is { } failure)
        {
            return Task.FromException<bool>(failure);
        }

        if (Declines)
        {
            return Task.FromResult(false);
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
        else
        {
            File.Delete(path);
        }

        Trashed.Add(path);

        return Task.FromResult(true);
    }
}

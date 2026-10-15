using System;
using System.IO;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// A folder under the temporary directory, removed when disposed.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "Enigma.TextEditor.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string PathOf(string name) => Path.Combine(Root, name);

    public string Write(string name, string text)
    {
        string path = PathOf(name);
        File.WriteAllText(path, text);
        return path;
    }

    public string Write(string name, byte[] bytes)
    {
        string path = PathOf(name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A log file the logger still holds, or a leftover temp folder, is not worth failing a
            // green test over.
        }
    }
}

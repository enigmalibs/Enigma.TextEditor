using System;
using System.IO;
using Enigma.TextEditor.Core.Instances;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Instances;

/// <summary>
/// The pointer to the instance used last: written whole, read back or not at all, cleared only by its owner.
/// </summary>
public sealed class ActiveInstanceFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Enigma.TextEditor.Tests", Guid.NewGuid().ToString("N"));

    private string PointerFile => Path.Combine(_root, "data", "instance.json");

    [Fact]
    public void AnEndpoint_RoundTrips_AndTheFolderIsMadeForIt()
    {
        InstanceEndpoint endpoint = new("Enigma.TextEditor.4242", 4242);

        ActiveInstanceFile.Write(PointerFile, endpoint);

        Assert.Equal(endpoint, ActiveInstanceFile.TryRead(PointerFile));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(PointerFile)!));
    }

    [Fact]
    public void TheLastWriterWins()
    {
        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.1", 1));
        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.2", 2));

        Assert.Equal(new InstanceEndpoint("Enigma.TextEditor.2", 2), ActiveInstanceFile.TryRead(PointerFile));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"pipeName\":\"\",\"processId\":12}")]
    [InlineData("{\"pipeName\":\"Enigma.TextEditor.12\",\"processId\":0}")]
    [InlineData("{\"processId\":12}")]
    [InlineData("null")]
    public void AFileThatIsNotAPointer_ReadsAsNone(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PointerFile)!);
        File.WriteAllText(PointerFile, content);

        Assert.Null(ActiveInstanceFile.TryRead(PointerFile));
    }

    [Fact]
    public void AMissingFile_ReadsAsNone() => Assert.Null(ActiveInstanceFile.TryRead(PointerFile));

    [Fact]
    public void Clearing_RemovesTheInstancesOwnPointer()
    {
        InstanceEndpoint mine = new("Enigma.TextEditor.7", 7);
        ActiveInstanceFile.Write(PointerFile, mine);

        ActiveInstanceFile.Clear(PointerFile, mine);

        Assert.False(File.Exists(PointerFile));
    }

    [Fact]
    public void Clearing_LeavesAnotherInstancesPointerAlone()
    {
        InstanceEndpoint other = new("Enigma.TextEditor.8", 8);
        ActiveInstanceFile.Write(PointerFile, other);

        ActiveInstanceFile.Clear(PointerFile, new InstanceEndpoint("Enigma.TextEditor.7", 7));
        ActiveInstanceFile.Clear(Path.Combine(_root, "nowhere.json"), other);

        Assert.Equal(other, ActiveInstanceFile.TryRead(PointerFile));
    }

    [Fact]
    public void TheCurrentProcess_IsNamedAfterItsId()
    {
        InstanceEndpoint endpoint = InstanceEndpoint.ForCurrentProcess();

        Assert.Equal(Environment.ProcessId, endpoint.ProcessId);
        Assert.Equal($"Enigma.TextEditor.{Environment.ProcessId}", endpoint.PipeName);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp folder is not worth failing a green test over.
        }
    }
}

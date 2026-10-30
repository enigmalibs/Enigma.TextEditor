using System;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Instances;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// The receiving side: this instance's name in the pointer file while its window is the one in use, and an instance
/// whose pipe cannot be served.
/// </summary>
public sealed class RunningInstanceTests : IDisposable
{
    private readonly TempFolder _folder = new();

    private string PointerFile => _folder.PathOf("instance.json");

    [Fact]
    public async Task Activating_NamesThisInstanceAgain_AfterAnotherTookThePointer()
    {
        InstanceEndpoint mine = NewEndpoint(1001);
        await using RunningInstance running = Start(mine);
        ActiveInstanceFile.Write(PointerFile, NewEndpoint(1002));

        await running.MarkActiveAsync();

        Assert.Equal(mine, ActiveInstanceFile.TryRead(PointerFile));
    }

    [Fact]
    public async Task Closing_LeavesAnotherInstancesPointerAlone()
    {
        InstanceEndpoint other = NewEndpoint(1002);
        RunningInstance running = Start(NewEndpoint(1001));
        ActiveInstanceFile.Write(PointerFile, other);

        await running.DisposeAsync();
        await running.DisposeAsync();

        Assert.Equal(other, ActiveInstanceFile.TryRead(PointerFile));
    }

    [Fact]
    public async Task APipeThatCannotBeServed_IsLogged_AndThatInstanceNeverNamesItself()
    {
        InstanceEndpoint first = NewEndpoint(1001);
        await using RunningInstance serving = Start(first);

        // The same name again: two pipes are allowed, a third is not.
        await using RunningInstance second = Start(first with { ProcessId = 1002 });
        await using RunningInstance third = Start(first with { ProcessId = 1003 });

        Assert.True(serving.IsListening);
        Assert.False(third.IsListening);

        await third.MarkActiveAsync();
        Assert.NotEqual(1003, ActiveInstanceFile.TryRead(PointerFile)?.ProcessId);
    }

    public void Dispose() => _folder.Dispose();

    private static InstanceEndpoint NewEndpoint(int processId) => new($"ET.Tests.{Guid.NewGuid():N}", processId);

    private RunningInstance Start(InstanceEndpoint endpoint)
        => RunningInstance.Start(PointerFile, endpoint, _ => true, NullLogger<RunningInstance>.Instance);
}

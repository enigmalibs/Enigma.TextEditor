using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Instances;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

/// <summary>
/// Whether a launch hands its files to the running instance — and, end to end, the files arriving there.
/// </summary>
public sealed class InstanceForwardingTests : IDisposable
{
    private const int OtherProcess = 424242;

    private readonly TempFolder _folder = new();
    private readonly List<(string Pipe, IReadOnlyList<string> Files)> _sent = [];

    private string PointerFile => _folder.PathOf("instance.json");

    [Fact]
    public async Task ALaunchWithFiles_HandsThemToTheInstanceUsedLast_AsFullPaths()
    {
        string a = _folder.Write("a.txt", "a");
        string missing = _folder.PathOf("new.txt");
        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.424242", OtherProcess));

        bool forwarded = await TryForwardAsync(Startup("a.txt", "new.txt"));

        Assert.True(forwarded);
        (string pipe, IReadOnlyList<string> files) = Assert.Single(_sent);
        Assert.Equal("Enigma.TextEditor.424242", pipe);
        Assert.Equal([a, missing], files);
    }

    [Fact]
    public async Task ALaunchWithoutFiles_StartsAnInstanceOfItsOwn()
    {
        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.424242", OtherProcess));
        Directory.CreateDirectory(_folder.PathOf("folder"));

        Assert.False(await TryForwardAsync(Startup()));
        Assert.False(await TryForwardAsync(Startup("folder")));
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task WithNoInstanceNamed_TheLaunchOpensTheFilesItself()
    {
        _folder.Write("a.txt", "a");

        Assert.False(await TryForwardAsync(Startup("a.txt")));
        Assert.Empty(_sent);
    }

    [Fact]
    public async Task AnInstanceThatHasGone_OrThisVeryProcess_IsNotAskedAtAll()
    {
        _folder.Write("a.txt", "a");

        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.424242", OtherProcess));
        Assert.False(await TryForwardAsync(Startup("a.txt"), isRunning: false));

        ActiveInstanceFile.Write(PointerFile, InstanceEndpoint.ForCurrentProcess());
        Assert.False(await TryForwardAsync(Startup("a.txt")));

        Assert.Empty(_sent);
    }

    [Fact]
    public async Task AnInstanceThatDoesNotTakeThem_LeavesThemToTheLaunch()
    {
        _folder.Write("a.txt", "a");
        ActiveInstanceFile.Write(PointerFile, new InstanceEndpoint("Enigma.TextEditor.424242", OtherProcess));

        Assert.False(await TryForwardAsync(Startup("a.txt"), accepted: false));
        Assert.Single(_sent);
    }

    [Fact]
    public async Task EndToEnd_TheFilesReachTheRunningInstance_AndItsNameIsClearedWhenItGoes()
    {
        string a = _folder.Write("a.txt", "a");
        List<IReadOnlyList<string>> received = [];
        InstanceEndpoint endpoint = new($"ET.Tests.{Guid.NewGuid():N}", OtherProcess);
        RunningInstance running = RunningInstance.Start(
            PointerFile,
            endpoint,
            files =>
            {
                lock (received)
                {
                    received.Add(files);
                }

                return true;
            },
            NullLogger<RunningInstance>.Instance);

        Assert.True(running.IsListening);
        Assert.Equal(endpoint, ActiveInstanceFile.TryRead(PointerFile));

        bool forwarded = await InstanceForwarding.TryForwardAsync(
            Startup("a.txt"),
            PointerFile,
            _ => true,
            (pipe, files) => InstanceClient.TryForwardAsync(pipe, files, cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(forwarded);
        Assert.Equal([a], Assert.Single(received));

        await running.DisposeAsync();

        Assert.Null(ActiveInstanceFile.TryRead(PointerFile));
        Assert.False(await InstanceForwarding.TryForwardAsync(
            Startup("a.txt"),
            PointerFile,
            _ => true,
            (pipe, files) => InstanceClient.TryForwardAsync(pipe, files)));
    }

    public void Dispose() => _folder.Dispose();

    private StartupFiles Startup(params string[] args)
        => StartupFiles.Parse(args, _folder.Root, File.Exists, Directory.Exists);

    private Task<bool> TryForwardAsync(StartupFiles startup, bool isRunning = true, bool accepted = true)
        => InstanceForwarding.TryForwardAsync(
            startup,
            PointerFile,
            _ => isRunning,
            (pipe, files) =>
            {
                _sent.Add((pipe, files));

                return Task.FromResult(accepted);
            });
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Instances;
using Xunit;

namespace Enigma.TextEditor.Core.UnitTests.Instances;

/// <summary>
/// The request a launch writes and the reply it reads, over a stream — and the requests that are refused.
/// </summary>
public sealed class InstanceProtocolTests
{
    [Fact]
    public async Task ARequest_RoundTripsEveryPathAsItLeft()
    {
        string[] files =
        [
            "/home/me/notes.txt",
            @"C:\Users\me\My Documents\a b.txt",
            "/tmp/çà et là — ünïcode 文件.md",
            "/tmp/a line\nbreak.txt",
            "/" + new string('x', 4000) + ".log",
        ];
        using MemoryStream stream = new();

        await InstanceProtocol.WriteRequestAsync(stream, files, TestContext.Current.CancellationToken);
        stream.Position = 0;
        IReadOnlyList<string>? read = await InstanceProtocol.ReadRequestAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal(files, read);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AReply_RoundTrips(bool accepted)
    {
        using MemoryStream stream = new();

        await InstanceProtocol.WriteReplyAsync(stream, accepted, TestContext.Current.CancellationToken);
        stream.Position = 0;

        Assert.Equal(accepted, await InstanceProtocol.ReadReplyAsync(stream, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoReplyAtAll_IsARefusal()
    {
        using MemoryStream stream = new();

        Assert.False(await InstanceProtocol.ReadReplyAsync(stream, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{\"version\":2,\"files\":[\"/a.txt\"]}")]
    [InlineData("{\"version\":1}")]
    [InlineData("{\"version\":1,\"files\":[null]}")]
    [InlineData("{\"version\":1,\"files\":\"/a.txt\"}")]
    [InlineData("not json at all")]
    [InlineData("[1,2,3]")]
    public async Task ARequestThisBuildDoesNotTake_IsRefused(string json)
    {
        using MemoryStream stream = Framed(Encoding.UTF8.GetBytes(json));

        Assert.Null(await InstanceProtocol.ReadRequestAsync(stream, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(InstanceProtocol.MaxRequestBytes + 1)]
    [InlineData(int.MaxValue)]
    public async Task ALengthOutOfBounds_IsRefused_WithoutReadingFurther(int length)
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, length);
        using MemoryStream stream = new(header);

        Assert.Null(await InstanceProtocol.ReadRequestAsync(stream, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ARequestCutShort_IsRefused()
    {
        byte[] whole = Framed(Encoding.UTF8.GetBytes("{\"version\":1,\"files\":[\"/a.txt\"]}")).ToArray();
        using MemoryStream truncated = new(whole[..^5]);
        using MemoryStream headerOnly = new(whole[..2]);

        Assert.Null(await InstanceProtocol.ReadRequestAsync(truncated, TestContext.Current.CancellationToken));
        Assert.Null(await InstanceProtocol.ReadRequestAsync(headerOnly, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ARequestTooLongToSend_IsNotWritten()
    {
        string[] files = [.. Enumerable.Range(0, 300).Select(i => $"/{i}/" + new string('x', 4000))];
        using MemoryStream stream = new();

        await Assert.ThrowsAsync<ArgumentException>(() => InstanceProtocol.WriteRequestAsync(stream, files, TestContext.Current.CancellationToken));
        Assert.Equal(0, stream.Length);
    }

    private static MemoryStream Framed(byte[] body)
    {
        MemoryStream stream = new();
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, body.Length);
        stream.Write(header);
        stream.Write(body);
        stream.Position = 0;

        return stream;
    }
}

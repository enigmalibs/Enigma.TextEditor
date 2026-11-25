using System;
using System.Text;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// What the hex tab hands the clipboard: the selected bytes in either column's form, read off the UI thread, up to the
/// limit — and a reason for the status line when there is nothing to hand over.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HexCopyTests(HeadlessAvaloniaFixture fixture)
{
    private static readonly byte[] Content = [.. Encoding.ASCII.GetBytes("Hello, hex!"), 0x00, 0x0A, 0xFF];

    [Fact]
    public void TheSelection_IsCopiedAsHexPairs_OrAsCharacters()
        => fixture.RunAsync(async () =>
        {
            EditorTabViewModel tab = Tab(new MemoryBytes(Content));

            tab.HexSelection = new HexSelection(5, 13, HexColumn.Hex);
            Assert.Equal(HexCopyResult.Copied("2C 20 68 65 78 21 00 0A FF"), await tab.CopyHexSelectionAsync(HexColumn.Hex));
            Assert.Equal(HexCopyResult.Copied(", hex!..."), await tab.CopyHexSelectionAsync(HexColumn.Text));
            Assert.Equal(HexCopyResult.Copied("0x00000005 - 0x0000000D"), tab.CopyHexRange());
        });

    [Fact]
    public void TheBytes_AreReadAwayFromTheUiThread()
        => fixture.RunAsync(async () =>
        {
            MemoryBytes bytes = new(Content);
            EditorTabViewModel tab = Tab(bytes);
            tab.HexSelection = new HexSelection(0, 4, HexColumn.Text);

            Assert.Equal(HexCopyResult.Copied("Hello"), await tab.CopyHexSelectionAsync(HexColumn.Text));

            Assert.NotNull(bytes.ReadOnThread);
            Assert.NotEqual(Environment.CurrentManagedThreadId, bytes.ReadOnThread);
        });

    [Fact]
    public void ASelectionOverTheLimit_IsRefused_WithoutReadingIt()
        => fixture.RunAsync(async () =>
        {
            MemoryBytes bytes = new(new byte[HexDocument.MaxCopyBytes + 16]);
            EditorTabViewModel tab = Tab(bytes);
            tab.HexSelection = new HexSelection(0, HexDocument.MaxCopyBytes, HexColumn.Hex);

            HexCopyResult result = await tab.CopyHexSelectionAsync(HexColumn.Text);

            Assert.Null(result.Text);
            Assert.Equal("The selection is too large to copy as text: the limit is 1 MB", result.Message);
            Assert.Null(bytes.ReadOnThread);
        });

    [Fact]
    public void BytesThatCannotBeRead_AreAMessage_NotAFailure()
        => fixture.RunAsync(async () =>
        {
            EditorTabViewModel tab = Tab(new MemoryBytes(Content) { Fails = true });
            tab.HexSelection = new HexSelection(0, 4, HexColumn.Hex);

            HexCopyResult result = await tab.CopyHexSelectionAsync(HexColumn.Hex);

            Assert.Null(result.Text);
            Assert.Equal("Could not read the selection: The device is not ready.", result.Message);
        });

    [Fact]
    public void ADocumentClosedMeanwhile_IsAMessage_NotAFailure()
        => fixture.RunAsync(async () =>
        {
            EditorTabViewModel tab = Tab(new MemoryBytes(Content));
            tab.HexSelection = new HexSelection(0, 4, HexColumn.Hex);
            tab.Document.Hex!.Dispose();

            HexCopyResult result = await tab.CopyHexSelectionAsync(HexColumn.Hex);

            Assert.Null(result.Text);
            Assert.StartsWith("Could not read the selection: ", result.Message, StringComparison.Ordinal);
        });

    [Fact]
    public void NoSelection_OrATextTab_CopiesNothing_AndSaysNothing()
        => fixture.RunAsync(async () =>
        {
            EditorTabViewModel hex = Tab(new MemoryBytes(Content));
            EditorTabViewModel text = new(new DocumentViewModel(null, "Untitled-1", new DocumentContent("abc", TextFileFormat.Default), Indentation.Default))
            {
                HexSelection = new HexSelection(0, 1, HexColumn.Hex),
            };

            Assert.Same(HexCopyResult.Nothing, await hex.CopyHexSelectionAsync(HexColumn.Hex));
            Assert.Same(HexCopyResult.Nothing, hex.CopyHexRange());
            Assert.Same(HexCopyResult.Nothing, await text.CopyHexSelectionAsync(HexColumn.Hex));
        });

    private static EditorTabViewModel Tab(IByteSource bytes)
        => new(new DocumentViewModel(
            "/data.bin",
            "Untitled-1",
            new DocumentContent(string.Empty, TextFileFormat.Default),
            Indentation.Default,
            new HexDocument(bytes)));

    // Bytes in memory that remember the thread they were read on, and can fail as a disk would.
    private sealed class MemoryBytes(byte[] bytes) : IByteSource
    {
        private bool _disposed;

        public bool Fails { get; init; }

        public int? ReadOnThread { get; private set; }

        public long Length => bytes.Length;

        public int Read(long offset, Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ReadOnThread = Environment.CurrentManagedThreadId;

            if (Fails)
            {
                throw new System.IO.IOException("The device is not ready.");
            }

            int count = (int)Math.Min(buffer.Length, bytes.Length - offset);
            bytes.AsSpan((int)offset, count).CopyTo(buffer);

            return count;
        }

        public void Dispose() => _disposed = true;
    }
}

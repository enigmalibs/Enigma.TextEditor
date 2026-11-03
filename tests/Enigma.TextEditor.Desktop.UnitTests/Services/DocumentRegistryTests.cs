using System;
using System.IO;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.Services;

public sealed class DocumentRegistryTests
{
    [Fact]
    public void ABufferIsFoundByItsFile_HoweverThePathIsSpelled()
    {
        DocumentRegistry registry = new();
        string path = Path.Combine(Path.GetTempPath(), "found.txt");
        DocumentViewModel document = registry.Add(Buffer(path));

        Assert.Same(document, registry.Find(path));
        Assert.Same(document, registry.Find(Path.Combine(Path.GetTempPath(), ".", "found.txt")));
        Assert.Null(registry.Find(Path.Combine(Path.GetTempPath(), "other.txt")));
    }

    [Fact]
    public void AFileCannotBeOpenInTwoBuffers()
    {
        DocumentRegistry registry = new();
        string path = Path.Combine(Path.GetTempPath(), "twice.txt");
        registry.Add(Buffer(path));

        Assert.Throws<InvalidOperationException>(() => registry.Add(Buffer(path)));
    }

    [Fact]
    public void UntitledBuffers_TakeTheLowestFreeNumber()
    {
        DocumentRegistry registry = new();

        DocumentViewModel first = registry.CreateUntitled(TextFileFormat.Default, Indentation.Default);
        DocumentViewModel second = registry.CreateUntitled(TextFileFormat.Default, Indentation.Default);
        registry.Retain(first);
        registry.Release(first);
        DocumentViewModel third = registry.CreateUntitled(TextFileFormat.Default, Indentation.Default);

        Assert.Equal("Untitled-1", first.Title);
        Assert.Equal("Untitled-2", second.Title);
        Assert.Equal("Untitled-1", third.Title);
    }

    [Fact]
    public void ABuffer_IsDroppedWithItsLastTab()
    {
        DocumentRegistry registry = new();
        DocumentViewModel document = registry.Add(Buffer(Path.Combine(Path.GetTempPath(), "shared.txt")));
        registry.Retain(document);
        registry.Retain(document);

        Assert.Equal(1, registry.Release(document));
        Assert.Contains(document, registry.Documents);

        Assert.Equal(0, registry.Release(document));
        Assert.DoesNotContain(document, registry.Documents);
    }

    [Fact]
    public void Relocating_AFolder_MovesTheBuffersUnderIt_AndNotThoseOfAFolderSharingItsPrefix()
    {
        DocumentRegistry registry = new();
        string root = Path.Combine(Path.GetTempPath(), "relocate");
        DocumentViewModel under = registry.Add(Buffer(Path.Combine(root, "docs", "a", "intro.txt")));
        DocumentViewModel sibling = registry.Add(Buffer(Path.Combine(root, "docs2", "other.txt")));
        DocumentViewModel untitled = registry.CreateUntitled(TextFileFormat.Default, Indentation.Default);

        int moved = registry.Relocate(Path.Combine(root, "docs"), Path.Combine(root, "guides"));

        Assert.Equal(1, moved);
        Assert.Equal(Path.Combine(root, "guides", "a", "intro.txt"), under.FilePath);
        Assert.Equal(Path.Combine(root, "docs2", "other.txt"), sibling.FilePath);
        Assert.True(untitled.IsUntitled);
        Assert.Same(under, registry.Find(Path.Combine(root, "guides", "a", "intro.txt")));
    }

    [Fact]
    public void Relocating_AFile_MovesItsBuffer_AndOnlyIt()
    {
        DocumentRegistry registry = new();
        string root = Path.Combine(Path.GetTempPath(), "relocate");
        DocumentViewModel file = registry.Add(Buffer(Path.Combine(root, "notes.txt")));
        DocumentViewModel longer = registry.Add(Buffer(Path.Combine(root, "notes.txt.bak")));

        registry.Relocate(Path.Combine(root, "notes.txt"), Path.Combine(root, "todo.txt"));

        Assert.Equal(Path.Combine(root, "todo.txt"), file.FilePath);
        Assert.Equal("todo.txt", file.Title);
        Assert.Equal(Path.Combine(root, "notes.txt.bak"), longer.FilePath);
        Assert.Equal([file], registry.FindUnder(Path.Combine(root, "todo.txt")));
    }

    private static DocumentViewModel Buffer(string path)
        => new(path, "Untitled-1", new DocumentContent(string.Empty, TextFileFormat.Default), Indentation.Default);
}

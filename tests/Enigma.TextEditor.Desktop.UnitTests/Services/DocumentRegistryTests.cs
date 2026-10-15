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

    private static DocumentViewModel Buffer(string path)
        => new(path, "Untitled-1", new DocumentContent(string.Empty, TextFileFormat.Default), Indentation.Default);
}

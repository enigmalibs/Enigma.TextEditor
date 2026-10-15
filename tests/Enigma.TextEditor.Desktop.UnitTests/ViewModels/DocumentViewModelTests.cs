using System.IO;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

public sealed class DocumentViewModelTests
{
    [Fact]
    public void AFreshBuffer_IsNotDirty()
        => Assert.False(Create("text").IsDirty);

    [Fact]
    public void AnEdit_MakesItDirtyAndUndoingTheEdit_MakesItCleanAgain()
    {
        DocumentViewModel document = Create("text");

        document.Document.Insert(0, "more ");
        Assert.True(document.IsDirty);

        document.Document.UndoStack.Undo();
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void Saving_TheVersionOnScreen_MakesItClean()
    {
        DocumentViewModel document = Create("text");
        document.Document.Insert(0, "x");

        document.MarkSaved(Path.Combine(Path.GetTempPath(), "saved.txt"), document.Document.Version);

        Assert.False(document.IsDirty);
        Assert.Equal("saved.txt", document.Title);
    }

    [Fact]
    public void AnEditMadeWhileTheSaveWasInFlight_KeepsItDirty()
    {
        DocumentViewModel document = Create("text");
        document.Document.Insert(0, "x");
        AvaloniaEdit.Document.ITextSourceVersion written = document.Document.Version;
        document.Document.Insert(0, "y");

        document.MarkSaved(Path.Combine(Path.GetTempPath(), "saved.txt"), written);

        Assert.True(document.IsDirty);
    }

    [Fact]
    public void AnUntitledBuffer_GoesByItsUntitledNameUntilSaved()
    {
        DocumentViewModel document = new(null, "Untitled-3", new DocumentContent(string.Empty, TextFileFormat.Default), Indentation.Default);

        Assert.True(document.IsUntitled);
        Assert.Equal("Untitled-3", document.Title);
        Assert.Equal("Untitled-3", document.ToolTip);

        string path = Path.Combine(Path.GetTempPath(), "named.txt");
        document.MarkSaved(path, document.Document.Version);

        Assert.False(document.IsUntitled);
        Assert.Equal("named.txt", document.Title);
        Assert.Equal(path, document.ToolTip);
    }

    [Fact]
    public void AReload_ReplacesTheTextWithoutMakingItDirtyOrUndoable()
    {
        DocumentViewModel document = Create("old");
        document.Document.Insert(0, "edited ");

        document.ApplyReload(new DocumentContent("new", TextFileFormat.Default.With(LineEndingStyle.CrLf)));

        Assert.Equal("new", document.Document.Text);
        Assert.False(document.IsDirty);
        Assert.False(document.Document.UndoStack.CanUndo);
        Assert.Equal(LineEndingStyle.CrLf, document.Format.LineEndings);
    }

    private static DocumentViewModel Create(string text)
        => new(
            Path.Combine(Path.GetTempPath(), "file.txt"),
            "Untitled-1",
            new DocumentContent(text, TextFileFormat.Default),
            Indentation.Default);
}

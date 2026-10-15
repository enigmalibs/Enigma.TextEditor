using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

public sealed class EditorPaneViewModelTests
{
    [Fact]
    public void ShowingABuffer_AddsATabAndActivatesIt()
    {
        EditorPaneViewModel pane = NewPane(out _);
        DocumentViewModel a = Buffer("a.txt");

        EditorTabViewModel tab = pane.Show(a, out bool created);

        Assert.True(created);
        Assert.Same(tab, pane.ActiveTab);
        Assert.Same(a, Assert.Single(pane.Tabs).Document);
    }

    [Fact]
    public void ShowingABufferAlreadyThere_ActivatesItsTabInsteadOfAddingOne()
    {
        EditorPaneViewModel pane = NewPane(out _);
        DocumentViewModel a = Buffer("a.txt");
        EditorTabViewModel first = pane.Show(a, out _);
        pane.Show(Buffer("b.txt"), out _);

        EditorTabViewModel again = pane.Show(a, out bool created);

        Assert.False(created);
        Assert.Same(first, again);
        Assert.Equal(2, pane.Tabs.Count);
        Assert.Same(first, pane.ActiveTab);
    }

    [Fact]
    public void RemovingTheActiveTab_ShowsItsRightNeighbourOrElseItsLeftOne()
    {
        EditorPaneViewModel pane = NewPane(out _);
        EditorTabViewModel a = pane.Show(Buffer("a.txt"), out _);
        EditorTabViewModel b = pane.Show(Buffer("b.txt"), out _);
        EditorTabViewModel c = pane.Show(Buffer("c.txt"), out _);

        pane.ActiveTab = b;
        pane.Remove(b);
        Assert.Same(c, pane.ActiveTab);

        pane.Remove(c);
        Assert.Same(a, pane.ActiveTab);

        pane.Remove(a);
        Assert.Null(pane.ActiveTab);
        Assert.False(pane.HasActiveTab);
    }

    [Fact]
    public void NextAndPrevious_WrapRoundTheStrip()
    {
        EditorPaneViewModel pane = NewPane(out _);
        EditorTabViewModel a = pane.Show(Buffer("a.txt"), out _);
        EditorTabViewModel b = pane.Show(Buffer("b.txt"), out _);

        pane.NextTab();
        Assert.Same(a, pane.ActiveTab);

        pane.PreviousTab();
        Assert.Same(b, pane.ActiveTab);
    }

    [Fact]
    public void TheCloseCommand_AsksTheWorkspace()
    {
        EditorPaneViewModel pane = NewPane(out List<EditorTabViewModel> closed);
        EditorTabViewModel tab = pane.Show(Buffer("a.txt"), out _);

        pane.CloseTabCommand.Execute(tab);

        Assert.Same(tab, Assert.Single(closed));
    }

    private static EditorPaneViewModel NewPane(out List<EditorTabViewModel> closed)
    {
        List<EditorTabViewModel> asked = [];
        closed = asked;

        using TempFolder folder = new();
        EditorPreferences preferences = new(SettingsLoader.Load(folder.PathOf(AppPaths.SettingsFileName)).Config);

        return new EditorPaneViewModel(preferences, new RecordingHost(asked));
    }

    private sealed class RecordingHost(List<EditorTabViewModel> closed) : IPaneHost
    {
        public IVimSession Vim => throw new System.NotSupportedException();

        public VimSharedState VimShared { get; } = new();

        public Task ExecuteWindowAsync(EditorPaneViewModel pane, VimWindowCommand command) => Task.CompletedTask;

        public Task<bool> CloseTabAsync(EditorPaneViewModel pane, EditorTabViewModel tab, bool discard = false)
        {
            closed.Add(tab);
            return Task.FromResult(true);
        }

        public Task ExecuteExAsync(EditorPaneViewModel pane, VimExRequest request) => Task.CompletedTask;

        public Task OpenDroppedAsync(EditorPaneViewModel pane, IReadOnlyList<string> paths) => Task.CompletedTask;

        public void ShowStatus(string message)
        {
        }
    }

    private static DocumentViewModel Buffer(string name)
        => new(Path.Combine(Path.GetTempPath(), name), "Untitled-1", new DocumentContent(name, TextFileFormat.Default), Indentation.Default);
}

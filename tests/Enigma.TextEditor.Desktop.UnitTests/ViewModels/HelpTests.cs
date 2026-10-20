using System;
using System.Collections.Generic;
using System.Linq;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.UnitTests.Infrastructure;
using Enigma.TextEditor.Desktop.ViewModels;
using Xunit;

namespace Enigma.TextEditor.Desktop.UnitTests.ViewModels;

/// <summary>
/// The key-binding help: what opens it, and what it says — held to the commands the engine has.
/// </summary>
[Collection(HeadlessCollection.Name)]
public sealed class HelpTests(HeadlessAvaloniaFixture fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData("vsplit")]
    public void TheHelpRequest_ShowsTheHelp_WhateverSubjectFollowsIt(string? subject)
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Workspace.ExecuteExAsync(harness.Workspace.ActivePane, new VimExRequest(VimExCommand.Help, subject));

            Assert.Equal(1, harness.Help.Shown);
        });

    [Fact]
    public void HelpKeyBindings_ShowsTheHelp()
        => fixture.RunAsync(async () =>
        {
            using EditorHarness harness = new();

            await harness.Window.HelpCommand.ExecuteAsync(null);

            Assert.Equal(1, harness.Help.Shown);
            Assert.Equal(0, harness.About.Shown);
        });

    [Fact]
    public void EveryRequestLeavingTheEngine_HasALine()
    {
        HashSet<VimExCommand> documented = [.. Rows().SelectMany(row => row.ExCommands)];

        Assert.Equal(Enum.GetValues<VimExCommand>().OrderBy(command => command), documented.OrderBy(command => command));
    }

    [Fact]
    public void EveryWindowCommand_HasALine()
    {
        HashSet<VimWindowCommand> documented = [.. Rows().SelectMany(row => row.WindowCommands)];

        Assert.Equal(Enum.GetValues<VimWindowCommand>().OrderBy(command => command), documented.OrderBy(command => command));
    }

    [Fact]
    public void EveryExSpellingTheHelpShows_IsReadAsTheCommandItDocuments()
    {
        foreach (HelpRow row in Rows().Where(row => row.ExCommands.Count > 0))
        {
            foreach (string spelling in row.ExCommand.Split(", "))
            {
                foreach (string line in Typed(spelling))
                {
                    Assert.True(
                        ExCommandParser.Parse(line) is { } request && row.ExCommands.Contains(request.Command),
                        $"'{spelling}' in \"{row.Title}\": ':{line}' does not read as {string.Join(", ", row.ExCommands)}.");
                }
            }
        }
    }

    [Theory]
    [InlineData(VimExCommand.VerticalSplit, ":vs[plit] [file]")]
    [InlineData(VimExCommand.WriteQuit, ":wq, :x")]
    [InlineData(VimExCommand.QuitDiscard, ":q!")]
    [InlineData(VimExCommand.QuitAllDiscard, ":qa[ll]!")]
    [InlineData(VimExCommand.Edit, ":e[dit] [file]")]
    [InlineData(VimExCommand.Help, ":h[elp]")]
    public void TheExColumn_IsWrittenFromTheParsersTable(VimExCommand command, string expected)
        => Assert.Equal(expected, Rows().Single(row => row.ExCommands.Contains(command)).ExCommand);

    [Fact]
    public void EverySection_HasLines_AndItsSubtitleCountsThem()
    {
        Assert.Equal(HelpCatalog.Sections.Count, HelpCatalog.Sections.Select(section => section.Category).Distinct().Count());

        foreach (HelpSection section in HelpCatalog.Sections)
        {
            Assert.NotEmpty(section.Rows);
            Assert.StartsWith($"{section.Rows.Count} command", section.Description, StringComparison.Ordinal);
            Assert.All(section.Rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Title)));
        }
    }

    [Fact]
    public void TheSectionsRead_InTheirOrder()
        => Assert.Equal(
            ["Moving", "Scrolling", "Inserting", "Editing", "Operators & text objects", "Visual mode", "Search & replace", "Registers & macros", "Files & tabs", "Panes", "Options", "Application"],
            HelpCatalog.Sections.Select(section => section.Category));

    [Fact]
    public void Close_RunsTheCloseItWasGiven()
    {
        int closed = 0;
        HelpViewModel help = new(() => closed++);

        help.CloseCommand.Execute(null);

        Assert.Equal(1, closed);
        Assert.Same(HelpCatalog.Sections, help.Sections);
    }

    private static IEnumerable<HelpRow> Rows() => HelpCatalog.Sections.SelectMany(section => section.Rows);

    /// <summary>
    /// The lines a vim spelling stands for: <c>:vs[plit] [file]</c> is <c>vs x.txt</c> and <c>vsplit x.txt</c>.
    /// </summary>
    private static IEnumerable<string> Typed(string spelling)
    {
        string[] parts = spelling.TrimStart(':').Split(' ', 2);
        string argument = parts.Length > 1 ? " x.txt" : string.Empty;
        string name = parts[0];
        int open = name.IndexOf('[', StringComparison.Ordinal);

        if (open < 0)
        {
            yield return name + argument;

            yield break;
        }

        int close = name.IndexOf(']', StringComparison.Ordinal);
        string tail = name[(close + 1)..];

        yield return name[..open] + tail + argument;
        yield return name[..open] + name[(open + 1)..close] + tail + argument;
    }
}

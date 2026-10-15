using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Enigma.Icons.Phosphor;
using Enigma.TextEditor.Core.Vim;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One line of the key-binding help.
/// </summary>
/// <param name="Keys">The keys, as people read them: <c>Ctrl+W v</c>; empty for an ex command alone.</param>
/// <param name="Title">What they do.</param>
/// <param name="ExCommand">The ex command that also does it, vim-style: <c>:vs[plit] [file]</c>; empty when there is none.</param>
public sealed record HelpRow(string Keys, string Title, string ExCommand)
{
    /// <summary>
    /// Gets the requests leaving the Vim engine that this line documents.
    /// </summary>
    internal IReadOnlyList<VimExCommand> ExCommands { get; init; } = [];

    /// <summary>
    /// Gets the window commands this line documents.
    /// </summary>
    internal IReadOnlyList<VimWindowCommand> WindowCommands { get; init; } = [];
}

/// <summary>
/// One section of the key-binding help, shown as a card of its own.
/// </summary>
/// <param name="Category">The section's title.</param>
/// <param name="Rows">Its lines.</param>
/// <param name="Icon">The card's icon.</param>
/// <param name="Description">The card's subtitle: how many lines, and anything worth knowing about them.</param>
public sealed record HelpSection(string Category, IReadOnlyList<HelpRow> Rows, PhosphorIcon Icon, string Description);

/// <summary>
/// The key-binding help: the sections of <see cref="HelpCatalog"/>, and the way to close it.
/// </summary>
public sealed class HelpViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpViewModel"/> class.
    /// </summary>
    /// <param name="close">Closes the help (<c>q</c>).</param>
    public HelpViewModel(Action close)
    {
        ArgumentNullException.ThrowIfNull(close);

        CloseCommand = new RelayCommand(close);
    }

    /// <summary>
    /// Gets the sections, in reading order.
    /// </summary>
    public IReadOnlyList<HelpSection> Sections => HelpCatalog.Sections;

    /// <summary>
    /// Gets the command that closes the help.
    /// </summary>
    public RelayCommand CloseCommand { get; }
}

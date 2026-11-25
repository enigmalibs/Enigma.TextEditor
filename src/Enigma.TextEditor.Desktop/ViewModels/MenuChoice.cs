using System;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One value of a radio group in a menu or a flyout: what it says, whether it is the value in force, and the command
/// that puts it in force.
/// </summary>
/// <remarks>
/// <para>
/// A group is a list of these, built once and kept: a menu and a flyout bind their items to it through one item
/// template, so offering one more value is one more entry in the list. A flyout's items follow the collection it first
/// showed, which is why the list never changes — only <see cref="IsCurrent"/> does, through <see cref="Refresh"/>.
/// </para>
/// <para>
/// The command decides what choosing does; the check only shows what is in force, so it is read again from the state
/// rather than flipped by the click.
/// </para>
/// </remarks>
public sealed class MenuChoice : ObservableObject
{
    private readonly Func<bool> _isCurrent;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuChoice"/> class.
    /// </summary>
    /// <param name="header">What the item says, with an underscore before its access key.</param>
    /// <param name="group">The radio group's name, shared by every choice of the group.</param>
    /// <param name="isCurrent">Whether this value is the one in force.</param>
    /// <param name="command">Puts this value in force.</param>
    public MenuChoice(string header, string group, Func<bool> isCurrent, ICommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        ArgumentException.ThrowIfNullOrWhiteSpace(group);

        Header = header;
        Group = group;
        _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        IsCurrent = _isCurrent();
    }

    /// <summary>
    /// Gets what the item says, with an underscore before its access key.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets the radio group's name.
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// Gets the command that puts this value in force.
    /// </summary>
    public ICommand Command { get; }

    /// <summary>
    /// Gets a value indicating whether this value is the one in force: the item's radio mark.
    /// </summary>
    public bool IsCurrent { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Reads every choice's <see cref="IsCurrent"/> again from the state, after it changed.
    /// </summary>
    /// <param name="choices">The group.</param>
    public static void Refresh(IEnumerable<MenuChoice> choices)
    {
        ArgumentNullException.ThrowIfNull(choices);

        foreach (MenuChoice choice in choices)
        {
            choice.IsCurrent = choice._isCurrent();
        }
    }
}

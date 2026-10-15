using System;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// One ex command that leaves the engine: how it may be spelled, and what it takes.
/// </summary>
/// <param name="Name">Its full name, as vim spells it: <c>vsplit</c>.</param>
/// <param name="MinimumLength">
/// The shortest start of the name that still names it, as vim reads names: 2 for <c>vsplit</c>, so
/// <c>vs</c>, <c>vsp</c>, <c>vspl</c>, <c>vspli</c> and <c>vsplit</c> are all of it. The name's own
/// length for a command with no shorter spelling.
/// </param>
/// <param name="Command">The request it raises.</param>
/// <param name="Argument">What may follow its name.</param>
/// <param name="BangCommand">
/// The request it raises with a <c>!</c> after its name, or <see langword="null"/> when a <c>!</c>
/// makes it no command at all.
/// </param>
public sealed record VimExCommandDefinition(
    string Name,
    int MinimumLength,
    VimExCommand Command,
    VimExArgument Argument = VimExArgument.None,
    VimExCommand? BangCommand = null)
{
    /// <summary>
    /// Whether a typed name is this command: a start of its name at least <see cref="MinimumLength"/>
    /// long.
    /// </summary>
    /// <param name="name">The name as typed, without its <c>!</c>.</param>
    /// <returns><see langword="true"/> when it names this command.</returns>
    public bool Matches(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return (name.Length >= MinimumLength) && Name.StartsWith(name, StringComparison.Ordinal);
    }

    /// <summary>
    /// How vim's documentation writes the command: the part that may be left out in brackets.
    /// </summary>
    /// <param name="bang">Whether to write it with its <c>!</c>.</param>
    /// <returns><c>:vs[plit]</c>, <c>:w</c>, <c>:qa[ll]!</c>.</returns>
    public string Spelling(bool bang = false)
    {
        string name = MinimumLength < Name.Length ? $"{Name[..MinimumLength]}[{Name[MinimumLength..]}]" : Name;

        return $":{name}{(bang ? "!" : string.Empty)}";
    }
}

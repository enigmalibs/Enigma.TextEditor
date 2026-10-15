namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What may follow an ex command's name.
/// </summary>
public enum VimExArgument
{
    /// <summary>
    /// Nothing: <c>:q notes.md</c> is not a command, because a file there would mean something
    /// <c>:q</c> does not do.
    /// </summary>
    None,

    /// <summary>
    /// A file, or nothing: <c>:w</c> and <c>:w notes.md</c>.
    /// </summary>
    Optional,

    /// <summary>
    /// A file: <c>:e</c> on its own reloads the buffer in vim, which this editor does not do.
    /// </summary>
    Required,
}

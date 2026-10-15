namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// An ex command the engine has recognised and is asking the application to carry out, with what was
/// typed after its name.
/// </summary>
/// <param name="Command">The command.</param>
/// <param name="Argument">
/// The path <c>:e</c> and <c>:w</c> were given, or <see langword="null"/> for a command typed bare.
/// </param>
public readonly record struct VimExRequest(VimExCommand Command, string? Argument = null);

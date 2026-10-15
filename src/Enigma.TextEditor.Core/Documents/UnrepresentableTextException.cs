using System;

namespace Enigma.TextEditor.Core.Documents;

/// <summary>
/// Text cannot be written in the encoding asked for, because one of its characters does not exist in
/// that encoding.
/// </summary>
/// <remarks>
/// Thrown before a single byte is written: the alternative — the encoder's default of writing a
/// <c>?</c> in its place — loses the character silently, and the user finds out the next time they
/// open the file.
/// </remarks>
public sealed class UnrepresentableTextException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnrepresentableTextException"/> class.
    /// </summary>
    /// <param name="character">The first character that cannot be written, as text (one or two UTF-16 units).</param>
    /// <param name="line">Its line, counted from one.</param>
    /// <param name="column">Its column, counted from one.</param>
    /// <param name="encodingLabel">The encoding it cannot be written in.</param>
    public UnrepresentableTextException(string character, int line, int column, string encodingLabel)
        : base($"'{character}' on line {line}, column {column} cannot be saved in {encodingLabel}. Choose an encoding that can hold it, such as UTF-8.")
    {
        Character = character ?? throw new ArgumentNullException(nameof(character));
        Line = line;
        Column = column;
        EncodingLabel = encodingLabel ?? throw new ArgumentNullException(nameof(encodingLabel));
    }

    /// <summary>
    /// Gets the first character that cannot be written.
    /// </summary>
    public string Character { get; }

    /// <summary>
    /// Gets its line, counted from one.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// Gets its column, counted from one.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Gets the label of the encoding it cannot be written in.
    /// </summary>
    public string EncodingLabel { get; }
}

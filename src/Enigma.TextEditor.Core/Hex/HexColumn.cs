namespace Enigma.TextEditor.Core.Hex;

/// <summary>
/// The two columns of a hex row that bytes can be selected in.
/// </summary>
public enum HexColumn
{
    /// <summary>
    /// The bytes as pairs of hex digits.
    /// </summary>
    Hex,

    /// <summary>
    /// The bytes as text, a dot for each one that is not printable ASCII.
    /// </summary>
    Text,
}

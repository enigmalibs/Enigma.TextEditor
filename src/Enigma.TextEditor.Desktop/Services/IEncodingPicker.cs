using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks the user to pick one of the editor's encodings.
/// </summary>
public interface IEncodingPicker
{
    /// <summary>
    /// Shows the picker.
    /// </summary>
    /// <param name="title">What the pick is for — <c>Reopen with encoding</c>, <c>Change file encoding</c>.</param>
    /// <param name="current">The buffer's encoding, selected to begin with.</param>
    /// <returns>The encoding picked, or <see langword="null"/> when the user cancelled.</returns>
    Task<TextEncoding?> PickAsync(string title, TextEncoding current);
}

using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks the user which language to colour a buffer as.
/// </summary>
public interface ILanguagePicker
{
    /// <summary>
    /// Shows the picker.
    /// </summary>
    /// <param name="current">The buffer's language, selected to begin with.</param>
    /// <returns>The language picked, or <see langword="null"/> when the user cancelled.</returns>
    Task<LanguageInfo?> PickAsync(LanguageInfo current);
}

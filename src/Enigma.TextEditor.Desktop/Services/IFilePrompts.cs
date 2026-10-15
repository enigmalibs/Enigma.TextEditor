using System.Collections.Generic;
using System.Threading.Tasks;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Asks the user which files to open and where to save one.
/// </summary>
public interface IFilePrompts
{
    /// <summary>
    /// Asks for files to open.
    /// </summary>
    /// <param name="startDirectory">Where the picker starts, or <see langword="null"/> for the platform's choice.</param>
    /// <returns>The chosen files; empty when the user cancelled.</returns>
    Task<IReadOnlyList<string>> AskFilesToOpenAsync(string? startDirectory);

    /// <summary>
    /// Asks where to save a file.
    /// </summary>
    /// <param name="suggestedName">The file name to suggest.</param>
    /// <param name="startDirectory">Where the picker starts, or <see langword="null"/> for the platform's choice.</param>
    /// <returns>The chosen path, or <see langword="null"/> when the user cancelled.</returns>
    Task<string?> AskWhereToSaveAsync(string suggestedName, string? startDirectory);
}

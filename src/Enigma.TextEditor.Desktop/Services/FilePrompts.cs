using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The platform's open and save pickers.
/// </summary>
public sealed class FilePrompts : IFilePrompts
{
    private readonly IFileDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePrompts"/> class.
    /// </summary>
    /// <param name="dialogs">The pickers.</param>
    public FilePrompts(IFileDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> AskFilesToOpenAsync(string? startDirectory)
    {
        IEnumerable<string> chosen = await _dialogs.ShowOpenFileDialogAsync(
            title: "Open",
            allowMultiple: true,
            suggestedStartLocation: startDirectory);

        return [.. chosen];
    }

    /// <inheritdoc />
    public Task<string?> AskWhereToSaveAsync(string suggestedName, string? startDirectory)
        => _dialogs.ShowSaveFileDialogAsync(
            title: "Save as",
            suggestedStartLocation: startDirectory,
            suggestedFileName: suggestedName,
            showOverwritePrompt: true);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The platform's open, folder and save pickers.
/// </summary>
public sealed class FilePrompts : IFilePrompts
{
    private readonly IFileDialogService _dialogs;
    private readonly IFolderDialogService _folders;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePrompts"/> class.
    /// </summary>
    /// <param name="dialogs">The file pickers.</param>
    /// <param name="folders">The folder picker.</param>
    public FilePrompts(IFileDialogService dialogs, IFolderDialogService folders)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _folders = folders ?? throw new ArgumentNullException(nameof(folders));
    }

    /// <inheritdoc />
    public async Task<string?> AskFolderToOpenAsync(string? startDirectory)
    {
        IEnumerable<string> chosen = await _folders.ShowOpenFolderDialogAsync(
            title: "Open Folder",
            allowMultiple: false,
            suggestedStartLocation: startDirectory);

        return chosen.FirstOrDefault();
    }

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

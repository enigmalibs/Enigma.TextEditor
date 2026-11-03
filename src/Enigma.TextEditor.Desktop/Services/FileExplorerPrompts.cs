using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;
using Enigma.Avalonia.Desktop.Services;
using Enigma.TextEditor.Core.Files;
using Enigma.TextEditor.Desktop.ViewModels;
using Enigma.TextEditor.Desktop.Views;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The explorer's questions, in the window's content dialog. Ported from Enigma.MarkdownEditor's
/// <c>FileOperationPrompts</c>.
/// </summary>
public sealed class FileExplorerPrompts : IFileExplorerPrompts
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileExplorerPrompts"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public FileExplorerPrompts(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public Task<string?> AskNewFileNameAsync(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        return AskNameAsync("New file", "Create", new FileNamePromptViewModel(directory, isDirectory: false, string.Empty));
    }

    /// <inheritdoc />
    public Task<string?> AskNewFolderNameAsync(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        return AskNameAsync("New folder", "Create", new FileNamePromptViewModel(directory, isDirectory: true, string.Empty));
    }

    /// <inheritdoc />
    public Task<string?> AskNewNameAsync(string path, bool isFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string full = Path.TrimEndingDirectorySeparator(path);
        string name = FilePaths.NameOf(full);
        string parent = Path.GetDirectoryName(full) ?? full;

        return AskNameAsync(
            isFolder ? "Rename folder" : "Rename file",
            "Rename",
            new FileNamePromptViewModel(parent, isFolder, name, currentName: name));
    }

    /// <inheritdoc />
    public Task<bool> ConfirmMoveToTrashAsync(string name, bool isFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ConfirmAsync(
            $"Delete '{name}'?",
            $"The {What(isFolder)} will be moved to the {TrashName}, where it can be restored from.",
            "Delete");
    }

    /// <inheritdoc />
    public Task<bool> ConfirmPermanentDeleteAsync(string name, bool isFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ConfirmAsync(
            $"Delete '{name}' permanently?",
            $"It cannot be moved to the {TrashName}, so the {What(isFolder)} would be deleted outright. This cannot be undone.",
            "Delete permanently");
    }

    // What the platform calls the place a deleted entry can be fetched back from: the name the user knows it by.
    private static string TrashName => OperatingSystem.IsWindows() ? "Recycle Bin" : "trash";

    private static string What(bool isFolder) => isFolder ? "folder and everything in it" : "file";

    // A question about removing something from disk: Cancel is the default button, and only a press of the confirming
    // one counts — Escape and a click outside answer None.
    private async Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        DialogResult result = await _dialogs.ShowAsync(dialog =>
        {
            dialog.Title = title;
            dialog.Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
            dialog.PrimaryButtonText = confirmText;
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = DefaultButton.Close;
        });

        return result == DialogResult.Primary;
    }

    private async Task<string?> AskNameAsync(string title, string confirmText, FileNamePromptViewModel prompt)
    {
        FileNamePromptView view = new() { DataContext = prompt };

        // The dialog's buttons belong to the content dialog, so Enter in the box cannot press one. It hides the dialog
        // instead — which answers None — and this flag tells that apart from an Escape.
        bool committedFromKeyboard = false;

        void OnCommitted(object? sender, EventArgs e)
        {
            committedFromKeyboard = true;
            _ = _dialogs.HideAsync();
        }

        prompt.Committed += OnCommitted;

        try
        {
            DialogResult result = await _dialogs.ShowAsync(dialog =>
            {
                dialog.Title = title;
                dialog.Content = view;
                dialog.PrimaryButtonText = confirmText;
                dialog.CloseButtonText = "Cancel";
                dialog.DefaultButton = DefaultButton.Primary;

                // Set here and then followed for every keystroke, so a dialog that opens on an empty box opens with its
                // confirming button already off.
                dialog.IsPrimaryButtonEnabled = prompt.IsValid;
                prompt.PropertyChanged += (_, changed) =>
                {
                    if (changed.PropertyName == nameof(FileNamePromptViewModel.IsValid))
                    {
                        dialog.IsPrimaryButtonEnabled = prompt.IsValid;
                    }
                };
            });

            bool confirmed = (result == DialogResult.Primary) || ((result == DialogResult.None) && committedFromKeyboard);

            // Checked again rather than trusted: the button's state is the view's, and this answer is what a file gets
            // created with.
            return confirmed && prompt.IsValid ? prompt.TrimmedName : null;
        }
        finally
        {
            prompt.Committed -= OnCommitted;
        }
    }
}

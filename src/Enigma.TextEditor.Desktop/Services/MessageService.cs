using System;
using System.Threading.Tasks;
using Enigma.Avalonia.Desktop.Services;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Shows messages in the window's content dialog.
/// </summary>
public sealed class MessageService : IMessageService
{
    private readonly IContentDialogService _dialogs;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageService"/> class.
    /// </summary>
    /// <param name="dialogs">The dialog host.</param>
    public MessageService(IContentDialogService dialogs)
        => _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    /// <inheritdoc />
    public Task ShowErrorAsync(string title, string message) => _dialogs.ShowMessageAsync(title, message);
}

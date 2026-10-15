using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What a pane asks of the editor area it lives in: the things that are about more than one pane.
/// </summary>
public interface IPaneHost
{
    /// <summary>
    /// Gets the Vim session the pane's engine reports its mode and messages to.
    /// </summary>
    IVimSession Vim { get; }

    /// <summary>
    /// Gets what every pane's Vim engine shares: registers, last search, last find.
    /// </summary>
    VimSharedState VimShared { get; }

    /// <summary>
    /// Closes a tab of a pane, asking about unsaved changes when it is the buffer's last tab.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="tab">The tab.</param>
    /// <param name="discard">Whether to throw unsaved changes away without asking.</param>
    /// <returns>Whether the tab was closed.</returns>
    Task<bool> CloseTabAsync(EditorPaneViewModel pane, EditorTabViewModel tab, bool discard = false);

    /// <summary>
    /// Carries out an ex command the pane's Vim engine recognised.
    /// </summary>
    /// <param name="pane">The pane it was typed in.</param>
    /// <param name="request">The command.</param>
    /// <returns>A task that completes when the command has run, dialogs included.</returns>
    Task ExecuteExAsync(EditorPaneViewModel pane, VimExRequest request);

    /// <summary>
    /// Carries out a window command the pane's Vim engine recognised.
    /// </summary>
    /// <param name="pane">The pane it was typed in.</param>
    /// <param name="command">The command.</param>
    /// <returns>A task that completes when the command has run, dialogs included.</returns>
    Task ExecuteWindowAsync(EditorPaneViewModel pane, VimWindowCommand command);

    /// <summary>
    /// Opens files dropped on a pane, in that pane.
    /// </summary>
    /// <param name="pane">The pane they were dropped on.</param>
    /// <param name="paths">Their full paths, in drop order.</param>
    /// <returns>A task that completes when every file has been opened or skipped, dialogs included.</returns>
    Task OpenDroppedAsync(EditorPaneViewModel pane, IReadOnlyList<string> paths);

    /// <summary>
    /// Shows a message on the status bar's message line.
    /// </summary>
    /// <param name="message">The message.</param>
    void ShowStatus(string message);
}

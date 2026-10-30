using System.Collections.Generic;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
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
    /// Whether a tab could be moved to a pane: it is on another pane of this editor area.
    /// </summary>
    /// <param name="target">The pane it would move to.</param>
    /// <param name="tab">The tab.</param>
    /// <returns>Whether <see cref="MoveTabTo"/> would move it.</returns>
    bool CanMoveTabTo(EditorPaneViewModel target, EditorTabViewModel tab);

    /// <summary>
    /// Moves a tab from whichever pane has it to another, which takes the keyboard.
    /// </summary>
    /// <param name="target">The pane to move it to.</param>
    /// <param name="tab">The tab.</param>
    /// <returns>Whether it moved.</returns>
    bool MoveTabTo(EditorPaneViewModel target, EditorTabViewModel tab);

    /// <summary>
    /// Whether a pane could be split to give a tab a pane of its own beside it: the tab is on a pane of this editor
    /// area, and that pane keeps a tab when the tab leaves it.
    /// </summary>
    /// <param name="target">The pane that would be split.</param>
    /// <param name="tab">The tab.</param>
    /// <returns>Whether <see cref="SplitWithTab"/> would split it.</returns>
    bool CanSplitWithTab(EditorPaneViewModel target, EditorTabViewModel tab);

    /// <summary>
    /// Splits a pane, half each, and moves a tab from whichever pane has it into the new half, which takes the
    /// keyboard.
    /// </summary>
    /// <param name="target">The pane to split.</param>
    /// <param name="tab">The tab.</param>
    /// <param name="side">The side of the pane the new one goes.</param>
    /// <returns>The new pane, or <see langword="null"/> when nothing was split.</returns>
    EditorPaneViewModel? SplitWithTab(EditorPaneViewModel target, EditorTabViewModel tab, FocusDirection side);

    /// <summary>
    /// Shows a message on the status bar's message line.
    /// </summary>
    /// <param name="message">The message.</param>
    void ShowStatus(string message);
}

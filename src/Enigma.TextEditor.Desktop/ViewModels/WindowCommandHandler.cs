using System;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// Carries out Vim's window commands — <c>Ctrl+W</c> and the key after it — on the workspace's panes.
/// </summary>
/// <remarks>
/// Each one is a View-menu command under another name, so the two never disagree: <c>Ctrl+W v</c> is
/// <em>Split Right</em>, <c>Ctrl+W c</c> is <em>Close Pane</em>, and so on. <c>Ctrl+W q</c> is
/// <c>:q</c>, so it goes through the ex handler. One handler for the application, registered in the container: the
/// editor area it acts on is handed to each call.
/// </remarks>
public sealed class WindowCommandHandler
{
    private readonly ExCommandHandler _ex;
    private readonly StatusMessages _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowCommandHandler"/> class.
    /// </summary>
    /// <param name="ex">The ex handler, for the commands that are ex commands under another name.</param>
    /// <param name="status">Where a command that cannot run says why.</param>
    public WindowCommandHandler(ExCommandHandler ex, StatusMessages status)
    {
        _ex = ex ?? throw new ArgumentNullException(nameof(ex));
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Carries out a window command typed in a pane.
    /// </summary>
    /// <param name="workspace">The editor area the pane is in.</param>
    /// <param name="pane">The pane.</param>
    /// <param name="command">The command.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public async Task ExecuteAsync(WorkspaceViewModel workspace, EditorPaneViewModel pane, VimWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(pane);

        // The command acts from the pane it was typed in, whatever had the keyboard a moment ago.
        workspace.Activate(pane);

        // A direction with no pane in it is silent in Vim, and the status line says nothing either: the keyboard simply
        // stays where it is.
        switch (command)
        {
            case VimWindowCommand.FocusLeft:
                workspace.Focus(FocusDirection.Left);
                break;

            case VimWindowCommand.FocusDown:
                workspace.Focus(FocusDirection.Down);
                break;

            case VimWindowCommand.FocusUp:
                workspace.Focus(FocusDirection.Up);
                break;

            case VimWindowCommand.FocusRight:
                workspace.Focus(FocusDirection.Right);
                break;

            case VimWindowCommand.FocusNext:
                workspace.FocusNext();
                break;

            case VimWindowCommand.FocusPrevious:
                workspace.FocusPrevious();
                break;

            case VimWindowCommand.SplitHorizontal:
                workspace.Split(SplitAxis.Rows);
                break;

            case VimWindowCommand.SplitVertical:
                workspace.Split(SplitAxis.Columns);
                break;

            case VimWindowCommand.Close:
                await _ex.ClosePaneAsync(workspace, pane);
                break;

            case VimWindowCommand.Quit:
                await _ex.QuitAsync(workspace, pane);
                break;

            case VimWindowCommand.Only:
                await workspace.OnlyAsync();
                break;

            case VimWindowCommand.Equalize:
                workspace.Equalize();
                break;

            case VimWindowCommand.Taller:
                workspace.Resize(SplitAxis.Rows, workspace.ResizeStep);
                break;

            case VimWindowCommand.Shorter:
                workspace.Resize(SplitAxis.Rows, -workspace.ResizeStep);
                break;

            case VimWindowCommand.Wider:
                workspace.Resize(SplitAxis.Columns, workspace.ResizeStep);
                break;

            case VimWindowCommand.Narrower:
                workspace.Resize(SplitAxis.Columns, -workspace.ResizeStep);
                break;

            case VimWindowCommand.MoveSplitterLeft:
                workspace.MoveSplitter(SplitAxis.Columns, -workspace.ResizeStep);
                break;

            case VimWindowCommand.MoveSplitterRight:
                workspace.MoveSplitter(SplitAxis.Columns, workspace.ResizeStep);
                break;

            case VimWindowCommand.MoveSplitterUp:
                workspace.MoveSplitter(SplitAxis.Rows, -workspace.ResizeStep);
                break;

            case VimWindowCommand.MoveSplitterDown:
                workspace.MoveSplitter(SplitAxis.Rows, workspace.ResizeStep);
                break;

            case VimWindowCommand.MoveTabLeft:
                workspace.MoveTab(FocusDirection.Left);
                break;

            case VimWindowCommand.MoveTabDown:
                workspace.MoveTab(FocusDirection.Down);
                break;

            case VimWindowCommand.MoveTabUp:
                workspace.MoveTab(FocusDirection.Up);
                break;

            case VimWindowCommand.MoveTabRight:
                workspace.MoveTab(FocusDirection.Right);
                break;

            default:
                break;
        }
    }
}

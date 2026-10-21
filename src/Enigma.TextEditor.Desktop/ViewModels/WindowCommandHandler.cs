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
/// <c>:q</c>, so it goes through the ex handler.
/// </remarks>
public sealed class WindowCommandHandler
{
    private readonly WorkspaceViewModel _workspace;
    private readonly ExCommandHandler _ex;
    private readonly StatusMessages _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowCommandHandler"/> class.
    /// </summary>
    /// <param name="workspace">The panes the commands act on.</param>
    /// <param name="ex">The ex handler, for the commands that are ex commands under another name.</param>
    /// <param name="status">Where a command that cannot run says why.</param>
    public WindowCommandHandler(WorkspaceViewModel workspace, ExCommandHandler ex, StatusMessages status)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _ex = ex ?? throw new ArgumentNullException(nameof(ex));
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Carries out a window command typed in a pane.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="command">The command.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public async Task ExecuteAsync(EditorPaneViewModel pane, VimWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(pane);

        // The command acts from the pane it was typed in, whatever had the keyboard a moment ago.
        _workspace.Activate(pane);

        switch (command)
        {
            case VimWindowCommand.FocusLeft:
                Move(FocusDirection.Left);
                break;

            case VimWindowCommand.FocusDown:
                Move(FocusDirection.Down);
                break;

            case VimWindowCommand.FocusUp:
                Move(FocusDirection.Up);
                break;

            case VimWindowCommand.FocusRight:
                Move(FocusDirection.Right);
                break;

            case VimWindowCommand.FocusNext:
                _workspace.FocusNext();
                break;

            case VimWindowCommand.FocusPrevious:
                _workspace.FocusPrevious();
                break;

            case VimWindowCommand.SplitHorizontal:
                _workspace.Split(SplitAxis.Rows);
                break;

            case VimWindowCommand.SplitVertical:
                _workspace.Split(SplitAxis.Columns);
                break;

            case VimWindowCommand.Close:
                await _ex.ClosePaneAsync(pane);
                break;

            case VimWindowCommand.Quit:
                await _ex.QuitAsync(pane);
                break;

            case VimWindowCommand.Only:
                await _workspace.OnlyAsync();
                break;

            case VimWindowCommand.Equalize:
                _workspace.Equalize();
                break;

            case VimWindowCommand.Taller:
                _workspace.Resize(SplitAxis.Rows, _workspace.ResizeStep);
                break;

            case VimWindowCommand.Shorter:
                _workspace.Resize(SplitAxis.Rows, -_workspace.ResizeStep);
                break;

            case VimWindowCommand.Wider:
                _workspace.Resize(SplitAxis.Columns, _workspace.ResizeStep);
                break;

            case VimWindowCommand.Narrower:
                _workspace.Resize(SplitAxis.Columns, -_workspace.ResizeStep);
                break;

            default:
                break;
        }
    }

    // A direction with no pane in it is silent in Vim, and the status line says nothing either: the
    // keyboard simply stays where it is.
    private void Move(FocusDirection direction) => _workspace.Focus(direction);
}

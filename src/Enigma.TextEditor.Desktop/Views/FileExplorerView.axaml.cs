using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The explorer on the files page: the folder's name and its buttons, and the folder's tree.
/// </summary>
/// <remarks>
/// The handlers here are the ones a binding cannot express: a double-click and Enter both mean "open this", and Escape
/// means "back to the text". Everything else — the rows, their expansion, the selection, the buttons — is bound. Where
/// the keyboard goes once a file opens is the window's to say, through <see cref="FileExplorerViewModel.FileOpened"/>.
/// </remarks>
public partial class FileExplorerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileExplorerView"/> class.
    /// </summary>
    public FileExplorerView()
    {
        InitializeComponent();

        // Bubbling, after the rows have had their say: a folder's row takes Enter itself, to expand or collapse.
        Tree.KeyDown += OnTreeKeyDown;
        Tree.SelectionChanged += OnTreeSelectionChanged;
    }

    /// <summary>
    /// Raised when Escape is pressed in the tree: the user is done with it.
    /// </summary>
    internal event EventHandler? LeaveRequested;

    /// <summary>
    /// Gets the folder's tree.
    /// </summary>
    internal TreeView TreeControl => Tree;

    /// <summary>
    /// Gives the explorer the keyboard: the tree, or the <em>Open Folder…</em> button with no folder open.
    /// </summary>
    internal void FocusContent()
    {
        if (DataContext is not FileExplorerViewModel { HasFolder: true })
        {
            PlaceholderOpenFolderButton.Focus(NavigationMethod.Tab);
        }
        else if (!FocusSelectedRow())
        {
            Tree.Focus(NavigationMethod.Tab);
        }
    }

    // The tree moves between rows from the row that has the keyboard: from the tree itself, Down only selects the first
    // one. So a row selected while the tree itself has the keyboard takes it, as a click would have given it.
    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Tree.IsFocused)
        {
            FocusSelectedRow();
        }
    }

    private bool FocusSelectedRow()
        => (Tree.SelectedItem is { } row)
            && (Tree.TreeContainerFromItem(row) is { } container)
            && container.Focus(NavigationMethod.Directional);

    // The sender is the row's own grid, so its DataContext is the row that was clicked — surer than the selection,
    // whose update order relative to the click is not guaranteed. Handled, so the folder's container does not expand
    // or collapse it a second time.
    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: FileTreeNodeViewModel row })
        {
            return;
        }

        e.Handled = true;
        OpenAsync(row).Forget();
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers != KeyModifiers.None) || (DataContext is not FileExplorerViewModel explorer))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter when explorer.SelectedNode is { } row:
                OpenAsync(row).Forget();
                break;

            case Key.Escape:
                LeaveRequested?.Invoke(this, EventArgs.Empty);
                break;

            default:
                return;
        }

        e.Handled = true;
    }

    private Task OpenAsync(FileTreeNodeViewModel row)
        => DataContext is FileExplorerViewModel explorer ? explorer.OpenAsync(row) : Task.CompletedTask;
}

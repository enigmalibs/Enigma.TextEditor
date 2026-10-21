using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.ViewModels;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The application's single window.
/// </summary>
public partial class MainWindow : Window
{
    // Set once the user has answered every unsaved-changes question, so the Close() that follows the
    // answers is let through rather than asked about again.
    private bool _closeConfirmed;
    private bool _confirming;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DialogKeyboard.Attach(HostDialog, FocusActivePane);
    }

    /// <summary>
    /// Gives the keyboard to the active pane: its editor, or its hex rows.
    /// </summary>
    internal void FocusActivePane() => Splits.ActivePaneView?.FocusEditor();

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel || _closeConfirmed || (DataContext is not MainWindowViewModel viewModel)
            || !viewModel.MustConfirmClose)
        {
            return;
        }

        // The questions are asynchronous and Closing is not: the close is cancelled, the questions
        // asked, and the window closed again once every one has been answered with something other
        // than Cancel.
        e.Cancel = true;

        if (!_confirming)
        {
            _ = ConfirmAndCloseAsync(viewModel);
        }
    }

    private async Task ConfirmAndCloseAsync(MainWindowViewModel viewModel)
    {
        _confirming = true;

        try
        {
            if (await viewModel.ConfirmCloseAsync())
            {
                _closeConfirmed = true;
                Close();
            }
        }
        finally
        {
            _confirming = false;
        }
    }

    /// <summary>
    /// Gets the editor of the active pane — what the Edit menu acts on.
    /// </summary>
    internal AvaloniaTextEditor? ActiveEditor
    {
        get
        {
            if (DataContext is not MainWindowViewModel { Workspace.ActivePane: { } pane })
            {
                return null;
            }

            EditorPaneView? view = this.GetVisualDescendants()
                .OfType<EditorPaneView>()
                .FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, pane));

            return view is { CurrentTab.Document.IsHex: false } ? view.EditorControl : null;
        }
    }

    // The editing commands are methods on the control rather than anything bindable, so the menu reaches
    // them here. Each hands the focus back to the text, which the menu took.
    private void OnUndoClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Undo());

    private void OnRedoClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Redo());

    private void OnCutClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Cut());

    private void OnCopyClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Copy());

    private void OnPasteClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Paste());

    private void OnSelectAllClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.SelectAll());

    private void Edit(Action<AvaloniaTextEditor> action)
    {
        if (ActiveEditor is not { } editor)
        {
            return;
        }

        action(editor);
        editor.TextArea.Focus();
    }
}

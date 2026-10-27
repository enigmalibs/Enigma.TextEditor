using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.ViewModels;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The application's single window: the menu bar, the rail and its two pages, and the status bar.
/// </summary>
public partial class MainWindow : Window
{
    // Set once the user has answered every unsaved-changes question, so the Close() that follows the
    // answers is let through rather than asked about again.
    private bool _closeConfirmed;
    private bool _confirming;
    private MainWindowViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DialogKeyboard.Attach(HostDialog, FocusCurrentPage);
        SettingsHost.KeyDown += OnSettingsKeyDown;
    }

    /// <summary>
    /// Gets the settings page, or <see langword="null"/> until it is first shown.
    /// </summary>
    internal SettingsPageView? SettingsPage => SettingsHost.Content as SettingsPageView;

    /// <summary>
    /// Gives the keyboard to the active pane: its editor, or its hex rows.
    /// </summary>
    internal void FocusActivePane() => Splits.ActivePaneView?.FocusEditor();

    /// <summary>
    /// Gives the keyboard to the page that is shown: the active pane, or the settings page.
    /// </summary>
    /// <remarks>
    /// Never the editor behind the settings page: a key it took there would edit a document nobody sees.
    /// </remarks>
    internal void FocusCurrentPage()
    {
        if (_viewModel?.Shell.IsSettingsPage == true)
        {
            SettingsPage?.Focus();
            return;
        }

        FocusActivePane();
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.Shell.PropertyChanged -= OnShellPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.Shell.PropertyChanged += OnShellPropertyChanged;
            ShowPage(_viewModel);
        }

        base.OnDataContextChanged(e);
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName != nameof(ShellViewModel.SelectedItem)) || (_viewModel is null))
        {
            return;
        }

        ShowPage(_viewModel);

        // Posted: the page that is coming has to be visible and laid out before it can take the keyboard.
        // Not while a dialog is open — a file opened behind a question brings the files back — as the
        // dialog gives the keyboard to the page shown when it closes.
        Dispatcher.UIThread.Post(
            () =>
            {
                if (!HostDialog.IsOpen)
                {
                    FocusCurrentPage();
                }
            },
            DispatcherPriority.Background);
    }

    private void ShowPage(MainWindowViewModel viewModel)
    {
        if (viewModel.Shell.IsSettingsPage && (SettingsHost.Content is null))
        {
            SettingsHost.Content = new SettingsPageView { DataContext = viewModel.SettingsPage };
        }
    }

    // Bubbled up from the settings page: a dropdown that is open takes its own Esc first.
    private void OnSettingsKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Key != Key.Escape) || (e.KeyModifiers != KeyModifiers.None) || e.Handled || (_viewModel is null))
        {
            return;
        }

        _viewModel.Shell.ShowFiles();
        e.Handled = true;
    }

    // Posted, after the flyout's own focus handling. Not while a dialog is open: an item that opened one
    // (Change File Encoding…) leaves the keyboard to it, and the dialog hands it back when it closes.
    private void OnStatusFlyoutClosed(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(
            () =>
            {
                if (!HostDialog.IsOpen)
                {
                    FocusCurrentPage();
                }
            },
            DispatcherPriority.Background);

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
    /// Gets the editor of the active pane — what the Edit menu acts on — or <see langword="null"/> while
    /// the settings page hides it.
    /// </summary>
    internal AvaloniaTextEditor? ActiveEditor
    {
        get
        {
            if (DataContext is not MainWindowViewModel { Shell.IsFilesPage: true, Workspace.ActivePane: { } pane })
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

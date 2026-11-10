using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.ViewModels;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The application's single window: the menu bar, the rail and its two pages, and the status bar.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// The explorer's width the first time it is shown.
    /// </summary>
    internal const double ExplorerDefaultWidth = 260;

    /// <summary>
    /// The narrowest the explorer may be dragged to.
    /// </summary>
    internal const double ExplorerMinWidth = 160;

    // The explorer's width while it is hidden: the one it was last shown at, for when it comes back.
    private double _explorerWidth = ExplorerDefaultWidth;

    // Set once the user has answered every unsaved-changes question, so the Close() that follows the
    // answers is let through rather than asked about again.
    private bool _closeConfirmed;
    private bool _confirming;
    private MainWindowViewModel? _viewModel;

    // What a minimised window comes back as: maximised, if it was before it went down.
    private WindowState _restoreState = WindowState.Normal;

    // One step per notch of the wheel; a touchpad's fractions of a notch add up to one.
    private readonly ZoomAccumulator _wheelZoom = new(1.0);

    // One step per tenth of magnification: spreading two fingers right across the touchpad is a few sizes.
    private readonly ZoomAccumulator _pinchZoom = new(0.1);

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DialogKeyboard.Attach(HostDialog, FocusCurrentPage);
        SettingsHost.KeyDown += OnSettingsKeyDown;
        Explorer.LeaveRequested += (_, _) => FocusActivePane();

        // Tunnelled, so the zoom sees a Ctrl+wheel before the text, the hex rows, a tab strip or a minimap scrolls.
        Splits.AddHandler(PointerWheelChangedEvent, OnSplitsPointerWheelChanged, RoutingStrategies.Tunnel);

        // Bubbled, the only way the touchpad's gestures are routed. Nothing in a pane handles a magnify, so it arrives.
        Splits.AddHandler(PointerTouchPadGestureMagnifyEvent, OnSplitsMagnify, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Gets the settings page, or <see langword="null"/> until it is first shown.
    /// </summary>
    internal SettingsPageView? SettingsPage => SettingsHost.Content as SettingsPageView;

    /// <summary>
    /// Brings the window forward, out of the taskbar if it was minimised, for the files another launch handed over.
    /// </summary>
    /// <remarks>
    /// On Windows a window may only take the foreground with the blessing of the process the user is working with;
    /// the launch that handed the files over gives it before it does.
    /// </remarks>
    internal void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = _restoreState;
        }

        Activate();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if ((change.Property == WindowStateProperty) && (change.GetNewValue<WindowState>() is var state and not WindowState.Minimized))
        {
            _restoreState = state;
        }
    }

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
            _viewModel.Explorer.PropertyChanged -= OnExplorerPropertyChanged;
            _viewModel.Explorer.FileOpened -= OnExplorerFileOpened;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
        {
            _viewModel.Shell.PropertyChanged += OnShellPropertyChanged;
            _viewModel.Explorer.PropertyChanged += OnExplorerPropertyChanged;
            _viewModel.Explorer.FileOpened += OnExplorerFileOpened;
            ShowPage(_viewModel);
            SizeExplorer(_viewModel.Explorer.IsVisible);
        }

        base.OnDataContextChanged(e);
    }

    /// <summary>
    /// Gets the explorer's column on the files page.
    /// </summary>
    internal ColumnDefinition ExplorerColumn => FilesPage.ColumnDefinitions[0];

    // Shown, the explorer takes the keyboard — the way in to the tree from the keys — once it is laid out. Hidden, it
    // gives the keyboard back to the pane when it had it, rather than leaving it nowhere.
    private void OnExplorerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName != nameof(FileExplorerViewModel.IsVisible)) || (_viewModel is null))
        {
            return;
        }

        bool shown = _viewModel.Explorer.IsVisible;
        bool hadKeyboard = !shown && Explorer.IsKeyboardFocusWithin;
        SizeExplorer(shown);

        if (shown)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (!HostDialog.IsOpen && (_viewModel?.Shell.IsFilesPage == true) && Explorer.IsVisible)
                    {
                        Explorer.FocusContent();
                    }
                },
                DispatcherPriority.Background);
        }
        else if (hadKeyboard)
        {
            PostFocusActivePane();
        }
    }

    // The column is the explorer's width while it is shown, nothing while it is hidden: a GridSplitter writes the
    // width it was dragged to into the column, which is where it is read back from before the column is emptied.
    private void SizeExplorer(bool shown)
    {
        ColumnDefinition column = ExplorerColumn;

        if (shown)
        {
            column.MinWidth = ExplorerMinWidth;
            column.Width = new GridLength(_explorerWidth, GridUnitType.Pixel);
            return;
        }

        if (column.ActualWidth >= ExplorerMinWidth)
        {
            _explorerWidth = column.ActualWidth;
        }

        column.MinWidth = 0;
        column.Width = new GridLength(0, GridUnitType.Pixel);
    }

    private void OnExplorerFileOpened(object? sender, EventArgs e) => PostFocusActivePane();

    // Posted: after a file opens, the pane shows it before it can take the keyboard. Not while a dialog is open, which
    // hands the keyboard back itself when it closes.
    private void PostFocusActivePane()
        => Dispatcher.UIThread.Post(
            () =>
            {
                if (!HostDialog.IsOpen && (_viewModel?.Shell.IsFilesPage == true))
                {
                    FocusActivePane();
                }
            },
            DispatcherPriority.Background);

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

    // Ctrl+wheel over the panes zooms the text, up larger, as in a browser, and never scrolls it. On Windows a pinch
    // on a precision touchpad arrives this way too, in fractions of a notch. A wheel without Ctrl is left to scroll.
    private void OnSplitsPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || (e.Delta.Y == 0) || (_viewModel is null))
        {
            return;
        }

        e.Handled = true;
        _viewModel.Settings.ZoomByAsync(_wheelZoom.Add(e.Delta.Y)).Forget();
    }

    // The touchpad's zoom gesture where the platform reports it as one (macOS, among others): spreading the fingers
    // zooms in, pinching them zooms out. The magnification is the delta's X.
    private void OnSplitsMagnify(object? sender, PointerDeltaEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        e.Handled = true;
        _viewModel.Settings.ZoomByAsync(_pinchZoom.Add(e.Delta.X)).Forget();
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

        if (e.Cancel || (DataContext is not MainWindowViewModel viewModel))
        {
            return;
        }

        // What the window leaves is what the next start brings back. Here rather than when the application exits,
        // because this is the last moment every way of closing - the button, Alt+F4, :qa, the system logging off -
        // passes through with the buffers still on hand, and the questions can still be asked if keeping fails.
        if (_closeConfirmed)
        {
            // After the questions: every change was saved or thrown away, and none is left to keep.
            viewModel.SaveSession();

            return;
        }

        // Kept, unsaved changes and all, the window closes without a question; so it does when nothing unsaved
        // would be lost. Otherwise - another instance owns the session, or it could not be written - it asks.
        if (viewModel.KeepSessionOnClose() || !viewModel.MustConfirmClose)
        {
            return;
        }

        // The questions are asynchronous and Closing is not: the close is cancelled, the questions
        // asked, and the window closed again once every one has been answered with something other
        // than Cancel.
        e.Cancel = true;

        if (!_confirming)
        {
            ConfirmAndCloseAsync(viewModel).Forget();
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
    internal AvaloniaTextEditor? ActiveEditor => ActiveEditorView?.EditorControl;

    // The pane whose editor the Edit menu acts on, or null while the settings page hides it or the
    // active tab is a hex view.
    private EditorPaneView? ActiveEditorView
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

            return view is { CurrentTab.Document.IsHex: false } ? view : null;
        }
    }

    // The editing commands are methods on the control rather than anything bindable, so the menu reaches
    // them here. Each hands the focus back to the text, which the menu took. Undo and redo go through
    // the pane, which sends them through vim while it is on (BUG-375B).
    private void OnUndoClick(object? sender, RoutedEventArgs e) => EditPane(view => view.Undo());

    private void OnRedoClick(object? sender, RoutedEventArgs e) => EditPane(view => view.Redo());

    private void OnCutClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Cut());

    private void OnCopyClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Copy());

    private void OnPasteClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.Paste());

    private void OnSelectAllClick(object? sender, RoutedEventArgs e) => Edit(editor => editor.SelectAll());

    private void Edit(Action<AvaloniaTextEditor> action) => EditPane(view => action(view.EditorControl));

    private void EditPane(Action<EditorPaneView> action)
    {
        if (ActiveEditorView is not { } view)
        {
            return;
        }

        action(view);
        view.EditorControl.TextArea.Focus();
    }
}

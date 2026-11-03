using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core.Files;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The explorer on the files page: whether it is shown, the one folder it shows, and that folder's tree.
/// </summary>
/// <remarks>
/// <para>
/// The folder's entries are the tree's top rows; the folder itself is the explorer's header. A folder below is listed
/// the first time it is expanded, off the UI thread, and the rows are only ever changed on it: each listing is awaited
/// back onto the thread that asked for it.
/// </para>
/// <para>
/// Nothing watches the file system. <see cref="RefreshAsync"/> lists every folder listed so far again, and merges each
/// listing into the rows already there: a row whose entry is still there is kept — and with it its expansion, its
/// selection and the rows under it.
/// </para>
/// <para>
/// A listing still running when the folder changes or closes is abandoned, and what it would have shown is dropped.
/// </para>
/// </remarks>
public sealed class FileExplorerViewModel : ObservableObject
{
    /// <summary>
    /// What the header says with no folder open.
    /// </summary>
    public const string DefaultHeader = "Explorer";

    private readonly WorkspaceViewModel _workspace;
    private readonly IFilePrompts _prompts;
    private CancellationTokenSource _work = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileExplorerViewModel"/> class, hidden and with no folder.
    /// </summary>
    /// <param name="workspace">The editor area, where files open.</param>
    /// <param name="prompts">The folder picker.</param>
    public FileExplorerViewModel(WorkspaceViewModel workspace, IFilePrompts prompts)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));

        ToggleCommand = new RelayCommand(() => IsVisible = !IsVisible);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderFromPromptAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => HasFolder);
        CloseFolderCommand = new RelayCommand(CloseFolder, () => HasFolder);
        OpenCommand = new AsyncRelayCommand<FileTreeNodeViewModel?>(OpenAsync);
    }

    /// <summary>
    /// Raised once a file opened from the explorer has been opened or refused, dialogs included.
    /// </summary>
    public event EventHandler? FileOpened;

    /// <summary>
    /// Gets or sets a value indicating whether the explorer is shown beside the panes.
    /// </summary>
    public bool IsVisible { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the full path of the folder shown, or <see langword="null"/> with none open.
    /// </summary>
    public string? FolderPath
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasFolder));
                OnPropertyChanged(nameof(Header));
                OnPropertyChanged(nameof(HeaderToolTip));
                RefreshCommand.NotifyCanExecuteChanged();
                CloseFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a folder is open.
    /// </summary>
    public bool HasFolder => FolderPath is not null;

    /// <summary>
    /// Gets what the explorer's header says: the folder's name, or <see cref="DefaultHeader"/> with none open.
    /// </summary>
    public string Header => FolderPath is null
        ? DefaultHeader
        : (Path.GetFileName(FolderPath) is { Length: > 0 } name ? name : FolderPath);

    /// <summary>
    /// Gets what hovering the header shows: the folder's full path, or <see cref="DefaultHeader"/>.
    /// </summary>
    public string HeaderToolTip => FolderPath ?? DefaultHeader;

    /// <summary>
    /// Gets the tree's top rows: the folder's entries.
    /// </summary>
    public ObservableCollection<FileTreeNodeViewModel> Roots { get; } = [];

    /// <summary>
    /// Gets or sets the row selected in the tree.
    /// </summary>
    public FileTreeNodeViewModel? SelectedNode { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the command that shows the explorer when it is hidden, and hides it when it is shown.
    /// </summary>
    public RelayCommand ToggleCommand { get; }

    /// <summary>
    /// Gets <em>Open Folder…</em>: asks which folder to show, starting in the one shown, else the active file's.
    /// </summary>
    public AsyncRelayCommand OpenFolderCommand { get; }

    /// <summary>
    /// Gets the command that lists every folder listed so far again.
    /// </summary>
    public AsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that closes the folder.
    /// </summary>
    public RelayCommand CloseFolderCommand { get; }

    /// <summary>
    /// Gets the command behind a double-click or Enter on a row: a file opens, a folder expands or collapses.
    /// </summary>
    public AsyncRelayCommand<FileTreeNodeViewModel?> OpenCommand { get; }

    /// <summary>
    /// Shows a folder in place of the one shown, and shows the explorer.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <returns>A task that completes when its top level has been listed.</returns>
    public async Task OpenFolderAsync(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        CancellationToken token = Restart();
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

        Roots.Clear();
        SelectedNode = null;
        FolderPath = full;
        IsVisible = true;

        await ListAsync(full, Roots, token);
    }

    /// <summary>
    /// Closes the folder: the tree empties, and a listing still running is abandoned.
    /// </summary>
    public void CloseFolder()
    {
        Restart();
        Roots.Clear();
        SelectedNode = null;
        FolderPath = null;
    }

    /// <summary>
    /// Lists the folder, and every folder below it listed so far, again, keeping the rows whose entries are still
    /// there.
    /// </summary>
    /// <returns>A task that completes when every listing has been merged.</returns>
    public async Task RefreshAsync()
    {
        if (FolderPath is { } folder)
        {
            await ListAsync(folder, Roots, _work.Token);
        }
    }

    /// <summary>
    /// Opens a file in the active pane, through the same door as <em>File → Open</em>; expands or collapses a folder.
    /// </summary>
    /// <param name="node">The row; <see langword="null"/> or a row about a folder does nothing.</param>
    /// <returns>A task that completes when the file has been opened or refused, dialogs included.</returns>
    public async Task OpenAsync(FileTreeNodeViewModel? node)
    {
        switch (node)
        {
            case { IsFile: true }:
                await OpenFileAsync(node.FullPath);
                break;

            case { IsFolder: true }:
                node.IsExpanded = !node.IsExpanded;
                break;
        }
    }

    private async Task OpenFileAsync(string path)
    {
        await _workspace.OpenAsync(path);
        FileOpened?.Invoke(this, EventArgs.Empty);
    }

    private async Task OpenFolderFromPromptAsync()
    {
        string? start = FolderPath
            ?? (_workspace.ActiveDocument?.FilePath is { } file ? Path.GetDirectoryName(file) : null);

        if (await _prompts.AskFolderToOpenAsync(start) is { Length: > 0 } folder)
        {
            await OpenFolderAsync(folder);
        }
    }

    // Abandons what the previous folder was still listing, and returns the token of the work that follows.
    private CancellationToken Restart()
    {
        _work.Cancel();
        _work.Dispose();
        _work = new CancellationTokenSource();

        return _work.Token;
    }

    // The first expansion of a folder. Observed: a failure here is a bug, and goes to the safety net.
    private Task OnFirstExpanded(FileTreeNodeViewModel node)
    {
        Task listing = LoadAsync(node, _work.Token);
        listing.Forget();

        return listing;
    }

    private async Task LoadAsync(FileTreeNodeViewModel node, CancellationToken token)
    {
        if (await ListAsync(node.FullPath, node.Children, token))
        {
            node.IsLoaded = true;
        }
    }

    // Lists one folder into its rows, then — on a refresh — the folders below it that were listed already. False when
    // the work was abandoned before the listing came back.
    private async Task<bool> ListAsync(string folder, ObservableCollection<FileTreeNodeViewModel> rows, CancellationToken token)
    {
        DirectoryListing listing;

        try
        {
            listing = await DirectoryLister.ListAsync(folder, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return false;
        }

        if (token.IsCancellationRequested)
        {
            return false;
        }

        Merge(rows, RowsFor(folder, listing, rows));

        foreach (FileTreeNodeViewModel loaded in rows.Where(row => row.IsFolder && row.IsLoaded).ToList())
        {
            await ListAsync(loaded.FullPath, loaded.Children, token);
        }

        return true;
    }

    // The rows a listing calls for, the rows already there reused where their entry still is.
    private List<FileTreeNodeViewModel> RowsFor(string folder, DirectoryListing listing, IEnumerable<FileTreeNodeViewModel> existing)
    {
        if (listing.Status != DirectoryListingStatus.Ok)
        {
            return [FileTreeNodeViewModel.Unreadable(folder, listing.Status)];
        }

        Dictionary<string, FileTreeNodeViewModel> kept = existing
            .Where(row => !row.IsInformational)
            .ToDictionary(row => row.FullPath, StringComparer.Ordinal);

        return
        [
            .. listing.Entries.Select(entry =>
                kept.TryGetValue(entry.FullPath, out FileTreeNodeViewModel? row) && (row.IsFolder == entry.IsDirectory)
                    ? row
                    : FileTreeNodeViewModel.For(entry, OnFirstExpanded)),
        ];
    }

    // Brings the rows in line with the ones wanted, moving and inserting rather than rebuilding, so that the tree keeps
    // the containers of the rows that stay — their expansion and their selection with them.
    private static void Merge(ObservableCollection<FileTreeNodeViewModel> rows, List<FileTreeNodeViewModel> wanted)
    {
        for (int index = 0; index < wanted.Count; index++)
        {
            if ((index < rows.Count) && ReferenceEquals(rows[index], wanted[index]))
            {
                continue;
            }

            int found = IndexOf(rows, wanted[index], index + 1);

            if (found >= 0)
            {
                rows.Move(found, index);
            }
            else
            {
                rows.Insert(index, wanted[index]);
            }
        }

        while (rows.Count > wanted.Count)
        {
            rows.RemoveAt(rows.Count - 1);
        }
    }

    private static int IndexOf(ObservableCollection<FileTreeNodeViewModel> rows, FileTreeNodeViewModel row, int start)
    {
        for (int index = start; index < rows.Count; index++)
        {
            if (ReferenceEquals(rows[index], row))
            {
                return index;
            }
        }

        return -1;
    }
}

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.Icons.Phosphor;
using Enigma.TextEditor.Core.Files;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One row of the explorer's tree: a folder, a file, or a row about a folder that is being or could not be listed.
/// </summary>
/// <remarks>
/// A node does not read the file system. Expanding a folder the first time calls back into the
/// <see cref="FileExplorerViewModel"/>, which lists it — so a node stays cheap enough to have thousands of. Ported from
/// Enigma.MarkdownEditor's <c>FileTreeNodeViewModel</c>, without its roots, colours and missing folders.
/// </remarks>
public sealed class FileTreeNodeViewModel : ObservableObject
{
    private readonly Func<FileTreeNodeViewModel, Task>? _onFirstExpanded;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileTreeNodeViewModel"/> class.
    /// </summary>
    /// <param name="fullPath">The entry's full path; for a row about a folder, that folder's.</param>
    /// <param name="name">The label shown in the tree.</param>
    /// <param name="kind">What the row stands for.</param>
    /// <param name="onFirstExpanded">Lists a folder the first time it is expanded. Ignored for every other kind.</param>
    public FileTreeNodeViewModel(string fullPath, string name, FileTreeNodeKind kind, Func<FileTreeNodeViewModel, Task>? onFirstExpanded = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        FullPath = fullPath;
        Name = name;
        Kind = kind;
        _onFirstExpanded = onFirstExpanded;
    }

    /// <summary>
    /// Gets the entry's full path, or — for a row about a folder — that folder's.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the label shown in the tree.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets what the row stands for.
    /// </summary>
    public FileTreeNodeKind Kind { get; }

    /// <summary>
    /// Gets the rows under this one. A folder not listed yet holds a single <see cref="FileTreeNodeKind.Reading"/> row,
    /// which gives it an expander.
    /// </summary>
    public ObservableCollection<FileTreeNodeViewModel> Children { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the row is a folder.
    /// </summary>
    public bool IsFolder => Kind == FileTreeNodeKind.Folder;

    /// <summary>
    /// Gets a value indicating whether the row is a file.
    /// </summary>
    public bool IsFile => Kind == FileTreeNodeKind.File;

    /// <summary>
    /// Gets a value indicating whether the row is about its folder rather than an entry of it: never opened, renamed
    /// or deleted.
    /// </summary>
    public bool IsInformational => Kind is FileTreeNodeKind.Reading or FileTreeNodeKind.Unreadable;

    /// <summary>
    /// Gets or sets a value indicating whether this folder has been listed.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Gets the listing the first expansion started, until it is done; <see langword="null"/> before.
    /// </summary>
    internal Task? Loading { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the row is expanded. The first time a folder is, it is listed.
    /// </summary>
    public bool IsExpanded
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IconKind));

            if (value && IsFolder && !IsLoaded && (Loading is null) && (_onFirstExpanded is not null))
            {
                Loading = _onFirstExpanded(this);
            }
        }
    }

    /// <summary>
    /// Gets the icon drawn before the name.
    /// </summary>
    public PhosphorIcon IconKind => Kind switch
    {
        FileTreeNodeKind.Folder => IsExpanded ? PhosphorIcon.FolderOpen : PhosphorIcon.Folder,
        FileTreeNodeKind.File => PhosphorIcon.File,
        FileTreeNodeKind.Reading => PhosphorIcon.HourglassSimple,
        _ => PhosphorIcon.Warning,
    };

    /// <summary>
    /// Gets what hovering the row shows: the entry's full path, or why its folder could not be listed.
    /// </summary>
    public string ToolTip => IsInformational ? Name : FullPath;

    /// <summary>
    /// Builds the row for a listed entry: a folder holding its <see cref="FileTreeNodeKind.Reading"/> row, or a file.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="onFirstExpanded">Lists a folder the first time it is expanded.</param>
    /// <returns>The row.</returns>
    public static FileTreeNodeViewModel For(FileSystemEntry entry, Func<FileTreeNodeViewModel, Task>? onFirstExpanded)
    {
        ArgumentNullException.ThrowIfNull(entry);

        FileTreeNodeViewModel node = new(
            entry.FullPath,
            entry.Name,
            entry.IsDirectory ? FileTreeNodeKind.Folder : FileTreeNodeKind.File,
            onFirstExpanded);

        if (entry.IsDirectory)
        {
            node.Children.Add(new FileTreeNodeViewModel(entry.FullPath, "Reading…", FileTreeNodeKind.Reading));
        }

        return node;
    }

    /// <summary>
    /// Builds the row a folder holds when it could not be listed.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <param name="status">How it answered.</param>
    /// <returns>The row.</returns>
    public static FileTreeNodeViewModel Unreadable(string folder, DirectoryListingStatus status)
        => new(
            folder,
            status switch
            {
                DirectoryListingStatus.Missing => "Not found",
                DirectoryListingStatus.AccessDenied => "No access",
                _ => "Cannot be read",
            },
            FileTreeNodeKind.Unreadable);
}

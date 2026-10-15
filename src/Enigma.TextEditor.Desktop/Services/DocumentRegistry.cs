using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// The open buffers, one per file, and how many tabs show each.
/// </summary>
/// <remarks>
/// <para>
/// The single place a buffer is looked up by its file, which is what keeps a file from being open
/// twice: opening one that is already open hands back the buffer every other tab is editing, so two
/// diverging copies of the same file cannot exist.
/// </para>
/// <para>
/// Tabs <see cref="Retain"/> the buffer they show and <see cref="Release"/> it when they close. A
/// buffer is dropped with its last tab — and that last tab is the one whose closing has to ask about
/// unsaved changes; closing any other tab loses nothing.
/// </para>
/// </remarks>
public sealed class DocumentRegistry
{
    private readonly Dictionary<DocumentViewModel, int> _views = [];
    private readonly List<DocumentViewModel> _documents = [];

    /// <summary>
    /// Gets the open buffers, in the order they were opened.
    /// </summary>
    public IReadOnlyList<DocumentViewModel> Documents => _documents;

    /// <summary>
    /// The comparison that decides whether two paths name the same file: case-insensitive where the
    /// platform's file names are.
    /// </summary>
    public static StringComparison PathComparison
        => OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Finds the buffer of a file, if it is open.
    /// </summary>
    /// <param name="filePath">The file, relative or full.</param>
    /// <returns>The buffer, or <see langword="null"/>.</returns>
    public DocumentViewModel? Find(string filePath) => Find(filePath, hex: false);

    /// <summary>
    /// Finds the hex view of a file, if it is open — a buffer of its own, apart from the file's text.
    /// </summary>
    /// <param name="filePath">The file, relative or full.</param>
    /// <returns>The buffer, or <see langword="null"/>.</returns>
    public DocumentViewModel? FindHex(string filePath) => Find(filePath, hex: true);

    /// <summary>
    /// Adds a buffer just read from — or about to be written to — its file.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <returns>The same buffer.</returns>
    /// <exception cref="InvalidOperationException">Its file is already open in another buffer.</exception>
    public DocumentViewModel Add(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_documents.Contains(document))
        {
            return document;
        }

        if ((document.FilePath is { } path) && (Find(path, document.IsHex) is not null))
        {
            throw new InvalidOperationException($"'{path}' is already open.");
        }

        _documents.Add(document);
        _views[document] = 0;

        return document;
    }

    /// <summary>
    /// The name the next untitled buffer gets: the lowest <c>Untitled-N</c> no open buffer uses.
    /// </summary>
    /// <returns>The name.</returns>
    public string NextUntitledName()
    {
        HashSet<string> taken = [.. _documents.Where(document => document.IsUntitled).Select(document => document.UntitledName)];

        for (int number = 1; ; number++)
        {
            string name = string.Create(CultureInfo.InvariantCulture, $"Untitled-{number}");

            if (!taken.Contains(name))
            {
                return name;
            }
        }
    }

    /// <summary>
    /// Creates and adds an empty, untitled buffer.
    /// </summary>
    /// <param name="format">How it will be written once saved.</param>
    /// <param name="indentation">How it is indented.</param>
    /// <returns>The buffer.</returns>
    public DocumentViewModel CreateUntitled(TextFileFormat format, Indentation indentation)
        => Add(new DocumentViewModel(null, NextUntitledName(), new DocumentContent(string.Empty, format), indentation));

    /// <summary>
    /// Records that one more tab shows a buffer.
    /// </summary>
    /// <param name="document">The buffer, which must be open.</param>
    public void Retain(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _views[document] = ViewCount(document) + 1;
    }

    /// <summary>
    /// Records that a tab showing a buffer closed, dropping the buffer with its last tab.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <returns>How many tabs still show it.</returns>
    public int Release(DocumentViewModel document)
    {
        ArgumentNullException.ThrowIfNull(document);

        int remaining = Math.Max(ViewCount(document) - 1, 0);

        if (remaining == 0)
        {
            _views.Remove(document);
            _documents.Remove(document);

            // A hex buffer holds the file open: closing its last tab lets it go.
            document.Hex?.Dispose();
        }
        else
        {
            _views[document] = remaining;
        }

        return remaining;
    }

    private DocumentViewModel? Find(string filePath, bool hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string full = Path.GetFullPath(filePath);

        return _documents.FirstOrDefault(document =>
            (document.IsHex == hex) && (document.FilePath is { } path) && string.Equals(path, full, PathComparison));
    }

    /// <summary>
    /// How many tabs show a buffer.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <returns>The count; zero for a buffer that is not open.</returns>
    public int ViewCount(DocumentViewModel document)
        => _views.TryGetValue(document, out int count) ? count : 0;
}

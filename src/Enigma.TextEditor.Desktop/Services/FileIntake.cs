using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// How files arrive in the editor area from outside it: picked in the open dialog, named on a command line — this
/// launch's, or another's that handed them over — or dropped on a pane.
/// </summary>
/// <remarks>
/// <para>
/// Every way in ends at the same door, the editor area's <see cref="WorkspaceViewModel.OpenAsync"/>, so a file already
/// open is shown rather than opened twice and a binary file asks its question whichever way it came. What differs is
/// what each way does with the rest: folders and paths that have gone are named once on the status line, a file named
/// on a command line that does not exist yet becomes an empty buffer, and a new tab — untitled, empty, unchanged — gives
/// its place to the first file that opens rather than standing beside it.
/// </para>
/// <para>
/// The editor area is handed to each call; one for the application.
/// </para>
/// </remarks>
public sealed class FileIntake
{
    private readonly IDocumentService _documents;
    private readonly DocumentRegistry _registry;
    private readonly IFilePrompts _prompts;
    private readonly StatusMessages _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileIntake"/> class.
    /// </summary>
    /// <param name="documents">Opens the files.</param>
    /// <param name="registry">The open buffers: whether a new tab's buffer is shown anywhere else.</param>
    /// <param name="prompts">The open dialog.</param>
    /// <param name="status">Where the paths that could not be opened are named.</param>
    public FileIntake(IDocumentService documents, DocumentRegistry registry, IFilePrompts prompts, StatusMessages status)
    {
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _status = status ?? throw new ArgumentNullException(nameof(status));
    }

    /// <summary>
    /// Asks which files to open, starting in the active buffer's folder, and opens each in the active pane —
    /// <em>File → Open…</em>, and <c>:e</c> with no file.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <returns>A task that completes when every chosen file has been opened or reported.</returns>
    public async Task OpenFromPromptAsync(WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        foreach (string path in await _prompts.AskFilesToOpenAsync(workspace.ActiveFolder()))
        {
            await workspace.OpenAsync(path);
        }
    }

    /// <summary>
    /// Asks which files to open, and opens each in the hex viewer in the active pane — <em>File → Open as Hex…</em>.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <returns>A task that completes when every chosen file has been opened or reported.</returns>
    public async Task OpenHexFromPromptAsync(WorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        foreach (string path in await _prompts.AskFilesToOpenAsync(workspace.ActiveFolder()))
        {
            await workspace.OpenHexAsync(path);
        }
    }

    /// <summary>
    /// Opens files dropped on a pane, in that pane.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="pane">The pane they were dropped on.</param>
    /// <param name="paths">Their full paths, in drop order.</param>
    /// <returns>A task that completes when every file has been opened or skipped, dialogs included.</returns>
    /// <remarks>
    /// <para>
    /// The pane takes the keyboard first, so every file opens where it was dropped, each through the same door as
    /// <em>File → Open</em>. Folders and paths that have gone are skipped and named once on the status line, as the
    /// command line's are.
    /// </para>
    /// <para>
    /// A drop on a new tab — an untitled buffer nothing has been typed into, which no other tab shows — puts the first
    /// file that opens in its place rather than beside it: a new pane or a new window starts with such a tab, and it is
    /// not what the user dropped a file on to keep.
    /// </para>
    /// </remarks>
    public async Task OpenDroppedAsync(WorkspaceViewModel workspace, EditorPaneViewModel pane, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(pane);
        ArgumentNullException.ThrowIfNull(paths);

        workspace.Activate(pane);

        List<string> problems = [];
        EditorTabViewModel? fresh = pane.ActiveTab is { } target && IsFresh(target) ? target : null;

        foreach (string path in paths)
        {
            switch (await _documents.ProbeAsync(path))
            {
                case PathKind.Folder:
                    problems.Add($"{NameOf(path)} is a folder");
                    break;

                case PathKind.File when fresh is not null:
                    if (await OpenInPlaceOfAsync(workspace, pane, fresh, path))
                    {
                        fresh = null;
                    }

                    break;

                case PathKind.File:
                    await workspace.OpenAsync(path);
                    break;

                default:
                    problems.Add($"{NameOf(path)} does not exist");
                    break;
            }
        }

        if (problems.Count > 0)
        {
            _status.Show($"Not opened: {string.Join("; ", problems)}.");
        }

        // A drive root has no name of its own.
        static string NameOf(string path)
            => Path.GetFileName(Path.TrimEndingDirectorySeparator(path)) is { Length: > 0 } name ? name : path;
    }

    /// <summary>
    /// Whether any of the paths dropped on a pane's edge is a file, which a pane of its own could open.
    /// </summary>
    /// <param name="paths">The paths.</param>
    /// <returns>Whether one is a file; with none, nothing is worth a split.</returns>
    public async Task<bool> AnyFileAsync(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        foreach (string path in paths)
        {
            if (await _documents.ProbeAsync(path) == PathKind.File)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Opens the files named on the command line — this launch's, or another's that handed them over — in the active
    /// pane, the last one active.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="startup">The files, as read from the arguments.</param>
    /// <returns>A task that completes when every file has been opened or reported.</returns>
    /// <remarks>
    /// <para>
    /// A missing file becomes an empty buffer the first save creates, unless its folder does not exist either, which no
    /// save could fix. With nothing to open, an untitled buffer is ready to type into, as Vim starts.
    /// </para>
    /// <para>
    /// A new tab showing — untitled, empty, unchanged — gives its place to the first file that opens, as it does to a
    /// dropped one: a window that has just been opened has one, and it is not what the files were sent to.
    /// </para>
    /// </remarks>
    public async Task OpenStartupFilesAsync(WorkspaceViewModel workspace, StartupFiles startup)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(startup);

        List<string> problems = [.. startup.Skipped.Select(skipped => $"{skipped} is not a file")];
        EditorPaneViewModel pane = workspace.ActivePane;
        EditorTabViewModel? fresh = pane.ActiveTab is { } target && IsFresh(target) ? target : null;

        foreach (StartupFile file in startup.Files)
        {
            if (file.Exists && (fresh is not null))
            {
                if (await OpenInPlaceOfAsync(workspace, pane, fresh, file.Path))
                {
                    fresh = null;
                }
            }
            else if (file.Exists)
            {
                await workspace.OpenAsync(file.Path);
            }
            else if ((Path.GetDirectoryName(file.Path) is { } folder) && (await _documents.ProbeAsync(folder) == PathKind.Folder))
            {
                workspace.Show(_documents.CreateForMissingFile(file.Path));
            }
            else
            {
                problems.Add($"{file.Path} is in a folder that does not exist");
            }
        }

        if (workspace.ActiveTab is null)
        {
            workspace.NewDocument();
        }

        if (problems.Count > 0)
        {
            _status.Show($"Not opened: {string.Join("; ", problems)}.");
        }
    }

    // Untitled, text, empty, never changed, and shown by this tab alone: nothing would be lost with it.
    private bool IsFresh(EditorTabViewModel tab)
        => tab.Document is { IsUntitled: true, IsHex: false, IsDirty: false, Document.TextLength: 0 }
           && (_registry.ViewCount(tab.Document) <= 1);

    /// <summary>
    /// Opens a file in a new tab's place.
    /// </summary>
    /// <returns>Whether the file took the tab's place; it may still have opened elsewhere.</returns>
    /// <remarks>
    /// The tab is checked again once the file has opened, since a question (a binary file) may have come in between. A
    /// file already on the pane is shown where it is, and the new tab is left for the next.
    /// </remarks>
    private async Task<bool> OpenInPlaceOfAsync(WorkspaceViewModel workspace, EditorPaneViewModel pane, EditorTabViewModel fresh, string path)
    {
        if (await _documents.OpenAsync(path) is not { } document)
        {
            return false;
        }

        if (!pane.Tabs.Contains(fresh) || !IsFresh(fresh) || pane.Tabs.Any(tab => ReferenceEquals(tab.Document, document)))
        {
            workspace.Show(document);

            return false;
        }

        workspace.ReplaceTab(pane, fresh, document);

        return true;
    }
}

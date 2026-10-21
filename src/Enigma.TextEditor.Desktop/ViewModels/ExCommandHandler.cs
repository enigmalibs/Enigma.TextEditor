using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using Enigma.TextEditor.Core.Layout;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// Carries out the ex commands that leave the Vim engine: writing, quitting, opening files, moving
/// between tabs and panes, and the help.
/// </summary>
/// <remarks>
/// <para>
/// Everything goes through the same doors the menus use, so <c>:q</c> asks about unsaved changes as
/// <em>Close Tab</em> does and <c>:w</c> reports a failed save as <em>Save</em> does.
/// </para>
/// <para>
/// <c>:q</c> closes the tab, and quits the application when that was the last tab anywhere — Vim's
/// <c>:q</c> on its last window. A file argument is relative to the active buffer's folder, or to the
/// working directory for an untitled buffer.
/// </para>
/// <para>
/// The tabs are the buffer list, so <c>:enew</c> adds an untitled tab as <em>File → New</em> does, and
/// the buffer that was showing stays open — Vim's <c>:enew</c> with <c>hidden</c> set. <c>:enew!</c>
/// is the one that replaces it, and the <c>!</c> is the user's answer to the unsaved-changes question.
/// </para>
/// </remarks>
public sealed class ExCommandHandler
{
    private readonly WorkspaceViewModel _workspace;
    private readonly IAppShutdown _shutdown;
    private readonly StatusMessages _status;
    private readonly IHelpDialogService _help;
    private readonly LanguageCatalog _languages;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExCommandHandler"/> class.
    /// </summary>
    /// <param name="workspace">The editor area the commands act on.</param>
    /// <param name="shutdown">Ends the application.</param>
    /// <param name="status">Where a malformed path is reported.</param>
    /// <param name="help">Shows the key-binding help, for <c>:help</c>.</param>
    /// <param name="languages">The languages <c>:set filetype</c> can name.</param>
    public ExCommandHandler(WorkspaceViewModel workspace, IAppShutdown shutdown, StatusMessages status, IHelpDialogService help, LanguageCatalog languages)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
        _status = status ?? throw new ArgumentNullException(nameof(status));
        _help = help ?? throw new ArgumentNullException(nameof(help));
        _languages = languages ?? throw new ArgumentNullException(nameof(languages));
    }

    /// <summary>
    /// Carries out a request typed in a pane.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="request">The request.</param>
    /// <returns>A task that completes when the command has run.</returns>
    public async Task ExecuteAsync(EditorPaneViewModel pane, VimExRequest request)
    {
        ArgumentNullException.ThrowIfNull(pane);

        EditorTabViewModel? tab = pane.ActiveTab;

        switch (request.Command)
        {
            case VimExCommand.Write when tab is not null:
                await WriteAsync(tab.Document, request.Argument);
                break;

            case VimExCommand.Quit when tab is not null:
                await QuitAsync(pane, tab, discard: false);
                break;

            case VimExCommand.QuitDiscard when tab is not null:
                await QuitAsync(pane, tab, discard: true);
                break;

            // The hex view has nothing to write, so ':wq' and ':x' there are ':q'.
            case VimExCommand.WriteQuit when tab is { Document.IsHex: true }:
                await QuitAsync(pane, tab, discard: false);
                break;

            case VimExCommand.WriteQuit when tab is not null:
                if (await _workspace.SaveAsync(tab.Document))
                {
                    await QuitAsync(pane, tab, discard: false);
                }

                break;

            case VimExCommand.Edit when request.Argument is { } file:
                if (Resolve(file, tab?.Document) is { } path)
                {
                    await _workspace.OpenAsync(path);
                }

                break;

            case VimExCommand.Edit:
                await _workspace.OpenFromPromptAsync();
                break;

            case VimExCommand.EditNew:
                _workspace.NewDocument();
                break;

            case VimExCommand.EditNewDiscard when tab is not null:
                _workspace.ReplaceWithNewDocument(pane, tab);
                break;

            // Nothing to replace in an empty pane: ':enew!' there is ':enew'.
            case VimExCommand.EditNewDiscard:
                _workspace.NewDocument();
                break;

            case VimExCommand.BufferNext:
                pane.NextTab();
                break;

            case VimExCommand.BufferPrevious:
                pane.PreviousTab();
                break;

            case VimExCommand.WriteAll:
                await _workspace.SaveAllAsync();
                break;

            case VimExCommand.QuitAll:
                _shutdown.RequestExit();
                break;

            case VimExCommand.QuitAllDiscard:
                _shutdown.ExitDiscardingChanges();
                break;

            case VimExCommand.Split:
                await SplitAsync(SplitAxis.Rows, request.Argument, tab?.Document);
                break;

            case VimExCommand.VerticalSplit:
                await SplitAsync(SplitAxis.Columns, request.Argument, tab?.Document);
                break;

            // What ':sp' and ':vs' do too, now that every split opens a new buffer.
            case VimExCommand.New:
                _workspace.Split(SplitAxis.Rows);
                break;

            case VimExCommand.VerticalNew:
                _workspace.Split(SplitAxis.Columns);
                break;

            case VimExCommand.Only:
                await _workspace.OnlyAsync();
                break;

            case VimExCommand.Close:
                await ClosePaneAsync(pane);
                break;

            // ':help {subject}' shows the one help there is.
            case VimExCommand.Help:
                await _help.ShowAsync();
                break;

            case VimExCommand.SetFileOptions when (tab is not null) && (request.Argument is { } words):
                if (FileOptions.Apply(words, tab.Document, _languages) is { } message)
                {
                    _status.Show(message);
                }

                break;

            default:
                break;
        }
    }

    /// <summary>
    /// <c>Ctrl+W q</c>, which is <c>:q</c> in the pane.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>A task that completes when the tab has closed or the user kept it.</returns>
    public Task QuitAsync(EditorPaneViewModel pane)
        => pane.ActiveTab is { } tab ? QuitAsync(pane, tab, discard: false) : Task.CompletedTask;

    /// <summary>
    /// Closes a pane, or says why the last one stays: there is always one pane.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <returns>A task that completes when the pane has closed or the user kept it.</returns>
    public async Task ClosePaneAsync(EditorPaneViewModel pane)
    {
        if (_workspace.Panes.Count <= 1)
        {
            // Vim's E444, in its own words: the last window is closed with :q, which quits.
            _status.Show("Cannot close last window");

            return;
        }

        await _workspace.ClosePaneAsync(pane);
    }

    private async Task SplitAsync(SplitAxis axis, string? file, DocumentViewModel? relativeTo)
    {
        if (file is null)
        {
            _workspace.Split(axis);

            return;
        }

        // Resolved before the split, against the buffer it was typed in.
        if (Resolve(file, relativeTo) is { } path)
        {
            EditorPaneViewModel created = _workspace.Split(axis, newDocument: false);

            // The pane was made for that file: when it could not be opened, the pane goes again rather
            // than stand empty beside the others, and the one it was split from takes the keyboard back.
            if ((await _workspace.OpenAsync(path) is null) && (created.Tabs.Count == 0))
            {
                await _workspace.ClosePaneAsync(created);
            }
        }
    }

    private async Task WriteAsync(DocumentViewModel document, string? file)
    {
        if (document.IsHex)
        {
            _status.Show("The hex view is read-only");

            return;
        }

        if (file is null)
        {
            await _workspace.SaveAsync(document);
        }
        else if (Resolve(file, document) is { } path)
        {
            await _workspace.SaveAsAsync(document, path);
        }
    }

    private async Task QuitAsync(EditorPaneViewModel pane, EditorTabViewModel tab, bool discard)
    {
        if (await _workspace.CloseTabAsync(pane, tab, discard) && !_workspace.HasTabs)
        {
            _shutdown.RequestExit();
        }
    }

    private string? Resolve(string file, DocumentViewModel? relativeTo)
    {
        string folder = relativeTo?.FilePath is { } current
            ? Path.GetDirectoryName(current) ?? Environment.CurrentDirectory
            : Environment.CurrentDirectory;

        try
        {
            return Path.GetFullPath(file, folder);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or SecurityException)
        {
            _status.Show($"Invalid path: {file}");

            return null;
        }
    }
}

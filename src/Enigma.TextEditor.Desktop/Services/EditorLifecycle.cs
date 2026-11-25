using System;
using System.Threading.Tasks;
using Enigma.TextEditor.Core;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// What happens to the editor as its window opens and closes: the welcome dialog of a first start, the session the
/// editor last closed with brought back, the files named on the command line, the session kept every few seconds while
/// the editor runs, and the session kept as it closes.
/// </summary>
/// <remarks>
/// <para>
/// A sequence rather than a feature: each collaborator is one of its steps, and the order between them — the dialog
/// before any question a file could ask, the session before the files named now, the explorer last, the autosave only
/// once all of that went through — is what this class is for.
/// </para>
/// <para>
/// One for the application, as the window it serves.
/// </para>
/// </remarks>
public sealed class EditorLifecycle
{
    private readonly WorkspaceViewModel _workspace;
    private readonly FileExplorerViewModel _explorer;
    private readonly FirstRunState _firstRun;
    private readonly IFirstRunDialogService _firstRunDialog;
    private readonly ISessionStore _session;
    private readonly SessionAutosave _autosave;
    private readonly IAppShutdown _shutdown;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorLifecycle"/> class.
    /// </summary>
    /// <param name="workspace">The editor area the session is restored into and captured from.</param>
    /// <param name="explorer">The explorer, whose folder is part of the session.</param>
    /// <param name="firstRun">Whether this is the very first start, decided before the settings were read.</param>
    /// <param name="firstRunDialog">The welcome dialog, shown on a first start only.</param>
    /// <param name="session">What the editor showed when it last closed, and where the next close keeps it.</param>
    /// <param name="autosave">Keeps the session while the editor runs, and writes the close's.</param>
    /// <param name="shutdown">Whether the close is throwing unsaved changes away (<c>:qa!</c>).</param>
    public EditorLifecycle(
        WorkspaceViewModel workspace,
        FileExplorerViewModel explorer,
        FirstRunState firstRun,
        IFirstRunDialogService firstRunDialog,
        ISessionStore session,
        SessionAutosave autosave,
        IAppShutdown shutdown)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _explorer = explorer ?? throw new ArgumentNullException(nameof(explorer));
        _firstRun = firstRun ?? throw new ArgumentNullException(nameof(firstRun));
        _firstRunDialog = firstRunDialog ?? throw new ArgumentNullException(nameof(firstRunDialog));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _autosave = autosave ?? throw new ArgumentNullException(nameof(autosave));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="StartAsync"/> is still bringing the editor up. What it shows meanwhile —
    /// the explorer of the previous session — does not take the keyboard from the active pane.
    /// </summary>
    public bool IsStarting { get; private set; }

    /// <summary>
    /// Runs what needs the window on screen: the welcome dialog of a first start, then the session the editor last
    /// closed with, then the files named on the command line, then the session's explorer — and from then on keeps the
    /// session every few seconds.
    /// </summary>
    /// <param name="startup">The files and folders named on the command line.</param>
    /// <returns>A task that completes once the files are open and the explorer's folder is listed.</returns>
    /// <remarks>
    /// In that order: the window has one dialog host, so a file that fails to open must not ask its question while the
    /// welcome dialog is up — and the files then open with the editor set up as chosen. The session comes back first,
    /// so the files named now open beside it, in the pane that had the keyboard. The explorer comes last: listing a
    /// large folder must not keep the documents waiting. The session is kept only once all that went through: its first
    /// write replaces the session it was restored from, which a start that failed half way must leave as it was.
    /// </remarks>
    public async Task StartAsync(StartupFiles startup)
    {
        ArgumentNullException.ThrowIfNull(startup);

        IsStarting = true;

        try
        {
            if (_firstRun.IsFirstRun)
            {
                await _firstRunDialog.ShowAsync();
            }

            SessionState? session = _session.Load();

            if (session is not null)
            {
                await _workspace.RestoreSessionAsync(session);
            }

            await _workspace.OpenStartupFilesAsync(startup);

            if (session is not null)
            {
                await _explorer.RestoreAsync(session.ExplorerFolder, session.ExplorerVisible);
            }

            // What a close would keep, so that a crash brings back what closing would have.
            _autosave.Start(() => CaptureSession(keepUnsaved: !_shutdown.DiscardChangesOnExit));
        }
        finally
        {
            IsStarting = false;
        }
    }

    /// <summary>
    /// Captures what the window shows, for the next start: the editor area — its splits, its tabs and, when asked, its
    /// unsaved changes — and the explorer, with its folder.
    /// </summary>
    /// <param name="keepUnsaved">Whether unsaved changes are kept, as <see cref="WorkspaceViewModel.CaptureSession"/> keeps them.</param>
    /// <returns>The session.</returns>
    public SessionState CaptureSession(bool keepUnsaved = false)
        => _workspace.CaptureSession(keepUnsaved) with
        {
            ExplorerFolder = _explorer.FolderPath,
            ExplorerVisible = _explorer.IsVisible,
        };

    /// <summary>
    /// Keeps what the window shows for the next start as it closes — its splits, its tabs, every unsaved change and the
    /// explorer — so that closing need not ask about any of them.
    /// </summary>
    /// <returns>
    /// Whether the session was kept. Not when this instance does not own the session, or the write failed: the unsaved
    /// changes are then asked about, as before there was a session.
    /// </returns>
    /// <remarks>
    /// After <c>:qa!</c> the changes are not kept — the user said to throw them away — but the tabs and splits are.
    /// </remarks>
    public bool KeepSessionOnClose() => _autosave.SaveNow(CaptureSession(keepUnsaved: !_shutdown.DiscardChangesOnExit));

    /// <summary>
    /// Keeps the splits, the tabs and the explorer for the next start, without unsaved changes — after every one has
    /// been asked about and saved or thrown away.
    /// </summary>
    /// <returns>Whether it was kept; not when this instance does not own the session, or the write failed.</returns>
    public bool SaveSession() => _autosave.SaveNow(CaptureSession());

    /// <summary>
    /// The window has closed: the session it kept as it closed is the last one written, and no later tick replaces it.
    /// </summary>
    public void WindowClosed() => _autosave.Stop();
}

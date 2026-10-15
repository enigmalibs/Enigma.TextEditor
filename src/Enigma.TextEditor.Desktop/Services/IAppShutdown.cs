namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Ends the application the way the user closing its window would.
/// </summary>
public interface IAppShutdown
{
    /// <summary>
    /// Asks the main window to close, which ends the application once it has.
    /// </summary>
    /// <remarks>
    /// A request rather than an exit: the window's own closing logic still runs, so nothing that
    /// asks before work is thrown away is bypassed.
    /// </remarks>
    void RequestExit();

    /// <summary>
    /// Ends the application without asking about unsaved changes — Vim's <c>:qa!</c>.
    /// </summary>
    void ExitDiscardingChanges();

    /// <summary>
    /// Gets a value indicating whether the user asked to quit throwing unsaved changes away, which the
    /// window's closing check honours instead of asking.
    /// </summary>
    bool DiscardChangesOnExit { get; }
}

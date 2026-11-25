namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What the engine's parts ask of the engine they work for: the few things that are its state rather than theirs.
/// </summary>
/// <remarks>
/// Implemented by <see cref="VimEngine"/>, explicitly, so that none of it shows on the engine's own surface. The parts —
/// the ex commands (<see cref="VimExCommands"/>), the search motions (<see cref="VimSearchMotion"/>) — each get this
/// rather than the engine, so what they can reach is written down here.
/// </remarks>
internal interface IVimEngineContext
{
    /// <summary>
    /// Gets the lines of the last selection, for the <c>'&lt;</c> and <c>'&gt;</c> marks; <see langword="null"/> before
    /// the first.
    /// </summary>
    (int AnchorLine, int CaretLine)? LastSelection { get; }

    /// <summary>
    /// Says why a command did nothing, and stops any macro it was part of.
    /// </summary>
    /// <param name="message">What the status bar says.</param>
    void Fail(string message);

    /// <summary>
    /// Stops any macro the command is part of, saying nothing: a motion that could not go anywhere.
    /// </summary>
    void StopMacro();

    /// <summary>
    /// Puts the caret on an offset, where normal mode allows, and forgets the column <c>j</c> and <c>k</c> aimed for.
    /// </summary>
    /// <param name="offset">The offset.</param>
    void MoveCaret(int offset);

    /// <summary>
    /// Hands a request that leaves the engine to the application (<see cref="VimEngine.ExCommandRequested"/>).
    /// </summary>
    /// <param name="request">The request.</param>
    void Request(VimExRequest request);

    /// <summary>
    /// Tells the application that an option the editor holds too has changed (<see cref="VimEngine.OptionsChanged"/>).
    /// </summary>
    void RaiseOptionsChanged();
}

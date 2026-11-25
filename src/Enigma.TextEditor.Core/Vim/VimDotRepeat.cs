using System;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// What <c>.</c> repeats: the last change, and the insert session it is being written by while it is open.
/// </summary>
/// <remarks>
/// <para>
/// A change that enters insert mode is not finished until the <c>Esc</c> that closes the session: the command is held
/// with the selection it was typed over, what the session types is read off the buffer's change reports, and the record
/// closes into the last change then. Any other change is the last change at once. A replay remembers nothing — it is
/// the last change, being done again.
/// </para>
/// <para>
/// Running the change again is the engine's: <see cref="Replay"/> works out the command and keeps the replay from
/// remembering itself, and hands the engine the rest.
/// </para>
/// </remarks>
internal sealed class VimDotRepeat
{
    // What the insert session now open has typed, read off the buffer's change reports.
    private readonly VimInsertRecorder _typed = new();

    // The command that opened the insert session now running, and the selection it was typed over, waiting for the Esc
    // that closes the record and makes it the last change.
    private VimCommand? _insertCommand;
    private VimVisualExtent? _insertVisual;

    // What '.' repeats, or null before the first change.
    private VimRecordedChange? _last;

    // Set while '.' is replaying: nothing the replay does is a change of its own to remember.
    private bool _replaying;

    /// <summary>
    /// Gets what the insert session open has typed so far.
    /// </summary>
    public string TypedSoFar => _typed.Text;

    /// <summary>
    /// Gets a value indicating whether an insert session's typing is to be recorded: one is open, and it is not a
    /// replay.
    /// </summary>
    public bool IsRecordingInsert => (_insertCommand is not null) && !_replaying;

    /// <summary>
    /// Reads a change of the buffer into the insert session's record.
    /// </summary>
    /// <param name="change">The change.</param>
    public void Observe(VimTextChange change) => _typed.Observe(change);

    /// <summary>
    /// Starts reading what is typed, from an offset: an insert session opening, or starting again after an undo inside
    /// it.
    /// </summary>
    /// <param name="offset">Where the typing starts.</param>
    public void BeginTyping(int offset) => _typed.Begin(offset);

    /// <summary>
    /// Takes note of a change that has just run: the last change at once, or — when it entered insert mode — held until
    /// the session closes.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="visual">The selection it ran over, for a visual command.</param>
    /// <param name="enteredInsert">Whether it left the engine in insert mode.</param>
    public void Record(VimCommand command, VimVisualExtent? visual, bool enteredInsert)
    {
        if (enteredInsert)
        {
            _insertCommand = command;
            _insertVisual = visual;
        }
        else if (!_replaying)
        {
            _last = new VimRecordedChange(command, null, visual);
        }
    }

    /// <summary>
    /// Closes the record of what the insert session typed: it becomes the last change, and goes into an open
    /// recording.
    /// </summary>
    /// <param name="macros">The recording, which takes the text as the keys that typed it.</param>
    /// <returns>
    /// The command that opened the session and the text it typed, for the engine to type again for every extra count;
    /// <see langword="null"/> when no change opened the session.
    /// </returns>
    /// <remarks>
    /// The copies come after the record has closed, so that they are not recorded as typing themselves — <c>3ix</c> is
    /// remembered as <c>3i</c> and <c>x</c>, not as <c>xxx</c>.
    /// </remarks>
    public (VimCommand Command, string Text)? CloseInsert(VimMacros macros)
    {
        string text = _typed.Text;
        _typed.Close();

        if (_insertCommand is not { } command)
        {
            return null;
        }

        _insertCommand = null;

        if (!_replaying)
        {
            _last = new VimRecordedChange(command, text, _insertVisual);

            if (macros.IsTaking)
            {
                macros.TakeTyped(text);
            }
        }

        _insertVisual = null;

        return (command, text);
    }

    /// <summary>
    /// Runs the last change again — <c>.</c>.
    /// </summary>
    /// <param name="dot">The <c>.</c> itself, for the count and the register it may carry.</param>
    /// <param name="run">
    /// Runs the change: given what was recorded and the command to run, with the new count and register in it.
    /// </param>
    /// <remarks>
    /// The new count takes the old one's place when there is one — and is kept, so that a bare <c>.</c> after
    /// <c>3.</c> is <c>3.</c> again, which is vim's rule. With no change to repeat nothing happens, without a word.
    /// </remarks>
    public void Replay(VimCommand dot, Action<VimRecordedChange, VimCommand> run)
    {
        if (_last is not { } last)
        {
            return;
        }

        VimCommand command = last.Command;

        if (dot.HasCount)
        {
            command = command with { Count = dot.Count };
        }

        if (dot.Register != '\0')
        {
            command = command with { Register = dot.Register };
        }

        _last = last with { Command = command };
        _replaying = true;

        try
        {
            run(last, command);
        }
        finally
        {
            _replaying = false;
        }
    }
}

/// <summary>
/// What <c>.</c> repeats: the command, the text typed after it when it entered insert mode, and the selection it was
/// over when it was one of the visual commands.
/// </summary>
/// <param name="Command">The command.</param>
/// <param name="Text">The text typed, for a command that entered insert mode.</param>
/// <param name="Visual">The selection it ran over, for a visual command.</param>
internal readonly record struct VimRecordedChange(VimCommand Command, string? Text, VimVisualExtent? Visual);

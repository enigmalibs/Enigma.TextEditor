using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// The command line while it is being typed — <c>:</c>, <c>/</c> or <c>?</c> and what follows — and, for a search, the
/// match <c>incsearch</c> shows as it is typed.
/// </summary>
/// <remarks>
/// <para>
/// Typing, Backspace, Enter and Esc, and nothing else. The arrows do not move along the line and there is no history to
/// page through — both are vim's, both are more command line than this one needs, and both would have to be typed blind
/// into a strip that shows no cursor of its own.
/// </para>
/// <para>
/// What a finished line does is the engine's: Enter hands the line back (<see cref="HandleKey"/>), with the command
/// that opened it, and the engine runs it.
/// </para>
/// </remarks>
internal sealed class VimCommandLine
{
    private readonly IVimBuffer _buffer;
    private readonly VimOptions _options;
    private readonly VimSharedState _shared;
    private readonly Action _incSearchChanged;

    // The line as it is being typed, first character and all, or null when it is not open.
    private StringBuilder? _line;

    // The command that opened the line now up — the operator, count and register a '/' or '?' was typed behind — handed
    // back with the line when Enter closes it.
    private VimCommand _opener;

    // Where the view's top line was when a '/' or '?' line opened, to go back to if it is cancelled: incsearch scrolls the
    // view to the match being typed, and Esc takes that back.
    private int _viewTop;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimCommandLine"/> class.
    /// </summary>
    /// <param name="buffer">Where the line is shown, and the text a search is previewed in.</param>
    /// <param name="options">The options a previewed pattern is compiled with.</param>
    /// <param name="shared">Whether <c>incsearch</c> is on.</param>
    /// <param name="incSearchChanged">Told whenever <see cref="IncSearch"/> changes.</param>
    public VimCommandLine(IVimBuffer buffer, VimOptions options, VimSharedState shared, Action incSearchChanged)
    {
        _buffer = buffer;
        _options = options;
        _shared = shared;
        _incSearchChanged = incSearchChanged;
    }

    /// <summary>
    /// Gets a value indicating whether the line is open: every key belongs to it until it closes.
    /// </summary>
    public bool IsOpen => _line is not null;

    /// <summary>
    /// Gets the line as it stands, including the <c>:</c>, <c>/</c> or <c>?</c> that opened it, or
    /// <see langword="null"/> when none is open.
    /// </summary>
    public string? Text => _line?.ToString();

    /// <summary>
    /// Gets what <c>incsearch</c> shows while a <c>/</c> or <c>?</c> is being typed, or <see langword="null"/>.
    /// </summary>
    public VimIncSearch? IncSearch { get; private set; }

    /// <summary>
    /// Opens the line.
    /// </summary>
    /// <param name="opener">
    /// The command that opened it. Its argument is the key, which is also the line's first character; its operator,
    /// count and register are what a search typed on the line will run with.
    /// </param>
    public void Open(VimCommand opener)
    {
        _opener = opener;
        _line = new StringBuilder().Append(opener.Argument);
        _viewTop = _buffer.FirstVisibleLine;
        _buffer.ShowCommandLine(_line.ToString());
    }

    /// <summary>
    /// Closes the line, whatever it held.
    /// </summary>
    public void Close()
    {
        if (_line is null)
        {
            return;
        }

        _line = null;
        SetIncSearch(null);
        _buffer.ShowCommandLine(null);
    }

    /// <summary>
    /// Closes the line without running it — <c>Esc</c> in it. A cancelled search leaves the view where it was before
    /// it was typed.
    /// </summary>
    public void Cancel()
    {
        bool searching = _line is { Length: > 0 } line && (line[0] is '/' or '?');

        Close();

        if (searching)
        {
            _buffer.ScrollToLine(_viewTop, VimScrollAlignment.Top);
        }
    }

    /// <summary>
    /// Reads one keystroke into the open line.
    /// </summary>
    /// <param name="key">What was pressed.</param>
    /// <param name="entered">The finished line and the command that opened it, when the key was Enter.</param>
    /// <returns>Whether the line took the key, as <see cref="VimEngine.HandleKey"/> answers.</returns>
    public bool HandleKey(VimKey key, out (string Line, VimCommand Opener)? entered)
    {
        entered = null;
        StringBuilder line = _line!;

        // Ctrl and Alt still belong to the window, exactly as they do in normal mode. Ctrl+S while a command line is open
        // saves the file, which is what the key is for; the line stays up.
        if (key.HasCommandModifier)
        {
            return false;
        }

        switch (key.Special)
        {
            case VimSpecialKey.Escape:
                Cancel();

                return true;

            case VimSpecialKey.Enter:
                entered = (line.ToString(), _opener);
                Close();

                return true;

            case VimSpecialKey.Backspace:

                // Rubbing out the ':' itself closes the line, which is vim's answer and the only one that leaves no way
                // to be stuck in a command line with nothing in it.
                line.Remove(line.Length - 1, 1);

                if (line.Length == 0)
                {
                    Close();
                    _buffer.ScrollToLine(_viewTop, VimScrollAlignment.Top);
                }
                else
                {
                    _buffer.ShowCommandLine(line.ToString());
                    UpdateIncSearch();
                }

                return true;

            case VimSpecialKey.None when !char.IsControl(key.Character):
                line.Append(key.Character);
                _buffer.ShowCommandLine(line.ToString());
                UpdateIncSearch();

                return true;

            default:

                // An arrow, a function key, or a control character some platform reported as text. Swallowed rather
                // than let through: the document is not what is being typed into.
                return true;
        }
    }

    /// <summary>
    /// incsearch: shows the matches of the pattern typed so far, and brings the one Enter would land on into view —
    /// without moving the caret, which only moves on Enter.
    /// </summary>
    /// <remarks>
    /// A pattern that does not compile yet — <c>foo(</c> on its way to <c>foo(x)</c> — shows nothing and says nothing:
    /// the error is for Enter to report. The view goes back to where it was when the line opened whenever there is
    /// nothing to show, and when the line is cancelled.
    /// </remarks>
    private void UpdateIncSearch()
    {
        if ((_line is not { Length: > 0 } line) || (line[0] is not ('/' or '?')) || !_shared.IncSearch)
        {
            return;
        }

        string pattern = line.ToString(1, line.Length - 1);
        VimIncSearch? preview = null;

        if ((pattern.Length > 0)
            && VimPattern.TryCompile(pattern, _options, smartCase: true, ignoreCase: null, out Regex? regex, out _))
        {
            try
            {
                if (VimSearch.FindMatch(_buffer, regex, _buffer.CaretOffset, forward: line[0] == '/') is { } match)
                {
                    preview = new VimIncSearch(regex, match.Offset, match.Length);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // A pattern too slow to finish shows nothing; Enter reports it.
            }
        }

        if (preview is null)
        {
            _buffer.ScrollToLine(_viewTop, VimScrollAlignment.Top);
        }
        else
        {
            BringIntoView(preview.MatchOffset);
        }

        SetIncSearch(preview);
    }

    private void BringIntoView(int offset)
    {
        int lineNumber = _buffer.GetLineByOffset(offset).Number;
        int top = _buffer.FirstVisibleLine;

        if ((lineNumber < top) || (lineNumber >= (top + _buffer.VisibleLineCount)))
        {
            _buffer.ScrollToLine(lineNumber, VimScrollAlignment.Center);
        }
    }

    private void SetIncSearch(VimIncSearch? preview)
    {
        if (ReferenceEquals(IncSearch, preview))
        {
            return;
        }

        IncSearch = preview;
        _incSearchChanged();
    }
}

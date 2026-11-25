using System.Text.RegularExpressions;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// Where a search motion lands — <c>/</c>, <c>?</c>, <c>n</c>, <c>N</c>, <c>*</c>, <c>#</c> — and the last search it
/// leaves behind for the next one.
/// </summary>
/// <remarks>
/// <para>
/// The engine runs a search as the motion it is, a move on its own or an operator's noun; this class only says where it
/// lands, or why it lands nowhere. Matching itself is <see cref="VimSearch"/>'s.
/// </para>
/// <para>
/// The one place the last search is written. <c>/</c>, <c>?</c>, <c>*</c> and <c>#</c> write it — the pattern, and the
/// way they went; <c>n</c> reads it as it is and <c>N</c> reads it the other way round without turning it, so that
/// <c>N</c> then <c>n</c> still goes the original way, exactly as <c>,</c> and <c>;</c> do for a find. A <c>/</c> with
/// nothing typed after it reads the pattern back and writes only the direction, which is vim's <c>/</c> and Enter.
/// <c>*</c> and <c>#</c> start from the word's first character rather than the caret's, so that the word under the caret
/// is the one skipped, and they mark the pattern as one <c>smartcase</c> must leave alone, because a capital in a word
/// taken off the page was not typed. A pattern that does not compile is not remembered: there is nothing for <c>n</c> to
/// do with it.
/// </para>
/// </remarks>
internal sealed class VimSearchMotion
{
    /// <summary>
    /// Vim's answer to <c>n</c>, <c>N</c> or an empty <c>/</c> before any search, and to <c>:s//…/</c> likewise.
    /// </summary>
    internal const string NoPreviousSearch = "No previous search pattern";

    // Vim's own wording, down to the capitals: someone who knows vim reads these without reading them, and someone who
    // does not is told which way the search went round.
    private const string HitBottom = "search hit BOTTOM, continuing at TOP";
    private const string HitTop = "search hit TOP, continuing at BOTTOM";
    private const string HitBottomNoWrap = "search hit BOTTOM without match for: ";
    private const string HitTopNoWrap = "search hit TOP without match for: ";

    private readonly IVimBuffer _buffer;
    private readonly VimOptions _options;
    private readonly VimSharedState _shared;
    private readonly IVimEngineContext _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimSearchMotion"/> class.
    /// </summary>
    /// <param name="buffer">The text searched.</param>
    /// <param name="options">The options a pattern compiles with, and <c>wrapscan</c>.</param>
    /// <param name="shared">The last search and its highlight, shared by every engine.</param>
    /// <param name="engine">The engine, for its messages and its macros.</param>
    public VimSearchMotion(IVimBuffer buffer, VimOptions options, VimSharedState shared, IVimEngineContext engine)
    {
        _buffer = buffer;
        _options = options;
        _shared = shared;
        _engine = engine;
    }

    /// <summary>
    /// Works out where a search motion lands, and remembers what it looked for.
    /// </summary>
    /// <param name="command">The command, whose motion is one of the six searches.</param>
    /// <returns>
    /// The offset of the match, or <see langword="null"/> when there is none — with the reason already on the status
    /// bar, and the macro stopped.
    /// </returns>
    public int? Target(VimCommand command)
    {
        int from = _buffer.CaretOffset;
        VimLastSearch search;
        bool remember;

        switch (command.Motion)
        {
            case VimMotion.SearchForward:
            case VimMotion.SearchBackward:
                bool forward = command.Motion == VimMotion.SearchForward;

                if (command.Pattern is { } typed)
                {
                    search = new VimLastSearch(typed, forward, SmartCase: true);
                }
                else if (_shared.LastSearch is { } previous)
                {
                    search = previous with { Forward = forward };
                }
                else
                {
                    _engine.Fail(NoPreviousSearch);

                    return null;
                }

                remember = true;
                break;

            case VimMotion.SearchNext:
            case VimMotion.SearchPrevious:
                if (_shared.LastSearch is not { } last)
                {
                    _engine.Fail(NoPreviousSearch);

                    return null;
                }

                search = command.Motion == VimMotion.SearchNext ? last : last with { Forward = !last.Forward };
                remember = false;
                break;

            case VimMotion.SearchWordForward:
            case VimMotion.SearchWordBackward:
                if (VimSearch.WordUnderCaret(_buffer, from) is not { } word)
                {
                    // Nothing on the line to look for. Silent, as a motion that cannot go anywhere is — and, like a find
                    // that misses, the end of any macro it was part of.
                    _engine.StopMacro();

                    return null;
                }

                search = new VimLastSearch(word.Pattern, command.Motion == VimMotion.SearchWordForward, SmartCase: false);
                from = word.Start;
                remember = true;
                break;

            default:
                return null;
        }

        if (!VimPattern.TryCompile(search.Pattern, _options, search.SmartCase, ignoreCase: null, out Regex? regex, out string? error))
        {
            _engine.Fail($"Invalid pattern: {error}");

            return null;
        }

        if (remember)
        {
            _shared.LastSearch = search;
        }

        // hlsearch: the pattern every search goes by is the one highlighted — n and N included, which is also what lifts
        // a :nohlsearch.
        _shared.Highlight(regex);

        return Search(regex, search.Pattern, from, search.Forward, command.EffectiveCount);
    }

    /// <summary>
    /// Walks <paramref name="count"/> matches along from <paramref name="from"/>, or says why it could not.
    /// </summary>
    /// <remarks>
    /// A count walks match by match rather than jumping, because each step has to start from where the last one landed
    /// for the wrap to be right. Nothing is moved here: a <c>3n</c> that runs out of matches leaves the caret where it
    /// was rather than two thirds of the way there, and the caller puts the caret — or the operator's range — where the
    /// answer says. With <c>wrapscan</c> off, the step that would have gone round the end is the step that fails, and
    /// the message says which end it hit; with it on, the message says the search went round.
    /// </remarks>
    private int? Search(Regex regex, string pattern, int from, bool forward, int count)
    {
        int at = from;
        bool wrapped = false;

        try
        {
            for (int index = 0; index < count; index++)
            {
                if (VimSearch.Find(_buffer, regex, at, forward) is not { } hit)
                {
                    _engine.Fail($"Pattern not found: {pattern}");

                    return null;
                }

                if (hit.Wrapped && !_options.WrapScan)
                {
                    _engine.Fail((forward ? HitBottomNoWrap : HitTopNoWrap) + pattern);

                    return null;
                }

                at = hit.Offset;
                wrapped |= hit.Wrapped;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            _engine.Fail($"Invalid pattern: {VimPattern.TimedOut}");

            return null;
        }

        if (wrapped)
        {
            _buffer.ShowStatus(forward ? HitBottom : HitTop);
        }

        return at;
    }
}

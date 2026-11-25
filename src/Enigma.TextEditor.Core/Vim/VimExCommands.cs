using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// Runs an ex line: a jump, a substitute, a <c>:set</c>, a <c>:nohlsearch</c>, or one of the requests that leave the
/// engine.
/// </summary>
/// <remarks>
/// <para>
/// The parser says what was typed and this says what it means, so that "Not an editor command" is one answer from one
/// place — for a name the engine does not know, and for a shape it does not read. The file requests are the only ones
/// that leave: everything else here is about the text, and the text is the engine's.
/// </para>
/// <para>
/// Beside <see cref="VimSubstitute"/> and <see cref="VimSet"/>, which read and apply their own arguments; this class
/// resolves the range, puts the caret where vim does, and reports.
/// </para>
/// </remarks>
internal sealed class VimExCommands
{
    /// <summary>
    /// Vim's answer to a range typed before a command that takes none.
    /// </summary>
    internal const string NoRangeAllowed = "No range allowed";

    /// <summary>
    /// Vim's answer to <c>'&lt;</c> or <c>'&gt;</c> before any selection.
    /// </summary>
    internal const string MarkNotSet = "Mark not set";

    private readonly IVimBuffer _buffer;
    private readonly VimOptions _options;
    private readonly VimSharedState _shared;
    private readonly IVimEngineContext _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimExCommands"/> class.
    /// </summary>
    /// <param name="buffer">The text the commands act on.</param>
    /// <param name="options">The options <c>:set</c> changes and a pattern compiles with.</param>
    /// <param name="shared">The last search, its highlight, and the options every engine shares.</param>
    /// <param name="engine">The engine, for its caret, its last selection and its messages.</param>
    public VimExCommands(IVimBuffer buffer, VimOptions options, VimSharedState shared, IVimEngineContext engine)
    {
        _buffer = buffer;
        _options = options;
        _shared = shared;
        _engine = engine;
    }

    private int CurrentLineNumber => _buffer.GetLineByOffset(_buffer.CaretOffset).Number;

    /// <summary>
    /// Runs an ex line.
    /// </summary>
    /// <param name="command">The line, without its <c>:</c>, trimmed and not empty.</param>
    public void Run(string command)
    {
        if (ExCommandParser.ParseLine(command) is not { } line)
        {
            _engine.Fail($"Not an editor command: {command}");

            return;
        }

        switch (line.Name)
        {
            case "" when line.Range is { } range:
                GoToLine(range);
                break;

            case "s":
            case "substitute":
                Substitute(line, command);
                break;

            case "se":
            case "set":
                Set(line);
                break;

            // ':noh[lsearch]' — any prefix of 'nohlsearch' from 'noh' on, as vim reads it.
            case { Length: >= 3 } name when "nohlsearch".StartsWith(name, StringComparison.Ordinal):
                if (line.Range is not null)
                {
                    _engine.Fail(NoRangeAllowed);
                }
                else
                {
                    _shared.SuppressHighlight();
                }

                break;

            default:
                if (ExCommandParser.FileCommand(line) is { } request)
                {
                    _engine.Request(request);
                }
                else
                {
                    _engine.Fail($"Not an editor command: {command}");
                }

                break;
        }
    }

    /// <summary>
    /// Runs a range typed on its own — <c>:42</c>, <c>:$</c>, <c>:'&gt;</c> — a jump to the last line it names, at its
    /// first non-blank.
    /// </summary>
    private void GoToLine(VimExRange range)
    {
        if (ResolveRange(range) is not { } lines)
        {
            return;
        }

        _engine.MoveCaret(_buffer.GetLine(lines.Last).FirstNonBlankOffset);
        _buffer.ScrollToCaret();
    }

    /// <summary>
    /// Runs <c>:s</c>.
    /// </summary>
    /// <param name="line">The parsed line.</param>
    /// <param name="command">The line as typed, for the message when it is not a substitute after all.</param>
    /// <remarks>
    /// The pattern becomes the last search pattern, as it does in vim — <c>n</c> after <c>:s/foo/bar/</c> finds the
    /// next <c>foo</c> — and an empty pattern reads it, so <c>/foo</c> then <c>:s//bar/</c> substitutes what was just
    /// searched for. It is remembered once it has compiled and before it is run, so a substitute that finds nothing
    /// still leaves its pattern behind. One write for the whole range and one undo step; the caret to the first
    /// non-blank of the last line a substitution touched, which is vim's landing.
    /// </remarks>
    private void Substitute(VimExLine line, string command)
    {
        if (!VimSubstitute.TryParse(line.Argument, out VimSubstituteRequest request, out string? trailing))
        {
            _engine.Fail(trailing ?? $"Not an editor command: {command}");

            return;
        }

        string? pattern = request.Pattern.Length > 0 ? request.Pattern : _shared.LastSearch?.Pattern;

        if (pattern is null)
        {
            _engine.Fail(VimSearchMotion.NoPreviousSearch);

            return;
        }

        (int First, int Last) lines;

        if (line.Range is { } range)
        {
            if (ResolveRange(range) is not { } resolved)
            {
                return;
            }

            lines = resolved;
        }
        else
        {
            int current = CurrentLineNumber;
            lines = (current, current);
        }

        if (!VimPattern.TryCompile(pattern, _options, smartCase: true, request.IgnoreCase, out Regex? regex, out string? error))
        {
            _engine.Fail($"Invalid pattern: {error}");

            return;
        }

        // The direction is left as it was: ':s' has none of its own, and 'n' afterwards goes on the way the last '/' or
        // '?' went.
        _shared.LastSearch = new VimLastSearch(pattern, _shared.LastSearch?.Forward ?? true, SmartCase: true);
        _shared.Highlight(regex);

        VimSubstitution? result;

        try
        {
            result = VimSubstitute.Apply(_buffer, regex, request.Replacement, request.Global, lines.First, lines.Last);
        }
        catch (RegexMatchTimeoutException)
        {
            _engine.Fail($"Invalid pattern: {VimPattern.TimedOut}");

            return;
        }

        if (result is not { } substitution)
        {
            _engine.Fail($"Pattern not found: {pattern}");

            return;
        }

        _buffer.Replace(substitution.Offset, substitution.Length, substitution.Text);
        _engine.MoveCaret(_buffer.GetLineByOffset(substitution.Offset + substitution.Caret).FirstNonBlankOffset);
        _buffer.ShowStatus(substitution.Message);
        _buffer.ScrollToCaret();
    }

    /// <summary>
    /// Runs <c>:set</c>.
    /// </summary>
    private void Set(VimExLine line)
    {
        // Vim's answer to ':3set': an option is not a thing lines have.
        if (line.Range is not null)
        {
            _engine.Fail(NoRangeAllowed);

            return;
        }

        (int ShiftWidth, bool ExpandTab) before = (_options.ShiftWidth, _options.ExpandTab);
        List<string> fileOptions = [];
        bool applied = VimSet.TryApply(line.Argument, _options, _shared, fileOptions, out string? message);

        // Told even when a later argument was refused: what came before it was applied, Vim's rule.
        if ((_options.ShiftWidth, _options.ExpandTab) != before)
        {
            _engine.RaiseOptionsChanged();
        }

        // The file's options are the application's to apply, and to answer for.
        if (fileOptions.Count > 0)
        {
            _engine.Request(new VimExRequest(VimExCommand.SetFileOptions, string.Join(' ', fileOptions)));
        }

        if (!applied)
        {
            _engine.Fail(message!);

            return;
        }

        if (message is not null)
        {
            _buffer.ShowStatus(message);
        }
    }

    /// <summary>
    /// Turns a range into line numbers the buffer has: <c>.</c> and <c>$</c> read now, the marks read off the last
    /// selection, the offsets added, both ends clamped, and the two put in order.
    /// </summary>
    /// <returns>
    /// The lines, or <see langword="null"/> — with the reason shown — when a mark names a selection that has not been
    /// made.
    /// </returns>
    private (int First, int Last)? ResolveRange(VimExRange range)
    {
        if ((ResolveAddress(range.Start) is not { } one) || (ResolveAddress(range.End) is not { } other))
        {
            return null;
        }

        int first = Math.Clamp(one, 1, _buffer.LineCount);
        int last = Math.Clamp(other, 1, _buffer.LineCount);

        // Vim asks whether to swap a backwards range. There is no way to ask here, and the lines meant are the same
        // either way.
        return (Math.Min(first, last), Math.Max(first, last));
    }

    private int? ResolveAddress(VimExAddress address)
    {
        int line;

        switch (address.Kind)
        {
            case VimExAddressKind.Line:
                line = address.Line;
                break;

            case VimExAddressKind.Current:
                line = CurrentLineNumber;
                break;

            case VimExAddressKind.Last:
                line = _buffer.LineCount;
                break;

            default:

                // '< and '>: the last selection's first and last lines, whichever end was the anchor.
                if (_engine.LastSelection is not { } selection)
                {
                    _engine.Fail(MarkNotSet);

                    return null;
                }

                line = address.Kind == VimExAddressKind.VisualStart
                    ? Math.Min(selection.AnchorLine, selection.CaretLine)
                    : Math.Max(selection.AnchorLine, selection.CaretLine);
                break;
        }

        return line + address.Offset;
    }
}

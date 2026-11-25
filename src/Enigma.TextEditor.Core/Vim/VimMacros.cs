using System;
using System.Collections.Generic;
using System.Text;

namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// Recording keys into a register — <c>qa</c> … <c>q</c> — and running a register as keys — <c>@a</c>, <c>@@</c>.
/// </summary>
/// <remarks>
/// <para>
/// While a recording is open every key the engine takes goes into it, after it has done its work; the <c>qa</c> that
/// opened it and the <c>q</c> that closes it are not part of it, and neither is anything a replay runs — that is already
/// in the register it came from (<see cref="IsTaking"/>).
/// </para>
/// <para>
/// A replay stops at the first thing that fails (<see cref="Failed"/>): a sequence that spells nothing, a find or a
/// search that finds nothing, an ex command that is not one, a register with nothing in it — and a nesting deeper than
/// <see cref="MaxDepth"/>, which is a macro running itself with no way out. What ran before that stays; vim's rule too,
/// and the reason a recursive macro is a way of saying "until this stops working".
/// </para>
/// </remarks>
internal sealed class VimMacros
{
    /// <summary>
    /// How deep <c>@a</c> inside <c>a</c> may go before it is called a mistake: vim's own limit, and far past any
    /// recursion that is doing work rather than looping.
    /// </summary>
    public const int MaxDepth = 100;

    private const string NoPreviousMacro = "No previously used register";
    private const string MacroNestingTooDeep = "Macro nesting too deep";

    private readonly VimSharedState _shared;

    // The keys collected so far by the recording open.
    private readonly StringBuilder _keys = new();

    // The register replayed last, for '@@'.
    private char _last;

    /// <summary>
    /// Initializes a new instance of the <see cref="VimMacros"/> class.
    /// </summary>
    /// <param name="shared">The registers a recording is written to and a replay read from.</param>
    public VimMacros(VimSharedState shared) => _shared = shared;

    /// <summary>
    /// Gets the register a <c>q</c> is recording into, as it was typed — a capital appends — or <see langword="null"/>
    /// when none is.
    /// </summary>
    public char? Recording { get; private set; }

    /// <summary>
    /// Gets how many replays are inside one another.
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the innermost replay has hit something that stops them all. Set by
    /// whatever fails, read by the replay loop.
    /// </summary>
    public bool Failed { get; set; }

    /// <summary>
    /// Gets a value indicating whether what is typed now goes into the recording: one is open, and no replay is typing
    /// it.
    /// </summary>
    public bool IsTaking => (Recording is not null) && (Depth == 0);

    /// <summary>
    /// Opens a recording — <c>qa</c> — or, when one is already open, closes it, which is what vim does with a <c>q</c>
    /// that reached the parser through a count.
    /// </summary>
    /// <param name="register">The register, as typed.</param>
    public void Toggle(char register)
    {
        if (Recording is not null)
        {
            Stop();

            return;
        }

        Recording = register;
        _keys.Clear();
    }

    /// <summary>
    /// Closes the recording open, writing what it collected into its register.
    /// </summary>
    /// <returns>Whether one was open.</returns>
    public bool Stop()
    {
        if (Recording is not { } register)
        {
            return false;
        }

        string keys = _keys.ToString();

        Recording = null;
        _keys.Clear();
        _shared.Registers.Record(register, keys);

        return true;
    }

    /// <summary>
    /// Adds a key the engine took to the recording.
    /// </summary>
    /// <param name="key">The key.</param>
    public void Take(VimKey key) => _keys.Append(VimKeyNotation.Format(key));

    /// <summary>
    /// Adds what an insert session typed to the recording, as the keys that typed it.
    /// </summary>
    /// <param name="text">The text typed.</param>
    public void TakeTyped(string text) => _keys.Append(VimKeyNotation.Format(TypedKeys(text)));

    /// <summary>
    /// Runs a register as keys, <paramref name="count"/> times.
    /// </summary>
    /// <param name="name">The register, or <c>@</c> for the one last run.</param>
    /// <param name="count">How many times.</param>
    /// <param name="handleKey">Offers the engine a key; <see langword="false"/> when it turned it down.</param>
    /// <param name="typeIntoBuffer">Types a key the engine turned down — insert mode's — as the editor would.</param>
    /// <param name="fail">Says why the replay could not start, and stops any replay it is inside.</param>
    /// <remarks>
    /// The keys go back through the engine one at a time, and a key it turns down is typed into the buffer the way the
    /// editor would have typed it, so that a macro is exactly what typing the keys would have been.
    /// </remarks>
    public void Play(char name, int count, Func<VimKey, bool> handleKey, Action<VimKey> typeIntoBuffer, Action<string> fail)
    {
        // A fresh run from the keyboard forgets how the last one ended; a nested one must not, or an inner failure would
        // not reach the outer loop.
        if (Depth == 0)
        {
            Failed = false;
        }

        if (name == '@')
        {
            if (_last == '\0')
            {
                fail(NoPreviousMacro);

                return;
            }

            name = _last;
        }

        if (Depth >= MaxDepth)
        {
            fail(MacroNestingTooDeep);

            return;
        }

        if (_shared.Registers.Get(name) is not { } register)
        {
            // Nothing to run. Silent, as vim's beep is, and still a failure for any macro this one is inside.
            Failed = true;

            return;
        }

        _last = char.ToLowerInvariant(name);

        IReadOnlyList<VimKey> keys = RegisterKeys(register);

        Depth++;

        try
        {
            for (int run = 0; (run < count) && !Failed; run++)
            {
                foreach (VimKey key in keys)
                {
                    if (!handleKey(key))
                    {
                        typeIntoBuffer(key);
                    }

                    if (Failed)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            Depth--;
        }
    }

    /// <summary>
    /// The keys that would have typed a piece of text: a line break is Enter, a tab is Tab, and everything else is
    /// itself.
    /// </summary>
    private static List<VimKey> TypedKeys(string text)
    {
        List<VimKey> keys = [];

        foreach (char character in text)
        {
            switch (character)
            {
                case '\r':
                    break;

                case '\n':
                    keys.Add(VimKey.Named(VimSpecialKey.Enter));
                    break;

                case '\t':
                    keys.Add(VimKey.Named(VimSpecialKey.Tab));
                    break;

                default:
                    keys.Add(VimKey.Typed(character));
                    break;
            }
        }

        return keys;
    }

    /// <summary>
    /// Reads a register as keys.
    /// </summary>
    /// <remarks>
    /// A recording is already in <see cref="VimKeyNotation"/>. Any other register is text, and is read the way vim reads
    /// one it is asked to run: a line break is an Enter, and a linewise register ends every line with one, so that a
    /// yanked line runs as a command.
    /// </remarks>
    private static IReadOnlyList<VimKey> RegisterKeys(VimRegister register)
    {
        string text = register.Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "<CR>", StringComparison.Ordinal)
            .Replace("\t", "<Tab>", StringComparison.Ordinal);

        if (register.Kind == VimRegisterKind.Linewise)
        {
            text += "<CR>";
        }

        return VimKeyNotation.Parse(text);
    }
}

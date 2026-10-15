namespace Enigma.TextEditor.Core.Vim;

/// <summary>
/// Which keys make a window command: <c>Ctrl+W</c>, then the key that names it.
/// </summary>
/// <remarks>
/// The second key may be held with Control too — <c>Ctrl+W Ctrl+J</c> is <c>Ctrl+W j</c> in Vim — so
/// Control is ignored on a letter. Shift is not: <c>W</c> is the previous pane where <c>w</c> is the
/// next one.
/// </remarks>
public static class VimWindowKeys
{
    /// <summary>
    /// Whether a key is the window prefix, <c>Ctrl+W</c>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> for <c>Ctrl+W</c> without Alt.</returns>
    public static bool IsPrefix(VimKey key)
        => key.IsCharacter
            && (key.Character is 'w' or 'W')
            && ((key.Modifiers & VimModifiers.Control) != VimModifiers.None)
            && ((key.Modifiers & VimModifiers.Alt) == VimModifiers.None);

    /// <summary>
    /// The window command a key names after the prefix.
    /// </summary>
    /// <param name="key">The key typed after <c>Ctrl+W</c>.</param>
    /// <returns>The command, or <see langword="null"/> for a key that names none.</returns>
    public static VimWindowCommand? Map(VimKey key)
    {
        switch (key.Special)
        {
            case VimSpecialKey.Left:
                return VimWindowCommand.FocusLeft;

            case VimSpecialKey.Down:
                return VimWindowCommand.FocusDown;

            case VimSpecialKey.Up:
                return VimWindowCommand.FocusUp;

            case VimSpecialKey.Right:
                return VimWindowCommand.FocusRight;

            case VimSpecialKey.None:
                break;

            default:
                return null;
        }

        bool control = (key.Modifiers & VimModifiers.Control) != VimModifiers.None;

        // A letter held with Control arrives lower case whatever Shift says; only a bare W is the
        // previous pane.
        return key.Character switch
        {
            'h' => VimWindowCommand.FocusLeft,
            'j' => VimWindowCommand.FocusDown,
            'k' => VimWindowCommand.FocusUp,
            'l' => VimWindowCommand.FocusRight,
            'w' => VimWindowCommand.FocusNext,
            'W' when control => VimWindowCommand.FocusNext,
            'W' => VimWindowCommand.FocusPrevious,
            's' or 'S' => VimWindowCommand.SplitHorizontal,
            'v' => VimWindowCommand.SplitVertical,
            'c' => VimWindowCommand.Close,
            'q' => VimWindowCommand.Quit,
            'o' => VimWindowCommand.Only,
            '=' => VimWindowCommand.Equalize,
            '+' => VimWindowCommand.Taller,
            '-' => VimWindowCommand.Shorter,
            '>' => VimWindowCommand.Wider,
            '<' => VimWindowCommand.Narrower,
            _ => null,
        };
    }
}

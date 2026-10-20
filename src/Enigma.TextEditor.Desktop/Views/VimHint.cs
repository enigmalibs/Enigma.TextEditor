using Avalonia;
using Avalonia.Controls;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The Vim keys a menu item has a twin in — <c>:w</c> beside <em>Save</em> — shown on the item's right
/// while Vim mode is on.
/// </summary>
/// <remarks>
/// <para>
/// Two attached properties. <see cref="KeysProperty"/> is set on an item, in the help dialog's notation.
/// Setting it gives the item the <see cref="ItemClass"/> class, whose style in <c>App.axaml</c> lays the
/// header out with the keys right-aligned before the shortcut column. <see cref="IsShownProperty"/> is
/// inherited: set once on the window and bound to the Vim setting, it reaches every menu and context
/// menu through the tree, popups included.
/// </para>
/// <para>
/// Only an item whose Vim twin does the same thing carries one. Copy has none, because <c>y</c> fills a
/// register, not the clipboard.
/// </para>
/// </remarks>
public sealed class VimHint : AvaloniaObject
{
    /// <summary>
    /// The class an item with keys is given, which its style selects.
    /// </summary>
    public const string ItemClass = "vimHint";

    /// <summary>
    /// The keys, as the help dialog writes them: <c>:w</c>, <c>Ctrl+W v</c>, <c>u</c>.
    /// </summary>
    public static readonly AttachedProperty<string?> KeysProperty =
        AvaloniaProperty.RegisterAttached<VimHint, MenuItem, string?>("Keys");

    /// <summary>
    /// Whether the keys show — Vim mode is on. Inherited, so the window's value reaches every menu.
    /// </summary>
    public static readonly AttachedProperty<bool> IsShownProperty =
        AvaloniaProperty.RegisterAttached<VimHint, Control, bool>("IsShown", inherits: true);

    static VimHint()
        => KeysProperty.Changed.AddClassHandler<MenuItem>(
            (item, e) => item.Classes.Set(ItemClass, e.NewValue is string { Length: > 0 }));

    private VimHint()
    {
    }

    /// <summary>
    /// Gets the Vim keys an item has a twin in.
    /// </summary>
    /// <param name="item">The menu item.</param>
    /// <returns>The keys, or <see langword="null"/> for none.</returns>
    public static string? GetKeys(MenuItem item) => item.GetValue(KeysProperty);

    /// <summary>
    /// Sets the Vim keys an item has a twin in.
    /// </summary>
    /// <param name="item">The menu item.</param>
    /// <param name="value">The keys, or <see langword="null"/> for none.</param>
    public static void SetKeys(MenuItem item, string? value) => item.SetValue(KeysProperty, value);

    /// <summary>
    /// Gets whether the Vim keys show under a control.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns><see langword="true"/> while Vim mode is on.</returns>
    public static bool GetIsShown(Control control) => control.GetValue(IsShownProperty);

    /// <summary>
    /// Sets whether the Vim keys show under a control.
    /// </summary>
    /// <param name="control">The control — the window, for the whole application.</param>
    /// <param name="value">Whether Vim mode is on.</param>
    public static void SetIsShown(Control control, bool value) => control.SetValue(IsShownProperty, value);
}

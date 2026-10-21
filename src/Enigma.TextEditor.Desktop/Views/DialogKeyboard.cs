using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.Avalonia.Desktop.Controls.ContentDialog;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Gives the window's dialog the keyboard while it is open, and gives it back when it closes.
/// </summary>
/// <remarks>
/// <para>
/// The dialog host takes no focus of its own: left alone, the keyboard stays in the editor behind it, so
/// Esc reaches Vim rather than the dialog and Tab walks the window. Here, once the dialog has opened and
/// its content has had the chance to take the focus itself (the pickers focus their list or filter, the
/// help its scroll area), a dialog nobody focused gets it on its default button — or, with none, its
/// first. Tab then moves between the buttons (the host cycles), and Esc reaches the host, which answers
/// Cancel.
/// </para>
/// <para>
/// The default button rather than always the first, so that a question whose safe answer is Cancel — a
/// confirmation to throw changes away — cannot be answered the other way by an Enter typed too soon.
/// "Save changes?" declares Save, its first button, as its default.
/// </para>
/// </remarks>
internal sealed class DialogKeyboard
{
    // Left to right, as the host's template lays them out.
    private static readonly string[] ButtonOrder = ["PART_PrimaryButton", "PART_SecondaryButton", "PART_CloseButton"];

    private readonly ContentDialog _host;
    private readonly Action _focusFallback;
    private IInputElement? _returnTo;

    private DialogKeyboard(ContentDialog host, Action focusFallback)
    {
        _host = host;
        _focusFallback = focusFallback;
    }

    /// <summary>
    /// Starts following a dialog host.
    /// </summary>
    /// <param name="host">The window's dialog host.</param>
    /// <param name="focusFallback">
    /// Gives the keyboard to the application when what had it before the dialog has gone — the active
    /// pane's editor.
    /// </param>
    /// <returns>The follower.</returns>
    public static DialogKeyboard Attach(ContentDialog host, Action focusFallback)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(focusFallback);

        DialogKeyboard keyboard = new(host, focusFallback);
        host.PropertyChanged += keyboard.OnHostPropertyChanged;

        return keyboard;
    }

    /// <summary>
    /// The button a dialog opens with the keyboard on: its default button, or else its first.
    /// </summary>
    /// <param name="host">The dialog host.</param>
    /// <returns>The button, or <see langword="null"/> when the dialog shows none.</returns>
    internal static Button? ButtonToFocus(ContentDialog host)
    {
        ArgumentNullException.ThrowIfNull(host);

        Button[] buttons = [.. ButtonOrder
            .Select(name => host.GetVisualDescendants().OfType<Button>().FirstOrDefault(button => button.Name == name))
            .OfType<Button>()
            .Where(button => button.IsEffectivelyVisible && button.IsEffectivelyEnabled)];

        string? preferred = host.DefaultButton switch
        {
            DefaultButton.Primary => ButtonOrder[0],
            DefaultButton.Secondary => ButtonOrder[1],
            DefaultButton.Close => ButtonOrder[2],
            _ => null,
        };

        return buttons.FirstOrDefault(button => button.Name == preferred) ?? buttons.FirstOrDefault();
    }

    private void OnHostPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ContentDialog.IsOpenProperty)
        {
            return;
        }

        if (e.GetNewValue<bool>())
        {
            Opened();
        }
        else
        {
            Closed();
        }
    }

    private void Opened()
    {
        IInputElement? focused = TopLevel.GetTopLevel(_host)?.FocusManager?.GetFocusedElement();
        _returnTo = (focused is Visual visual) && _host.IsVisualAncestorOf(visual) ? null : focused;

        // Background: after the layout that builds the dialog, and after whatever focus its content
        // claims as it loads.
        Dispatcher.UIThread.Post(TakeKeyboard, DispatcherPriority.Background);
    }

    private void TakeKeyboard()
    {
        if (!_host.IsOpen || _host.IsKeyboardFocusWithin)
        {
            return;
        }

        ButtonToFocus(_host)?.Focus(NavigationMethod.Tab);
    }

    // Posted: the host hides as it closes, and the focus it held has to have left before it is put back.
    private void Closed()
    {
        IInputElement? returnTo = _returnTo;
        _returnTo = null;

        Dispatcher.UIThread.Post(
            () =>
            {
                // A dialog opened again in the meantime keeps the keyboard.
                if (_host.IsOpen)
                {
                    return;
                }

                if ((returnTo is Visual { IsEffectivelyVisible: true } visual) && visual.IsAttachedToVisualTree() && (returnTo.Focus()))
                {
                    return;
                }

                _focusFallback();
            },
            DispatcherPriority.Background);
    }
}

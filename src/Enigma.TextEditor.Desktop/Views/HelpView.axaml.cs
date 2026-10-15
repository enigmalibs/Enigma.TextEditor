using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The key-binding help shown by <c>:help</c> and F1: one card per section in a single scroll area, driven
/// the way the editor is — <c>j</c>/<c>k</c>, <c>Ctrl+D</c>/<c>Ctrl+U</c>, <c>g</c>/<c>G</c> — and closed
/// with <c>q</c> or Esc.
/// </summary>
/// <remarks>
/// Enigma.FileExplorer's help, in look and in keys. The help reads its own keys: nothing behind a dialog
/// should see them. It also keeps the dialog card from scrolling: two nested scroll areas fight over the
/// wheel and the bar. And it gives the keyboard back to whatever had it — the editor, after <c>:help</c>
/// — once it closes, so typing carries on where it was.
/// </remarks>
public partial class HelpView : UserControl
{
    /// <summary>
    /// How far one <c>j</c> or <c>k</c> scrolls, in pixels: about a line and a half of help.
    /// </summary>
    internal const double LineStep = 40;

    /// <summary>
    /// The tallest the scroll area grows, so the help sits in the window as a dialog, not over it.
    /// </summary>
    private const double TallestScroller = 440;

    /// <summary>
    /// The smallest it shrinks to in a short window, where the card may then scroll a little.
    /// </summary>
    private const double ShortestScroller = 120;

    private TopLevel? _topLevel;
    private InputElement? _returnFocusTo;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpView"/> class.
    /// </summary>
    public HelpView()
    {
        InitializeComponent();

        // Tunnel: the help's keys win over anything inside it, the expanders' headers included.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets the help's scroll area.
    /// </summary>
    internal ScrollViewer ScrollArea => Scroller;

    /// <summary>
    /// Runs a help key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifiers held with it.</param>
    /// <param name="symbol">What it types on the user's layout, which is how the letters are read.</param>
    /// <returns>Whether the help used it; Esc is left to the dialog, which closes on it.</returns>
    internal bool Handle(Key key, KeyModifiers modifiers, string? symbol)
    {
        double page = Scroller.Viewport.Height;

        if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != KeyModifiers.None)
        {
            return key switch
            {
                Key.D => ScrollBy(page / 2),
                Key.U => ScrollBy(-page / 2),
                _ => false,
            };
        }

        if ((modifiers & KeyModifiers.Alt) != KeyModifiers.None)
        {
            return false;
        }

        switch (key)
        {
            case Key.Down:
                return ScrollBy(LineStep);

            case Key.Up:
                return ScrollBy(-LineStep);

            case Key.PageDown:
            case Key.Space:
                return ScrollBy(page);

            case Key.PageUp:
                return ScrollBy(-page);

            case Key.Home:
                return ScrollTo(0);

            case Key.End:
                return ScrollTo(double.PositiveInfinity);

            default:
                break;
        }

        switch (symbol)
        {
            case "j":
                return ScrollBy(LineStep);

            case "k":
                return ScrollBy(-LineStep);

            case "g":
                return ScrollTo(0);

            case "G":
                return ScrollTo(double.PositiveInfinity);

            case "q":
                (DataContext as HelpViewModel)?.CloseCommand.Execute(null);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Gives the keyboard back to whatever had it when the help opened, unless that has gone meanwhile.
    /// </summary>
    /// <remarks>
    /// Called by whoever closed the help: the dialog keeps its content attached once it is closed, so
    /// leaving the visual tree is not a signal the help gets.
    /// </remarks>
    internal void ReturnFocus()
    {
        if (_returnFocusTo is { } previous)
        {
            _returnFocusTo = null;

            if (previous.IsAttachedToVisualTree())
            {
                previous.Focus();
            }
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);

        if (_topLevel is not null)
        {
            _topLevel.SizeChanged += OnTopLevelSizeChanged;
            _returnFocusTo = _topLevel.FocusManager?.GetFocusedElement() as InputElement;
        }

        // Posted: the dialog lays its card out around the help first; then the help fits it and takes the keys.
        Dispatcher.UIThread.Post(
            () =>
            {
                FitToCard();
                Scroller.Focus();
            },
            DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is not null)
        {
            _topLevel.SizeChanged -= OnTopLevelSizeChanged;
            _topLevel = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnTopLevelSizeChanged(object? sender, SizeChangedEventArgs e) => FitToCard();

    private void FitToCard() => DialogFit.FitToCard(Scroller, TallestScroller, ShortestScroller);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Handle(e.Key, e.KeyModifiers, e.KeySymbol))
        {
            e.Handled = true;
        }
    }

    private bool ScrollBy(double delta) => ScrollTo(Scroller.Offset.Y + delta);

    private bool ScrollTo(double y)
    {
        double bottom = Math.Max(0, Scroller.Extent.Height - Scroller.Viewport.Height);
        Scroller.Offset = new Vector(Scroller.Offset.X, Math.Clamp(y, 0, bottom));

        return true;
    }
}

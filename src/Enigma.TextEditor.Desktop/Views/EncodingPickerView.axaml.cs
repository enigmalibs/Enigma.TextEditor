using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The encoding picker's content: the list of encodings. Enter or a double click picks.
/// </summary>
public partial class EncodingPickerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncodingPickerView"/> class.
    /// </summary>
    public EncodingPickerView()
    {
        InitializeComponent();

        Encodings.AddHandler(KeyDownEvent, OnEncodingsKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Raised when an encoding has been picked by Enter or a double click.
    /// </summary>
    public event EventHandler? Picked;

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Posted: the dialog takes the focus for itself as it opens.
        Dispatcher.UIThread.Post(
            () =>
            {
                Encodings.Focus();

                if (DataContext is EncodingPickerViewModel { Selected: { } selected })
                {
                    Encodings.ScrollIntoView(selected);
                }
            },
            DispatcherPriority.Background);
    }

    private void OnEncodingsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Pick();
            e.Handled = true;
        }
    }

    private void OnEncodingsDoubleTapped(object? sender, TappedEventArgs e) => Pick();

    private void Pick()
    {
        if (DataContext is not EncodingPickerViewModel { Selected: not null } picker)
        {
            return;
        }

        picker.Commit();
        Picked?.Invoke(this, EventArgs.Empty);
    }
}

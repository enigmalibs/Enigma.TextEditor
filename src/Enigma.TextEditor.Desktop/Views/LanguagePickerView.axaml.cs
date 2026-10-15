using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The language picker's content: a filter box over the list of languages.
/// </summary>
/// <remarks>
/// Keyboard-first: the filter has the focus, the arrows move through the list from it, and Enter
/// picks — as does a double click. Either one closes the dialog through <see cref="Picked"/>.
/// </remarks>
public partial class LanguagePickerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LanguagePickerView"/> class.
    /// </summary>
    public LanguagePickerView()
    {
        InitializeComponent();

        FilterBox.AddHandler(KeyDownEvent, OnFilterKeyDown, RoutingStrategies.Tunnel);
        Languages.AddHandler(KeyDownEvent, OnLanguagesKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Raised when a language has been picked by Enter or a double click.
    /// </summary>
    public event EventHandler? Picked;

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Posted: the dialog takes the focus for itself as it opens.
        Dispatcher.UIThread.Post(() => FilterBox.Focus(), DispatcherPriority.Background);

        if (DataContext is LanguagePickerViewModel { Selected: { } selected })
        {
            Languages.ScrollIntoView(selected);
        }
    }

    private void OnFilterKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not LanguagePickerViewModel picker)
        {
            return;
        }

        int index = picker.Selected is { } selected ? picker.Items.IndexOf(selected) : -1;

        switch (e.Key)
        {
            case Key.Down when index < (picker.Items.Count - 1):
                picker.Selected = picker.Items[index + 1];
                Languages.ScrollIntoView(picker.Selected);
                e.Handled = true;
                break;

            case Key.Up when index > 0:
                picker.Selected = picker.Items[index - 1];
                Languages.ScrollIntoView(picker.Selected);
                e.Handled = true;
                break;

            case Key.Enter:
                Pick(picker);
                e.Handled = true;
                break;

            default:
                break;
        }
    }

    private void OnLanguagesKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Key == Key.Enter) && (DataContext is LanguagePickerViewModel picker))
        {
            Pick(picker);
            e.Handled = true;
        }
    }

    private void OnLanguagesDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is LanguagePickerViewModel picker)
        {
            Pick(picker);
        }
    }

    private void Pick(LanguagePickerViewModel picker)
    {
        if (picker.Selected is null)
        {
            return;
        }

        picker.Commit();
        Picked?.Invoke(this, EventArgs.Empty);
    }
}

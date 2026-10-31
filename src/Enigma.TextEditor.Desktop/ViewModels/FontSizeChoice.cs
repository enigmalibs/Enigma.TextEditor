using System.Globalization;
using System.Windows.Input;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One size of the status bar's font size list.
/// </summary>
/// <param name="Size">The size.</param>
/// <param name="IsCurrent">Whether the text is at this size now, which checks it.</param>
/// <param name="IsDefault">Whether it is the size the text starts at, which the list says.</param>
/// <param name="Command">Sets the text to it, given <paramref name="Size"/>.</param>
public sealed record FontSizeChoice(int Size, bool IsCurrent, bool IsDefault, ICommand Command)
{
    /// <summary>
    /// Gets what the list shows: <c>12</c>, or <c>14 (default)</c>.
    /// </summary>
    public string Header => IsDefault
        ? string.Create(CultureInfo.InvariantCulture, $"{Size} (default)")
        : Size.ToString(CultureInfo.InvariantCulture);
}

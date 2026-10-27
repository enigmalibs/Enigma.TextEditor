using Avalonia.Controls;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The welcome dialog's content: the four choices a first start asks for.
/// </summary>
/// <remarks>
/// Logic-free, like <see cref="AboutView"/>: everything the rows do is the view-model's.
/// </remarks>
public partial class FirstRunView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FirstRunView"/> class.
    /// </summary>
    public FirstRunView() => InitializeComponent();
}

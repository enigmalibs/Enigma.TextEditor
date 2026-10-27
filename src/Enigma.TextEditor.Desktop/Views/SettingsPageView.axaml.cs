using Avalonia.Controls;
using Avalonia.Data.Converters;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The settings page, behind the rail's gear: one card per category in a centred column, Enigma.GitClient's
/// settings rows inside each.
/// </summary>
/// <remarks>
/// The page takes the keyboard itself when it is shown, so no key reaches the hidden editor and none
/// changes a setting by landing on a control; Tab then walks its controls. Esc back to the files is the
/// window's.
/// </remarks>
public partial class SettingsPageView : UserControl
{
    /// <summary>
    /// What a theme choice reads as in its dropdown.
    /// </summary>
    public static readonly IValueConverter ThemeText = new FuncValueConverter<AppTheme, string>(theme => theme switch
    {
        AppTheme.System => "Follow the system",
        AppTheme.Light => "Light",
        AppTheme.Dark => "Dark",
        _ => theme.ToString(),
    });

    /// <summary>
    /// What a line-ending choice reads as in its dropdown.
    /// </summary>
    public static readonly IValueConverter LineEndingText = new FuncValueConverter<LineEndingStyle, string>(lineEnding => lineEnding switch
    {
        LineEndingStyle.Lf => "LF — Linux and macOS",
        LineEndingStyle.CrLf => "CRLF — Windows",
        _ => lineEnding.ToString(),
    });

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageView"/> class.
    /// </summary>
    public SettingsPageView() => InitializeComponent();

    /// <summary>
    /// Gets the scroll area the cards are in.
    /// </summary>
    internal ScrollViewer ScrollArea => Scroller;
}

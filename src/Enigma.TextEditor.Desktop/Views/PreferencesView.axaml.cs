using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Threading;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Settings;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The Preferences dialog: one card per category in a single scroll area, Enigma.GitClient's settings rows
/// inside each.
/// </summary>
/// <remarks>
/// The view keeps the dialog card from scrolling, as the help does: two nested scroll areas fight over
/// the wheel and the bar. The keyboard is the dialog host's to give and take back.
/// </remarks>
public partial class PreferencesView : UserControl
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
    /// The tallest the scroll area grows, so the preferences sit in the window as a dialog, not over it.
    /// </summary>
    private const double TallestScroller = 440;

    /// <summary>
    /// The smallest it shrinks to in a short window, where the card may then scroll a little.
    /// </summary>
    private const double ShortestScroller = 120;

    private TopLevel? _topLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesView"/> class.
    /// </summary>
    public PreferencesView() => InitializeComponent();

    /// <summary>
    /// Gets the scroll area the cards are in.
    /// </summary>
    internal ScrollViewer ScrollArea => Scroller;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _topLevel = TopLevel.GetTopLevel(this);

        if (_topLevel is not null)
        {
            _topLevel.SizeChanged += OnTopLevelSizeChanged;
        }

        // Posted: the dialog lays its card out around the view first.
        Dispatcher.UIThread.Post(FitToCard, DispatcherPriority.Loaded);
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
}

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Extensions;
using Enigma.TextEditor.Desktop.Services;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The read-only hex viewer: one row per sixteen bytes, scrolled like any list.
/// </summary>
/// <remarks>
/// <para>
/// The list's own keys move through the rows — arrows, Page Up and Down, Home and End — and with Vim
/// on, so do <c>j</c>, <c>k</c>, <c>gg</c> and <c>G</c>. Every other key goes to a Vim engine over an
/// empty buffer (<see cref="HexVimBuffer"/>), so that <c>:q</c>, <c>:bn</c>, <c>:sp</c> and
/// <c>Ctrl+W</c> work here as they do in a text tab.
/// </para>
/// </remarks>
public partial class HexView : UserControl
{
    private EditorPaneViewModel? _pane;
    private IVimSession? _session;
    private VimEngine? _engine;
    private bool _pendingG;

    /// <summary>
    /// Initializes a new instance of the <see cref="HexView"/> class.
    /// </summary>
    public HexView()
    {
        InitializeComponent();

        Rows.AddHandler(KeyDownEvent, OnRowsKeyDown, RoutingStrategies.Tunnel);
        Rows.AddHandler(TextInputEvent, OnRowsTextInput, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets the list of rows.
    /// </summary>
    internal ListBox RowList => Rows;

    /// <summary>
    /// Hands the view the pane it lives in, for the Vim session and the commands its keys raise.
    /// </summary>
    /// <param name="pane">The pane.</param>
    /// <param name="session">The session as this pane sees it.</param>
    internal void Attach(EditorPaneViewModel pane, IVimSession session)
    {
        _pane = pane ?? throw new ArgumentNullException(nameof(pane));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        _engine ??= CreateEngine(pane);
    }

    /// <summary>
    /// Moves the keyboard focus onto the rows.
    /// </summary>
    public void FocusRows()
    {
        if (Rows.SelectedIndex < 0 && Rows.ItemCount > 0)
        {
            Rows.SelectedIndex = 0;
        }

        Rows.Focus();
    }

    /// <summary>
    /// Selects a row and scrolls it into view.
    /// </summary>
    /// <param name="row">The row, clamped to the rows there are.</param>
    internal void GoTo(int row)
    {
        if (Rows.ItemCount == 0)
        {
            return;
        }

        int target = Math.Clamp(row, 0, Rows.ItemCount - 1);
        Rows.SelectedIndex = target;
        Rows.ScrollIntoView(target);
    }

    private VimEngine CreateEngine(EditorPaneViewModel pane)
    {
        VimEngine engine = new(
            new HexVimBuffer(
                message => _session?.ReportStatus(message),
                line => _session?.ReportCommandLine(line)),
            options: null,
            pane.VimShared);

        // Not awaited, as in the editor: a keystroke handler has nowhere to return a task to.
        engine.ExCommandRequested += (_, request) => _pane?.ExecuteExAsync(request).Forget();
        engine.WindowCommandRequested += (_, command) => _pane?.ExecuteWindowAsync(command).Forget();

        return engine;
    }

    private bool VimIsOn => (_engine is not null) && (_session?.IsEnabled == true);

    private void OnRowsKeyDown(object? sender, KeyEventArgs e)
    {
        if (!VimIsOn)
        {
            return;
        }

        // While a ':' line or a Ctrl+W is pending, every key belongs to the engine — Enter, Esc and
        // Backspace included, which the list would otherwise take.
        if (_engine!.HasPendingSequence && (VimEditorBehavior.Translate(e) is { } pending))
        {
            e.Handled = _engine.HandleKey(pending);
            return;
        }

        // Ctrl+W: the window prefix.
        if ((e.Key == Key.W) && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = _engine.HandleKey(VimKey.Typed('w', VimModifiers.Control));
        }
    }

    private void OnRowsTextInput(object? sender, TextInputEventArgs e)
    {
        if (!VimIsOn || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        e.Handled = true;

        foreach (char character in e.Text)
        {
            if (_engine!.HasPendingSequence || !Navigate(character))
            {
                _engine.HandleKey(VimKey.Typed(character));
            }
        }
    }

    // The motions that mean something in a list of rows. Anything else goes to the engine.
    private bool Navigate(char character)
    {
        bool wasPendingG = _pendingG;
        _pendingG = false;

        switch (character)
        {
            case 'j':
                GoTo(Rows.SelectedIndex + 1);
                return true;

            case 'k':
                GoTo(Rows.SelectedIndex - 1);
                return true;

            case 'G':
                GoTo(Rows.ItemCount - 1);
                return true;

            case 'g' when wasPendingG:
                GoTo(0);
                return true;

            case 'g':
                _pendingG = true;
                return true;

            default:
                return false;
        }
    }
}

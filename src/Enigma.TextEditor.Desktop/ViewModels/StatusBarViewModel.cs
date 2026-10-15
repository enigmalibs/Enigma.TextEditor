using System;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// What the status bar says about the active tab.
/// </summary>
/// <remarks>
/// Follows the workspace's active tab, and that tab's own changes, so the bar always describes what
/// has the keyboard.
/// </remarks>
public sealed class StatusBarViewModel : ObservableObject
{
    private readonly WorkspaceViewModel _workspace;
    private EditorTabViewModel? _tab;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusBarViewModel"/> class.
    /// </summary>
    /// <param name="workspace">The editor area.</param>
    /// <param name="messages">The message line.</param>
    public StatusBarViewModel(WorkspaceViewModel workspace, StatusMessages messages)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _workspace.Vim.Changed += OnVimChanged;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;

        Follow(_workspace.ActiveTab);
    }

    /// <summary>
    /// Gets the message line.
    /// </summary>
    public StatusMessages Messages { get; }

    /// <summary>
    /// Gets a value indicating whether Vim is on, which is when the mode chip shows.
    /// </summary>
    public bool IsVimEnabled => _workspace.Vim.IsEnabled;

    /// <summary>
    /// Gets the mode chip's text.
    /// </summary>
    public string ModeText => IsHexTab ? "HEX" : _workspace.Vim.Mode switch
    {
        VimMode.Insert => "INSERT",
        VimMode.Visual => "VISUAL",
        VimMode.VisualLine => "V-LINE",
        _ => "NORMAL",
    };

    /// <summary>
    /// Gets a value indicating whether the chip shows insert mode, which colours it green.
    /// </summary>
    public bool IsInsertMode => _workspace.Vim.Mode == VimMode.Insert;

    /// <summary>
    /// Gets a value indicating whether the chip shows a visual mode, which colours it amber.
    /// </summary>
    public bool IsVisualMode => _workspace.Vim.Mode is VimMode.Visual or VimMode.VisualLine;

    /// <summary>
    /// Gets a value indicating whether the mode chip shows: with Vim on, and always on a hex tab, where
    /// it says HEX.
    /// </summary>
    public bool IsModeChipVisible => IsVimEnabled || IsHexTab;

    /// <summary>
    /// Gets a value indicating whether the active tab is the hex view of a file.
    /// </summary>
    public bool IsHexTab => _tab?.Document.IsHex == true;

    /// <summary>
    /// Gets a value indicating whether the active tab is text, which is when the format items show.
    /// </summary>
    public bool IsTextTab => _tab is { Document.IsHex: false };

    /// <summary>
    /// Gets the size of the file a hex tab shows.
    /// </summary>
    public string SizeText => _tab?.Document.Hex is { } hex ? HexDocument.FormatSize(hex.Length) : string.Empty;

    /// <summary>
    /// Gets the ex or search line being typed, <c>:</c> or <c>/</c> included.
    /// </summary>
    public string? CommandLine => _workspace.Vim.CommandLine;

    /// <summary>
    /// Gets a value indicating whether a command line is being typed. It takes the message line's place
    /// while it is.
    /// </summary>
    public bool IsCommandLineVisible => _workspace.Vim.IsEnabled && (_workspace.Vim.CommandLine is not null);

    /// <summary>
    /// Gets a value indicating whether a tab is active, which is when the document items show.
    /// </summary>
    public bool HasActiveTab => _tab is not null;

    /// <summary>
    /// Gets the active buffer's indentation: <c>Spaces: 4</c> or <c>Tab Size: 4</c>.
    /// </summary>
    public string IndentationText => _tab?.Document.Indentation.Label ?? string.Empty;

    /// <summary>
    /// Gets the active buffer's line endings: <c>LF</c> or <c>CRLF</c>.
    /// </summary>
    public string LineEndingText => _tab?.Document.Format.LineEndings switch
    {
        null => string.Empty,
        LineEndingStyle.CrLf => "CRLF",
        _ => "LF",
    };

    /// <summary>
    /// Gets the active buffer's encoding: <c>UTF-8</c>, <c>UTF-16 LE</c>, <c>Windows-1252</c>.
    /// </summary>
    public string EncodingText => _tab?.Document.Encoding.Label ?? string.Empty;

    /// <summary>
    /// Gets the active buffer's language.
    /// </summary>
    public string LanguageText => _tab?.Document.Language.DisplayName ?? string.Empty;

    /// <summary>
    /// Gets the caret's position: <c>Ln 12, Col 5</c>.
    /// </summary>
    public string CaretText => _tab switch
    {
        null => string.Empty,
        { Document.Hex: { } hex } => string.Create(
            CultureInfo.InvariantCulture,
            $"Offset 0x{HexRows.OffsetOf(Math.Max(_tab.HexRow, 0)).ToString(HexFormatter.OffsetDigits(hex.Length) == 16 ? "X16" : "X8", CultureInfo.InvariantCulture)}"),
        _ => string.Create(CultureInfo.InvariantCulture, $"Ln {_tab.CaretLine}, Col {_tab.CaretColumn}"),
    };

    private void OnVimChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsVimEnabled));
        OnPropertyChanged(nameof(IsModeChipVisible));
        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(IsInsertMode));
        OnPropertyChanged(nameof(IsVisualMode));
        OnPropertyChanged(nameof(CommandLine));
        OnPropertyChanged(nameof(IsCommandLineVisible));
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.ActiveTab))
        {
            Follow(_workspace.ActiveTab);
        }
    }

    private void Follow(EditorTabViewModel? tab)
    {
        if (_tab is not null)
        {
            _tab.PropertyChanged -= OnTabPropertyChanged;
            _tab.Document.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _tab = tab;

        if (_tab is not null)
        {
            _tab.PropertyChanged += OnTabPropertyChanged;
            _tab.Document.PropertyChanged += OnDocumentPropertyChanged;
        }

        OnPropertyChanged(nameof(HasActiveTab));
        OnPropertyChanged(nameof(CaretText));
        OnDocumentChanged();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnDocumentChanged();

    // Every document-level item is re-announced on any document change: they are cheap strings, and
    // listing which property feeds which item would be a second place to keep in step.
    private void OnDocumentChanged()
    {
        OnPropertyChanged(nameof(IsHexTab));
        OnPropertyChanged(nameof(IsTextTab));
        OnPropertyChanged(nameof(IsModeChipVisible));
        OnPropertyChanged(nameof(ModeText));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(IndentationText));
        OnPropertyChanged(nameof(EncodingText));
        OnPropertyChanged(nameof(LanguageText));
        OnPropertyChanged(nameof(LineEndingText));
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorTabViewModel.CaretLine) or nameof(EditorTabViewModel.CaretColumn) or nameof(EditorTabViewModel.HexRow))
        {
            OnPropertyChanged(nameof(CaretText));
        }
    }
}

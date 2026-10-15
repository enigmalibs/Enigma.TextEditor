using System;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The Tools menu: commands that change how the active buffer is read, written or shown.
/// </summary>
/// <remarks>
/// The status bar's items run the same commands, so a tool is one click away from where its current
/// value is shown.
/// </remarks>
public sealed class ToolsViewModel : ObservableObject
{
    private readonly WorkspaceViewModel _workspace;
    private readonly ILanguagePicker _languages;
    private readonly IEncodingPicker _encodings;
    private readonly IConfirmationPrompt _confirm;
    private readonly IDocumentService _documents;
    private DocumentViewModel? _followed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolsViewModel"/> class.
    /// </summary>
    /// <param name="workspace">The editor area whose active buffer the tools act on.</param>
    /// <param name="languages">The language picker.</param>
    /// <param name="encodings">The encoding picker.</param>
    /// <param name="confirm">Asks before unsaved changes are thrown away.</param>
    /// <param name="documents">Reads a buffer's file again.</param>
    public ToolsViewModel(
        WorkspaceViewModel workspace,
        ILanguagePicker languages,
        IEncodingPicker encodings,
        IConfirmationPrompt confirm,
        IDocumentService documents)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _languages = languages ?? throw new ArgumentNullException(nameof(languages));
        _encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
        _confirm = confirm ?? throw new ArgumentNullException(nameof(confirm));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));

        SetLanguageCommand = new AsyncRelayCommand(OnSetLanguageAsync, HasDocument);
        ReopenWithEncodingCommand = new AsyncRelayCommand(
            OnReopenWithEncodingAsync,
            () => _workspace.ActiveDocument is { IsUntitled: false, IsHex: false });
        ChangeEncodingCommand = new AsyncRelayCommand(OnChangeEncodingAsync, HasDocument);

        ReopenAsHexCommand = new AsyncRelayCommand(
            () => _workspace.ReopenActiveAsync(asHex: true),
            () => _workspace.ActiveDocument is { IsUntitled: false, IsHex: false });
        ReopenAsTextCommand = new AsyncRelayCommand(
            () => _workspace.ReopenActiveAsync(asHex: false),
            () => _workspace.ActiveDocument is { IsHex: true });

        UseLfCommand = new RelayCommand(() => _workspace.ActiveDocument?.ChangeLineEndings(LineEndingStyle.Lf), HasDocument);
        UseCrLfCommand = new RelayCommand(() => _workspace.ActiveDocument?.ChangeLineEndings(LineEndingStyle.CrLf), HasDocument);
        IndentWithTwoSpacesCommand = new RelayCommand(() => Indent(Indentation.Spaces(2)), HasDocument);
        IndentWithFourSpacesCommand = new RelayCommand(() => Indent(Indentation.Spaces(4)), HasDocument);
        IndentWithTabsCommand = new RelayCommand(() => Indent(Indentation.Tabs(TabSize)), HasDocument);

        _workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceViewModel.ActiveDocument))
            {
                FollowDocument();
            }
        };
    }

    /// <summary>
    /// Gets <em>Tools → Set Language…</em>.
    /// </summary>
    public AsyncRelayCommand SetLanguageCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Reopen with Encoding…</em>: read the file again, decoding its bytes another way.
    /// </summary>
    public AsyncRelayCommand ReopenWithEncodingCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Change File Encoding…</em>: write the buffer in another encoding on the next save.
    /// </summary>
    public AsyncRelayCommand ChangeEncodingCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Reopen as Hex</em>: the active file, in the hex viewer, in the same tab.
    /// </summary>
    public AsyncRelayCommand ReopenAsHexCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Reopen as Text</em>: the active hex tab's file, as text, in the same tab.
    /// </summary>
    public AsyncRelayCommand ReopenAsTextCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Line Endings → LF</em>.
    /// </summary>
    public RelayCommand UseLfCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Line Endings → CRLF</em>.
    /// </summary>
    public RelayCommand UseCrLfCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Indentation → Spaces: 2</em>.
    /// </summary>
    public RelayCommand IndentWithTwoSpacesCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Indentation → Spaces: 4</em>.
    /// </summary>
    public RelayCommand IndentWithFourSpacesCommand { get; }

    /// <summary>
    /// Gets <em>Tools → Indentation → Tabs</em>.
    /// </summary>
    public RelayCommand IndentWithTabsCommand { get; }

    /// <summary>
    /// Gets a value indicating whether the active buffer is written with LF.
    /// </summary>
    public bool IsLf => _workspace.ActiveDocument?.Format.LineEndings == LineEndingStyle.Lf;

    /// <summary>
    /// Gets a value indicating whether the active buffer is written with CRLF.
    /// </summary>
    public bool IsCrLf => _workspace.ActiveDocument?.Format.LineEndings == LineEndingStyle.CrLf;

    /// <summary>
    /// Gets a value indicating whether the active buffer is indented with two spaces.
    /// </summary>
    public bool IsTwoSpaces => _workspace.ActiveDocument?.Indentation == Indentation.Spaces(2);

    /// <summary>
    /// Gets a value indicating whether the active buffer is indented with four spaces.
    /// </summary>
    public bool IsFourSpaces => _workspace.ActiveDocument?.Indentation == Indentation.Spaces(4);

    /// <summary>
    /// Gets a value indicating whether the active buffer is indented with tabs.
    /// </summary>
    public bool IsTabs => _workspace.ActiveDocument?.Indentation.UseTabs == true;

    /// <summary>
    /// How wide a tab is shown once a buffer is set to indent with tabs.
    /// </summary>
    public const int TabSize = 4;

    private void FollowDocument()
    {
        if (_followed is not null)
        {
            _followed.PropertyChanged -= OnDocumentPropertyChanged;
        }

        _followed = _workspace.ActiveDocument;

        if (_followed is not null)
        {
            _followed.PropertyChanged += OnDocumentPropertyChanged;
        }

        SetLanguageCommand.NotifyCanExecuteChanged();
        ReopenWithEncodingCommand.NotifyCanExecuteChanged();
        ChangeEncodingCommand.NotifyCanExecuteChanged();
        ReopenAsHexCommand.NotifyCanExecuteChanged();
        ReopenAsTextCommand.NotifyCanExecuteChanged();
        UseLfCommand.NotifyCanExecuteChanged();
        UseCrLfCommand.NotifyCanExecuteChanged();
        IndentWithTwoSpacesCommand.NotifyCanExecuteChanged();
        IndentWithFourSpacesCommand.NotifyCanExecuteChanged();
        IndentWithTabsCommand.NotifyCanExecuteChanged();
        OnFormatChanged();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentViewModel.Format) or nameof(DocumentViewModel.Indentation))
        {
            OnFormatChanged();
        }
    }

    // The checked marks of the two radio submenus.
    private void OnFormatChanged()
    {
        OnPropertyChanged(nameof(IsLf));
        OnPropertyChanged(nameof(IsCrLf));
        OnPropertyChanged(nameof(IsTwoSpaces));
        OnPropertyChanged(nameof(IsFourSpaces));
        OnPropertyChanged(nameof(IsTabs));
    }

    // The tools are about text: the hex view has no encoding, line endings, indentation or language.
    private bool HasDocument() => _workspace.ActiveDocument is { IsHex: false };

    // The indentation is the buffer's: every pane showing it follows, and nothing is written — the
    // text keeps the indentation it has.
    private void Indent(Indentation indentation)
    {
        if (_workspace.ActiveDocument is { } document)
        {
            document.Indentation = indentation;
        }
    }

    private async Task OnReopenWithEncodingAsync()
    {
        if (_workspace.ActiveDocument is not { IsUntitled: false } document)
        {
            return;
        }

        if (await _encodings.PickAsync("Reopen with encoding", document.Encoding) is not { } encoding)
        {
            return;
        }

        // Reading again replaces the text: what has not been saved would be lost.
        if (document.IsDirty
            && !await _confirm.ConfirmAsync(
                "Discard unsaved changes?",
                $"Reopening {document.Title} as {encoding.Label} replaces its text with what the file holds, and the changes you have not saved are lost.",
                "Discard and reopen"))
        {
            return;
        }

        await _documents.ReopenWithEncodingAsync(document, encoding);
    }

    private async Task OnChangeEncodingAsync()
    {
        if (_workspace.ActiveDocument is not { } document)
        {
            return;
        }

        if (await _encodings.PickAsync("Change file encoding", document.Encoding) is { } encoding)
        {
            document.ChangeEncoding(encoding);
        }
    }

    private async Task OnSetLanguageAsync()
    {
        if (_workspace.ActiveDocument is not { } document)
        {
            return;
        }

        if (await _languages.PickAsync(document.Language) is { } language)
        {
            document.ChooseLanguage(language);
        }
    }
}

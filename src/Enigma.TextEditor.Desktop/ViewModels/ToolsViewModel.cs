using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        LineEndingChoices =
        [
            new MenuChoice("LF", "lineEndings", () => Active?.Format.LineEndings == LineEndingStyle.Lf, DocumentCommand(document => document.ChangeLineEndings(LineEndingStyle.Lf))),
            new MenuChoice("CRLF", "lineEndings", () => Active?.Format.LineEndings == LineEndingStyle.CrLf, DocumentCommand(document => document.ChangeLineEndings(LineEndingStyle.CrLf))),
        ];
        IndentationChoices =
        [
            new MenuChoice("Spaces: 2", "indentation", () => Active?.Indentation == Indentation.Spaces(2), DocumentCommand(document => document.Indentation = Indentation.Spaces(2))),
            new MenuChoice("Spaces: 4", "indentation", () => Active?.Indentation == Indentation.Spaces(4), DocumentCommand(document => document.Indentation = Indentation.Spaces(4))),
            new MenuChoice("Tabs", "indentation", () => Active?.Indentation.UseTabs == true, DocumentCommand(document => document.Indentation = Indentation.Tabs(TabSize))),
        ];

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
    /// Gets <em>Tools → Line Endings</em>, and the status bar's line-ending list: LF, CRLF, for the active buffer.
    /// </summary>
    public IReadOnlyList<MenuChoice> LineEndingChoices { get; }

    /// <summary>
    /// Gets <em>Tools → Indentation</em>, and the status bar's indentation list: two spaces, four spaces, tabs, for the
    /// active buffer.
    /// </summary>
    /// <remarks>
    /// The indentation is the buffer's: every pane showing it follows, and nothing is written — the text keeps the
    /// indentation it has.
    /// </remarks>
    public IReadOnlyList<MenuChoice> IndentationChoices { get; }

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

        foreach (MenuChoice choice in LineEndingChoices.Concat(IndentationChoices))
        {
            ((RelayCommand)choice.Command).NotifyCanExecuteChanged();
        }

        OnFormatChanged();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DocumentViewModel.Format) or nameof(DocumentViewModel.Indentation))
        {
            OnFormatChanged();
        }
    }

    private DocumentViewModel? Active => _workspace.ActiveDocument;

    // The radio marks of the two groups.
    private void OnFormatChanged()
    {
        MenuChoice.Refresh(LineEndingChoices);
        MenuChoice.Refresh(IndentationChoices);
    }

    // The tools are about text: the hex view has no encoding, line endings, indentation or language.
    private bool HasDocument() => _workspace.ActiveDocument is { IsHex: false };

    // A choice acts on the active buffer, and only on a text one.
    private RelayCommand DocumentCommand(Action<DocumentViewModel> change)
        => new(
            () =>
            {
                if (_workspace.ActiveDocument is { } document)
                {
                    change(document);
                }
            },
            HasDocument);

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

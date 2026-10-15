using System;
using System.ComponentModel;
using System.IO;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Hex;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One open buffer: the text of a file — or of a file not saved yet — and how it is to be written.
/// </summary>
/// <remarks>
/// <para>
/// A buffer, not a tab. The same buffer can be shown by several tabs, in several panes, at once —
/// Vim's buffers, and the reason an edit in one split appears in the other. What differs between those
/// tabs (where the caret is, how far the view is scrolled) lives on <see cref="EditorTabViewModel"/>.
/// </para>
/// <para>
/// The text is held with line feeds alone; <see cref="Format"/> says what the file used, and a save
/// writes it that way again (see <see cref="DocumentFile"/>).
/// </para>
/// </remarks>
public sealed class DocumentViewModel : ObservableObject
{
    private bool _replacing;
    private bool _editedSinceSave;

    // A change of encoding or line endings: nothing in the text moved, so the undo stack cannot say the
    // buffer differs from its file, and this does until the next save.
    private bool _formatChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentViewModel"/> class.
    /// </summary>
    /// <param name="filePath">The file the buffer belongs to, or <see langword="null"/> for an untitled one.</param>
    /// <param name="untitledName">The name an untitled buffer is shown by; ignored when there is a file.</param>
    /// <param name="content">The text and how it was stored.</param>
    /// <param name="indentation">How the text is indented.</param>
    /// <param name="hex">
    /// The file's bytes when the buffer is the hex view of it: read-only, never dirty, and its text
    /// empty. <see langword="null"/> for a text buffer.
    /// </param>
    public DocumentViewModel(string? filePath, string untitledName, DocumentContent content, Indentation indentation, HexDocument? hex = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(untitledName);

        FilePath = filePath is null ? null : Path.GetFullPath(filePath);
        UntitledName = untitledName;
        Format = content.Format;
        Indentation = indentation;
        Hex = hex;

        Document = new TextDocument(content.Text);
        Document.UndoStack.PropertyChanged += OnUndoStackPropertyChanged;
        Document.TextChanged += OnTextChanged;
    }

    /// <summary>
    /// Gets the text.
    /// </summary>
    public TextDocument Document { get; }

    /// <summary>
    /// Gets the full path of the file, or <see langword="null"/> for a buffer never saved.
    /// </summary>
    public string? FilePath
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ToolTip));
                OnPropertyChanged(nameof(IsUntitled));
            }
        }
    }

    /// <summary>
    /// Gets the name the buffer goes by while it has no file — <c>Untitled-1</c>.
    /// </summary>
    public string UntitledName { get; }

    /// <summary>
    /// Gets a value indicating whether the buffer has never been saved to a file.
    /// </summary>
    public bool IsUntitled => FilePath is null;

    /// <summary>
    /// Gets the name a tab shows: the file's name, or the untitled name — and for the hex view of a
    /// file, the file's name marked as such, since the same file can be open as text too.
    /// </summary>
    public string Title => FilePath is null
        ? UntitledName
        : IsHex ? $"{Path.GetFileName(FilePath)} (hex)" : Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the file's bytes when this is the hex view of a file, or <see langword="null"/> for text.
    /// </summary>
    public HexDocument? Hex { get; }

    /// <summary>
    /// Gets a value indicating whether this is the read-only hex view of a file.
    /// </summary>
    public bool IsHex => Hex is not null;

    /// <summary>
    /// Gets what hovering a tab shows: the full path, or the untitled name.
    /// </summary>
    public string ToolTip => FilePath ?? UntitledName;

    /// <summary>
    /// Gets how the text is written: encoding, byte-order mark and line terminator.
    /// </summary>
    public TextFileFormat Format
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(Encoding));
            }
        }
    }

    /// <summary>
    /// Gets the encoding the buffer is written in, as the status bar and the pickers name it.
    /// </summary>
    public TextEncoding Encoding => TextEncodings.For(Format);

    /// <summary>
    /// Gets or sets how the text is indented, which is what a Tab key and Vim's shifts insert.
    /// </summary>
    public Indentation Indentation { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets the language the buffer is coloured as.
    /// </summary>
    public LanguageInfo Language { get; private set => SetProperty(ref field, value); } = LanguageInfo.PlainText;

    /// <summary>
    /// Gets a value indicating whether the user chose the language, which a later rename then leaves
    /// alone.
    /// </summary>
    public bool IsLanguageChosen { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the buffer holds something its file does not.
    /// </summary>
    public bool IsDirty { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Sets the language the user picked, for as long as the buffer is open.
    /// </summary>
    /// <param name="language">The language.</param>
    public void ChooseLanguage(LanguageInfo language)
    {
        ArgumentNullException.ThrowIfNull(language);

        IsLanguageChosen = true;
        Language = language;
    }

    /// <summary>
    /// Sets the language the file's name implies, unless the user chose one.
    /// </summary>
    /// <param name="language">The language for the file's name.</param>
    public void DetectLanguage(LanguageInfo language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (!IsLanguageChosen)
        {
            Language = language;
        }
    }

    /// <summary>
    /// Converts the buffer to another encoding: written in it by the next save, which the buffer now
    /// has pending.
    /// </summary>
    /// <param name="encoding">The encoding, with its byte-order mark rule.</param>
    public void ChangeEncoding(TextEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        ChangeFormat(Format.With(encoding));
    }

    /// <summary>
    /// Switches the buffer to other line endings: written with them by the next save, which the
    /// buffer now has pending.
    /// </summary>
    /// <param name="lineEndings">The line terminator.</param>
    public void ChangeLineEndings(LineEndingStyle lineEndings) => ChangeFormat(Format.With(lineEndings));

    /// <summary>
    /// Records that the buffer was written to a file.
    /// </summary>
    /// <param name="filePath">Where it was written, which becomes its file.</param>
    /// <param name="written">The version of the text that was written.</param>
    /// <param name="writtenFormat">The format it was written in, or <see langword="null"/> for the current one.</param>
    /// <remarks>
    /// The version travels with the write because the write is awaited: a keystroke landing while it is
    /// in flight is not in the file, and must leave the buffer dirty.
    /// </remarks>
    public void MarkSaved(string filePath, ITextSourceVersion written, TextFileFormat? writtenFormat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(written);

        FilePath = Path.GetFullPath(filePath);

        // The format is the one written: a change made while the save was in flight is not in the
        // file, and stays pending.
        if ((writtenFormat is null) || ReferenceEquals(writtenFormat, Format))
        {
            _formatChanged = false;
        }

        if (written.CompareAge(Document.Version) != 0)
        {
            return;
        }

        Document.UndoStack.MarkAsOriginalFile();
        IsDirty = _formatChanged;
        _editedSinceSave = false;
    }

    /// <summary>
    /// Replaces the whole text with what was just read from the file, as a fresh start.
    /// </summary>
    /// <param name="content">The text and format read.</param>
    /// <remarks>
    /// The undo history goes with it: undoing across a reload would mix two readings of the file.
    /// </remarks>
    public void ApplyReload(DocumentContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Format = content.Format;
        _replacing = true;

        try
        {
            Document.Text = content.Text;
        }
        finally
        {
            _replacing = false;
        }

        Document.UndoStack.ClearAll();
        Document.UndoStack.MarkAsOriginalFile();
        _formatChanged = false;
        IsDirty = false;
        _editedSinceSave = false;
    }

    private void ChangeFormat(TextFileFormat format)
    {
        // A hex buffer has no text to write: its format is not something it can change.
        if (IsHex)
        {
            return;
        }

        if ((format.Encoding.CodePage == Format.Encoding.CodePage)
            && (format.HasByteOrderMark == Format.HasByteOrderMark)
            && (format.LineEndings == Format.LineEndings))
        {
            return;
        }

        Format = format;
        _formatChanged = true;
        IsDirty = true;
    }

    // What clears the flag when the buffer is undone back to the saved state, and the one place the
    // stack has to be corrected: a group closing over a save made inside it — a ':w' typed during a
    // Vim insert session's group — reports "modified" for text that is exactly what was written.
    private void OnUndoStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UndoStack.IsOriginalFile))
        {
            return;
        }

        if (!Document.UndoStack.IsOriginalFile && !_editedSinceSave)
        {
            Document.UndoStack.MarkAsOriginalFile();

            return;
        }

        IsDirty = !Document.UndoStack.IsOriginalFile || _formatChanged;
    }

    // Set on the first keystroke rather than waiting for the undo stack, which cannot say anything
    // while a group is still open — so the dirty dot appears as soon as an insert session types.
    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (!_replacing)
        {
            IsDirty = true;
        }

        _editedSinceSave = true;
    }
}

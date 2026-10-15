using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One tab of a pane: a buffer, and where this tab was left in it.
/// </summary>
/// <remarks>
/// The view state belongs to the tab rather than to the buffer, because two panes showing the same
/// buffer are two places in it.
/// </remarks>
public sealed class EditorTabViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditorTabViewModel"/> class.
    /// </summary>
    /// <param name="document">The buffer the tab shows.</param>
    public EditorTabViewModel(DocumentViewModel document)
        => Document = document ?? throw new ArgumentNullException(nameof(document));

    /// <summary>
    /// Gets the buffer the tab shows.
    /// </summary>
    public DocumentViewModel Document { get; }

    /// <summary>
    /// Gets or sets the caret, as an offset into the text.
    /// </summary>
    public int CaretOffset { get; set; }

    /// <summary>
    /// Gets or sets the caret's line, counted from one.
    /// </summary>
    public int CaretLine { get; set => SetProperty(ref field, value); } = 1;

    /// <summary>
    /// Gets or sets the caret's column, counted from one.
    /// </summary>
    public int CaretColumn { get; set => SetProperty(ref field, value); } = 1;

    /// <summary>
    /// Gets or sets the row selected in the hex view, counted from zero; -1 for none.
    /// </summary>
    public int HexRow { get; set => SetProperty(ref field, value); } = -1;

    /// <summary>
    /// Gets or sets how far the view was scrolled down.
    /// </summary>
    public double VerticalScrollOffset { get; set; }

    /// <summary>
    /// Gets or sets how far the view was scrolled right.
    /// </summary>
    public double HorizontalScrollOffset { get; set; }
}

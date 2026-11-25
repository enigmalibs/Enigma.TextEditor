using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.TextEditor.Core.Hex;

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
    /// Gets or sets the bytes selected in the hex view; <see langword="null"/> before any are.
    /// </summary>
    /// <remarks>
    /// Not kept in the session: a selection is made to be copied, not to be come back to.
    /// </remarks>
    public HexSelection? HexSelection { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets how far the view was scrolled down.
    /// </summary>
    public double VerticalScrollOffset { get; set; }

    /// <summary>
    /// Gets or sets how far the view was scrolled right.
    /// </summary>
    public double HorizontalScrollOffset { get; set; }

    /// <summary>
    /// The selected bytes as one of the hex view's columns shows them, for the clipboard.
    /// </summary>
    /// <param name="format">The column whose form the bytes take: hex pairs, or characters.</param>
    /// <returns>
    /// The text; or, past <see cref="HexDocument.MaxCopyBytes"/> or when the bytes cannot be read, the reason for the
    /// status line; or nothing, with no selection or on a text tab.
    /// </returns>
    /// <remarks>
    /// The bytes are read from the file when copied, up to the limit — a selection can reach across gigabytes, and its
    /// text is built whole before the clipboard takes it — and on the thread pool, with the formatting: a megabyte from a
    /// slow or remote disk must not stall the window. <see cref="HexDocument.Read"/> reads at an offset, so the rows
    /// painted meanwhile do not mind; a tab closed meanwhile is a message, not a failure.
    /// </remarks>
    public async Task<HexCopyResult> CopyHexSelectionAsync(HexColumn format)
    {
        if ((HexSelection is not { } selection) || (Document.Hex is not { } hex))
        {
            return HexCopyResult.Nothing;
        }

        if (selection.Length > HexDocument.MaxCopyBytes)
        {
            return HexCopyResult.Refused(
                $"The selection is too large to copy as {(format == HexColumn.Hex ? "hex" : "text")}: "
                + $"the limit is {HexDocument.FormatSize(HexDocument.MaxCopyBytes)}");
        }

        try
        {
            return HexCopyResult.Copied(await Task.Run(() =>
            {
                byte[] bytes = hex.Read(selection.Start, (int)selection.Length);

                return format == HexColumn.Hex ? HexFormatter.FormatHexString(bytes) : HexFormatter.FormatText(bytes);
            }));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            return HexCopyResult.Refused($"Could not read the selection: {exception.Message}");
        }
    }

    /// <summary>
    /// The first and last addresses of the selected bytes, for the clipboard.
    /// </summary>
    /// <returns>The range; nothing with no selection or on a text tab.</returns>
    public HexCopyResult CopyHexRange()
        => (HexSelection is { } selection) && (Document.Hex is { } hex)
            ? HexCopyResult.Copied(HexFormatter.FormatAddressRange(selection.Start, selection.End, hex.OffsetDigits))
            : HexCopyResult.Nothing;
}

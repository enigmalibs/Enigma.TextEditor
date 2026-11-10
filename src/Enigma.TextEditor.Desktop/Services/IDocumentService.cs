using System.Threading.Tasks;
using Enigma.TextEditor.Core.Documents;
using Enigma.TextEditor.Core.Sessions;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Services;

/// <summary>
/// Opens files into buffers and saves buffers into files, telling the user when either fails.
/// </summary>
/// <remarks>
/// Every failure is reported here — a dialog and a log entry — and comes back as a plain answer
/// (<see langword="null"/>, <see langword="false"/>), so no caller has to know which exceptions file
/// I/O can throw.
/// </remarks>
public interface IDocumentService
{
    /// <summary>
    /// Opens a file, or finds the buffer it is already open in.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <param name="forceText">
    /// Whether to open it as text without looking at it first — <em>Reopen as Text</em>, or the user's
    /// own answer to the binary-file question. Without it, a file that looks binary or is too large to
    /// edit asks how to open it.
    /// </param>
    /// <returns>
    /// The buffer — a text buffer, or the hex view of the file when the user chose it — or
    /// <see langword="null"/> when nothing was opened.
    /// </returns>
    Task<DocumentViewModel?> OpenAsync(string filePath, bool forceText = false);

    /// <summary>
    /// Opens a file in the read-only hex viewer, or finds the hex buffer it is already open in.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <returns>The buffer, or <see langword="null"/> when the file could not be opened.</returns>
    Task<DocumentViewModel?> OpenHexAsync(string filePath);

    /// <summary>
    /// Brings a buffer of a saved session back, or finds the one it is already open in — without a question or a
    /// dialog: a failure is logged and comes back as <see langword="null"/>, for the restore to name.
    /// </summary>
    /// <param name="saved">The buffer as the session stored it.</param>
    /// <returns>The buffer, or <see langword="null"/> when its file is gone or cannot be read.</returns>
    /// <remarks>
    /// A buffer whose unsaved text the session kept comes back with that text and format, marked unsaved, without
    /// reading its file — which may be gone: a save writes it again. Otherwise a file is read again, in the encoding
    /// the session names, with the indentation and the chosen language the buffer had; a hex view opens its file
    /// again; an untitled buffer comes back empty, under its name.
    /// </remarks>
    Task<DocumentViewModel?> RestoreAsync(SessionDocument saved);

    /// <summary>
    /// Creates an empty, untitled buffer in the default format.
    /// </summary>
    /// <returns>The buffer.</returns>
    DocumentViewModel CreateUntitled();

    /// <summary>
    /// Creates an empty buffer bound to a file that does not exist yet — the first save creates it.
    /// </summary>
    /// <param name="filePath">The file.</param>
    /// <returns>The buffer, or the one the file is already open in.</returns>
    DocumentViewModel CreateForMissingFile(string filePath);

    /// <summary>
    /// Saves a buffer to its file, or asks where to save it when it has none.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <returns>Whether it was written.</returns>
    Task<bool> SaveAsync(DocumentViewModel document);

    /// <summary>
    /// Saves a buffer to another file, which becomes its file.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <param name="filePath">Where to save it, or <see langword="null"/> to ask.</param>
    /// <returns>Whether it was written.</returns>
    Task<bool> SaveAsAsync(DocumentViewModel document, string? filePath = null);

    /// <summary>
    /// Reads a buffer's file again in another encoding, replacing the text — and the undo history —
    /// with what the bytes say in that encoding.
    /// </summary>
    /// <param name="document">The buffer; it must have a file.</param>
    /// <param name="encoding">The encoding to read it in.</param>
    /// <returns>Whether the file was read.</returns>
    Task<bool> ReopenWithEncodingAsync(DocumentViewModel document, TextEncoding encoding);
}

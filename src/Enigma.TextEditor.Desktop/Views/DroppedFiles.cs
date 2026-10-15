using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The view side of a file drop: which files a drag carries, and the cursor it gets.
/// </summary>
internal static class DroppedFiles
{
    /// <summary>
    /// Reads the files a drag carries.
    /// </summary>
    /// <param name="data">The drag's data.</param>
    /// <returns>
    /// Their local paths, in the order the source gave them; empty for a drag that carries no file, such
    /// as text, and without the items that have no local path, which nothing here could open.
    /// </returns>
    public static IReadOnlyList<string> ReadPaths(IDataTransfer data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return data.TryGetFiles() is { } items ? [.. items.Select(item => item.TryGetLocalPath()).OfType<string>()] : [];
    }

    /// <summary>
    /// The effect a drag gets: a copy when it carries files, none otherwise.
    /// </summary>
    /// <param name="paths">The files the drag carries.</param>
    /// <returns><see cref="DragDropEffects.Copy"/>, or <see cref="DragDropEffects.None"/> when there is
    /// no file to open.</returns>
    /// <remarks>
    /// <para>
    /// Never a move, even where the source would allow one: a file manager deletes its original after a
    /// reported move, and opening a file must not cost the user the file.
    /// </para>
    /// <para>
    /// Not checked against what the source allows: the event carries no such list once it reaches the
    /// pane — the text area underneath writes over it — and every source that offers files offers them
    /// to be copied.
    /// </para>
    /// </remarks>
    public static DragDropEffects EffectFor(IReadOnlyList<string> paths)
        => paths.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None;
}

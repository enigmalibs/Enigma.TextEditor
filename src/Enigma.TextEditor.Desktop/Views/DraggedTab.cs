using System;
using Avalonia.Input;
using Enigma.TextEditor.Desktop.ViewModels;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The view side of a tab drag: what the drag carries, and reading it back where it is dropped.
/// </summary>
/// <remarks>
/// The format stays in the process: the tab itself rides on the drag, and nothing is handed to the platform.
/// Another application — another instance of this one included — sees a drag it cannot read, and refuses it.
/// </remarks>
internal static class DraggedTab
{
    /// <summary>
    /// The format a dragged tab travels in.
    /// </summary>
    public static readonly DataFormat<EditorTabViewModel> Format =
        DataFormat.CreateInProcessFormat<EditorTabViewModel>("Enigma.TextEditor.Tab");

    /// <summary>
    /// What a drag of a tab carries.
    /// </summary>
    /// <param name="tab">The tab.</param>
    /// <returns>The drag's data.</returns>
    public static DataTransfer Carrying(EditorTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        DataTransfer data = new();
        data.Add(DataTransferItem.Create(Format, tab));

        return data;
    }

    /// <summary>
    /// Reads the tab a drag carries.
    /// </summary>
    /// <param name="data">The drag's data.</param>
    /// <returns>The tab, or <see langword="null"/> for a drag of anything else.</returns>
    public static EditorTabViewModel? Read(IDataTransfer data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return data.TryGetValue(Format);
    }
}

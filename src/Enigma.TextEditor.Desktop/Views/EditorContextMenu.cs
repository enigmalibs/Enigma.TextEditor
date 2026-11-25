using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// The editor's own menu, opened on the text: what each item does, and whether it can when the menu opens.
/// </summary>
/// <remarks>
/// <para>
/// The items are the view's, declared with their headers, gestures and Vim hints; here each one, found by its name, is
/// given its action and the editor's answer to whether it can run. Those answers are not observable, so they are read
/// as the menu opens. Undo and redo go through the view, which sends them to Vim while it is on.
/// </para>
/// <para>
/// After an item has run, the keyboard goes back to the text the menu was opened on, which also gives its pane the
/// keyboard.
/// </para>
/// </remarks>
internal static class EditorContextMenu
{
    /// <summary>
    /// Gives each item of the menu its action and its enabling.
    /// </summary>
    /// <param name="menu">The menu, holding items named <c>MenuUndo</c>, <c>MenuRedo</c>, <c>MenuCut</c>, <c>MenuCopy</c>,
    /// <c>MenuPaste</c>, <c>MenuDelete</c> and <c>MenuSelectAll</c>.</param>
    /// <param name="editor">The editor the menu is opened on.</param>
    /// <param name="undo">Takes back the last change, as the Edit menu does.</param>
    /// <param name="redo">Puts back what undo took.</param>
    /// <exception cref="InvalidOperationException">An item is missing from the menu.</exception>
    public static void Attach(ContextMenu menu, AvaloniaTextEditor editor, Action undo, Action redo)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(redo);

        (string Name, Func<bool> CanRun, Action Run)[] items =
        [
            ("MenuUndo", () => editor.CanUndo, undo),
            ("MenuRedo", () => editor.CanRedo, redo),
            ("MenuCut", () => editor.CanCut, editor.Cut),
            ("MenuCopy", () => editor.CanCopy, editor.Copy),
            ("MenuPaste", () => editor.CanPaste, editor.Paste),
            ("MenuDelete", () => editor.CanDelete, editor.Delete),
            ("MenuSelectAll", () => editor.CanSelectAll, editor.SelectAll),
        ];

        Dictionary<string, MenuItem> named = menu.Items.OfType<MenuItem>()
            .Where(item => item.Name is not null)
            .ToDictionary(item => item.Name!, StringComparer.Ordinal);

        foreach ((string name, Func<bool> canRun, Action run) in items)
        {
            if (!named.TryGetValue(name, out MenuItem? item))
            {
                throw new InvalidOperationException($"The editor's menu has no item named {name}.");
            }

            item.Click += (_, _) =>
            {
                run();
                editor.TextArea.Focus();
            };

            menu.Opening += (_, _) => item.IsEnabled = canRun();
        }
    }
}

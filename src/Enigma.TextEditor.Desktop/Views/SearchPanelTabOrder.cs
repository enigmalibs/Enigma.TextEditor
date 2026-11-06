using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit.Search;
using AvaloniaTextEditor = AvaloniaEdit.TextEditor;

namespace Enigma.TextEditor.Desktop.Views;

/// <summary>
/// Makes <c>Tab</c> go from the find field of AvaloniaEdit's find and replace panel straight to its
/// replace field, and <c>Shift+Tab</c> back (BUG-B026).
/// </summary>
/// <remarks>
/// <para>
/// The panel's template puts its toggles — match case, whole words, regular expression — and its
/// find buttons between the two fields, so the platform's own focus order takes four presses of
/// <c>Tab</c> to get from what to look for to what to put instead. The two fields are what is typed
/// into, one after the other; the toggles and buttons stay reachable with the mouse and their keys
/// (<c>F3</c>, <c>Alt+R</c>, <c>Alt+A</c>).
/// </para>
/// <para>
/// Only in replace mode (<c>Ctrl+H</c>): the find-only panel hides the replace field, and its
/// <c>Tab</c> keeps the platform's order. Vim on or off makes no difference — the panel is the
/// editor's, and the vim behaviour leaves every key typed into it alone.
/// </para>
/// <para>
/// A tunnelling handler on the editor, so that it runs before the window's focus navigation, which
/// acts on a <c>Tab</c> nothing else has handled.
/// </para>
/// </remarks>
internal static class SearchPanelTabOrder
{
    /// <summary>
    /// The name of the panel's find field in its template.
    /// </summary>
    internal const string FindFieldName = "PART_searchTextBox";

    /// <summary>
    /// The name of the panel's replace field in its template.
    /// </summary>
    internal const string ReplaceFieldName = "PART_replaceTextBox";

    /// <summary>
    /// Starts handling <c>Tab</c> in the panel of an editor, for as long as the editor lives.
    /// </summary>
    /// <param name="editor">The editor whose find panel is meant.</param>
    /// <exception cref="ArgumentNullException"><paramref name="editor"/> is <see langword="null"/>.</exception>
    public static void Attach(AvaloniaTextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        editor.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Which field <c>Tab</c> or <c>Shift+Tab</c> pressed in a field of the panel moves to.
    /// </summary>
    /// <param name="panel">The panel.</param>
    /// <param name="field">The field the key was pressed in.</param>
    /// <param name="modifiers">The modifiers held with <c>Tab</c>.</param>
    /// <returns>
    /// The replace field for <c>Tab</c> in the find field, the find field for <c>Shift+Tab</c> in the
    /// replace field, in replace mode; otherwise <see langword="null"/>, and the platform's order applies.
    /// </returns>
    internal static TextBox? NextField(SearchPanel panel, TextBox field, KeyModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(panel);
        ArgumentNullException.ThrowIfNull(field);

        if (!panel.IsReplaceMode || !panel.IsVisualAncestorOf(field))
        {
            return null;
        }

        string? target = (modifiers, field.Name) switch
        {
            (KeyModifiers.None, FindFieldName) => ReplaceFieldName,
            (KeyModifiers.Shift, ReplaceFieldName) => FindFieldName,
            _ => null,
        };

        return target is null
            ? null
            : panel.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(box => box.Name == target && box.IsEffectivelyVisible);
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.Key != Key.Tab)
            || (sender is not AvaloniaTextEditor { SearchPanel: { IsOpened: true } panel })
            || (e.Source is not TextBox field)
            || (NextField(panel, field, e.KeyModifiers) is not { } next))
        {
            return;
        }

        e.Handled = next.Focus(NavigationMethod.Tab);
    }
}

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Enigma.TextEditor.Desktop.UnitTests.Infrastructure;

/// <summary>
/// What a submenu shows when it opens: the header and the check of each item.
/// </summary>
/// <remarks>
/// A submenu whose items come from a list (<see cref="ItemsControl.ItemsSource"/>) has no item controls until it opens:
/// it is opened here, with the menus above it, read, and closed again.
/// </remarks>
internal static class MenuItems
{
    /// <summary>
    /// The items a menu item shows, as a user sees them.
    /// </summary>
    /// <param name="parent">The menu item whose submenu is read.</param>
    /// <returns>Each item's header and check, in order.</returns>
    public static (string? Header, bool IsChecked)[] Shown(MenuItem parent)
    {
        if (parent.ItemsSource is null)
        {
            return [.. parent.Items.OfType<MenuItem>().Select(item => ((string?)item.Header, item.IsChecked))];
        }

        List<MenuItem> opened = [.. parent.GetLogicalAncestors().OfType<MenuItem>().Reverse(), parent];

        foreach (MenuItem item in opened)
        {
            item.IsSubMenuOpen = true;
            Dispatcher.UIThread.RunJobs();
        }

        (string? Header, bool IsChecked)[] shown =
        [
            .. Enumerable.Range(0, parent.ItemCount)
                .Select(parent.ContainerFromIndex)
                .OfType<MenuItem>()
                .Select(item => ((string?)item.Header, item.IsChecked)),
        ];

        foreach (MenuItem item in Enumerable.Reverse(opened))
        {
            item.IsSubMenuOpen = false;
        }

        Dispatcher.UIThread.RunJobs();

        return shown;
    }
}

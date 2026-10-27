using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.Avalonia.Desktop.Controls.Navigation;
using Enigma.Icons.Avalonia;
using Enigma.Icons.Phosphor;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The rail on the left of the window, and which of its two pages is shown: the files — the split
/// editor — or the settings.
/// </summary>
/// <remarks>
/// <para>
/// The pages are not built by Enigma.Avalonia.Desktop's navigation service: it swaps a page out of the
/// window and builds the next one, and the editor area has to stay where it is — its panes hold the
/// documents' carets, scroll positions and highlighting. The window shows one page and hides the other
/// instead, following <see cref="IsFilesPage"/> and <see cref="IsSettingsPage"/>.
/// </para>
/// <para>
/// The items are the library's own <see cref="NavigationItem"/> controls, which its rail is built for;
/// they are created here, as in Enigma.GitClient, and never shown anywhere else.
/// </para>
/// </remarks>
public sealed class ShellViewModel : ObservableObject
{
    /// <summary>
    /// The files page's name: its icon's tooltip and what a screen reader says.
    /// </summary>
    public const string FilesHeader = "Files";

    /// <summary>
    /// The settings page's name: its icon's tooltip and what a screen reader says.
    /// </summary>
    public const string SettingsHeader = "Settings";

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellViewModel"/> class, on the files page.
    /// </summary>
    public ShellViewModel()
    {
        FilesItem = Item(FilesHeader, PhosphorIcon.Files);
        SettingsItem = Item(SettingsHeader, PhosphorIcon.Gear);
        Items = [FilesItem];
        FooterItems = [SettingsItem];
        SelectedItem = FilesItem;

        ShowFilesCommand = new RelayCommand(ShowFiles);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
    }

    /// <summary>
    /// Gets the files page's item, at the top of the rail.
    /// </summary>
    public NavigationItem FilesItem { get; }

    /// <summary>
    /// Gets the settings page's item, at the bottom of the rail.
    /// </summary>
    public NavigationItem SettingsItem { get; }

    /// <summary>
    /// Gets the items at the top of the rail.
    /// </summary>
    public IReadOnlyList<NavigationItem> Items { get; }

    /// <summary>
    /// Gets the items at the bottom of the rail.
    /// </summary>
    public IReadOnlyList<NavigationItem> FooterItems { get; }

    /// <summary>
    /// Gets or sets the item whose page is shown.
    /// </summary>
    /// <remarks>
    /// The rail clears its selection as it rebuilds its lists; a page is always shown, so a
    /// <see langword="null"/> — or an item that is not on the rail — is not a choice and is ignored.
    /// </remarks>
    public NavigationItem? SelectedItem
    {
        get;
        set
        {
            if (((value != FilesItem) && (value != SettingsItem)) || !SetProperty(ref field, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsFilesPage));
            OnPropertyChanged(nameof(IsSettingsPage));
        }
    }

    /// <summary>
    /// Gets a value indicating whether the split editor is shown.
    /// </summary>
    public bool IsFilesPage => SelectedItem == FilesItem;

    /// <summary>
    /// Gets a value indicating whether the settings page is shown.
    /// </summary>
    public bool IsSettingsPage => SelectedItem == SettingsItem;

    /// <summary>
    /// Gets the command that shows the split editor: Esc on the settings page.
    /// </summary>
    public RelayCommand ShowFilesCommand { get; }

    /// <summary>
    /// Gets the command that shows the settings page: <em>Settings → All Settings</em> (Ctrl+,).
    /// </summary>
    public RelayCommand ShowSettingsCommand { get; }

    /// <summary>
    /// Shows the split editor.
    /// </summary>
    public void ShowFiles() => SelectedItem = FilesItem;

    /// <summary>
    /// Shows the settings page.
    /// </summary>
    public void ShowSettings() => SelectedItem = SettingsItem;

    private static NavigationItem Item(string header, PhosphorIcon icon)
        => new()
        {
            Header = header,
            IconData = PhosphorIconSet.Instance.GetGlyph(icon, PhosphorWeight.Regular).ToGeometry(),
        };
}

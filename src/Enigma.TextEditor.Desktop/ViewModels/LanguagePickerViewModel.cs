using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// The language picker: a filter box over every language, with the buffer's current one selected.
/// </summary>
public sealed class LanguagePickerViewModel : ObservableObject
{
    private readonly IReadOnlyList<LanguageInfo> _all;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguagePickerViewModel"/> class.
    /// </summary>
    /// <param name="languages">Every language.</param>
    /// <param name="current">The buffer's language, selected to begin with.</param>
    public LanguagePickerViewModel(IReadOnlyList<LanguageInfo> languages, LanguageInfo current)
    {
        _all = languages ?? throw new ArgumentNullException(nameof(languages));
        ArgumentNullException.ThrowIfNull(current);

        Items = [.. _all];
        Selected = _all.FirstOrDefault(language => language.Equals(current)) ?? _all.FirstOrDefault();
    }

    /// <summary>
    /// Gets the languages the filter lets through.
    /// </summary>
    public ObservableCollection<LanguageInfo> Items { get; }

    /// <summary>
    /// Gets or sets the highlighted language.
    /// </summary>
    public LanguageInfo? Selected { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets the filter: languages whose name, id or an extension contains it.
    /// </summary>
    public string Filter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Gets the language picked by a double click or Enter, which closes the picker without the OK
    /// button; <see langword="null"/> until then.
    /// </summary>
    public LanguageInfo? Committed { get; private set; }

    /// <summary>
    /// Picks the highlighted language.
    /// </summary>
    public void Commit() => Committed = Selected;

    private void ApplyFilter()
    {
        string filter = Filter.Trim();

        List<LanguageInfo> matches = filter.Length == 0
            ? [.. _all]
            : [.. _all.Where(language => Matches(language, filter))];

        Items.Clear();

        foreach (LanguageInfo language in matches)
        {
            Items.Add(language);
        }

        if ((Selected is null) || !Items.Contains(Selected))
        {
            Selected = Items.FirstOrDefault();
        }
    }

    private static bool Matches(LanguageInfo language, string filter)
        => language.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || language.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || language.Extensions.Any(extension => extension.Contains(filter, StringComparison.OrdinalIgnoreCase));
}

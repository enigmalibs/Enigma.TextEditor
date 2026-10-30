using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Enigma.TextEditor.Core.Vim;
using Enigma.TextEditor.Desktop.Services;

namespace Enigma.TextEditor.Desktop.ViewModels;

/// <summary>
/// One editor pane: its tabs, and which of them is showing.
/// </summary>
/// <remarks>
/// The pane only arranges tabs. Closing one, and the ex commands its Vim engine raises, go through the
/// <see cref="IPaneHost"/> it lives in, because closing the last tab of a buffer is where unsaved work
/// is asked about, and that is a question about every pane, not this one.
/// </remarks>
public sealed class EditorPaneViewModel : ObservableObject
{
    private readonly IPaneHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorPaneViewModel"/> class.
    /// </summary>
    /// <param name="preferences">How every editor looks: font, size, wrapping.</param>
    /// <param name="host">The editor area the pane lives in.</param>
    public EditorPaneViewModel(EditorPreferences preferences, IPaneHost host)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _host = host ?? throw new ArgumentNullException(nameof(host));

        CloseTabCommand = new AsyncRelayCommand<EditorTabViewModel>(OnCloseTabAsync);
    }

    /// <summary>
    /// Gets how the editor looks.
    /// </summary>
    public EditorPreferences Preferences { get; }

    /// <summary>
    /// Gets the Vim session the pane's engine reports to.
    /// </summary>
    public IVimSession Vim => _host.Vim;

    /// <summary>
    /// Gets what the pane's Vim engine shares with every other pane's.
    /// </summary>
    public VimSharedState VimShared => _host.VimShared;

    /// <summary>
    /// Gets the tabs, in strip order.
    /// </summary>
    public ObservableCollection<EditorTabViewModel> Tabs { get; } = [];

    /// <summary>
    /// Gets or sets the tab being shown.
    /// </summary>
    public EditorTabViewModel? ActiveTab
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(HasActiveTab));
                OnPropertyChanged(nameof(IsTextActive));
                OnPropertyChanged(nameof(IsHexActive));
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the pane has the keyboard — the one the menus act on.
    /// </summary>
    public bool IsActive { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets or sets a value indicating whether the pane is drawn with the active frame: the active one,
    /// when there is more than one to tell apart.
    /// </summary>
    public bool IsFramed { get; set => SetProperty(ref field, value); }

    /// <summary>
    /// Gets a value indicating whether a tab is showing.
    /// </summary>
    public bool HasActiveTab => ActiveTab is not null;

    /// <summary>
    /// Gets a value indicating whether the tab showing is text, which the editor shows.
    /// </summary>
    public bool IsTextActive => ActiveTab is { Document.IsHex: false };

    /// <summary>
    /// Gets a value indicating whether the tab showing is the hex view of a file.
    /// </summary>
    public bool IsHexActive => ActiveTab is { Document.IsHex: true };

    /// <summary>
    /// Gets the command behind a tab's close button, and a middle click on it.
    /// </summary>
    public AsyncRelayCommand<EditorTabViewModel> CloseTabCommand { get; }

    /// <summary>
    /// Shows a buffer: activates the tab already showing it, or adds one at the end of the strip.
    /// </summary>
    /// <param name="document">The buffer.</param>
    /// <param name="created">Set to whether a tab had to be added.</param>
    /// <returns>The tab showing it.</returns>
    public EditorTabViewModel Show(DocumentViewModel document, out bool created)
    {
        ArgumentNullException.ThrowIfNull(document);

        EditorTabViewModel? tab = Tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Document, document));
        created = tab is null;

        if (tab is null)
        {
            tab = new EditorTabViewModel(document);
            Tabs.Add(tab);
        }

        ActiveTab = tab;

        return tab;
    }

    /// <summary>
    /// Takes in a tab another pane gave up, at the end of the strip, and shows it.
    /// </summary>
    /// <param name="tab">The tab, with the place it was left at in its buffer.</param>
    /// <remarks>
    /// The tab itself moves rather than a new one being made, so the caret and the scroll come with it. The
    /// caller makes sure this pane does not already show its buffer.
    /// </remarks>
    public void Adopt(EditorTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (!Tabs.Contains(tab))
        {
            Tabs.Add(tab);
        }

        ActiveTab = tab;
    }

    /// <summary>
    /// Puts another buffer in a tab's place on the strip, and shows it.
    /// </summary>
    /// <param name="tab">The tab to replace.</param>
    /// <param name="document">The buffer to show instead.</param>
    /// <param name="created">Set to whether a tab had to be made — not when the pane already showed the buffer.</param>
    /// <returns>The tab now showing the buffer.</returns>
    public EditorTabViewModel Replace(EditorTabViewModel tab, DocumentViewModel document, out bool created)
    {
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentNullException.ThrowIfNull(document);

        int index = Tabs.IndexOf(tab);

        if (Tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Document, document)) is { } existing)
        {
            created = false;
            ActiveTab = existing;
            Remove(tab);
            ActiveTab = existing;

            return existing;
        }

        EditorTabViewModel replacement = new(document);
        created = true;

        if (index < 0)
        {
            Tabs.Add(replacement);
        }
        else
        {
            Tabs[index] = replacement;
        }

        ActiveTab = replacement;

        return replacement;
    }

    /// <summary>
    /// Takes a tab off the strip, showing its right-hand neighbour — or the left-hand one at the end.
    /// </summary>
    /// <param name="tab">The tab.</param>
    /// <returns>Whether the tab was on this pane.</returns>
    /// <remarks>
    /// The neighbour is shown before the tab leaves the strip. The other way round, the tab strip — a
    /// list whose selection is bound to <see cref="ActiveTab"/> — sees its selected tab disappear and
    /// writes <see langword="null"/> back, and the pane is left with tabs but none showing.
    /// </remarks>
    public bool Remove(EditorTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        int index = Tabs.IndexOf(tab);

        if (index < 0)
        {
            return false;
        }

        if (ReferenceEquals(ActiveTab, tab))
        {
            ActiveTab = Tabs.Count == 1 ? null : Tabs[index == (Tabs.Count - 1) ? index - 1 : index + 1];
        }

        Tabs.RemoveAt(index);

        return true;
    }

    /// <summary>
    /// Shows the next tab, wrapping round to the first.
    /// </summary>
    public void NextTab() => Step(1);

    /// <summary>
    /// Shows the previous tab, wrapping round to the last.
    /// </summary>
    public void PreviousTab() => Step(-1);

    private void Step(int direction)
    {
        if (Tabs.Count == 0)
        {
            return;
        }

        int index = ActiveTab is null ? 0 : Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(((index + direction) % Tabs.Count) + Tabs.Count) % Tabs.Count];
    }

    /// <summary>
    /// Carries out an ex command this pane's Vim engine recognised.
    /// </summary>
    /// <param name="request">The command.</param>
    /// <returns>A task that completes when it has run.</returns>
    public Task ExecuteExAsync(VimExRequest request) => _host.ExecuteExAsync(this, request);

    /// <summary>
    /// Carries out a window command this pane's Vim engine recognised.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>A task that completes when it has run.</returns>
    public Task ExecuteWindowAsync(VimWindowCommand command) => _host.ExecuteWindowAsync(this, command);

    /// <summary>
    /// Opens files dropped on this pane, here.
    /// </summary>
    /// <param name="paths">Their full paths, in drop order.</param>
    /// <returns>A task that completes when every file has been opened or skipped.</returns>
    public Task OpenDroppedAsync(IReadOnlyList<string> paths) => _host.OpenDroppedAsync(this, paths);

    /// <summary>
    /// Reports that colouring the text failed, without interrupting the editing.
    /// </summary>
    /// <param name="failure">What went wrong.</param>
    public void ReportHighlightingFailure(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        _host.ShowStatus($"Syntax highlighting stopped: {failure.Message}");
    }

    private Task OnCloseTabAsync(EditorTabViewModel? tab)
        => tab is null ? Task.CompletedTask : _host.CloseTabAsync(this, tab);
}
